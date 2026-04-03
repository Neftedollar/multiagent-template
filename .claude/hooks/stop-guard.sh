#!/bin/bash
# Stop hook: remind to run tests and tag OpenBrain
# Uses exit 0 with stdout to inject a reminder into context
# Does NOT block — just nudges Claude to verify before finishing

INPUT=$(cat)
STOP_ACTIVE=$(echo "$INPUT" | jq -r '.stop_hook_active // false')

# Prevent infinite loop: if stop hook already triggered once, let it go
if [ "$STOP_ACTIVE" = "true" ]; then
  exit 0
fi

# Check if any code files were modified in the current git status
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-.}"
CODE_CHANGED=false

if [ -d "$PROJECT_DIR/.git" ] || git -C "$PROJECT_DIR" rev-parse --git-dir &>/dev/null 2>&1; then
  CHANGED_FILES=$(git -C "$PROJECT_DIR" diff --name-only HEAD 2>/dev/null || true)
  if echo "$CHANGED_FILES" | grep -qE '\.(ts|tsx|js|jsx|py|go|rs|rb|php|fs|fsx|cs|java|kt|swift|vue|svelte)$'; then
    CODE_CHANGED=true
  fi
fi

if [ "$CODE_CHANGED" = "true" ]; then
  cat <<'REMINDER'
STOP GUARD: Code files were changed in this session. Before finishing, verify:
1. Were tests run? If not, run them now.
2. If this was a pipeline task, was OpenBrain tagged with the appropriate status?
If both are done, you may finish.
REMINDER
fi

exit 0
