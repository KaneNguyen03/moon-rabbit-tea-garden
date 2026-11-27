#!/usr/bin/env bash
# Builds a player from the command line: tools/build.sh mac | ios | android | webgl
# iOS and Android need their Unity module (Unity Hub → Installs → Add modules) and Xcode or the Android SDK;
# without the module the script stops with exit code 2 and says what to install.
set -u
cd "$(dirname "$0")/.."
. tools/unity-env.sh
case "${1:-}" in
  mac) METHOD=BuildMac ;;
  ios) METHOD=BuildIOS ;;
  android) METHOD=BuildAndroid ;;
  android-apk) METHOD=BuildAndroidApk ;;
  webgl) METHOD=BuildWebGL ;;
  *) echo "usage: tools/build.sh mac|ios|android|android-apk|webgl" >&2; exit 64 ;;
esac
tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/build.sh "$@"
mkdir -p Logs
"$UNITY" -batchmode -projectPath "$PWD" -logFile "$PWD/Logs/build-$1.log" -executeMethod VuonTra.EditorTools.BuildScript.$METHOD -quit
code=$?
grep -E "\[VuonTra\] (Build|Thiếu)" "Logs/build-$1.log" | tail -3
exit $code
