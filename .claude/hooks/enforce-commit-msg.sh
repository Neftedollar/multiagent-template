#!/bin/bash
# PreToolUse hook: enforce conventional commit messages
# Blocks git commit if message doesn't follow conventional commits format
# Pattern: type(scope)?: description
# Types: feat, fix, chore, docs, style, refactor, perf, test, ci, build, revert

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

# Only check git commit commands
if ! echo "$COMMAND" | grep -qE 'git[[:space:]]+commit'; then
  exit 0
fi

# Extract commit message — handles:
#   git commit -m "message"
#   git commit -m 'message'
#   git commit -m "$(cat <<'EOF'\nmessage\n...\nEOF\n)"
# The command arrives as a single string from JSON, newlines are literal \n

# Try double-quoted -m "..."
COMMIT_MSG=$(echo "$COMMAND" | sed -n 's/.*-m[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

# Try single-quoted -m '...'
if [ -z "$COMMIT_MSG" ]; then
  COMMIT_MSG=$(echo "$COMMAND" | sed -n "s/.*-m[[:space:]]*'\([^']*\)'.*/\1/p" | head -1)
fi

# Try HEREDOC: extract first content line after EOF marker
if [ -z "$COMMIT_MSG" ]; then
  COMMIT_MSG=$(echo "$COMMAND" | sed -n "s/.*EOF[[:space:]]*$//" | sed -n '/^[[:space:]]*[a-z]/p' | head -1)
fi

# If HEREDOC with literal \n: git commit -m "$(cat <<'EOF'\nfeat: ...\n..."
if [ -z "$COMMIT_MSG" ]; then
  # Split on literal \n, find first line that looks like a commit message
  COMMIT_MSG=$(printf '%s' "$COMMAND" | tr '\n' '\0' | sed 's/\\n/\n/g' | grep -m1 -E '^[[:space:]]*(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)' | sed 's/^[[:space:]]*//')
fi

# If we still can't extract, let it through
if [ -z "$COMMIT_MSG" ]; then
  exit 0
fi

# First line, trimmed
FIRST_LINE=$(echo "$COMMIT_MSG" | head -1 | sed 's/^[[:space:]]*//')

# Check conventional commits pattern
# \s doesn't work on macOS grep -E, use [[:space:]]
PATTERN='^(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)(\(.+\))?!?:[[:space:]].+'

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
