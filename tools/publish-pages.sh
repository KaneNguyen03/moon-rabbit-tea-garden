#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
SITE=Build/WebGL
REMOTE="${1:-$(git remote get-url origin)}"
BRANCH="${PAGES_BRANCH:-gh-pages}"
ACCOUNT="${PAGES_ACCOUNT:-KaneNguyen03}"

if [ ! -f "$SITE/index.html" ]; then
  echo "publish-pages: $SITE/index.html is missing; run tools/build-webgl.sh first" >&2
  exit 2
fi
big=$(find "$SITE" -type f -size +95M)
if [ -n "$big" ]; then
  echo "publish-pages: GitHub rejects files over 100 MB:" >&2
  printf '  %s\n' "$big" >&2
  exit 3
fi

STAGE=$(mktemp -d "${TMPDIR:-/tmp}/pages.XXXXXX")
trap 'rm -rf "$STAGE"' EXIT
cp -R "$SITE"/. "$STAGE"/
touch "$STAGE/.nojekyll"

SOURCE=$(git rev-parse --short HEAD)
NAME=$(git config user.name || echo "$ACCOUNT")
EMAIL=$(git config user.email || echo "$ACCOUNT@users.noreply.github.com")
git -C "$STAGE" init -q -b "$BRANCH"
git -C "$STAGE" add -A
git -C "$STAGE" -c user.name="$NAME" -c user.email="$EMAIL" commit -q -m "Web build from $SOURCE"

case "$REMOTE" in
  https://*)
    PAGES_TOKEN=$(env -u GITHUB_TOKEN -u GH_TOKEN gh auth token --user "$ACCOUNT")
    export PAGES_TOKEN
    git -C "$STAGE" -c credential.helper= -c "credential.helper=!f() { echo username=$ACCOUNT; echo password=\$PAGES_TOKEN; }; f" \
      push --force "$REMOTE" "$BRANCH:$BRANCH"
    unset PAGES_TOKEN
    ;;
  *)
    git -C "$STAGE" push --force "$REMOTE" "$BRANCH:$BRANCH"
    ;;
esac

echo "publish-pages: $BRANCH on $REMOTE now holds $SITE from $SOURCE ($(du -sh "$SITE" | cut -f1))"
case "$REMOTE" in
  *github.com[:/]*)
    repo=$(basename "${REMOTE%.git}")
    owner=$(basename "$(dirname "${REMOTE%.git}")" | sed 's/.*://')
    echo "publish-pages: https://$(tr '[:upper:]' '[:lower:]' <<<"$owner").github.io/$repo/"
    ;;
esac
