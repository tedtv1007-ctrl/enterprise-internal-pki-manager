#!/usr/bin/env bash
set -euo pipefail

# This script applies patch.diff created by generate_patch.sh, commits, and pushes a branch.
WORKDIR="/tmp/auto_fix_work"
# find the latest issue dir
ISSUE_DIR=$(ls -1d $WORKDIR/* | tail -n1 || true)
if [ -z "$ISSUE_DIR" ]; then
  echo "No issue workdir found"
  exit 1
fi
cd "$ISSUE_DIR"

# Extract repo info from environment if available
REPO_FULL=${GITHUB_REPOSITORY:-}
if [ -z "$REPO_FULL" ]; then
  echo "GITHUB_REPOSITORY not set; cannot determine repo"
  exit 1
fi

# Clone the repo shallowly
CLONE_DIR="$WORKDIR/clone"
rm -rf "$CLONE_DIR"
git clone "https://x-access-token:${GITHUB_TOKEN}@github.com/${REPO_FULL}.git" "$CLONE_DIR"
cd "$CLONE_DIR"

# Determine issue number from path
ISSUE_NUMBER=$(basename "$ISSUE_DIR")
BRANCH="auto/fix-issue-${ISSUE_NUMBER}"

git checkout -b "$BRANCH"

# Apply patch.diff if present (this uses a simple format from generate_patch.sh)
if [ -f "$ISSUE_DIR/patch.diff" ]; then
  echo "Applying patch"
  # Our patch.diff is in a simple custom format; for demo we'll create file from patch
  # Parse the Add File blocks
  awk '/^*** Add File: /{file=$3;next} /^\+/{sub(/^\+/,"",$0); print $0 > file}' "$ISSUE_DIR/patch.diff"
  git add .
  git commit -m "Fixes #${ISSUE_NUMBER}: automated fix"
  echo "Pushing branch $BRANCH"
  git push --set-upstream origin "$BRANCH"
else
  echo "No patch.diff found in $ISSUE_DIR"
  exit 1
fi

echo "Branch pushed: $BRANCH"
exit 0
