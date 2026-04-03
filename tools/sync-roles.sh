#!/usr/bin/env bash
# Syncs agent roles from agency-agents repo into ~/.claude/commands/ (global)
# so they're available as /slash-commands in any Claude Code session.
#
# Source: https://github.com/msitarzewski/agency-agents
#
# Usage:
#   ./tools/sync-roles.sh          # sync from existing clone
#   ./tools/sync-roles.sh --pull   # git pull before sync
#   ./tools/sync-roles.sh --clone  # clone repo if missing, then sync
#
# Roles are installed GLOBALLY (~/.claude/commands/) so all projects can use them.
# Project-specific ad-hoc roles go into .claude/commands/ (project-level).

set -euo pipefail

AGENCY_REPO="https://github.com/msitarzewski/agency-agents.git"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORKSPACE="$(dirname "$SCRIPT_DIR")"
AGENCY_DIR="${AGENCY_DIR:-${WORKSPACE}/../agency-agents}"  # override via env or default
COMMANDS_DIR="$HOME/.claude/commands"
ACTION="${1:-}"

# --- Clone or pull ---

if [ "$ACTION" = "--clone" ]; then
  if [ ! -d "$AGENCY_DIR" ]; then
    echo "Cloning agency-agents..."
    git clone "$AGENCY_REPO" "$AGENCY_DIR"
  else
    echo "agency-agents already exists at $AGENCY_DIR"
  fi
  ACTION="--pull"
fi

if [ "$ACTION" = "--pull" ]; then
  if [ -d "$AGENCY_DIR/.git" ]; then
    echo "Pulling latest roles..."
    (cd "$AGENCY_DIR" && git pull --ff-only 2>/dev/null) || echo "WARN: git pull failed, using existing"
  fi
fi

if [ ! -d "$AGENCY_DIR" ]; then
  echo "Error: agency-agents not found at $AGENCY_DIR"
  echo "Run: $0 --clone"
  exit 1
fi

# --- Sync ---

MARKER="<!-- auto-generated from agency-agents -->"

# Clean previous auto-generated commands
if [ -d "$COMMANDS_DIR" ]; then
  grep -rl "$MARKER" "$COMMANDS_DIR" 2>/dev/null | xargs rm -f || true
fi

mkdir -p "$COMMANDS_DIR"

count=0
skipped=0

while IFS= read -r role_file; do
  basename_file="$(basename "$role_file")"

  # Skip non-role files
  case "$basename_file" in
    README.md|CONTRIBUTING.md|LICENSE|PULL_REQUEST_TEMPLATE.md) continue ;;
    EXECUTIVE-BRIEF.md|QUICKSTART.md) continue ;;
  esac

  # Skip non-role directories
  rel_path="${role_file#$AGENCY_DIR/}"
  case "$rel_path" in
    strategy/*|examples/*|integrations/*|.github/*) continue ;;
  esac

  # Must have frontmatter with name:
  if ! head -20 "$role_file" | grep -q '^name:'; then
    continue
  fi

  cmd_name="${basename_file%.md}"

  # Don't overwrite project-level commands
  if [ -f "${WORKSPACE}/.claude/commands/${cmd_name}.md" ]; then
    skipped=$((skipped + 1))
    continue
  fi

  # Extract content after frontmatter
  role_content=$(awk 'BEGIN{n=0} /^---$/{n++; next} n>=2{print}' "$role_file")

  cat > "${COMMANDS_DIR}/${cmd_name}.md" << CMDEOF
${MARKER}

Adopt the following expert role for this conversation. Apply this role's full knowledge, methodology, and communication style to the task below.

<role>
${role_content}
</role>

Now, using the expertise above, help with the following:

\$ARGUMENTS
CMDEOF

  count=$((count + 1))
done < <(find "$AGENCY_DIR" -name "*.md" -type f | sort)

echo ""
echo "Synced $count roles to $COMMANDS_DIR"
[ $skipped -gt 0 ] && echo "Skipped $skipped (project-level override exists)"
echo ""
echo "Check for new roles periodically:"
echo "  $0 --pull"
