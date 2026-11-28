#!/bin/bash
set -u
export LC_ALL=C
STATE="${UNITY_GATE_STATE:-/tmp}"
WAIT="$STATE/unity-wait"
RUNNING="$STATE/unity-running"
INFO="$STATE/unity-gate-info"
LOCK="$STATE/unity-gate.lock"
GUARD="$STATE/save-guard"
OVERRIDE="$STATE/unity-gate-rules.sh"
POLL="${UNITY_GATE_POLL:-5}"
MAX_WAIT="${UNITY_GATE_MAX_WAIT:-0}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [ -n "${UNITY_GATE_BUSY:-}" ]; then BUSY="$UNITY_GATE_BUSY"
elif [ -d /mnt/d/Projects ]; then BUSY=/mnt/d/Projects/.pc-busy
else BUSY="$STATE/unity-gate-busy"; fi
. "$ROOT/tools/unity-env.sh"

usage() {
  cat >&2 <<'EOF'
usage: tools/unity-gate.sh <priority> <command...>   waits for a Unity slot, then runs the command (lower priority number goes first)
       tools/unity-gate.sh status                    shows who holds Unity, who waits and for how long
       tools/unity-gate.sh held                      exits 0 when a parent process already holds a slot
       tools/unity-gate.sh busy [reason]             the PC is busy (the owner plays): no new run starts, runs holding a slot go on
       tools/unity-gate.sh free                      the PC is free again: waiting runs start
tools/verify.sh, tools/test.sh and tools/build.sh call this themselves (priority from UNITY_GATE_PRIORITY, default 50).
UNITY_GATE_MAX_WAIT=<seconds> gives up with exit 75 when no slot came in that time.
The busy flag is a file: UNITY_GATE_BUSY, else /mnt/d/Projects/.pc-busy (D:\Projects\.pc-busy) on the PC, else $STATE/unity-gate-busy.
EOF
  exit 64
}

sees_processes() { ps -p 1 >/dev/null 2>&1; }

alive() {
  local message
  message=$(kill -0 "$1" 2>&1) && return 0
  case "$message" in *"not permitted"*) return 0 ;; esac
  return 1
}

unity_below() {
  local child
  for child in $(pgrep -P "$1" 2>/dev/null); do
    ps -o args= -p "$child" | grep -qE "$UNITY_PROCESS" && return 0
    unity_below "$child" && return 0
  done
  return 1
}

unity_projects() {
  local pid
  for pid in $(pgrep -f "$UNITY_PROCESS" 2>/dev/null); do
    ps -o args= -p "$pid" | grep -oE 'projectPath (/Users/[A-Za-z]|[A-Za-z]:\\)([^ ]| [^-])*'
  done | sort -u
}

unity_count() { unity_projects | wc -l | tr -d ' '; }

n() {
  local reserved=0 marker pid
  for marker in "$RUNNING"/*; do
    [ -e "$marker" ] || continue
    pid="${marker##*/}"
    if ! alive "$pid"; then rm -f "$marker"
    elif ! unity_below "$pid"; then reserved=$((reserved + 1)); fi
  done
  echo $(($(unity_count) + reserved))
}

power() {
  BATTERY=$(pmset -g batt 2>/dev/null | grep -o '[0-9]*%' | head -1 | tr -d '%'); BATTERY=${BATTERY:-100}
  WATTS=$(pmset -g ac 2>/dev/null | awk '/Wattage/{gsub("W","",$3); print $3}'); WATTS=${WATTS:-0}
  ON_AC=$(pmset -g batt 2>/dev/null | grep -c "AC Power")
  FREE=$(memory_pressure 2>/dev/null | tail -1 | grep -o '[0-9]*%' | tr -d '%'); FREE=${FREE:-0}
  SWAP=$(sysctl -n vm.swapusage 2>/dev/null | awk '{gsub("M","",$6); printf "%d", $6}'); SWAP=${SWAP:-0}
}

lim() {
  power
  if [ "$ON_AC" -eq 0 ] && [ "$BATTERY" -lt 30 ]; then echo 0
  elif [ "$WATTS" -lt 60 ] && [ "$BATTERY" -le 50 ]; then echo 1
  elif [ "$SWAP" -gt 5120 ]; then echo 2
  elif [ "$FREE" -gt 25 ] && { [ "$BATTERY" -gt 30 ] || [ "$ON_AC" -gt 0 ]; }; then echo 3
  else echo 2; fi
}

load_override() { [ -f "$OVERRIDE" ] && . "$OVERRIDE"; return 0; }

HAVE_LOCK=0
lock() {
  until mkdir "$LOCK" 2>/dev/null; do
    [ -n "$(find "$LOCK" -maxdepth 0 -mmin +2 2>/dev/null)" ] && rmdir "$LOCK" 2>/dev/null
    sleep 0.2
  done
  HAVE_LOCK=1
}
unlock() { rmdir "$LOCK" 2>/dev/null; HAVE_LOCK=0; }

first() { ls "$WAIT" | sort -n | head -1; }

old_gate_pids() {
  local pid
  for pid in $(pgrep -f "^[^ ]*bash [^ ]*unity-gate\.sh $1 " 2>/dev/null); do
    [ -e "$INFO/$pid" ] || echo "$pid"
  done
}

prune() {
  local entry name file
  for entry in "$WAIT"/*; do
    [ -e "$entry" ] || continue
    name="${entry##*/}"
    case "$name" in
      *.*.*) alive "${name##*.}" || rm -f "$entry" ;;
      *) [ -n "$(find "$entry" -mmin +1 2>/dev/null)" ] && [ -z "$(old_gate_pids "$name")" ] && rm -f "$entry" ;;
    esac
  done
  for file in "$INFO"/*; do
    [ -e "$file" ] || continue
    alive "${file##*/}" || rm -f "$file"
  done
}

holder() {
  local pid=$$
  while [ "${pid:-0}" -gt 1 ]; do
    [ -e "$RUNNING/$pid" ] && { echo "$pid"; return 0; }
    pid=$(ps -o ppid= -p "$pid" 2>/dev/null | tr -d ' ')
  done
  return 1
}

player_save() { local dir; dir=$(player_dir) || return 1; printf '%s\n' "$dir/save.json"; }

span() {
  local seconds=$1
  if [ "$seconds" -ge 3600 ]; then echo "$((seconds / 3600))h$(((seconds % 3600) / 60))m"
  elif [ "$seconds" -ge 60 ]; then echo "$((seconds / 60))m$((seconds % 60))s"
  else echo "${seconds}s"; fi
}

age() { echo $(($(date +%s) - $(file_time "$1" 2>/dev/null || date +%s))); }

is_busy() { [ -e "$BUSY" ]; }

busy_line() {
  local reason
  reason=$(head -1 "$BUSY" 2>/dev/null | tr -d '\r' | sed 's/[[:space:]]*$//')
  printf 'the PC is busy (%s) for %s: %s' "${reason:-no reason given}" "$(span "$(age "$BUSY")")" "$BUSY"
}

busy() {
  local reason="${*:-busy}"
  printf '%s\n' "$reason" > "$BUSY" || exit 1
  echo "unity-gate: $(busy_line)"
  echo "  no new Unity run starts until tools/unity-gate.sh free; runs already holding a slot go on"
}

free() {
  if is_busy; then rm -f "$BUSY" || exit 1; echo "unity-gate: the PC is free again; waiting runs start ($BUSY removed)"
  else echo "unity-gate: the PC was not marked busy ($BUSY)"; fi
}

field() { sed -n "s/^$2=//p" "$INFO/$1" 2>/dev/null | head -1; }

describe() {
  local pid=$1 priority=${2:-} where what args
  if [ -e "$INFO/$pid" ]; then
    priority=$(field "$pid" priority); where=$(field "$pid" checkout); what=$(field "$pid" command)
  else
    args=$(ps -o args= -p "$pid" 2>/dev/null)
    where=$(lsof -a -d cwd -p "$pid" -Fn 2>/dev/null | sed -n 's/^n//p' | head -1)
    what=$(printf '%s\n' "$args" | sed -E 's/^.*unity-gate\.sh [^ ]+ //')
    [ -n "$priority" ] || priority=$(printf '%s\n' "$args" | sed -nE 's/^.*unity-gate\.sh ([^ ]+) .*/\1/p')
    [ -n "$args" ] || { where="not visible from this process"; what="?"; }
  fi
  printf 'priority %s  pid %s  %s  $ %s' "${priority:-?}" "$pid" "${where:-?}" "${what:-?}"
}

status() {
  local used limit marker entry name pid count=0 waiting=0 line project
  load_override
  used=$(n 2>/dev/null); limit=$(lim 2>/dev/null)
  for entry in "$WAIT"/*; do [ -e "$entry" ] && waiting=$((waiting + 1)); done
  echo "unity-gate: ${used:-?} of ${limit:-?} slots in use, $waiting waiting (state in $STATE)"
  if is_busy; then
    echo "HOLD: $(busy_line)"
    echo "  no new run starts until tools/unity-gate.sh free (or the file is deleted); runs already holding a slot go on"
  else
    echo "hold: none; tools/unity-gate.sh busy [reason] holds every new run while the owner uses the PC"
  fi
  sees_processes || echo "  warning: this process cannot see other processes (the Claude Code sandbox hides them), so the slot count is too low and names are missing; run status outside the sandbox"
  echo "running:"
  for marker in "$RUNNING"/*; do
    [ -e "$marker" ] || continue
    pid="${marker##*/}"; count=$((count + 1))
    echo "  held $(span "$(age "$marker")")  $(describe "$pid")"
  done
  [ $count -gt 0 ] || echo "  nobody"
  echo "waiting, next first:"
  count=0
  for name in $(ls "$WAIT" 2>/dev/null | sort -n); do
    entry="$WAIT/$name"; count=$((count + 1))
    case "$name" in
      *.*.*) line=$(describe "${name##*.}") ;;
      *) pid=$(old_gate_pids "$name" | head -1)
         if [ -n "$pid" ]; then line=$(describe "$pid" "$name"); else line="priority $name  no process found for this entry (from the old /tmp gate; removed by the next waiter when over a minute old)"; fi ;;
    esac
    echo "  waited $(span "$(age "$entry")")  $line"
  done
  [ $count -gt 0 ] || echo "  nobody"
  echo "Unity batch or editor processes, one line per project:"
  count=0
  while read -r project; do
    [ -n "$project" ] || continue
    count=$((count + 1)); echo "  ${project#projectPath }"
  done < <(unity_projects)
  [ $count -gt 0 ] || echo "  none"
  if [ -f "$OVERRIDE" ]; then echo "limit: $limit, from the override file $OVERRIDE"
  else echo "limit: $limit (battery ${BATTERY:-?}%, AC $([ "${ON_AC:-0}" -gt 0 ] && echo yes || echo no) at ${WATTS:-?} W, swap ${SWAP:-?} MB, memory free ${FREE:-?}%)"; fi
}

run() {
  local priority=$1; shift
  case "$priority" in ''|*[!0-9]*) usage ;; esac
  [ $# -ge 1 ] || usage
  if holder >/dev/null; then exec "$@"; fi
  if ! sees_processes || ! mkdir -p "$WAIT" "$RUNNING" "$INFO" "$GUARD" 2>/dev/null || ! [ -w "$WAIT" ]; then
    echo "unity-gate: cannot see other processes or cannot write the queue in $STATE, so the command did not run. The Claude Code sandbox causes both; run the command outside it." >&2
    exit 73
  fi
  local started entry name save stamp code waited limit told=0
  save="${UNITY_GATE_SAVE:-}"
  [ -n "$save" ] || save=$(player_save) || exit 1
  started=$(date +%s)
  name="$priority.$started.$$"
  entry="$WAIT/$name"
  printf 'priority=%s\ncheckout=%s\ncommand=%s\nqueued=%s\n' "$priority" "$PWD" "$*" "$started" > "$INFO/$$"
  trap '[ $HAVE_LOCK -eq 1 ] && unlock; rm -f "$entry" "$RUNNING/$$" "$INFO/$$"' EXIT
  trap 'exit 143' TERM
  trap 'exit 130' INT
  trap 'exit 129' HUP
  : > "$entry"
  echo "unity-gate: queued at priority $priority; tools/unity-gate.sh status shows the queue" >&2
  while true; do
    if is_busy; then
      [ $told -eq 1 ] || echo "unity-gate: waiting, $(busy_line); tools/unity-gate.sh free lets runs go" >&2
      told=1
    else
      [ $told -eq 1 ] && echo "unity-gate: the PC is free again" >&2
      told=0
      load_override
      limit=$(lim)
      lock
      prune
      if [ "$(first)" = "$name" ] && [ "$(n)" -lt "$limit" ]; then
        : > "$RUNNING/$$"; rm -f "$entry"; unlock; break
      fi
      unlock
    fi
    waited=$(($(date +%s) - started))
    if [ "$MAX_WAIT" -gt 0 ] && [ "$waited" -ge "$MAX_WAIT" ]; then
      echo "unity-gate: no slot after $(span "$waited") (UNITY_GATE_MAX_WAIT=$MAX_WAIT); the command did not run" >&2
      exit 75
    fi
    sleep "$POLL"
  done
  echo "unity-gate: slot after $(span $(($(date +%s) - started)))" >&2
  stamp="$(date +%Y%m%d-%H%M%S)-p$priority-$$"
  [ -f "$save" ] && cp -p "$save" "$GUARD/$stamp.json"
  "$@"
  code=$?
  if [ -f "$GUARD/$stamp.json" ] && ! cmp -s "$save" "$GUARD/$stamp.json"; then
    [ -f "$save" ] && cp -p "$save" "$GUARD/$stamp.after.json"
    echo "$(date) PRIO $priority: player save changed during the run (kept both copies, save NOT touched)" >> "$GUARD/changed.log"
    echo "unity-gate: the player save changed during this run; the copy from before is $GUARD/$stamp.json" >&2
  fi
  exit $code
}

case "${1:-}" in
  status) status ;;
  held) holder >/dev/null ;;
  busy) shift; busy "$@" ;;
  free) free ;;
  ''|-h|--help) usage ;;
  *) run "$@" ;;
esac
