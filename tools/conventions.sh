#!/bin/bash
set -uo pipefail
cd "$(dirname "$0")/.."
RULES=tools/conventions.rules
BASELINE=tools/conventions.baseline

usage() {
  echo "usage: tools/conventions.sh [--lower | --since REV] [file ...]" >&2
  echo "  no files: checks every tracked .cs file under Assets/VuonTra and Assets/Tests" >&2
  echo "  --lower: rewrites the baseline down to today's counts; it never raises a count" >&2
  echo "  --since REV: fails when the baseline gained or raised an entry since REV" >&2
  exit 64
}

LOWER=0
SINCE=""
FILES=()
while [ $# -gt 0 ]; do
  case "$1" in
    --lower) LOWER=1 ;;
    --since) [ $# -ge 2 ] || usage; SINCE=$2; shift ;;
    -*) usage ;;
    *) FILES+=("$1") ;;
  esac
  shift
done

if [ -n "$SINCE" ]; then
  raised=$(awk -F'\t' 'FILENAME==ARGV[1] { old[$1 FS $2]=$3; next }
                       !(($1 FS $2) in old) || $3 > old[$1 FS $2] { print "  " $1 "  " $2 ": " $3 }' \
             <(git show "$SINCE:$BASELINE" 2>/dev/null) "$BASELINE")
  if [ -n "$raised" ]; then
    echo "conventions: $BASELINE gained or raised entries since $SINCE; only --lower may change it:" >&2
    printf '%s\n' "$raised" >&2
    exit 1
  fi
  exit 0
fi

SCOPED=1
if [ ${#FILES[@]} -eq 0 ]; then
  SCOPED=0
  while IFS= read -r f; do FILES+=("$f"); done < <(git ls-files 'Assets/VuonTra/*.cs' 'Assets/Tests/*.cs')
fi

current=$(mktemp)
trap 'rm -f "$current"' EXIT

while IFS=$'\t' read -r rule scope pattern; do
  [ -z "$rule" ] && continue
  matched=()
  for f in ${FILES[@]+"${FILES[@]}"}; do
    case "$f" in $scope) [ -f "$f" ] && matched+=("$f") ;; esac
  done
  [ ${#matched[@]} -eq 0 ] && continue
  for f in "${matched[@]}"; do
    n=$(grep -cE -- "$pattern" "$f")
    [ "$n" -gt 0 ] && printf '%s\t%s\t%s\n' "$rule" "$f" "$n"
  done
done < "$RULES" > "$current"

if [ $LOWER -eq 1 ]; then
  [ $SCOPED -eq 1 ] && { echo "conventions: --lower runs on the whole tree, pass no files" >&2; exit 64; }
  awk -F'\t' -v OFS='\t' 'FILENAME==ARGV[1] { now[$1 FS $2]=$3; next }
              ($1 FS $2) in now { n=now[$1 FS $2]; if (n < $3) $3=n; print $1, $2, $3 }' \
    "$current" "$BASELINE" | sort > "$BASELINE.new"
  mv "$BASELINE.new" "$BASELINE"
  echo "conventions: baseline lowered to $(awk -F'\t' '{s+=$3} END {print s+0}' "$BASELINE") known violations"
  exit 0
fi

report=$(awk -F'\t' 'FILENAME==ARGV[1] { base[$1 FS $2]=$3; next }
  { b = (($1 FS $2) in base) ? base[$1 FS $2] : 0
    if ($3 > b) printf "  %s  %s: %d now, %d allowed\n", $1, $2, $3, b }' "$BASELINE" "$current")

if [ -n "$report" ]; then
  echo "conventions: new violations of the project rules (tools/conventions.rules):" >&2
  printf '%s\n' "$report" >&2
  echo "Fix the cause instead of working around it: remove the comment, drive motion from an Animator or DOTween," >&2
  echo "wire references through [SerializeField] or events, and replace the deferred or swallowed call with the real" >&2
  echo "ordering. The baseline only goes down (tools/conventions.sh --lower); raising it needs the owner's review." >&2
  exit 1
fi

if [ $SCOPED -eq 0 ]; then
  slack=$(awk -F'\t' 'FILENAME==ARGV[1] { now[$1 FS $2]=$3; next }
    { n = (($1 FS $2) in now) ? now[$1 FS $2] : 0; if (n < $3) c++ } END { print c+0 }' "$current" "$BASELINE")
  total=$(awk -F'\t' '{s+=$3} END {print s+0}' "$current")
  echo "conventions: ok ($total known violations left$([ "$slack" -gt 0 ] && echo ", $slack entries can be lowered: tools/conventions.sh --lower"))"
fi
exit 0
