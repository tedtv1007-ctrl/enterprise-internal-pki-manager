#!/usr/bin/env bash
set -euo pipefail

EVENT_PATH="$1"

# Read issue number and body from event payload
ISSUE_NUMBER=$(jq -r '.issue.number' "$EVENT_PATH")
ISSUE_BODY=$(jq -r '.issue.body' "$EVENT_PATH")
REPO_FULL=$(jq -r '.repository.full_name' "$EVENT_PATH")
WORKDIR="/tmp/auto_fix_work/$ISSUE_NUMBER"
mkdir -p "$WORKDIR"
cd "$WORKDIR"

echo "Generating patch for issue #$ISSUE_NUMBER in $REPO_FULL"

# Placeholder: if LLM_API_KEY present, call external LLM to generate patch (user must provide implementation)
if [ -n "${LLM_API_KEY:-}" ]; then
  echo "LLM key detected — (placeholder) calling LLM to generate patch..."
  # Example: call to external API should be implemented by owner.
  # The script should write a patch file at patch.diff
  # For safety, we create a dummy patch that appends a note to ISSUE_AUTOFIX.md
  cat > patch.diff <<'PATCH'
*** Begin Patch
*** Add File: ISSUE_AUTOFIX.md
+Auto-generated placeholder fix for issue
+Please replace with real patch generation logic.
*** End Patch
PATCH
else
  echo "No LLM_API_KEY — creating placeholder patch"
  cat > patch.diff <<'PATCH'
*** Begin Patch
*** Add File: ISSUE_AUTOFIX.md
+Auto-generated placeholder fix for issue
+Please replace with real patch generation logic.
*** End Patch
PATCH
fi

echo "Patch written to $WORKDIR/patch.diff"

# Show summary
echo "--- PATCH PREVIEW ---"
sed -n '1,120p' patch.diff || true

# Do not apply here in action; commit_and_push.sh will apply
exit 0
