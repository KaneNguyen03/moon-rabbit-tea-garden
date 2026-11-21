#!/bin/bash
# One command checks the whole project. Exit code is non-zero when any step fails.
#   tools/verify.sh           seed, previews, EditMode tests
#   tools/verify.sh --build   also builds the Mac player, opens it for a few seconds and reads Player.log
# Close the Unity Editor first: batch mode cannot open a project that an editor holds.
set -u
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
BUILD=0; [ "${1:-}" = "--build" ] && BUILD=1
mkdir -p Logs
declare -a NAMES RESULTS
fail=0
note() { NAMES+=("$1"); RESULTS+=("$2"); case "$2" in FAIL*) fail=1;; esac; }

if [ -f Temp/UnityLockfile ] && pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null; then
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

echo "== 1/4 seed content and string tables"
run_unity "seed" Logs/seed.log -executeMethod VuonTra.EditorTools.ContentSeeder.Seed -quit && note "seed" "ok"

echo "== 2/4 preview shots"
rm -rf Logs/preview
if run_unity "preview" Logs/preview.log -executeMethod VuonTra.EditorTools.PreviewShot.Render -quit; then
  n=$(ls Logs/preview/*.png 2>/dev/null | wc -l | tr -d ' ')
  if [ "$n" -ge 50 ]; then note "preview" "ok ($n frames)"; else note "preview" "FAIL ($n frames, expected 50 or more)"; fi
fi

echo "== 3/4 compile + EditMode tests"
rm -f TestResults.xml
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "$PWD/TestResults.xml" -logFile "$PWD/Logs/test.log" >/dev/null 2>&1
cs=$(grep -E "error CS[0-9]+" Logs/test.log | head -5)
if [ -n "$cs" ]; then printf '%s\n' "$cs" >&2; note "compile" "FAIL"; note "tests" "skipped"
else
  note "compile" "ok"
  if [ -f TestResults.xml ]; then
    sum=$(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' TestResults.xml | head -1)
    total=$(sed -E 's/.*total="([0-9]+)".*/\1/' <<<"$sum"); passed=$(sed -E 's/.*passed="([0-9]+)".*/\1/' <<<"$sum"); failed=$(sed -E 's/.*failed="([0-9]+)".*/\1/' <<<"$sum")
    if [ "${failed:-1}" = "0" ] && [ "${total:-0}" -gt 0 ]; then note "tests" "ok ($passed/$total)"
    else
      grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="Failed"' TestResults.xml | sed -E 's/.*fullname="([^"]*)".*/  failed: \1/' | head -20 >&2
      note "tests" "FAIL ($passed/$total passed, $failed failed)"
    fi
  else note "tests" "FAIL (no TestResults.xml, see Logs/test.log)"; fi
fi

if [ $BUILD -eq 1 ]; then
  echo "== 4/4 Mac build"
  pkill -f "VuonTraTho.app/Contents/MacOS" 2>/dev/null; sleep 1
  if run_unity "build" Logs/build.log -executeMethod VuonTra.EditorTools.BuildScript.BuildMac -quit; then
    plog="$HOME/Library/Logs/VuonTraTho/Vườn Trà Thỏ Trăng/Player.log"
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
