#!/bin/bash
# PostToolUse hook: auto-lint/format after file edits
# Reads linter config from .claude/hooks/lint.json
# Falls back to detecting common linter configs in the project

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

if [ -z "$FILE_PATH" ] || [ ! -f "$FILE_PATH" ]; then
  exit 0
fi

EXT=".${FILE_PATH##*.}"
HOOK_DIR="$(cd "$(dirname "$0")" && pwd)"
LINT_CONFIG="$HOOK_DIR/lint.json"

# Try lint.json config first
if [ -f "$LINT_CONFIG" ]; then
  LINT_CMD=$(jq -r --arg ext "$EXT" '.linters[$ext] // empty' "$LINT_CONFIG")
  if [ -n "$LINT_CMD" ]; then
    # Run without eval to prevent command injection from lint.json
    $LINT_CMD "$FILE_PATH" 2>/dev/null
    exit 0
  fi
fi

# Fallback: detect linter from project
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-.}"

case "$EXT" in
  .ts|.tsx|.js|.jsx|.css|.html|.json|.md)
    if [ -f "$PROJECT_DIR/.prettierrc" ] || [ -f "$PROJECT_DIR/.prettierrc.json" ] || [ -f "$PROJECT_DIR/prettier.config.js" ]; then
      npx prettier --write "$FILE_PATH" 2>/dev/null
    elif [ -f "$PROJECT_DIR/.eslintrc" ] || [ -f "$PROJECT_DIR/.eslintrc.json" ] || [ -f "$PROJECT_DIR/eslint.config.js" ]; then
      npx eslint --fix "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .fs|.fsx|.fsi)
    if command -v fantomas &>/dev/null; then
      fantomas "$FILE_PATH" 2>/dev/null
    elif command -v dotnet &>/dev/null; then
      dotnet fantomas "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .py)
    if command -v ruff &>/dev/null; then
      ruff format "$FILE_PATH" 2>/dev/null
    elif command -v black &>/dev/null; then
      black -q "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .go)
    if command -v gofmt &>/dev/null; then
      gofmt -w "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .rs)
    if command -v rustfmt &>/dev/null; then
      rustfmt "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .rb)
    if command -v rubocop &>/dev/null; then
      rubocop -A "$FILE_PATH" 2>/dev/null
    fi
    ;;
  .php)
    if [ -f "$PROJECT_DIR/vendor/bin/pint" ]; then
      "$PROJECT_DIR/vendor/bin/pint" "$FILE_PATH" 2>/dev/null
    elif command -v php-cs-fixer &>/dev/null; then
      php-cs-fixer fix "$FILE_PATH" 2>/dev/null
    fi
    ;;
esac

exit 0
