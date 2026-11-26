#!/bin/bash
set -u
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
if [ $# -lt 1 ] || [ -z "$1" ]; then
  echo "usage: tools/test.sh <test filter>   runs the EditMode tests whose full name matches the filter (Unity -testFilter)" >&2
  exit 64
fi
FILTER=$1
tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/test.sh "$@"
RESULTS="$PWD/Logs/TestResults-filter.xml"
LOG="$PWD/Logs/test-filter.log"
mkdir -p Logs
rm -f "$RESULTS"
company=$(sed -n 's/^  companyName: //p' ProjectSettings/ProjectSettings.asset)
product=$(sed -n 's/^  productName: //p' ProjectSettings/ProjectSettings.asset)
player_dir="$HOME/Library/Application Support/$company/$product"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter "$FILTER" -testResults "$RESULTS" -logFile "$LOG" >/dev/null 2>&1
rm -f "$player_dir/TestResults.xml" "$player_dir/PerformanceTestResults.json"
cs=$(grep -E "error CS[0-9]+" "$LOG" | head -5)
if [ -n "$cs" ]; then printf '%s\n' "$cs" >&2; echo "test: FAIL (compile errors, see Logs/test-filter.log)"; exit 1; fi
if [ ! -f "$RESULTS" ]; then echo "test: FAIL (no results, see Logs/test-filter.log)"; exit 1; fi
sum=$(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$RESULTS" | head -1)
total=$(sed -E 's/.*total="([0-9]+)".*/\1/' <<<"$sum"); passed=$(sed -E 's/.*passed="([0-9]+)".*/\1/' <<<"$sum"); failed=$(sed -E 's/.*failed="([0-9]+)".*/\1/' <<<"$sum")
if [ "${total:-0}" -eq 0 ]; then echo "test: FAIL (no test matched \"$FILTER\")"; exit 1; fi
if [ "${failed:-1}" != "0" ]; then
  grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="Failed"' "$RESULTS" | sed -E 's/.*fullname="([^"]*)".*/  failed: \1/' | head -20 >&2
  echo "test: FAIL ($passed/$total passed, see Logs/TestResults-filter.xml)"; exit 1
fi
echo "test: ok ($passed/$total)"
