#!/bin/bash
# Multi-Agent Workspace — Bootstrap for a clean machine
# Installs all dependencies and creates the workspace in one command.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject
#   # or locally:
#   ./bootstrap.sh MyProject [github-org]

set -euo pipefail

PROJECT_NAME="${1:?Usage: ./bootstrap.sh <project-name> [github-org]}"
GITHUB_ORG="${2:-}"

OS="$(uname -s)"
has() { command -v "$1" &>/dev/null; }

echo "============================================"
echo "  Multi-Agent Workspace Bootstrap"
echo "  Project: $PROJECT_NAME"
echo "  OS: $OS $(uname -m)"
echo "============================================"
echo ""

# ─── git ─────────────────────────────────────────────────────

if ! has git; then
  echo "FAIL: git not found. Install it first:"
  echo "  macOS:  xcode-select --install"
  echo "  Ubuntu: sudo apt install git"
  echo "  Fedora: sudo dnf install git"
  exit 1
fi
echo "  OK: git"

# ─── Homebrew (macOS) ─────────────────────────────────────────

if [ "$OS" = "Darwin" ] && ! has brew; then
  echo "  ..  Installing Homebrew..."
  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
  eval "$(/opt/homebrew/bin/brew shellenv 2>/dev/null || /usr/local/bin/brew shellenv 2>/dev/null)"
fi

# ─── jq ──────────────────────────────────────────────────────

if ! has jq; then
  echo "  ..  Installing jq..."
  if [ "$OS" = "Darwin" ]; then brew install jq
  elif [ -f /etc/debian_version ]; then sudo apt-get install -y -qq jq
  elif [ -f /etc/redhat-release ]; then sudo dnf install -y jq 2>/dev/null || sudo yum install -y jq
  fi
fi
echo "  OK: jq"

# ─── gh CLI ──────────────────────────────────────────────────

if ! has gh; then
  echo "  ..  Installing gh CLI..."
  if [ "$OS" = "Darwin" ]; then
    brew install gh
  elif [ -f /etc/debian_version ]; then
    sudo mkdir -p -m 755 /etc/apt/keyrings
    wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg >/dev/null
    sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
      | sudo tee /etc/apt/sources.list.d/github-cli.list >/dev/null
    sudo apt-get update -qq && sudo apt-get install -y -qq gh
  elif [ -f /etc/redhat-release ]; then
    sudo dnf install -y gh 2>/dev/null || sudo yum install -y gh
  fi
fi
echo "  OK: gh"

# ─── .NET SDK ────────────────────────────────────────────────

if ! has dotnet; then
  echo "  ..  Installing .NET SDK..."
  if [ "$OS" = "Darwin" ]; then
    brew install dotnet
  else
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0
    export PATH="$HOME/.dotnet:$PATH"
  fi
fi
echo "  OK: dotnet $(dotnet --version 2>/dev/null)"

# ─── Claude Code ─────────────────────────────────────────────

if ! has claude; then
  echo "  ..  Installing Claude Code..."
  if has npm; then npm install -g @anthropic-ai/claude-code
  elif [ "$OS" = "Darwin" ] && has brew; then brew install claude
  else echo "  WARN: install Claude Code manually: https://docs.anthropic.com/en/docs/claude-code"
  fi
fi
has claude && echo "  OK: claude" || true

# ─── Create workspace ─────────────────────────────────────────

echo ""
echo "Creating workspace..."
echo ""

export PATH="$PATH:$HOME/.dotnet/tools"

if ! dotnet tool list -g 2>/dev/null | grep -q '^multiagent-setup\b'; then
  dotnet tool install -g multiagent-setup
fi

if [ -n "$GITHUB_ORG" ]; then
  multiagent-setup "$PROJECT_NAME" "$GITHUB_ORG"
else
  multiagent-setup "$PROJECT_NAME"
fi
