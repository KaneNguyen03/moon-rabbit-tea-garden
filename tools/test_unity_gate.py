import os
import shutil
import signal
import subprocess
import tempfile
import time
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GATE = os.path.join(ROOT, "tools", "unity-gate.sh")
SEES_PROCESSES = subprocess.run("ps -p 1", shell=True, capture_output=True).returncode == 0


@unittest.skipUnless(SEES_PROCESSES, "the gate needs to see other processes; run outside the Claude Code sandbox")
class GateTest(unittest.TestCase):
    def setUp(self):
        self.state = tempfile.mkdtemp(prefix="gate-test-")
        self.log = os.path.join(self.state, "events.log")
        self.save = os.path.join(self.state, "save.json")
        self.children = []
        self.limit(1)

    def tearDown(self):
        for child in self.children:
            if child.poll() is None:
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                child.wait()
            for pipe in (child.stdout, child.stderr):
                if pipe:
                    pipe.close()
        shutil.rmtree(self.state, ignore_errors=True)

    def limit(self, slots):
        with open(os.path.join(self.state, "unity-gate-rules.sh"), "w") as rules:
            rules.write("lim(){ echo %d; }\nunity_count(){ echo 0; }\n" % slots)

    def env(self, **extra):
        env = dict(os.environ, UNITY_GATE_STATE=self.state, UNITY_GATE_POLL="0.1", UNITY_GATE_SAVE=self.save)
        env.update(extra)
        return env

    def gate(self, *args, **extra):
        child = subprocess.Popen([GATE, *args], env=self.env(**extra), stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 text=True, start_new_session=True, cwd=ROOT)
        self.children.append(child)
        return child

    def job(self, name, seconds):
        return ["sh", "-c", f'echo "start {name}" >> "{self.log}"; sleep {seconds}; echo "end {name}" >> "{self.log}"']

    def events(self):
        if not os.path.exists(self.log):
            return []
        with open(self.log) as log:
            return [line.split() for line in log.read().splitlines()]

    def until(self, condition, seconds=20):
        deadline = time.time() + seconds
        while time.time() < deadline:
            if condition():
                return
            time.sleep(0.05)
        self.fail("timed out; events so far: %r, status:\n%s" % (self.events(), self.status()))

    def kill(self, child):
        os.killpg(child.pid, signal.SIGKILL)
        child.wait()

    def kill_outside_the_lock(self, child):
        lock = os.path.join(self.state, "unity-gate.lock")
        for _ in range(200):
            os.killpg(child.pid, signal.SIGSTOP)
            time.sleep(0.05)
            if not os.path.exists(lock):
                self.kill(child)
                return
            os.killpg(child.pid, signal.SIGCONT)
            time.sleep(0.03)
        self.fail("the waiter never left the lock")

    def read(self, *parts):
        with open(os.path.join(self.state, *parts)) as handle:
            return handle.read()

    def started(self, name):
        return ["start", name] in self.events()

    def names(self, folder):
        return sorted(os.listdir(os.path.join(self.state, folder)))

    def status(self):
        return subprocess.run([GATE, "status"], env=self.env(), capture_output=True, text=True, cwd=ROOT).stdout

    def most_at_once(self):
        running = most = 0
        for kind, _ in self.events():
            running += 1 if kind == "start" else -1
            most = max(most, running)
        return most

    def test_one_slot_runs_contending_commands_one_at_a_time(self):
        jobs = [self.gate("50", *self.job(f"job{i}", 0.3)) for i in range(5)]
        for child in jobs:
            self.assertEqual(child.wait(timeout=60), 0)
        self.assertEqual(len(self.events()), 10)
        self.assertEqual(self.most_at_once(), 1)

    def test_two_slots_run_two_at_once_and_never_three(self):
        self.limit(2)
        jobs = [self.gate("50", *self.job(f"job{i}", 0.6)) for i in range(5)]
        for child in jobs:
            self.assertEqual(child.wait(timeout=60), 0)
        self.assertEqual(self.most_at_once(), 2)

    def test_zero_slots_runs_nothing(self):
        self.limit(0)
        self.gate("50", *self.job("never", 0))
        time.sleep(1.5)
        self.assertEqual(self.events(), [])
        self.assertEqual(len(self.names("unity-wait")), 1)

    def test_lower_priority_number_goes_first_and_equal_priority_keeps_arrival_order(self):
        holder = self.gate("50", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        order = [("50", "second-fifty"), ("50", "third-fifty"), ("5", "urgent")]
        waiting = []
        for priority, name in order:
            waiting.append(self.gate(priority, *self.job(name, 0.1)))
            time.sleep(1.1)
        self.assertEqual(len(self.names("unity-wait")), 3)
        self.kill(holder)
        for child in waiting:
            self.assertEqual(child.wait(timeout=60), 0)
        starts = [name for kind, name in self.events() if kind == "start"]
        self.assertEqual(starts, ["holder", "urgent", "second-fifty", "third-fifty"])

    def test_killed_holder_frees_its_slot(self):
        holder = self.gate("50", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        waiter = self.gate("50", *self.job("waiter", 0))
        time.sleep(0.5)
        self.assertFalse(self.started("waiter"))
        self.kill(holder)
        self.assertEqual(waiter.wait(timeout=30), 0)
        self.assertTrue(self.started("waiter"))
        self.assertEqual(self.names("unity-running"), [])

    def test_killed_waiter_does_not_block_those_behind_it(self):
        holder = self.gate("50", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        dead = self.gate("1", *self.job("dead", 0))
        self.until(lambda: len(self.names("unity-wait")) == 1)
        self.kill_outside_the_lock(dead)
        self.assertEqual(len(self.names("unity-wait")), 1)
        waiter = self.gate("50", *self.job("waiter", 0))
        self.until(lambda: len(self.names("unity-wait")) == 1 and self.names("unity-wait")[0].startswith("50."))
        self.kill(holder)
        self.assertEqual(waiter.wait(timeout=30), 0)
        self.assertFalse(self.started("dead"))
        self.assertEqual(self.names("unity-wait"), [])

    def test_interrupted_waiter_leaves_the_queue(self):
        holder = self.gate("50", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        waiter = self.gate("50", *self.job("waiter", 0))
        self.until(lambda: len(self.names("unity-wait")) == 1)
        waiter.terminate()
        self.assertEqual(waiter.wait(timeout=10), 143)
        self.assertEqual(self.names("unity-wait"), [])
        self.assertEqual(self.names("unity-gate-info"), [str(holder.pid)])

    def test_lock_left_by_a_killed_process_is_broken_after_two_minutes(self):
        lock = os.path.join(self.state, "unity-gate.lock")
        os.mkdir(lock)
        old = time.time() - 180
        os.utime(lock, (old, old))
        self.assertEqual(self.gate("50", *self.job("job", 0)).wait(timeout=30), 0)
        self.assertFalse(os.path.exists(lock))

    def test_fresh_lock_is_respected(self):
        lock = os.path.join(self.state, "unity-gate.lock")
        os.mkdir(lock)
        child = self.gate("50", *self.job("job", 0))
        time.sleep(1.0)
        self.assertFalse(self.started("job"))
        os.rmdir(lock)
        self.assertEqual(child.wait(timeout=30), 0)

    def test_run_through_the_old_tmp_gate_holds_a_slot(self):
        old_holder = subprocess.Popen(["sleep", "300"], start_new_session=True)
        self.children.append(old_holder)
        os.makedirs(os.path.join(self.state, "unity-running"))
        marker = os.path.join(self.state, "unity-running", str(old_holder.pid))
        open(marker, "w").close()
        child = self.gate("50", *self.job("job", 0))
        time.sleep(1.0)
        self.assertFalse(self.started("job"))
        old_holder.kill()
        old_holder.wait()
        self.assertEqual(child.wait(timeout=30), 0)
        self.assertFalse(os.path.exists(marker))

    def test_waiter_of_the_old_tmp_gate_keeps_its_turn_while_it_lives(self):
        os.makedirs(os.path.join(self.state, "unity-wait"))
        entry = os.path.join(self.state, "unity-wait", "7")
        open(entry, "w").close()
        old = time.time() - 600
        os.utime(entry, (old, old))
        old_gate = os.path.join(self.state, "old", "unity-gate.sh")
        os.makedirs(os.path.dirname(old_gate))
        with open(old_gate, "w") as script:
            script.write("sleep 300\n")
        old_waiter = subprocess.Popen(["bash", old_gate, "7", "tools/verify.sh"], start_new_session=True)
        self.children.append(old_waiter)
        child = self.gate("50", *self.job("job", 0))
        time.sleep(1.5)
        self.assertFalse(self.started("job"))
        self.assertIn("priority 7  pid %d" % old_waiter.pid, self.status())
        self.kill(old_waiter)
        self.assertEqual(child.wait(timeout=30), 0)
        self.assertFalse(os.path.exists(entry))

    def test_command_under_a_held_slot_runs_without_queueing_again(self):
        outer = self.gate("50", GATE, "50", *self.job("inner", 0))
        self.assertEqual(outer.wait(timeout=30), 0)
        self.assertTrue(self.started("inner"))

    def test_held_tells_whether_a_parent_holds_a_slot(self):
        self.assertNotEqual(subprocess.run([GATE, "held"], env=self.env(), cwd=ROOT).returncode, 0)
        self.assertEqual(self.gate("50", GATE, "held").wait(timeout=30), 0)

    def test_exit_code_of_the_command_is_passed_on(self):
        self.assertEqual(self.gate("50", "sh", "-c", "exit 7").wait(timeout=30), 7)
        self.assertEqual(self.names("unity-running"), [])
        self.assertEqual(self.names("unity-gate-info"), [])

    def test_gives_up_after_the_longest_wait_asked_for(self):
        holder = self.gate("50", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        waiter = self.gate("50", *self.job("waiter", 0), UNITY_GATE_MAX_WAIT="1")
        self.assertEqual(waiter.wait(timeout=30), 75)
        self.assertIn("the command did not run", waiter.stderr.read())
        self.assertFalse(self.started("waiter"))
        self.assertEqual(self.names("unity-wait"), [])

    def test_status_names_who_holds_and_who_waits(self):
        self.assertIn("0 of 1 slots in use, 0 waiting", self.status())
        holder = self.gate("5", *self.job("holder", 300))
        self.until(lambda: self.started("holder"))
        waiter = self.gate("60", *self.job("waiter", 0))
        self.until(lambda: len(self.names("unity-wait")) == 1)
        time.sleep(1.1)
        status = self.status()
        self.assertIn("1 of 1 slots in use, 1 waiting", status)
        self.assertRegex(status, r"held \d+s  priority 5  pid %d  %s  \$ sh -c echo \"start holder\"" % (holder.pid, ROOT))
        self.assertRegex(status, r"waited \d+s  priority 60  pid %d  %s  \$ sh -c echo \"start waiter\"" % (waiter.pid, ROOT))
        self.assertLess(status.index("priority 5 "), status.index("priority 60 "))

    def test_player_save_changed_by_a_run_is_reported_and_both_copies_kept(self):
        with open(self.save, "w") as save:
            save.write("before")
        child = self.gate("50", "sh", "-c", f'printf after > "{self.save}"')
        self.assertEqual(child.wait(timeout=30), 0)
        self.assertIn("player save changed", child.stderr.read())
        copies = {name: self.read("save-guard", name) for name in self.names("save-guard")}
        self.assertEqual(sorted(copies.values()), sorted(["before", "after", copies["changed.log"]]))
        self.assertIn("save NOT touched", copies["changed.log"])
        self.assertEqual(self.read("save.json"), "after")

    def test_untouched_player_save_is_not_reported(self):
        with open(self.save, "w") as save:
            save.write("before")
        child = self.gate("50", "true")
        self.assertEqual(child.wait(timeout=30), 0)
        self.assertEqual([self.read("save-guard", name) for name in self.names("save-guard")], ["before"])

    def test_bad_arguments_are_refused(self):
        for args in ([], ["fast", "true"], ["50"]):
            self.assertEqual(self.gate(*args).wait(timeout=10), 64)
        self.assertEqual(self.events(), [])


class CallersTest(unittest.TestCase):
    def test_every_tool_that_starts_unity_goes_through_the_gate(self):
        tools = os.path.join(ROOT, "tools")
        for name in sorted(os.listdir(tools)):
            path = os.path.join(tools, name)
            if not name.endswith(".sh") or name == "unity-gate.sh":
                continue
            with open(path) as script:
                lines = script.read().splitlines()
            launches = [i for i, line in enumerate(lines) if '"$UNITY" -' in line]
            if not launches:
                continue
            gated = [i for i, line in enumerate(lines)
                     if line.startswith("tools/unity-gate.sh held || exec tools/unity-gate.sh ") and f"tools/{name}" in line]
            self.assertTrue(gated and gated[0] < launches[0], f"tools/{name} starts Unity without tools/unity-gate.sh")


if __name__ == "__main__":
    unittest.main()
