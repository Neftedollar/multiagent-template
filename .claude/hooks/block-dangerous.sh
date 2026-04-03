#!/bin/bash
# PreToolUse hook: block dangerous bash commands
# Blocks: rm -rf /, force push to main, drop table, disk wipes

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if [ -z "$COMMAND" ]; then
  exit 0
fi

# Patterns that should never run
# Using [[:space:]] instead of \s for macOS BSD grep compatibility
DANGEROUS_PATTERNS=(
  'rm[[:space:]]+-rf[[:space:]]+/'
  'rm[[:space:]]+-rf[[:space:]]+\.'
  'rm[[:space:]]+-rf[[:space:]]+\*'
  'git[[:space:]]+push[[:space:]]+.*--force.*[[:space:]]+(main|master)'
  'git[[:space:]]+push[[:space:]]+-f[[:space:]]+.*[[:space:]]+(main|master)'
  'git[[:space:]]+reset[[:space:]]+--hard[[:space:]]+origin/(main|master)'
  'git[[:space:]]+clean[[:space:]]+-fd'
  'DROP[[:space:]]+(TABLE|DATABASE)'
  'TRUNCATE[[:space:]]+TABLE'
  'mkfs\.'
  'dd[[:space:]]+if=.*of=/dev/'
  'chmod[[:space:]]+-R[[:space:]]+777[[:space:]]+/'
  'chown[[:space:]]+-R.*[[:space:]]+/'
)

for pattern in "${DANGEROUS_PATTERNS[@]}"; do
  if echo "$COMMAND" | grep -qEi "$pattern"; then
    echo "{
      \"hookSpecificOutput\": {
        \"hookEventName\": \"PreToolUse\",
        \"permissionDecision\": \"deny\",
        \"permissionDecisionReason\": \"Blocked by safety hook: dangerous command detected. If you need this, ask the user to run it manually.\"
      }
    }"
    exit 0
  fi
done

exit 0
