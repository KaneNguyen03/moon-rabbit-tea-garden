#!/usr/bin/env bash
set -u
cd "$(dirname "$0")/.."
UNITY_GATE_PRIORITY="${1:-${UNITY_GATE_PRIORITY:-4}}" tools/build.sh webgl
code=$?
if [ $code -eq 0 ] && [ -f Build/WebGL/index.html ]; then
  echo "Build/WebGL: $(du -sh Build/WebGL | cut -f1) on disk"
  ls -l Build/WebGL/Build | awk 'NR>1 {printf "  %-40s %8.1f MB\n", $9, $5/1048576}'
fi
exit $code
