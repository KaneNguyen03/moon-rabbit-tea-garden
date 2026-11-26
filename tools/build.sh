#!/usr/bin/env bash
# Builds a player from the command line: tools/build.sh mac | ios | android | webgl
# iOS and Android need their Unity module (Unity Hub → Installs → Add modules) and Xcode or the Android SDK;
# without the module the script stops with exit code 2 and says what to install.
set -u
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
cd "$(dirname "$0")/.."
case "${1:-mac}" in
  mac) METHOD=BuildMac ;;
  ios) METHOD=BuildIOS ;;
  android) METHOD=BuildAndroid ;;
  webgl) METHOD=BuildWebGL ;;
  *) echo "usage: tools/build.sh mac|ios|android|webgl" >&2; exit 64 ;;
esac
tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/build.sh "$@"
mkdir -p Logs
"$UNITY" -batchmode -projectPath "$PWD" -logFile "$PWD/Logs/build-$1.log" -executeMethod VuonTra.EditorTools.BuildScript.$METHOD -quit
code=$?
grep -E "\[VuonTra\] (Build|Thiếu)" "Logs/build-$1.log" | tail -3
exit $code
