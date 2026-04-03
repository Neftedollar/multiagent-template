#!/bin/bash
# Multi-Agent Workspace Setup
# Usage: ./setup.sh <project-name> [github-org]

set -euo pipefail

PROJECT_NAME="${1:?Usage: ./setup.sh <project-name> [github-org]}"
GITHUB_ORG="${2:-$PROJECT_NAME}"
GRAPH_NAME="${PROJECT_NAME,,}-ops"  # lowercase
TARGET_DIR="$(dirname "$0")/../${PROJECT_NAME}"
TEMPLATE_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ -d "$TARGET_DIR" ]; then
  echo "Error: $TARGET_DIR already exists"
  exit 1
fi

# --- Helpers ---

OS="$(uname -s)"
ARCH="$(uname -m)"

has() { command -v "$1" &>/dev/null; }

install_or_fail() {
  local name="$1" brew_pkg="${2:-$1}" apt_pkg="${3:-$2}"
  if has "$name"; then
    echo "  OK: $name"
    return 0
  fi
  echo "  ..  $name not found, installing..."
  if [ "$OS" = "Darwin" ]; then
    if ! has brew; then
      echo "FAIL: $name not found and brew not available to install it"
      return 1
    fi
    brew install "$brew_pkg"
  elif [ -f /etc/debian_version ]; then
    sudo apt-get update -qq && sudo apt-get install -y -qq "$apt_pkg"
  elif [ -f /etc/redhat-release ]; then
    sudo dnf install -y -q "$apt_pkg" 2>/dev/null || sudo yum install -y -q "$apt_pkg"
  else
    echo "FAIL: $name not found, unknown package manager"
    return 1
  fi
  if has "$name"; then
    echo "  OK: $name installed"
  else
    echo "FAIL: $name installation failed"
    return 1
  fi
}

# --- Pre-flight checks + auto-install ---
echo "Pre-flight checks..."
echo ""
ERRORS=0

# git
install_or_fail git git git || ERRORS=$((ERRORS+1))

# gh CLI
if ! has gh; then
  echo "  ..  gh CLI not found, installing..."
  if [ "$OS" = "Darwin" ] && has brew; then
    brew install gh
  elif [ -f /etc/debian_version ]; then
    (type -p wget >/dev/null || sudo apt-get install -y wget) \
      && sudo mkdir -p -m 755 /etc/apt/keyrings \
      && wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg >/dev/null \
      && sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
      && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list >/dev/null \
      && sudo apt-get update -qq && sudo apt-get install -y -qq gh
  elif [ -f /etc/redhat-release ]; then
    sudo dnf install -y -q gh 2>/dev/null || sudo yum install -y -q gh
  fi
  if has gh; then
    echo "  OK: gh installed"
  else
    echo "WARN: gh installation failed, install manually: https://cli.github.com"
  fi
else
  echo "  OK: gh CLI"
fi

if has gh; then
  if ! gh auth status &>/dev/null 2>&1; then
    echo "WARN: gh not authenticated. Run: gh auth login"
  else
    echo "  OK: gh authenticated"
  fi
fi

# Claude Code
if ! has claude; then
  echo "  ..  claude CLI not found, installing..."
  if has npm; then
    npm install -g @anthropic-ai/claude-code
  elif has brew; then
    brew install claude
  fi
  if has claude; then
    echo "  OK: claude installed"
  else
    echo "WARN: claude CLI not installed. See: https://docs.anthropic.com/en/docs/claude-code"
  fi
else
  echo "  OK: claude CLI"
fi

# Agency agents (role library)
AGENCY_DIR="$(dirname "$TARGET_DIR")/agency-agents"
if [ ! -d "$AGENCY_DIR" ]; then
  echo "  ..  agency-agents not found, cloning..."
  git clone https://github.com/msitarzewski/agency-agents.git "$AGENCY_DIR" 2>/dev/null \
    && echo "  OK: agency-agents cloned to $AGENCY_DIR" \
    || echo "WARN: could not clone agency-agents (roles won't be available)"
else
  echo "  OK: agency-agents at $AGENCY_DIR"
  # Pull latest
  (cd "$AGENCY_DIR" && git pull --ff-only 2>/dev/null) \
    && echo "  OK: agency-agents updated" \
    || echo "WARN: could not update agency-agents"
fi

# --- Summary ---
echo ""
if [ $ERRORS -gt 0 ]; then
  echo "ABORT: $ERRORS critical check(s) failed. Fix above issues and re-run."
  exit 1
fi

echo "All checks passed."
echo ""
echo "Creating workspace: $TARGET_DIR"
echo "  Project:    $PROJECT_NAME"
echo "  GitHub org: $GITHUB_ORG"
echo "  Graph:      $GRAPH_NAME"
echo ""

# --- Create workspace ---

# Directory structure
mkdir -p "$TARGET_DIR"/{code,docs/{workflows,archive,obsolete-docs},.claude/commands,tools}

# Copy and interpolate template files
for f in CLAUDE.md docs/process.md docs/role-capabilities.md \
         docs/workflows/REGISTRY.md \
         docs/workflows/WORKFLOW-feature-pipeline.md \
         docs/workflows/WORKFLOW-infra-pipeline.md \
         docs/workflows/WORKFLOW-content-pipeline.md \
         docs/workflows/WORKFLOW-spike-pipeline.md \
         .claude/commands/orchestrator.md .claude/settings.json; do
  if [ -f "$TEMPLATE_DIR/$f" ]; then
    mkdir -p "$TARGET_DIR/$(dirname "$f")"
    sed \
      -e "s|{{PROJECT_NAME}}|$PROJECT_NAME|g" \
      -e "s|{{PROJECT_DESCRIPTION}}|$PROJECT_NAME project workspace|g" \
      -e "s|{{FOUNDER}}|$(whoami)|g" \
      -e "s|{{PHASE}}|early development|g" \
      -e "s|{{GITHUB_ORG}}|$GITHUB_ORG|g" \
      -e "s|{{GITHUB_REPO}}|$PROJECT_NAME|g" \
      -e "s|{{GRAPH_NAME}}|$GRAPH_NAME|g" \
      -e "s|{{DATE}}|$(date +%Y-%m-%d)|g" \
      "$TEMPLATE_DIR/$f" > "$TARGET_DIR/$f"
  fi
done

# Copy tools (not interpolated, standalone)
cp "$TEMPLATE_DIR/tools/sync-roles.sh" "$TARGET_DIR/tools/sync-roles.sh"
cp "$TEMPLATE_DIR/tools/install-mcps.sh" "$TARGET_DIR/tools/install-mcps.sh"
chmod +x "$TARGET_DIR/tools/sync-roles.sh" "$TARGET_DIR/tools/install-mcps.sh"

# Sync roles from agency-agents → global ~/.claude/commands/
if [ -d "$AGENCY_DIR" ]; then
  echo "Syncing roles from agency-agents..."
  AGENCY_DIR_ABS="$(cd "$AGENCY_DIR" && pwd)"
  # Temporarily set AGENCY_DIR for sync script to find
  (cd "$TARGET_DIR" && AGENCY_DIR="$AGENCY_DIR_ABS" bash tools/sync-roles.sh)
fi

# Init git
cd "$TARGET_DIR"
git init -q
echo "code/" > .gitignore
echo "*.png" >> .gitignore
echo ".DS_Store" >> .gitignore
echo ".claude/settings.local.json" >> .gitignore
git add -A
git commit -q -m "init: multi-agent workspace from template"

# Copy completions
cp "$TEMPLATE_DIR/tools/completions.zsh" "$TARGET_DIR/tools/completions.zsh"

# Offer to install completions
COMPLETIONS_LINE="source \"$TARGET_DIR/tools/completions.zsh\""
if [ -f "$HOME/.zshrc" ]; then
  if ! grep -qF "completions.zsh" "$HOME/.zshrc" 2>/dev/null; then
    echo ""
    read -p "Add zsh completions to ~/.zshrc? [y/N] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
      echo "" >> "$HOME/.zshrc"
      echo "# Multi-agent workspace completions ($PROJECT_NAME)" >> "$HOME/.zshrc"
      echo "$COMPLETIONS_LINE" >> "$HOME/.zshrc"
      echo "  OK: completions added to ~/.zshrc (restart shell or: source ~/.zshrc)"
    fi
  fi
fi

echo ""
echo "Done! Workspace created at: $TARGET_DIR"
echo ""
echo "Next steps:"
echo "  1. cd $TARGET_DIR"
echo "  2. Clone your code repo into code/$PROJECT_NAME"
echo "  3. (Optional) Install MCP servers: ./tools/install-mcps.sh"
echo "  4. Start working: claude then /orchestrator <your first task>"
echo ""
echo "To update roles later:"
echo "  ./tools/sync-roles.sh --pull"
echo ""
echo "For zsh completions (if skipped):"
echo "  source $TARGET_DIR/tools/completions.zsh"
