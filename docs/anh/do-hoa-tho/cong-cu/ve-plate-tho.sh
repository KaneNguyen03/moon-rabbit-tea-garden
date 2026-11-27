#!/bin/bash
set -u
DIR="$1"
shift
KEEP='Keep EVERYTHING else exactly identical: same image size 1024x1024, same position, same scale, same colours, same outline, same flat dark green background. Do not move, redraw or restyle anything else.'
draw() {
  local master="$1" kind="$2"
  local name base out
  name=$(basename "$master" .png)
  base="$DIR/plates/$name"
  case "$kind" in
    body)
      out="$base-body.png"
      agy --effort medium --print-timeout 540s --dangerously-skip-permissions -p="Use the image-generator subagent to EDIT the image $master and save the result as $out. Edit: remove the bunny's whole HEAD including both ears, and remove BOTH arms and paws, leaving only the body with its clothing or accessory, its feet and its tail. Where the head was, close the top of the body with a soft rounded shoulder line in the same purple ink outline; where the arms and paws were, continue the body fur and clothing naturally. $KEEP After saving, reply only with the saved file path." > "${TMPDIR:-/tmp}/$name-body.log" 2>&1
      ;;
    arms)
      out="$base-arms.png"
      agy --effort medium --print-timeout 540s --dangerously-skip-permissions -p="Use the image-generator subagent to EDIT the image $master and save the result as $out. Edit: keep ONLY the bunny's arms with their paws (for a three-quarter view keep only the arm nearest to the viewer), each as a complete closed shape with its full purple ink outline all around, including the side that touched the body and a rounded top at the shoulder. Erase everything else (head, ears, body, clothing, feet, tail, hat) to the flat dark green background. The arms must stay at exactly the same position, size and angle as in the original. Keep image size 1024x1024. After saving, reply only with the saved file path." > "${TMPDIR:-/tmp}/$name-arms.log" 2>&1
      ;;
    head)
      out="$base-head.png"
      agy --effort medium --print-timeout 540s --dangerously-skip-permissions -p="Use the image-generator subagent to EDIT the image $master and save the result as $out. Edit: keep ONLY the bunny's HEAD with both ears and the face, as one complete closed shape with its full purple ink outline all around, including a smooth rounded chin line at the bottom where the head met the body. Erase everything else (body, neck scarf or neckerchief, apron, arms, paws, feet, tail, hat on the back) to the flat dark green background. The head must stay at exactly the same position, size and angle as in the original. Keep image size 1024x1024. After saving, reply only with the saved file path." > "${TMPDIR:-/tmp}/$name-head.log" 2>&1
      ;;
  esac
  [ -f "$out" ] && echo "ok $out" || echo "FAIL $out $(tail -c 300 "${TMPDIR:-/tmp}/$name-$kind.log")"
}
mkdir -p "$DIR/plates"
for job in "$@"; do
  draw "${job%:*}" "${job##*:}" &
done
wait
