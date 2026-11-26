#!/bin/bash
set -u
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity}"
NAMES=tools/shot-names.txt
SOURCE=Assets/VuonTra/Editor/PreviewShot.Select.cs
OUT=Logs/shot
LOG=Logs/shot.log

usage() {
  cat >&2 <<'EOF'
usage: tools/shot.sh <name>...        renders the named preview frames to Logs/shot/<frame>.png and prints the paths
       tools/shot.sh --list [name]... prints "group<TAB>frame" for every frame, or for the ones the names match (no Unity)
       tools/shot.sh --groups         prints each group with its number of frames (no Unity)
       tools/shot.sh --names [group]  asks Unity which frames each group draws and rewrites tools/shot-names.txt (no pictures)
A name is a frame (113-ruong-mo), a pattern in quotes ('113-*'), or a group (trunk).
Unity runs a whole group to reach one frame, so one frame costs its group's time; only the named frames are drawn.
--seed runs the content seeder first; needed only after a change to seeded content, art imports or string tables.
The run queues through tools/unity-gate.sh (UNITY_GATE_PRIORITY, UNITY_GATE_MAX_WAIT) and must start outside the Claude Code sandbox.
EOF
  exit 64
}

groups() { sed -n 's/^ *new Group("\([a-z0-9-]*\)".*/\1/p' "$SOURCE"; }

is_group() { groups | grep -qx -- "$1"; }

matching() {
  local pattern=$1 group frame
  while IFS=$'\t' read -r group frame; do
    case "$frame" in $pattern) printf '%s\t%s\n' "$group" "$frame" ;; esac
  done < "$NAMES"
}

of_group() { awk -F'\t' -v group="$1" '$1 == group' "$2"; }

MODE=render; SEED=0; WANT=()
for arg in "$@"; do
  case "$arg" in
    --list) MODE=list ;;
    --groups) MODE=groups ;;
    --names) MODE=names ;;
    --seed) SEED=1 ;;
    -h|--help) usage ;;
    -*) echo "shot: unknown option $arg" >&2; usage ;;
    *) WANT+=("$arg") ;;
  esac
done

if [ "$MODE" = groups ]; then
  for group in $(groups); do printf '%s\t%s\n' "$group" "$(of_group "$group" "$NAMES" | wc -l | tr -d ' ')"; done
  exit 0
fi

WHOLE=" "; PICKED=""; unknown=0
for name in ${WANT[@]+"${WANT[@]}"}; do
  case "$name" in *[!A-Za-z0-9*?-]*) echo "shot: \"$name\" is not a frame, pattern or group name" >&2; unknown=1; continue ;; esac
  if is_group "$name"; then WHOLE="$WHOLE$name "; continue; fi
  found=$(matching "$name")
  if [ -z "$found" ] || [ "$MODE" = names ]; then
    echo "shot: nothing is named \"$name\". tools/shot.sh --list shows frames, --groups shows groups; a frame added since tools/shot-names.txt was written is reached through its group." >&2
    unknown=1
  else PICKED="$PICKED$found"$'\n'; fi
done
[ $unknown -eq 0 ] || exit 64

if [ "$MODE" = list ]; then
  if [ ${#WANT[@]} -eq 0 ]; then cat "$NAMES"; exit 0; fi
  for group in $(groups); do
    case "$WHOLE" in *" $group "*) of_group "$group" "$NAMES" ;; *) printf '%s' "$PICKED" | of_group "$group" /dev/stdin | awk '!seen[$0]++' ;; esac
  done
  exit 0
fi

SPEC=""
if [ "$MODE" = names ] && [ ${#WANT[@]} -eq 0 ]; then SPEC=$(groups | tr '\n' ' ')
else
  [ ${#WANT[@]} -gt 0 ] || usage
  for group in $(groups); do
    case "$WHOLE" in *" $group "*) SPEC="$SPEC$group "; continue ;; esac
    frames=$(printf '%s' "$PICKED" | awk -F'\t' -v group="$group" '$1 == group && !seen[$2]++ { printf "%s%s", comma, $2; comma = "," }')
    [ -z "$frames" ] || SPEC="$SPEC$group:$frames "
  done
fi

tools/unity-gate.sh held || exec tools/unity-gate.sh "${UNITY_GATE_PRIORITY:-50}" tools/shot.sh "$@"

if [ -f Temp/UnityLockfile ] && pgrep -f "Unity.app/Contents/MacOS/Unity .*-projectPath $PWD( |\$)" >/dev/null; then
  echo "shot: the Unity Editor holds this project (Temp/UnityLockfile). Close it first." >&2
  exit 2
fi

unity() {
  local log=$1 code errs; shift
  "$UNITY" -batchmode -projectPath "$PWD" -logFile "$PWD/$log" "$@" -quit >/dev/null 2>&1
  code=$?
  errs=$(grep -E "error CS[0-9]+|^[A-Za-z0-9_.]*Exception: |^Unhandled Exception" "$log" 2>/dev/null | head -5)
  if [ -n "$errs" ]; then printf '%s\n' "$errs" >&2; echo "shot: FAIL (errors in $log)" >&2; return 1; fi
  if [ $code -ne 0 ]; then echo "shot: FAIL (Unity exit $code, see $log)" >&2; return 1; fi
}

mkdir -p "$OUT"
rm -f "$OUT"/*.png "$OUT/names.txt"
before=$(git status --porcelain 2>/dev/null)
started=$(date +%s)
if [ $SEED -eq 1 ]; then unity Logs/seed.log -executeMethod VuonTra.EditorTools.ContentSeeder.Seed || exit 1; fi
names_only=0; [ "$MODE" = names ] && names_only=1
VUONTRA_SHOTS="$SPEC" VUONTRA_SHOT_NAMES_ONLY=$names_only unity "$LOG" -executeMethod VuonTra.EditorTools.PreviewShot.RenderSelected || exit 1
seconds=$(($(date +%s) - started))
if [ ! -f "$OUT/names.txt" ]; then echo "shot: FAIL (Unity wrote no $OUT/names.txt, see $LOG)" >&2; exit 1; fi

merged=$(mktemp)
for group in $(groups); do
  if grep -q "^$group	" "$OUT/names.txt"; then of_group "$group" "$OUT/names.txt" | awk '!seen[$0]++'; else of_group "$group" "$NAMES"; fi
done > "$merged"
if cmp -s "$merged" "$NAMES"; then rm -f "$merged"
else mv "$merged" "$NAMES"; echo "shot: $NAMES now lists $(wc -l < "$NAMES" | tr -d ' ') frames; commit it with your change" >&2; fi

changed=$(diff <(printf '%s\n' "$before") <(git status --porcelain 2>/dev/null) | sed -n 's/^> //p' | grep -v " $NAMES$")
if [ -n "$changed" ]; then
  echo "shot: Unity changed these files in the checkout; restore the ones that are not part of your change:" >&2
  printf '%s\n' "$changed" >&2
fi

if [ "$MODE" = names ]; then echo "shot: names read in ${seconds}s"; exit 0; fi

missing=0; count=0
for part in $SPEC; do
  group=${part%%:*}
  if [ "$part" = "$group" ]; then frames=$(of_group "$group" "$OUT/names.txt" | cut -f2 | awk '!seen[$0]++'); else frames=$(printf '%s' "${part#*:}" | tr ',' '\n'); fi
  for frame in $frames; do
    if [ -s "$OUT/$frame.png" ]; then echo "$OUT/$frame.png"; count=$((count + 1))
    else echo "shot: no picture for $frame (group $group, see $LOG)" >&2; missing=1; fi
  done
done
if [ $missing -ne 0 ] || [ $count -eq 0 ]; then echo "shot: FAIL ($count frames written)"; exit 1; fi
echo "shot: ok ($count frames, Unity ran ${seconds}s)"
