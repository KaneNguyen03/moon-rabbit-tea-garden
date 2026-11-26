import glob
import os
import re
import shutil
import subprocess
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = "Assets/VuonTra/Editor/PreviewShot.Select.cs"
NAMES = "tools/shot-names.txt"
SEES_PROCESSES = subprocess.run("ps -p 1", shell=True, capture_output=True).returncode == 0

FAKE_UNITY = r"""#!/bin/bash
project=""; log=""; method=""
while [ $# -gt 0 ]; do
  case "$1" in
    -projectPath) project=$2; shift ;;
    -logFile) log=$2; shift ;;
    -executeMethod) method=$2; shift ;;
  esac
  shift
done
echo "$method|${VUONTRA_SHOTS:-}|${VUONTRA_SHOT_NAMES_ONLY:-}" >> "$project/unity-calls.txt"
: > "$log"
if [ -n "${FAKE_EXCEPTION:-}" ]; then echo "NullReferenceException: fake" >> "$log"; exit 1; fi
case "$method" in *RenderSelected) ;; *) exit 0 ;; esac
mkdir -p "$project/Logs/shot"
for part in $VUONTRA_SHOTS; do
  group=${part%%:*}
  only=""; [ "$part" = "$group" ] || only=",${part#*:},"
  while IFS=$'\t' read -r owner frame; do
    [ "$owner" = "$group" ] || continue
    printf '%s\t%s\n' "$owner" "$frame" >> "$project/Logs/shot/names.txt"
    [ "${VUONTRA_SHOT_NAMES_ONLY:-0}" = 1 ] && continue
    [ "$frame" = "${FAKE_DROPS:-}" ] && continue
    case "$only" in ""|*",$frame,"*) echo png > "$project/Logs/shot/$frame.png" ;; esac
  done < "$project/truth.txt"
done
"""


def read(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as text:
        return text.read()


def source_groups():
    return re.findall(r'^\s*new Group\("([a-z0-9-]+)"', read(SOURCE), re.M)


def listed():
    return [tuple(line.split("\t")) for line in read(NAMES).splitlines()]


class ListedNamesTest(unittest.TestCase):
    def test_every_line_is_a_known_group_and_a_plain_frame_name(self):
        groups = set(source_groups())
        for row in listed():
            self.assertEqual(len(row), 2, row)
            self.assertIn(row[0], groups)
            self.assertRegex(row[1], r"^[A-Za-z0-9-]+$")

    def test_every_group_in_the_editor_code_has_frames_listed(self):
        with_frames = {group for group, _ in listed()}
        self.assertEqual([group for group in source_groups() if group not in with_frames], [])

    def test_every_frame_name_written_in_the_editor_code_is_listed(self):
        frames = [frame for _, frame in listed()]
        missing = []
        for path in sorted(glob.glob(os.path.join(ROOT, "Assets/VuonTra/Editor/PreviewShot*.cs"))):
            with open(path, encoding="utf-8") as text:
                code = text.read()
            starts = re.findall(r'\bCapture\(screen, "([^"]+)"', code) + re.findall(r'\bShootGarden\([^;"]*"[^"]*"[^;"]*"([^"]+)"\);', code)
            missing += [start for start in starts if not any(frame.startswith(start) for frame in frames)]
        self.assertEqual(missing, [], "run tools/shot.sh <group> or tools/shot.sh --names to list them")


class ShotTest(unittest.TestCase):
    def setUp(self):
        self.project = tempfile.mkdtemp(prefix="shot-test-")
        self.state = tempfile.mkdtemp(prefix="shot-gate-")
        os.makedirs(os.path.join(self.project, "tools"))
        os.makedirs(os.path.join(self.project, os.path.dirname(SOURCE)))
        for name in ("tools/shot.sh", "tools/unity-gate.sh", SOURCE):
            shutil.copy(os.path.join(ROOT, name), os.path.join(self.project, name))
        self.first, self.second = source_groups()[:2]
        self.write(NAMES, [(self.first, "1-title"), (self.first, "2-garden"), (self.second, "30-buoc-1"), (self.second, "30-buoc-2")])
        self.write("truth.txt", [(self.first, "1-title"), (self.first, "2-garden"), (self.second, "30-buoc-1"), (self.second, "30-buoc-2"), (self.second, "30-buoc-moi")])
        self.unity = os.path.join(self.project, "fake-unity.sh")
        with open(self.unity, "w") as script:
            script.write(FAKE_UNITY)
        os.chmod(self.unity, 0o755)
        with open(os.path.join(self.state, "unity-gate-rules.sh"), "w") as rules:
            rules.write("lim(){ echo 1; }\nunity_count(){ echo 0; }\n")

    def tearDown(self):
        shutil.rmtree(self.project, ignore_errors=True)
        shutil.rmtree(self.state, ignore_errors=True)

    def write(self, name, rows):
        with open(os.path.join(self.project, name), "w") as out:
            out.write("".join("%s\t%s\n" % row for row in rows))

    def shot(self, *args, **extra):
        env = dict(os.environ, UNITY=self.unity, UNITY_GATE_STATE=self.state, UNITY_GATE_POLL="0.1",
                   UNITY_GATE_SAVE=os.path.join(self.state, "save.json"))
        env.update(extra)
        return subprocess.run([os.path.join(self.project, "tools/shot.sh"), *args], env=env, capture_output=True, text=True, timeout=60)

    def calls(self):
        path = os.path.join(self.project, "unity-calls.txt")
        if not os.path.exists(path):
            return []
        with open(path) as calls:
            return calls.read().splitlines()

    def pictures(self):
        return sorted(os.listdir(os.path.join(self.project, "Logs/shot"))) if os.path.isdir(os.path.join(self.project, "Logs/shot")) else []

    def names(self):
        with open(os.path.join(self.project, NAMES)) as names:
            return names.read().splitlines()


class WithoutUnityTest(ShotTest):
    def test_groups_prints_every_group_of_the_editor_code_with_its_frame_count(self):
        result = self.shot("--groups")
        self.assertEqual(result.returncode, 0, result.stderr)
        lines = result.stdout.splitlines()
        self.assertEqual([line.split("\t")[0] for line in lines], source_groups())
        self.assertEqual(lines[0], self.first + "\t2")
        self.assertEqual(self.calls(), [])

    def test_list_prints_every_frame_with_its_group(self):
        result = self.shot("--list")
        self.assertEqual(result.stdout.splitlines(), self.names())
        self.assertEqual(self.calls(), [])

    def test_list_narrows_to_a_pattern_and_to_a_group(self):
        self.assertEqual(self.shot("--list", "30-*").stdout.splitlines(), [self.second + "\t30-buoc-1", self.second + "\t30-buoc-2"])
        self.assertEqual(self.shot("--list", self.first).stdout.splitlines(), [self.first + "\t1-title", self.first + "\t2-garden"])

    def test_an_unknown_name_is_refused_before_any_wait_for_unity(self):
        result = self.shot("1-title", "999-khong-co")
        self.assertEqual(result.returncode, 64)
        self.assertIn('nothing is named "999-khong-co"', result.stderr)
        self.assertIn("--list", result.stderr)
        self.assertNotIn("unity-gate", result.stderr)
        self.assertEqual(self.calls(), [])

    def test_no_name_and_an_unknown_option_print_the_usage(self):
        for args in ((), ("--fast",)):
            result = self.shot(*args)
            self.assertEqual(result.returncode, 64)
            self.assertIn("usage: tools/shot.sh", result.stderr)
        self.assertEqual(self.calls(), [])

    def test_a_name_with_other_characters_is_refused(self):
        result = self.shot("1-title:x")
        self.assertEqual(result.returncode, 64)
        self.assertEqual(self.calls(), [])


@unittest.skipUnless(SEES_PROCESSES, "the Unity gate needs to see other processes; run outside the Claude Code sandbox")
class ThroughTheGateTest(ShotTest):
    def test_one_frame_asks_unity_for_that_frame_of_its_group_and_prints_its_path(self):
        result = self.shot("30-buoc-2")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls(), ["VuonTra.EditorTools.PreviewShot.RenderSelected|%s:30-buoc-2 |0" % self.second])
        self.assertEqual(result.stdout.splitlines()[0], "Logs/shot/30-buoc-2.png")
        self.assertIn("shot: ok (1 frames", result.stdout)
        self.assertIn("unity-gate: slot after", result.stderr)
        self.assertEqual([name for name in self.pictures() if name.endswith(".png")], ["30-buoc-2.png"])

    def test_a_pattern_over_two_groups_asks_for_each_group_once(self):
        result = self.shot("*-1", "1-title", "2-*")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls()[0].split("|")[1], "%s:1-title,2-garden %s:30-buoc-1 " % (self.first, self.second))
        self.assertEqual(result.stdout.splitlines()[:3], ["Logs/shot/1-title.png", "Logs/shot/2-garden.png", "Logs/shot/30-buoc-1.png"])

    def test_a_group_draws_all_its_frames_and_lists_one_added_since(self):
        result = self.shot(self.second)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls()[0].split("|")[1], self.second + " ")
        self.assertIn("Logs/shot/30-buoc-moi.png", result.stdout.splitlines())
        self.assertIn("shot: ok (3 frames", result.stdout)
        self.assertIn(self.second + "\t30-buoc-moi", self.names())
        self.assertIn("now lists 5 frames", result.stderr)

    def test_pictures_of_an_earlier_run_are_gone(self):
        self.shot("1-title")
        self.shot("30-buoc-1")
        self.assertEqual([name for name in self.pictures() if name.endswith(".png")], ["30-buoc-1.png"])

    def test_a_frame_unity_did_not_write_fails(self):
        result = self.shot("1-title", "2-garden", FAKE_DROPS="2-garden")
        self.assertEqual(result.returncode, 1)
        self.assertIn("no picture for 2-garden", result.stderr)
        self.assertIn("FAIL", result.stdout)

    def test_an_exception_in_the_unity_log_fails(self):
        result = self.shot("1-title", FAKE_EXCEPTION="1")
        self.assertEqual(result.returncode, 1)
        self.assertIn("NullReferenceException: fake", result.stderr)
        self.assertNotIn("shot: ok", result.stdout)

    def test_seed_runs_the_content_seeder_first(self):
        result = self.shot("--seed", "1-title")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual([call.split("|")[0].rsplit(".", 2)[-2:] for call in self.calls()], [["ContentSeeder", "Seed"], ["PreviewShot", "RenderSelected"]])

    def test_names_rewrites_the_list_and_draws_nothing(self):
        result = self.shot("--names")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls()[0].split("|")[1:], [" ".join(source_groups()) + " ", "1"])
        self.assertEqual(len(self.names()), 5)
        self.assertEqual([name for name in self.pictures() if name.endswith(".png")], [])

    def test_inside_a_held_slot_it_does_not_queue_again(self):
        env = dict(os.environ, UNITY=self.unity, UNITY_GATE_STATE=self.state, UNITY_GATE_POLL="0.1", UNITY_GATE_SAVE=os.path.join(self.state, "save.json"))
        result = subprocess.run([os.path.join(self.project, "tools/unity-gate.sh"), "50", os.path.join(self.project, "tools/shot.sh"), "1-title"],
                                env=env, capture_output=True, text=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stderr.count("unity-gate: queued"), 1)

if __name__ == "__main__":
    unittest.main()
