#!/usr/bin/env bash
set -u
cd "$(dirname "$0")/.."
GATE="${UNITY_GATE:-/tmp/unity-gate.sh}"
PRIO="${1:-4}"
if [ -x "$GATE" ]; then "$GATE" "$PRIO" tools/build.sh webgl; else tools/build.sh webgl; fi
code=$?
if [ $code -eq 0 ] && [ -f Build/WebGL/index.html ]; then
  echo "Build/WebGL: $(du -sh Build/WebGL | cut -f1) on disk"
  ls -l Build/WebGL/Build | awk 'NR>1 {printf "  %-40s %8.1f MB\n", $9, $5/1048576}'
fi
exit $code
