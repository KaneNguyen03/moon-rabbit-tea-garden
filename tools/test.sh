#!/bin/bash
set -u
cd "$(dirname "$0")/.."
. tools/unity-env.sh
if [ "${1:-}" = "--changed" ]; then
  shift
  FILTER=$(python3 -I tools/test_areas.py "$@") || exit 1
  if [ -z "$FILTER" ]; then echo "test: ok (tools/test-areas.tsv maps no Unity test to these files)"; exit 0; fi
  set -- --fast "$FILTER"
fi
FAST=0
if [ "${1:-}" = "--fast" ]; then FAST=1; shift; fi
if [ $# -ne 1 ] || [ -z "$1" ]; then
  cat >&2 <<'USAGE'
usage: tools/test.sh <filter>            EditMode tests whose full name matches the filter, a regular expression (Unity -testFilter)
       tools/test.sh --fast <filter>     the same without the [Category("Slow")] tests
       tools/test.sh --changed           the tests tools/test-areas.tsv maps to the files changed since main, without the Slow ones
       tools/test.sh --changed <path>... the same for the files named
USAGE
  exit 64
fi
FILTER=$1
[ $FAST -eq 1 ] && set -- --fast "$FILTER"
tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/test.sh "$@"
CATEGORY=(); [ $FAST -eq 1 ] && CATEGORY=(-testCategory "!Slow")
STARTED=$SECONDS
RESULTS="$PWD/Logs/TestResults-filter.xml"
LOG="$PWD/Logs/test-filter.log"
mkdir -p Logs
rm -f "$RESULTS"
PLAYER_DIR=$(player_dir)
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter "$FILTER" ${CATEGORY[@]+"${CATEGORY[@]}"} -testResults "$RESULTS" -logFile "$LOG" >/dev/null 2>&1
rm -f "$PLAYER_DIR/TestResults.xml" "$PLAYER_DIR/PerformanceTestResults.json"
cs=$(grep -E "error CS[0-9]+" "$LOG" | head -5)
if [ -n "$cs" ]; then printf '%s\n' "$cs" >&2; echo "test: FAIL (compile errors, see Logs/test-filter.log)"; exit 1; fi
if [ ! -f "$RESULTS" ]; then echo "test: FAIL (no results, see Logs/test-filter.log)"; exit 1; fi
sum=$(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$RESULTS" | head -1)
total=$(sed -E 's/.*total="([0-9]+)".*/\1/' <<<"$sum"); passed=$(sed -E 's/.*passed="([0-9]+)".*/\1/' <<<"$sum"); failed=$(sed -E 's/.*failed="([0-9]+)".*/\1/' <<<"$sum")
took="$((SECONDS - STARTED)) s in Unity"
if [ "${total:-0}" -eq 0 ] && [ $FAST -eq 1 ]; then echo "test: FAIL (\"$FILTER\" matches only Slow tests or none; tools/test.sh \"$FILTER\" runs the Slow ones too)"; exit 1; fi
if [ "${total:-0}" -eq 0 ]; then echo "test: FAIL (no test matched \"$FILTER\")"; exit 1; fi
if [ "${failed:-1}" != "0" ]; then
  grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="Failed"' "$RESULTS" | sed -E 's/.*fullname="([^"]*)".*/  failed: \1/' | head -20 >&2
  echo "test: FAIL ($passed/$total passed, see Logs/TestResults-filter.xml)"; exit 1
fi
echo "test: ok ($passed/$total, $took)"
