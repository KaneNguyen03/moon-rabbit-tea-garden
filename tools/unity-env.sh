UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
UNITY_TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_ENV_VARS=(VUONTRA_SHOTS VUONTRA_SHOT_NAMES_ONLY VUONTRA_MONETIZATION VUONTRA_ANDROID_KEYSTORE/p VUONTRA_ANDROID_KEYSTORE_PASSWORD VUONTRA_ANDROID_KEY_ALIAS VUONTRA_ANDROID_KEY_PASSWORD)
UNITY_PROCESS='^(/Applications/Unity/Hub/Editor/[^ ]*/Unity\.app/Contents/MacOS/Unity|/init /mnt/[a-z]/.*/Unity\.exe) '

case "$UNITY" in
  *.exe) export UNITY_EXE="$UNITY"; UNITY=unity_wsl ;;
esac

unity_wsl() {
  local arg args=() shared
  if ! command -v wslpath >/dev/null 2>&1; then "$UNITY_EXE" "$@"; return; fi
  shared=$(IFS=:; printf '%s' "${UNITY_ENV_VARS[*]}")
  for arg in "$@"; do
    case "$arg" in
      /*) args+=("$(wslpath -w "$arg")") ;;
      *) args+=("$arg") ;;
    esac
  done
  WSLENV="${WSLENV:+$WSLENV:}$shared" "$UNITY_EXE" "${args[@]}"
}

windows_home() {
  local profile
  profile=$(cd /mnt/c 2>/dev/null && /mnt/c/Windows/System32/cmd.exe /d /c 'echo %USERPROFILE%' 2>/dev/null | tr -d '\r')
  if [ -z "$profile" ] || [ "$profile" = "%USERPROFILE%" ]; then
    echo "unity-env: UNITY is $UNITY_EXE, but cmd.exe gave no %USERPROFILE%, so the player save cannot be found" >&2
    return 1
  fi
  wslpath -u "$profile"
}

player_dir() {
  local settings="$UNITY_TOOLS/../ProjectSettings/ProjectSettings.asset" company product base
  company=$(sed -n 's/^  companyName: //p' "$settings" 2>/dev/null)
  product=$(sed -n 's/^  productName: //p' "$settings" 2>/dev/null)
  if [ -n "${UNITY_EXE:-}" ]; then base="$(windows_home)/AppData/LocalLow" || return 1
  else base="$HOME/Library/Application Support"; fi
  printf '%s\n' "$base/${company:-VuonTraTho}/${product:-Moon Rabbit Tea Garden}"
}

file_time() { stat -c %Y "$1" 2>/dev/null || stat -f %m "$1"; }

unity_editor_open() {
  if [ -n "${UNITY_EXE:-}" ]; then
    local project
    project=$(wslpath -w "$(cd "$UNITY_TOOLS/.." && pwd)" | tr '[:upper:]' '[:lower:]')
    (cd /mnt/c && /mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe -NoProfile -NonInteractive -Command \
      "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | ForEach-Object { \$_.CommandLine }" 2>/dev/null) |
      tr -d '\r' | tr '[:upper:]' '[:lower:]' | grep -v -- '-batchmode' | grep -qF -- "$project"
  else
    pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null
  fi
}
