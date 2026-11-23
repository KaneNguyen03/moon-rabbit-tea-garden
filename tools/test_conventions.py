import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(HERE, "conventions.py")
sys.path.insert(0, HERE)
import conventions


class Counting(unittest.TestCase):
    def counts(self, text, path="Assets/VuonTra/Game/Probe.cs"):
        return conventions.count(path, text)

    def test_line_block_and_trailing_comments_count(self):
        self.assertEqual(self.counts("// a\nint x = 1; /* b */\nint y; // c\n").get("comment"), 3)

    def test_comment_markers_inside_strings_do_not_count(self):
        text = 'var u = "https://x/*y*/"; var v = @"a//b""c"; var w = $"{n}//"; char s = \'/\';\n'
        self.assertEqual(self.counts(text), {})

    def test_multiline_empty_catch_is_a_workaround(self):
        text = "void Save() { try { Read(); } catch (System.Exception)\n{\n}\n}\n"
        self.assertEqual(self.counts(text).get("workaround"), 1)

    def test_catch_holding_only_a_comment_is_a_workaround(self):
        text = "try { Read(); }\ncatch\n{\n    // nothing to do\n}\n"
        self.assertEqual(self.counts(text).get("workaround"), 1)

    def test_catch_that_logs_is_fine(self):
        self.assertNotIn("workaround", self.counts("try { Read(); } catch (Exception e) { Log(e); }\n"))

    def test_unity_update_counts_but_a_parameterized_update_does_not(self):
        self.assertEqual(self.counts("void Update() { }\npublic void Update(float dt) { }\n").get("per-frame-update"), 1)

    def test_event_invoke_is_not_invoke_by_name(self):
        self.assertEqual(self.counts("Changed?.Invoke(1); handler.Invoke(); Invoke(\"Go\", 0f);\n").get("workaround"), 1)

    def test_math_that_builds_data_is_not_animation(self):
        line = "var a = Mathf.Sin(1) * Mathf.Lerp(0, 1, 0.5f);\n"
        for path in ("Assets/VuonTra/Game/Audio/Sfx.cs", "Assets/VuonTra/Game/UI/Palette.cs", "Assets/VuonTra/Game/UI/PaperGrain.cs"):
            self.assertNotIn("procedural-animation", self.counts(line, path), path)
        for path in ("Assets/VuonTra/Game/UI/FishingView.cs", "Assets/VuonTra/Game/Audio/StationLoopPlayer.cs"):
            self.assertEqual(self.counts(line, path).get("procedural-animation"), 2, path)

    def test_game_only_rules_skip_core(self):
        self.assertEqual(self.counts("var a = Mathf.Sin(1);\n", "Assets/VuonTra/Core/Probe.cs"), {})


class Repository(unittest.TestCase):
    def setUp(self):
        self.root = os.path.realpath(tempfile.mkdtemp())
        os.makedirs(os.path.join(self.root, "tools"))
        os.makedirs(os.path.join(self.root, "Assets/VuonTra/Game"))
        shutil.copy(SCRIPT, os.path.join(self.root, "tools/conventions.py"))
        self.git("init", "-q")
        self.git("config", "user.email", "t@t")
        self.git("config", "user.name", "t")
        self.write("Assets/VuonTra/Game/Old.cs", "// old\nclass Old {}\n")
        self.write("tools/conventions.baseline", "comment\tAssets/VuonTra/Game/Old.cs\t1\n")
        self.git("add", "-A")
        self.git("commit", "-qm", "base")

    def tearDown(self):
        shutil.rmtree(self.root)

    def git(self, *args):
        subprocess.run(["git", "-C", self.root, *args], check=True, capture_output=True)

    def write(self, path, text):
        full = os.path.join(self.root, path)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w", encoding="utf-8") as f:
            f.write(text)

    def run_script(self, *args, stdin=None):
        return subprocess.run([sys.executable, "-I", os.path.join(self.root, "tools/conventions.py"), *args],
                              cwd=self.root, input=stdin, capture_output=True, text=True).returncode

    def test_known_violations_pass_and_a_new_one_fails(self):
        self.assertEqual(self.run_script(), 0)
        self.write("Assets/VuonTra/Game/New.cs", "class New { int x; /* new */ }\n")
        self.git("add", "-A")
        self.assertEqual(self.run_script(), 1)

    def test_missing_baseline_fails_closed(self):
        os.remove(os.path.join(self.root, "tools/conventions.baseline"))
        self.assertEqual(self.run_script(), 2)
        self.assertEqual(self.run_script("Assets/VuonTra/Game/Old.cs"), 2)

    def test_vietnamese_file_names_are_scanned(self):
        self.write("Assets/VuonTra/Game/Thỏ.cs", "// new\nclass Tho {}\n")
        self.git("add", "-A")
        self.assertEqual(self.run_script(), 1)

    def test_staged_snapshot_is_checked_not_the_working_tree(self):
        self.write("Assets/VuonTra/Game/Probe.cs", "class Probe {}\n// new\n")
        self.git("add", "-A")
        self.write("Assets/VuonTra/Game/Probe.cs", "class Probe {}\n")
        self.assertEqual(self.run_script("--staged"), 1)

    def test_raised_baseline_in_the_index_fails_even_if_the_working_copy_is_restored(self):
        self.write("tools/conventions.baseline", "comment\tAssets/VuonTra/Game/Old.cs\t9\n")
        self.git("add", "-A")
        self.write("tools/conventions.baseline", "comment\tAssets/VuonTra/Game/Old.cs\t1\n")
        self.assertEqual(self.run_script("--since", "HEAD", "--index"), 1)
        self.assertEqual(self.run_script("--since", "HEAD"), 0)

    def test_lower_only_goes_down(self):
        self.write("Assets/VuonTra/Game/Old.cs", "class Old {}\n")
        self.assertEqual(self.run_script("--lower"), 0)
        with open(os.path.join(self.root, "tools/conventions.baseline")) as f:
            self.assertEqual(f.read(), "")

    def test_hook_blocks_the_baseline_through_any_path_spelling(self):
        for path in ("tools/conventions.baseline", "tools/../tools/conventions.baseline", "./tools/./conventions.baseline"):
            event = json.dumps({"hook_event_name": "PreToolUse",
                                "tool_input": {"file_path": os.path.join(self.root, path)}})
            self.assertEqual(self.run_script("--hook", stdin=event), 2, path)

    def test_hook_checks_an_edited_file_through_a_dotted_path(self):
        self.write("Assets/VuonTra/Game/Probe.cs", "// new\nclass Probe {}\n")
        event = json.dumps({"hook_event_name": "PostToolUse",
                            "tool_input": {"file_path": os.path.join(self.root, "Assets/./VuonTra/Game/Probe.cs")}})
        self.assertEqual(self.run_script("--hook", stdin=event), 2)


if __name__ == "__main__":
    unittest.main()
