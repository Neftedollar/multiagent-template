#!/bin/bash
# PreToolUse hook: block dangerous bash commands
# Blocks: rm -rf /, force push to main, drop table, disk wipes

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if [ -z "$COMMAND" ]; then
  exit 0
fi

# Patterns that should never run
DANGEROUS_PATTERNS=(
  'rm\s+-rf\s+/'
  'rm\s+-rf\s+\.'
  'rm\s+-rf\s+\*'
  'git\s+push\s+.*--force.*\s+(main|master)'
  'git\s+push\s+-f\s+.*\s+(main|master)'
  'git\s+reset\s+--hard\s+origin/(main|master)'
  'git\s+clean\s+-fd'
  'DROP\s+(TABLE|DATABASE)'
  'TRUNCATE\s+TABLE'
  'mkfs\.'
  'dd\s+if=.*of=/dev/'
  ':\(\)\s*\{\s*:\|:\s*&\s*\}\s*;'
  'chmod\s+-R\s+777\s+/'
  'chown\s+-R.*\s+/'
)

for pattern in "${DANGEROUS_PATTERNS[@]}"; do
  if echo "$COMMAND" | grep -qEi "$pattern"; then
    echo "{
      \"hookSpecificOutput\": {
        \"hookEventName\": \"PreToolUse\",
        \"permissionDecision\": \"deny\",
        \"permissionDecisionReason\": \"Blocked by safety hook: matches dangerous pattern '$pattern'. If you need this, ask the user to run it manually.\"
      }
    }"
    exit 0
  fi
done

exit 0
