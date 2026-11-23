import argparse
import fnmatch
import json
import os
import re
import subprocess
import sys

ROOT = os.path.realpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
BASELINE = "tools/conventions.baseline"
SCANNED = ("Assets/VuonTra/*.cs", "Assets/Tests/*.cs")

BUILDS_DATA_NOT_FRAMES = (
    "Assets/VuonTra/Game/Audio/Sfx.cs",
    "Assets/VuonTra/Game/UI/Palette.cs",
    "Assets/VuonTra/Game/UI/PaperGrain.cs",
)

RULES = [
    ("procedural-animation", "Assets/VuonTra/Game/*.cs", BUILDS_DATA_NOT_FRAMES,
     r"Mathf\.(Sin|Cos|PingPong|Lerp|LerpUnclamped|SmoothStep|SmoothDamp)\b|\b(Vector2|Vector3|Color|Quaternion)\.(Lerp|Slerp|LerpUnclamped)\b"),
    ("scene-search", "Assets/VuonTra/Game/*.cs", (),
     r"GameObject\.Find|\bFind(Objects?OfType|FirstObjectByType|AnyObjectByType|ObjectsByType)\b"),
    ("per-frame-update", "Assets/VuonTra/Game/*.cs", (),
     r"\bvoid\s+(Update|LateUpdate|FixedUpdate)\s*\(\s*\)"),
    ("workaround", "Assets/*.cs", (),
     r"yield\s+return\s+null\b|WaitForEndOfFrame|(?<![.?\w])Invoke\(\s*(\"|nameof)|\bInvokeRepeating\b|\bSendMessage\(|DelayedCall\(\s*0(\.0*)?f?\s*,"
     r"|\bcatch\s*(\([^)]*\))?\s*\{\s*\}|#pragma\s+warning\s+disable|\bSuppressMessage\b"),
]

HINT = ("Fix the cause instead of working around it: remove the comment, drive motion from an Animator or DOTween, "
        "wire references through [SerializeField] or events, and replace the deferred or swallowed call with the real "
        "ordering. The baseline only goes down (tools/conventions.py --lower); raising it needs the owner's review.")


class Fatal(Exception):
    pass


def git(*args, check=True):
    result = subprocess.run(["git", "-C", ROOT, *args], capture_output=True)
    if check and result.returncode != 0:
        raise Fatal("git " + " ".join(args) + ": " + result.stderr.decode(errors="replace").strip())
    return result


def split_code(text):
    code = []
    comments = 0
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""
        if c == "/" and nxt == "/":
            comments += 1
            while i < n and text[i] != "\n":
                i += 1
        elif c == "/" and nxt == "*":
            comments += 1
            end = text.find("*/", i + 2)
            end = n if end < 0 else end + 2
            code.append("\n" * text.count("\n", i, end) or " ")
            i = end
        elif c == "'" :
            j = i + 1
            while j < n and text[j] != "'" and text[j] != "\n":
                j += 2 if text[j] == "\\" else 1
            code.append("''")
            i = j + 1
        elif c == '"' or (c in "@$" and nxt in '"@$'):
            j = i
            verbatim = False
            while j < n and text[j] in "@$":
                verbatim |= text[j] == "@"
                j += 1
            if j >= n or text[j] != '"':
                code.append(c)
                i += 1
                continue
            j += 1
            while j < n:
                if verbatim and text[j] == '"' and j + 1 < n and text[j + 1] == '"':
                    j += 2
                elif not verbatim and text[j] == "\\":
                    j += 2
                elif text[j] == '"' or (not verbatim and text[j] == "\n"):
                    break
                else:
                    j += 1
            code.append('""')
            i = j + 1
        else:
            code.append(c)
            i += 1
    return "".join(code), comments


def count(path, text):
    code, comments = split_code(text)
    counts = {}
    if comments:
        counts["comment"] = comments
    for rule, scope, exempt, pattern in RULES:
        if fnmatch.fnmatchcase(path, scope) and path not in exempt:
            hits = len(re.findall(pattern, code))
            if hits:
                counts[rule] = hits
    return counts


def in_scope(path):
    return path.endswith(".cs") and any(fnmatch.fnmatchcase(path, s) for s in SCANNED)


def tracked_files():
    out = git("ls-files", "-z", "--", *SCANNED).stdout
    return [p for p in out.decode().split("\0") if p]


def staged_files():
    out = git("diff", "--cached", "--name-only", "-z", "--diff-filter=ACMR", "--", *SCANNED).stdout
    return [p for p in out.decode().split("\0") if p]


def read_worktree(path):
    with open(os.path.join(ROOT, path), encoding="utf-8-sig") as f:
        return f.read()


def read_index(path):
    return git("show", ":" + path).stdout.decode("utf-8-sig")


def parse_baseline(text):
    table = {}
    for number, line in enumerate(text.splitlines(), 1):
        if not line.strip():
            continue
        parts = line.split("\t")
        if len(parts) != 3 or not parts[2].isdigit():
            raise Fatal(f"{BASELINE}:{number}: expected rule<TAB>path<TAB>count")
        table[(parts[0], parts[1])] = int(parts[2])
    return table


def load_baseline(staged):
    if staged:
        result = git("show", ":" + BASELINE, check=False)
        if result.returncode != 0:
            raise Fatal(f"{BASELINE} is not in the index; the check cannot run without it")
        return parse_baseline(result.stdout.decode())
    path = os.path.join(ROOT, BASELINE)
    if not os.path.isfile(path):
        raise Fatal(f"{BASELINE} is missing; the check cannot run without it")
    with open(path, encoding="utf-8") as f:
        return parse_baseline(f.read())


def write_baseline(table):
    lines = [f"{r}\t{p}\t{n}\n" for (r, p), n in sorted(table.items()) if n > 0]
    with open(os.path.join(ROOT, BASELINE), "w", encoding="utf-8") as f:
        f.writelines(lines)


def measure(paths, reader):
    current = {}
    for path in paths:
        for rule, n in count(path, reader(path)).items():
            current[(rule, path)] = n
    return current


def check(paths, reader, baseline, whole_tree):
    current = measure(paths, reader)
    over = [(k, n, baseline.get(k, 0)) for k, n in sorted(current.items()) if n > baseline.get(k, 0)]
    if over:
        print("conventions: new violations of the project rules (tools/conventions.py):", file=sys.stderr)
        for (rule, path), n, allowed in over:
            print(f"  {rule}  {path}: {n} now, {allowed} allowed", file=sys.stderr)
        print(HINT, file=sys.stderr)
        return 1
    if whole_tree:
        slack = sum(1 for k, n in baseline.items() if current.get(k, 0) < n)
        note = f", {slack} entries can be lowered: tools/conventions.py --lower" if slack else ""
        print(f"conventions: ok ({sum(current.values())} known violations left{note})")
    return 0


def since(rev, staged):
    old = git("show", f"{rev}:{BASELINE}", check=False)
    if old.returncode != 0:
        raise Fatal(f"{rev} has no {BASELINE} to compare with")
    before = parse_baseline(old.stdout.decode())
    after = load_baseline(staged)
    raised = [(k, n) for k, n in sorted(after.items()) if n > before.get(k, 0)]
    if raised:
        print(f"conventions: {BASELINE} gained or raised entries since {rev}; only --lower may change it:", file=sys.stderr)
        for (rule, path), n in raised:
            print(f"  {rule}  {path}: {n}", file=sys.stderr)
        return 1
    return 0


def hook():
    event = json.load(sys.stdin)
    tool_input = event.get("tool_input") or {}
    raw = tool_input.get("file_path") or tool_input.get("notebook_path")
    if not raw:
        return 0
    file = os.path.realpath(raw)
    probe = os.path.dirname(file)
    while probe and not os.path.isdir(probe):
        probe = os.path.dirname(probe)
    top = subprocess.run(["git", "-C", probe, "rev-parse", "--show-toplevel"], capture_output=True)
    if top.returncode != 0:
        return 0
    root = os.path.realpath(top.stdout.decode().strip())
    rel = os.path.relpath(file, root).replace(os.sep, "/")
    if rel.startswith("../"):
        return 0
    if event.get("hook_event_name") == "PreToolUse":
        if rel == BASELINE:
            print(f"{BASELINE} only goes down, through tools/conventions.py --lower. Fix the violation in the code "
                  "instead; raising a count needs the owner's review.", file=sys.stderr)
            return 2
        return 0
    if not in_scope(rel):
        return 0
    script = os.path.join(root, "tools", "conventions.py")
    if not os.path.isfile(script):
        return 0
    result = subprocess.run([sys.executable, "-I", script, rel], capture_output=True, text=True)
    if result.returncode == 0:
        return 0
    sys.stderr.write(result.stderr or result.stdout)
    return 2


def main():
    parser = argparse.ArgumentParser(prog="tools/conventions.py")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--lower", action="store_true", help="rewrite the baseline down to today's counts; never raises")
    mode.add_argument("--since", metavar="REV", help="fail when the baseline gained or raised an entry since REV")
    mode.add_argument("--staged", action="store_true", help="check the staged snapshot: files and baseline from the index")
    mode.add_argument("--hook", action="store_true", help="Claude Code hook: read the tool event from stdin")
    parser.add_argument("--index", action="store_true", help="with --since, read the new baseline from the index")
    parser.add_argument("files", nargs="*")
    args = parser.parse_args()

    if args.hook:
        return hook()
    if args.since:
        return since(args.since, args.index)
    if args.staged:
        return check([p for p in staged_files()], read_index, load_baseline(True), False)
    baseline = load_baseline(False)
    if args.lower:
        if args.files:
            raise Fatal("--lower runs on the whole tree, pass no files")
        current = measure(tracked_files(), read_worktree)
        lowered = {k: min(n, current.get(k, 0)) for k, n in baseline.items()}
        write_baseline(lowered)
        print(f"conventions: baseline lowered to {sum(lowered.values())} known violations")
        return 0
    if args.files:
        paths = []
        for f in args.files:
            rel = os.path.relpath(os.path.realpath(os.path.join(ROOT, f)), ROOT).replace(os.sep, "/")
            if in_scope(rel):
                paths.append(rel)
        return check(paths, read_worktree, baseline, False)
    return check(tracked_files(), read_worktree, baseline, True)


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Fatal as error:
        print(f"conventions: {error}", file=sys.stderr)
        sys.exit(2)
