#!/bin/bash
set -uo pipefail
input=$(cat)
event=$(jq -r '.hook_event_name // empty' <<<"$input")
file=$(jq -r '.tool_input.file_path // .tool_input.notebook_path // empty' <<<"$input")
[ -z "$file" ] && exit 0
dir=$(dirname "$file")
[ -d "$dir" ] || exit 0
root=$(git -C "$dir" rev-parse --show-toplevel 2>/dev/null) || exit 0
rel=${file#"$root"/}

if [ "$event" = "PreToolUse" ]; then
  if [ "$rel" = "tools/conventions.baseline" ]; then
    echo "tools/conventions.baseline only goes down, through tools/conventions.sh --lower. Fix the violation in the code instead; raising a count needs the owner's review." >&2
    exit 2
  fi
  exit 0
fi

case "$rel" in Assets/*.cs) ;; *) exit 0 ;; esac
[ -x "$root/tools/conventions.sh" ] || exit 0
out=$("$root/tools/conventions.sh" "$rel" 2>&1) && exit 0
printf '%s\n' "$out" >&2
exit 2
