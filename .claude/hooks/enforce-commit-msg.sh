#!/bin/bash
# PreToolUse hook: enforce conventional commit messages
# Blocks git commit if message doesn't follow conventional commits format
# Pattern: type(scope)?: description
# Types: feat, fix, chore, docs, style, refactor, perf, test, ci, build, revert

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

# Only check git commit commands
if ! echo "$COMMAND" | grep -qE 'git\s+commit'; then
  exit 0
fi

# Extract commit message from -m flag
# Handle both 'git commit -m "msg"' and 'git commit -m "$(cat <<...)"' (HEREDOC)
COMMIT_MSG=$(echo "$COMMAND" | grep -oP '(?<=-m\s")[^"]+' | head -1)

# Also try single quotes
if [ -z "$COMMIT_MSG" ]; then
  COMMIT_MSG=$(echo "$COMMAND" | grep -oP "(?<=-m\s')[^']+" | head -1)
fi

# Try HEREDOC pattern (cat <<'EOF' ... EOF)
if [ -z "$COMMIT_MSG" ]; then
  COMMIT_MSG=$(echo "$COMMAND" | grep -oP '(?<=EOF\n).*?(?=\n.*EOF)' | head -1)
fi

# If we can't extract the message, let it through (might be interactive or complex)
if [ -z "$COMMIT_MSG" ]; then
  exit 0
fi

# First line of commit message
FIRST_LINE=$(echo "$COMMIT_MSG" | head -1 | sed 's/^[[:space:]]*//')

# Check conventional commits pattern
PATTERN='^(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)(\(.+\))?!?:\s.+'

if ! echo "$FIRST_LINE" | grep -qE "$PATTERN"; then
  echo "{
    \"hookSpecificOutput\": {
      \"hookEventName\": \"PreToolUse\",
      \"permissionDecision\": \"deny\",
      \"permissionDecisionReason\": \"Commit message must follow conventional commits: type(scope)?: description. Types: feat, fix, chore, docs, style, refactor, perf, test, ci, build, revert. Got: '$FIRST_LINE'\"
    }
  }"
  exit 0
fi

exit 0
