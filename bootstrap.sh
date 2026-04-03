#!/bin/bash
# Multi-Agent Workspace — Bootstrap for a clean machine
# Run this FIRST on a new machine. It installs everything and creates your workspace.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject
#   # or locally:
#   ./bootstrap.sh MyProject [github-org]

set -euo pipefail

PROJECT_NAME="${1:?Usage: ./bootstrap.sh <project-name> [github-org]}"
GITHUB_ORG="${2:-$PROJECT_NAME}"

OS="$(uname -s)"
has() { command -v "$1" &>/dev/null; }

echo "============================================"
echo "  Multi-Agent Workspace Bootstrap"
echo "  Project: $PROJECT_NAME"
echo "  OS: $OS $(uname -m)"
echo "============================================"
echo ""

# ─── PHASE 1: System essentials ───────────────────────────────

echo "Phase 1: System essentials"
echo ""

# Prerequisite: git must be installed by user
if ! has git; then
  echo "FAIL: git not found. Install it first:"
  echo "  macOS:  xcode-select --install"
  echo "  Ubuntu: sudo apt install git"
  echo "  Fedora: sudo dnf install git"
  exit 1
fi
echo "  OK: git $(git --version | cut -d' ' -f3)"

# Homebrew (macOS)
if [ "$OS" = "Darwin" ]; then
  if ! has brew; then
    echo "  ..  Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    eval "$(/opt/homebrew/bin/brew shellenv 2>/dev/null || /usr/local/bin/brew shellenv 2>/dev/null)"
  fi
  echo "  OK: brew"
elif [ -f /etc/debian_version ]; then
  sudo apt-get update -qq
  sudo apt-get install -y -qq curl wget build-essential
fi

# ─── PHASE 2: Runtime dependencies ───────────────────────────

echo ""
echo "Phase 2: Runtime dependencies"
echo ""

# Node.js (for claude CLI)
if ! has node; then
  echo "  ..  Installing Node.js..."
  if [ "$OS" = "Darwin" ]; then
    brew install node
  elif [ -f /etc/debian_version ]; then
    curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
    sudo apt-get install -y -qq nodejs
  elif [ -f /etc/redhat-release ]; then
    curl -fsSL https://rpm.nodesource.com/setup_lts.x | sudo bash -
    sudo dnf install -y nodejs 2>/dev/null || sudo yum install -y nodejs
  fi
fi
echo "  OK: node $(node --version)"

# gh CLI
if ! has gh; then
  echo "  ..  Installing gh CLI..."
  if [ "$OS" = "Darwin" ]; then
    brew install gh
  elif [ -f /etc/debian_version ]; then
    (type -p wget >/dev/null || sudo apt-get install -y wget) \
      && sudo mkdir -p -m 755 /etc/apt/keyrings \
      && wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg >/dev/null \
      && sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
      && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list >/dev/null \
      && sudo apt-get update -qq && sudo apt-get install -y -qq gh
  elif [ -f /etc/redhat-release ]; then
    sudo dnf install -y gh 2>/dev/null || sudo yum install -y gh
  fi
fi
echo "  OK: gh"

# Claude Code
if ! has claude; then
  echo "  ..  Installing Claude Code..."
  npm install -g @anthropic-ai/claude-code
fi
echo "  OK: claude"

# ─── PHASE 3: Auth ────────────────────────────────────────────

echo ""
echo "Phase 3: Authentication"
echo ""

if ! gh auth status &>/dev/null 2>&1; then
  echo "  >>  GitHub CLI needs authentication."
  echo "  >>  Running: gh auth login"
  gh auth login
fi
echo "  OK: gh authenticated"

# ─── PHASE 4: Project infrastructure ─────────────────────────

echo ""
echo "Phase 4: Project infrastructure"
echo ""

WORK_DIR="$(pwd)"

# Agency agents (role library)
AGENCY_DIR="${WORK_DIR}/agency-agents"
if [ ! -d "$AGENCY_DIR" ]; then
  echo "  ..  Cloning agency-agents..."
  git clone https://github.com/msitarzewski/agency-agents.git "$AGENCY_DIR"
fi
echo "  OK: agency-agents"

# ─── PHASE 5: Create workspace ────────────────────────────────

echo ""
echo "Phase 5: Create workspace"
echo ""

# Clone or locate the template
TEMPLATE_DIR="${WORK_DIR}/multiagent-template"
if [ ! -d "$TEMPLATE_DIR" ]; then
  echo "  ..  Template not found locally, looking for setup.sh..."
  # If running from curl pipe, the template IS this repo
  if [ -f "./setup.sh" ]; then
    TEMPLATE_DIR="."
  else
    echo "FAIL: multiagent-template not found. Clone it first or run from template dir."
    exit 1
  fi
fi

# Run setup.sh
echo "  Running setup.sh..."
bash "$TEMPLATE_DIR/setup.sh" "$PROJECT_NAME" "$GITHUB_ORG"

# ─── Done ─────────────────────────────────────────────────────

echo ""
echo "============================================"
echo "  Bootstrap complete!"
echo "============================================"
echo ""
echo "  Workspace: ${WORK_DIR}/${PROJECT_NAME}"
echo "  Roles:     $(ls "$HOME/.claude/commands/"*.md 2>/dev/null | wc -l | tr -d ' ') slash-commands"
echo "  MCP:       run ./tools/install-mcps.sh to add agemcp"
echo ""
echo "  Get started:"
echo "    cd ${PROJECT_NAME}"
echo "    claude"
echo "    /orchestrator <your first task>"
echo ""
