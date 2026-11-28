import os
import shutil
import subprocess
import tempfile
import unittest

SCRIPT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "publish-pages.sh")
IDENTITY = {"GIT_AUTHOR_NAME": "Test", "GIT_AUTHOR_EMAIL": "test@example.com",
            "GIT_COMMITTER_NAME": "Test", "GIT_COMMITTER_EMAIL": "test@example.com"}


def git(cwd, *args):
    return subprocess.run(["git", "-C", cwd, *args], check=True, capture_output=True, text=True,
                          env={**os.environ, **IDENTITY}).stdout


def write(root, files):
    for path, text in files.items():
        full = os.path.join(root, path)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w") as handle:
            handle.write(text)


class PublishPages(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.mkdtemp()
        self.remote = os.path.join(self.tmp, "remote.git")
        git(self.tmp, "init", "-q", "--bare", self.remote)
        seed = os.path.join(self.tmp, "seed")
        git(self.tmp, "init", "-q", "-b", "gh-pages", seed)
        write(seed, {
            ".nojekyll": "",
            "index.html": "live root",
            "Build/WebGL.data.unityweb": "live data",
            "Build/stale.unityweb": "only in the live build",
            "old/index.html": "previous build",
            "old/Build/WebGL.data.unityweb": "previous data",
            "extra/notes.txt": "kept only when listed",
        })
        git(seed, "add", "-A")
        git(seed, "commit", "-q", "-m", "live")
        git(seed, "push", "-q", self.remote, "gh-pages")
        self.project = os.path.join(self.tmp, "project")
        os.makedirs(os.path.join(self.project, "tools"))
        shutil.copy(SCRIPT, os.path.join(self.project, "tools", "publish-pages.sh"))
        git(self.tmp, "init", "-q", self.project)
        git(self.project, "commit", "-q", "--allow-empty", "-m", "source")
        write(self.project, {"Build/WebGL/index.html": "new root", "Build/WebGL/Build/WebGL.data.unityweb": "new data"})

    def tearDown(self):
        shutil.rmtree(self.tmp)

    def publish(self, **env):
        clean = {k: v for k, v in os.environ.items() if k != "PAGES_KEEP"}
        return subprocess.run(["bash", os.path.join(self.project, "tools", "publish-pages.sh"), self.remote],
                              capture_output=True, text=True, env={**clean, **IDENTITY, **env})

    def published(self):
        return sorted(git(self.remote, "ls-tree", "-r", "--name-only", "gh-pages").split())

    def show(self, path):
        return git(self.remote, "show", "gh-pages:" + path)

    def test_a_publish_keeps_old_byte_for_byte_and_replaces_the_rest(self):
        before = git(self.remote, "rev-parse", "gh-pages:old")
        result = self.publish()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(before, git(self.remote, "rev-parse", "gh-pages:old"))
        self.assertEqual([".nojekyll", "Build/WebGL.data.unityweb", "index.html",
                          "old/Build/WebGL.data.unityweb", "old/index.html"], self.published())
        self.assertEqual("new root", self.show("index.html"))
        self.assertEqual(2, len(git(self.remote, "rev-list", "gh-pages").split()))

    def test_publishing_the_same_build_twice_pushes_nothing(self):
        self.assertEqual(0, self.publish().returncode)
        again = self.publish()
        self.assertEqual(0, again.returncode, again.stderr)
        self.assertIn("nothing to push", again.stdout)
        self.assertEqual(2, len(git(self.remote, "rev-list", "gh-pages").split()))

    def test_the_keep_list_can_name_more_paths_or_none(self):
        self.assertEqual(0, self.publish(PAGES_KEEP="old extra").returncode)
        self.assertIn("extra/notes.txt", self.published())
        self.assertIn("old/index.html", self.published())
        self.assertEqual(0, self.publish(PAGES_KEEP="").returncode)
        self.assertFalse([path for path in self.published() if path.startswith(("old/", "extra/"))])

    def test_a_build_that_holds_a_kept_path_stops_before_pushing(self):
        write(self.project, {"Build/WebGL/old/index.html": "clash"})
        result = self.publish()
        self.assertEqual(4, result.returncode)
        self.assertEqual(1, len(git(self.remote, "rev-list", "gh-pages").split()))


if __name__ == "__main__":
    unittest.main()
