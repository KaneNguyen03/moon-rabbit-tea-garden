#!/bin/bash
# Makes a signed gift code (#68): 4 characters that say what it gives and the last day it works, then 4 characters
# of HMAC-SHA256 signature. The game checks the signature with the same key (Core/GiftCodes.cs), so a new code needs
# no change to the game's code table. Codes are printed as XXXX-XXXX; a player may type them with or without the dash.
#
# The key is NOT in the repository. It lives in Assets/Resources/keys.json, which git ignores:
#     {"giftKey":"<a long random string>"}
# Make one with:   tools/make-code.sh --new-key
# The build carries the key in its Resources; a build made without keys.json simply refuses every signed code
# (the public codes in the table, TRATHO-2026 and CAMON-PLAYTEST, still work). Keep a copy of the key somewhere
# safe: codes made with one key do not work in a build made with another. VTT_GIFT_KEY overrides the file.
#
# Usage:   tools/make-code.sh <gift> <last day, YYYY-MM-DD>
#   gift:  golden5   5 golden carrots
#          golden10  10 golden carrots
#          clovers2  2 four-leaf clovers
#          bundle    5 golden carrots and 2 clovers
# Example: tools/make-code.sh golden10 2027-02-28
# Each garden can use a code once. Two codes with the same gift and the same last day are the same code.
set -euo pipefail
cd "$(dirname "$0")/.."
KEYS=Assets/Resources/keys.json
ALPHABET=23456789ABCDEFGHJKLMNPQRSTUVWXYZ # Profile.CodeAlphabet: no 0, O, 1 or I
BASE_DAY=20454                            # 2026-01-01 as a day number (days since 1970-01-01), GiftCodes.BaseDay

usage() { sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//' >&2; exit 2; }

if [ "${1:-}" = "--new-key" ]; then
  if [ -f "$KEYS" ]; then echo "make-code: $KEYS already exists; not replacing it" >&2; exit 1; fi
  mkdir -p "$(dirname "$KEYS")"
  printf '{"giftKey":"%s"}\n' "$(openssl rand -hex 32)" > "$KEYS"
  echo "make-code: wrote a new key to $KEYS (git ignores it; keep a copy somewhere safe)"
  exit 0
fi
[ $# -eq 2 ] || usage

case "$1" in
  golden5) kind=0 ;;
  golden10) kind=1 ;;
  clovers2) kind=2 ;;
  bundle) kind=3 ;;
  *) usage ;;
esac

key="${VTT_GIFT_KEY:-}"
if [ -z "$key" ]; then
  [ -f "$KEYS" ] || { echo "make-code: no $KEYS (make one with --new-key)" >&2; exit 1; }
  key=$(sed -n 's/.*"giftKey"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$KEYS" | head -1)
  [ -n "$key" ] || { echo "make-code: $KEYS has no giftKey" >&2; exit 1; }
fi

# The last day as the game counts days: whole days since 1970-01-01 on the player's calendar.
secs=$(TZ=UTC date -j -f "%Y-%m-%d %H:%M:%S" "$2 00:00:00" +%s 2>/dev/null || TZ=UTC date -d "$2" +%s 2>/dev/null) \
  || { echo "make-code: '$2' is not a date (YYYY-MM-DD)" >&2; exit 2; }
offset=$(( secs / 86400 - BASE_DAY ))
if [ $offset -lt 0 ] || [ $offset -gt 32767 ]; then echo "make-code: the last day must be from 2026-01-01 to 2115" >&2; exit 2; fi

type="${ALPHABET:$kind:1}${ALPHABET:$(( (offset >> 10) & 31 )):1}${ALPHABET:$(( (offset >> 5) & 31 )):1}${ALPHABET:$(( offset & 31 )):1}"
hex=$(printf '%s' "VTT1:$type" | openssl dgst -sha256 -hmac "$key" -binary | od -An -tx1 | tr -d ' \n')
v=$(( 16#${hex:0:5} )) # the first 20 bits of the signature
sig="${ALPHABET:$(( (v >> 15) & 31 )):1}${ALPHABET:$(( (v >> 10) & 31 )):1}${ALPHABET:$(( (v >> 5) & 31 )):1}${ALPHABET:$(( v & 31 )):1}"
echo "$type-$sig"
