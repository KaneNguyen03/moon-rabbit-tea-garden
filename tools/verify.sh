#!/bin/bash
# One command checks the whole project. Exit code is non-zero when any step fails.
#   tools/verify.sh           seed, previews, EditMode tests
#   tools/verify.sh --build   also builds the Mac player, opens it for a few seconds and reads Player.log
#   tools/verify.sh --fast    seed + compile + EditMode tests without the [Category("Slow")] ones (multi-seed pacing
#                             sims, exhaustive sweeps) and without preview shots; for agents iterating on a branch.
#                             Add --preview to take the preview shots too. The full verify (no flag) runs everything
#                             and is what main needs after each round of merges.
# Close the Unity Editor first: batch mode cannot open a project that an editor holds.
set -u
cd "$(dirname "$0")/.."
. tools/unity-env.sh
BUILD=0; FAST=0; PREVIEW=1
for arg in "$@"; do
  case "$arg" in
    --build) BUILD=1 ;;
    --fast) FAST=1; PREVIEW=0 ;;
    --preview) PREVIEW=1 ;;
    *) echo "verify: unknown option $arg (use --build, --fast, --preview)" >&2; exit 64 ;;
  esac
done
tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/verify.sh "$@"
mkdir -p Logs
declare -a NAMES RESULTS
fail=0
note() { NAMES+=("$1"); RESULTS+=("$2"); case "$2" in FAIL*) fail=1;; esac; }

if [ -f Temp/UnityLockfile ] && unity_editor_open; then
  echo "verify: the Unity Editor holds this project (Temp/UnityLockfile). Close it first." >&2
  exit 2
fi

# Compiler errors and unhandled exceptions in a batch log. File names in the build size report
# also contain "Exception", so only lines that start with an exception type count.
log_errors() { grep -E "error CS[0-9]+|^[A-Za-z0-9_.]*Exception: |^Unhandled Exception" "$1" | grep -v "^ *[0-9.]* kb" | head -5; }

run_unity() { # name, logfile, args...
  local name=$1 log=$2; shift 2
  "$UNITY" -batchmode -projectPath "$PWD" -logFile "$PWD/$log" "$@" >/dev/null 2>&1
  local code=$?
  local errs; errs=$(log_errors "$log")
  if [ -n "$errs" ]; then printf '%s\n' "$errs" >&2; note "$name" "FAIL (errors in $log)"; return 1; fi
  if [ $code -ne 0 ]; then note "$name" "FAIL (exit $code, see $log)"; return 1; fi
  return 0
}

PLAYER_DIR=$(player_dir)
player_save() { printf '%s\n' "$PLAYER_DIR/save.json"; }
save_stamp() {
  local f; f=$(player_save)
  if [ -f "$f" ]; then printf '%s %s\n' "$(file_time "$f")" "$(shasum "$f" | cut -d' ' -f1)"; else echo "none"; fi
}
SAVE_BEFORE=$(save_stamp)

echo "== 0/4 project rules"
if python3 -I tools/conventions.py >Logs/conventions.log 2>&1; then note "rules" "ok"; else cat Logs/conventions.log >&2; note "rules" "FAIL (see Logs/conventions.log)"; fi
if python3 -I tools/test_test_areas.py >Logs/test-areas.log 2>&1; then note "areas" "ok"; else tail -20 Logs/test-areas.log >&2; note "areas" "FAIL (tools/test-areas.tsv, see Logs/test-areas.log)"; fi

echo "== 1/4 seed content and string tables"
run_unity "seed" Logs/seed.log -executeMethod VuonTra.EditorTools.ContentSeeder.Seed -quit && note "seed" "ok"

echo "== 2/4 preview shots"
if [ $PREVIEW -eq 0 ]; then note "preview" "skipped (--fast)"
else
rm -rf Logs/preview
if run_unity "preview" Logs/preview.log -executeMethod VuonTra.EditorTools.PreviewShot.Render -quit; then
  n=$(ls Logs/preview/*.png 2>/dev/null | wc -l | tr -d ' ')
  english=Logs/preview/english-in-vi.txt
  if [ "$n" -lt 50 ]; then note "preview" "FAIL ($n frames, expected 50 or more)"
  elif [ ! -f "$english" ]; then note "preview" "FAIL (no language check, $english missing)"
  elif [ -s "$english" ]; then head -10 "$english" >&2; note "preview" "FAIL ($(wc -l <"$english" | tr -d ' ') English labels in vi frames, see $english)"
  else note "preview" "ok ($n frames, vi frames all from the vi table)"; fi
fi
fi

echo "== 3/4 compile + EditMode tests"
rm -f TestResults.xml
CATEGORY=(); [ $FAST -eq 1 ] && CATEGORY=(-testCategory "!Slow")
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode ${CATEGORY[@]+"${CATEGORY[@]}"} -testResults "$PWD/TestResults.xml" -logFile "$PWD/Logs/test.log" >/dev/null 2>&1
cs=$(grep -E "error CS[0-9]+" Logs/test.log | head -5)
if [ -n "$cs" ]; then printf '%s\n' "$cs" >&2; note "compile" "FAIL"; note "tests" "skipped"
else
  note "compile" "ok"
  if [ -f TestResults.xml ]; then
    sum=$(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' TestResults.xml | head -1)
    total=$(sed -E 's/.*total="([0-9]+)".*/\1/' <<<"$sum"); passed=$(sed -E 's/.*passed="([0-9]+)".*/\1/' <<<"$sum"); failed=$(sed -E 's/.*failed="([0-9]+)".*/\1/' <<<"$sum")
    if [ "${failed:-1}" = "0" ] && [ "${total:-0}" -gt 0 ]; then note "tests" "ok ($passed/$total$([ $FAST -eq 1 ] && echo ", Slow skipped"))"
    else
      grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="Failed"' TestResults.xml | sed -E 's/.*fullname="([^"]*)".*/  failed: \1/' | head -20 >&2
      note "tests" "FAIL ($passed/$total passed, $failed failed)"
    fi
  else note "tests" "FAIL (no TestResults.xml, see Logs/test.log)"; fi
fi

SAVE_AFTER=$(save_stamp)
rm -f "$PLAYER_DIR/TestResults.xml" "$PLAYER_DIR/PerformanceTestResults.json"
if [ "$SAVE_BEFORE" = "$SAVE_AFTER" ]; then note "save" "ok (player save.json untouched: $SAVE_AFTER)"
else note "save" "FAIL (player save.json changed during the run: $SAVE_BEFORE -> $SAVE_AFTER; was the game open?)"; fi

if [ $BUILD -eq 1 ]; then
  echo "== 4/4 Mac build"
  pkill -f "VuonTraTho.app/Contents/MacOS" 2>/dev/null; sleep 1
  if run_unity "build" Logs/build.log -executeMethod VuonTra.EditorTools.BuildScript.BuildMac -quit; then
    plog="$HOME/Library/Logs/VuonTraTho/Moon Rabbit Tea Garden/Player.log"
    rm -f "$plog"
    open Build/Mac/VuonTraTho.app; sleep 8
    pkill -f "VuonTraTho.app/Contents/MacOS"; sleep 1
    if [ ! -f "$plog" ]; then note "build" "FAIL (app wrote no Player.log)"
    elif grep -qiE "exception|error CS" "$plog"; then grep -iE "exception" "$plog" | head -5 >&2; note "build" "FAIL (exception in Player.log)"
    else note "build" "ok (app ran, Player.log clean)"; fi
  fi
else
  note "build" "skipped (pass --build)"
fi

echo; echo "== verify"
for i in "${!NAMES[@]}"; do printf '  %-8s %s\n' "${NAMES[$i]}" "${RESULTS[$i]}"; done
[ $fail -eq 0 ] && echo "  RESULT   GREEN" || echo "  RESULT   RED"
exit $fail
