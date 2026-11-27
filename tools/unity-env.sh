UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
UNITY_TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_PROCESS='^(/Applications/Unity/Hub/Editor/[^ ]*/Unity\.app/Contents/MacOS/Unity|/init /mnt/[a-z]/[^ ]*/Unity\.exe) '

case "$UNITY" in
  *.exe) export UNITY_EXE="$UNITY"; UNITY=unity_wsl ;;
esac

unity_wsl() {
  local arg args=()
  if ! command -v wslpath >/dev/null 2>&1; then "$UNITY_EXE" "$@"; return; fi
  for arg in "$@"; do
    case "$arg" in
      /*) args+=("$(wslpath -w "$arg")") ;;
      *) args+=("$arg") ;;
    esac
  done
  "$UNITY_EXE" "${args[@]}"
}

windows_home() {
  local profile
  profile=$(cd /mnt/c 2>/dev/null && /mnt/c/Windows/System32/cmd.exe /d /c 'echo %USERPROFILE%' 2>/dev/null | tr -d '\r')
  [ -n "$profile" ] && wslpath -u "$profile"
}

player_dir() {
  local settings="$UNITY_TOOLS/../ProjectSettings/ProjectSettings.asset" company product base
  company=$(sed -n 's/^  companyName: //p' "$settings" 2>/dev/null)
  product=$(sed -n 's/^  productName: //p' "$settings" 2>/dev/null)
  if [ -n "${UNITY_EXE:-}" ]; then base="$(windows_home)/AppData/LocalLow"
  else base="$HOME/Library/Application Support"; fi
  printf '%s\n' "$base/${company:-VuonTraTho}/${product:-Moon Rabbit Tea Garden}"
}

file_time() { stat -c %Y "$1" 2>/dev/null || stat -f %m "$1"; }

unity_editor_open() {
  if [ -n "${UNITY_EXE:-}" ]; then
    /mnt/c/Windows/System32/tasklist.exe /FI "IMAGENAME eq Unity.exe" /NH 2>/dev/null | grep -qi '^Unity\.exe'
  else
    pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null
  fi
}
