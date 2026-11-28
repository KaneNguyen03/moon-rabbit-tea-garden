import glob
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import test_areas
from test_areas import ROOT, TESTS


def tracked(pattern):
    return [os.path.relpath(path, ROOT) for path in glob.glob(os.path.join(ROOT, pattern), recursive=True)]


def test_files():
    return [path for path in tracked(TESTS + "*.cs") if test_areas.fixtures(path)]


class TestAreas(unittest.TestCase):
    def setUp(self):
        self.areas = test_areas.load()
        self.every_fixture = sorted({name for path in test_files() for name in test_areas.fixtures(path)})

    def test_every_game_and_editor_file_has_an_area(self):
        sources = tracked("Assets/VuonTra/Game/**/*.cs") + tracked("Assets/VuonTra/Editor/**/*.cs")
        self.assertGreater(len(sources), 100)
        homeless = [path for path in sources if test_areas.place(path, self.areas)[0] is None]
        self.assertEqual([], homeless, "add these files to a row of tools/test-areas.tsv")

    def test_every_source_pattern_matches_a_file(self):
        everything = tracked("Assets/**/*") + tracked("ProjectSettings/*")
        for area in self.areas:
            for source in area.sources:
                self.assertTrue(any(test_areas.fnmatch.fnmatchcase(path, source) for path in everything),
                                area.name + ": no file matches " + source)

    def test_every_filter_part_is_a_plain_name_that_matches_a_fixture(self):
        for area in self.areas:
            for part in area.filter.split("|"):
                self.assertRegex(part, r"^\w+$", area.name)
                self.assertTrue(any(part in name for name in self.every_fixture), area.name + ": no test class matches " + part)

    def test_every_fixture_that_needs_unity_is_in_some_area(self):
        parts = {part for area in self.areas for part in area.filter.split("|")}
        needing = {name for path in test_files() if test_areas.needs_unity(path) for name in test_areas.fixtures(path)}
        self.assertGreater(len(needing), 40)
        orphans = sorted(name for name in needing if not any(part in name for part in parts))
        self.assertEqual([], orphans, "no row of tools/test-areas.tsv runs these fixtures")

    def test_the_first_matching_row_wins_so_a_view_with_its_own_row_skips_the_wide_one(self):
        name, test_filter, _ = test_areas.place("Assets/VuonTra/Game/UI/FishingView.cs", self.areas)
        self.assertEqual("fishing", name)
        self.assertEqual("PondScreenTests|Fishing", test_filter)
        self.assertEqual("ui", test_areas.place("Assets/VuonTra/Game/UI/ShopView.cs", self.areas)[0])

    def test_a_changed_web_graphics_api_list_runs_the_web_graphics_tests(self):
        name, test_filter, _ = test_areas.place("ProjectSettings/ProjectSettings.asset", self.areas)
        self.assertEqual("web", name)
        self.assertIn("WebGraphicsTests", test_filter.split("|"))

    def test_a_changed_unity_test_file_runs_its_own_fixture(self):
        self.assertEqual(("its own tests", "SaveStoreTests", ""), test_areas.place(TESTS + "SaveStoreTests.cs", self.areas))

    def test_core_and_pure_tests_need_no_unity(self):
        test_filter, lines = test_areas.resolve(["Assets/VuonTra/Core/BigNum.cs", TESTS + "BigNumTests.cs", "docs/x.md"], self.areas)
        self.assertEqual("", test_filter)
        self.assertEqual(2, len(lines))

    def test_several_files_join_their_filters_without_repeats(self):
        test_filter, _ = test_areas.resolve(["Assets/VuonTra/Game/UI/AwayView.cs", "Assets/VuonTra/Game/UI/FeelLayer.cs"], self.areas)
        self.assertEqual("AwaySheetFeelTests|JuiceTests|EaseTests", test_filter)

    def test_the_test_script_asks_this_map_and_leaves_slow_tests_out(self):
        with open(os.path.join(ROOT, "tools", "test.sh"), encoding="utf-8") as handle:
            script = handle.read()
        self.assertIn("tools/test_areas.py", script)
        self.assertRegex(script, r'-testCategory "!Slow"')


if __name__ == "__main__":
    unittest.main()
