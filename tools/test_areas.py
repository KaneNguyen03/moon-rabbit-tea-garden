import fnmatch
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TABLE = os.path.join(ROOT, "tools", "test-areas.tsv")
TESTS = "Assets/Tests/EditMode/"
CORE = "Assets/VuonTra/Core/"
UNITY_BOUND = re.compile(r"UnityEngine|UnityEditor|VuonTra\.Game|VuonTra\.EditorTools")
FIXTURE = re.compile(r"^    (?:\w+ )*class (\w+)", re.MULTILINE)


class Area:
    def __init__(self, name, test_filter, sources, note):
        self.name = name
        self.filter = test_filter
        self.sources = sources
        self.note = note

    def owns(self, path):
        return any(fnmatch.fnmatchcase(path, source) for source in self.sources)


def load(table=TABLE):
    with open(table, encoding="utf-8") as handle:
        lines = [line.rstrip("\n") for line in handle if line.strip()]
    areas = []
    for line in lines[1:]:
        cells = line.split("\t")
        if len(cells) != 4:
            raise ValueError("tools/test-areas.tsv needs four tab-separated cells in: " + line)
        areas.append(Area(cells[0], cells[1], cells[2].split(), cells[3]))
    return areas


def read(path):
    full = os.path.join(ROOT, path)
    if not os.path.isfile(full):
        return ""
    with open(full, encoding="utf-8") as handle:
        return handle.read()


def fixtures(path):
    text = read(path)
    if "[Test" not in text:
        return []
    return sorted(set(FIXTURE.findall(text)))


def needs_unity(path):
    return bool(UNITY_BOUND.search(read(path)))


def place(path, areas):
    if path.startswith(TESTS) and path.endswith(".cs"):
        names = fixtures(path)
        if names and needs_unity(path):
            return "its own tests", "|".join(names), ""
        if names:
            return "core", "", ""
    for area in areas:
        if area.owns(path):
            return area.name, area.filter, area.note
    if path.startswith(CORE) or (path.startswith(TESTS) and path.endswith(".cs")):
        return "core", "", ""
    return None, "", ""


def resolve(paths, areas):
    parts, lines = [], []
    for path in paths:
        name, test_filter, note = place(path, areas)
        if name is None:
            continue
        if name == "core":
            lines.append(path + ": Core tests, tools/ci/core-tests.sh, no Unity")
            continue
        lines.append(path + ": " + name + (" (" + note + ")" if note else ""))
        for part in test_filter.split("|"):
            if part not in parts:
                parts.append(part)
    return "|".join(parts), lines


def git(*args):
    done = subprocess.run(["git", "-C", ROOT] + list(args), capture_output=True, text=True)
    return done.stdout.splitlines() if done.returncode == 0 else []


def changed():
    base = []
    for ref in ("origin/main", "main"):
        base = git("merge-base", "HEAD", ref)
        if base:
            break
    paths = git("diff", "--name-only", base[0]) if base else git("diff", "--name-only", "HEAD")
    return sorted(set(paths + git("ls-files", "--others", "--exclude-standard")))


def main(argv):
    paths = [os.path.relpath(os.path.abspath(path), ROOT) for path in argv] or changed()
    test_filter, lines = resolve(paths, load())
    for line in lines:
        print(line, file=sys.stderr)
    print(test_filter)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
