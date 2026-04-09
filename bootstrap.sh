#!/bin/bash
# Multi-Agent Workspace — Bootstrap for a clean machine
# Installs all dependencies and creates the workspace in one command.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject
#   bash -s -- MyProject --provider gemini
#   bash -s -- MyProject my-org --provider all
#   # Init mode (inject into existing repo):
#   bash -s -- . --provider claude
#   bash -s -- /path/to/existing/repo --provider nessy
#   # or locally:
#   ./bootstrap.sh MyProject [github-org] [--provider <name>]
#   ./bootstrap.sh . [--provider <name>]

set -euo pipefail

MULTIAGENT_VERSION="1.31.0"

FIRST_ARG="${1:-}"
if [[ -z "$FIRST_ARG" ]]; then
  echo "Usage: ./bootstrap.sh <project-name> [github-org] [--provider <name>]" >&2
  echo "       ./bootstrap.sh <dir-or-.> [--provider <name>]" >&2
  exit 1
fi
shift

PROJECT_NAME=""
GITHUB_ORG=""
PROVIDER=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --provider) PROVIDER="$2"; shift 2 ;;
    --provider=*) PROVIDER="${1#*=}"; shift ;;
    -*) shift ;;
    *) GITHUB_ORG="$1"; shift ;;
  esac
done

OS="$(uname -s)"
has() { command -v "$1" &>/dev/null; }

# Detect init vs new mode early (for header display)
if [[ "$FIRST_ARG" == "." || "$FIRST_ARG" == /* || "$FIRST_ARG" == ./* || "$FIRST_ARG" == ../* ]] || \
   ( [[ -n "$FIRST_ARG" ]] && [[ -d "$FIRST_ARG" ]] ); then
  _MODE="init"
  _TARGET="$FIRST_ARG"
else
  _MODE="new"
  _TARGET="$FIRST_ARG"
fi

echo "============================================"
echo "  Multi-Agent Workspace Bootstrap"
echo "  Mode:     $_MODE"
echo "  Target:   $_TARGET"
echo "  Provider: ${PROVIDER:-claude (default)}"
echo "  OS:       $OS $(uname -m)"
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
  /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" # nosemgrep: curl-pipe-bash
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
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 # nosemgrep: curl-pipe-bash
    export PATH="$HOME/.dotnet:$PATH"
  fi
fi
echo "  OK: dotnet $(dotnet --version 2>/dev/null)"

# ─── Claude Code ─────────────────────────────────────────────

if ! has claude; then
  echo "  ..  Installing Claude Code..."
  if has npm; then npm install -g @anthropic-ai/claude-code
  elif [ "$OS" = "Darwin" ] && has brew; then brew install claude
  fi
fi
if has claude; then
  echo "  OK: claude $(claude --version 2>/dev/null | head -1 || true)"
else
  echo "  WARN: Claude Code not installed — install manually:"
  echo "        npm install -g @anthropic-ai/claude-code"
  echo "        or: https://docs.anthropic.com/en/docs/claude-code"
fi

# ─── Create workspace ─────────────────────────────────────────

echo ""
echo "Creating workspace..."
echo ""

export PATH="$PATH:$HOME/.dotnet/tools"

if has multiagent-setup; then
  _ms_installed="$(multiagent-setup --version 2>/dev/null | awk '{print $2}')"
  if [[ "$_ms_installed" == "$MULTIAGENT_VERSION" ]]; then
    echo "  OK: multiagent-setup $_ms_installed"
  else
    echo "  ..  Updating multiagent-setup $_ms_installed → $MULTIAGENT_VERSION..."
    if [ "$OS" = "Darwin" ] && has brew; then
      brew upgrade Neftedollar/multiagent-template/multiagent-setup 2>/dev/null \
        || brew reinstall Neftedollar/multiagent-template/multiagent-setup
    else
      dotnet tool update -g multiagent-setup
      # Persist .dotnet/tools to PATH in shell rc (Linux only)
      if [[ "$OS" != "Darwin" ]]; then
        _dotnet_line='export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"'
        _added=false
        for _rc in "$HOME/.zshrc" "$HOME/.bashrc" "$HOME/.profile"; do
          if [[ -f "$_rc" ]] && ! grep -q '\.dotnet' "$_rc" 2>/dev/null; then
            echo "" >> "$_rc"
            echo "# multiagent-setup: .NET tools path" >> "$_rc"
            echo "$_dotnet_line" >> "$_rc"
            echo "  INFO: Added .dotnet to PATH in $_rc"
            _added=true
            break
          elif [[ -f "$_rc" ]] && grep -q '\.dotnet' "$_rc" 2>/dev/null; then
            _added=true
            break
          fi
        done
        if [[ "$_added" == false ]]; then
          echo "$_dotnet_line" >> "$HOME/.profile"
          echo "  INFO: Added .dotnet to PATH in ~/.profile"
        fi
      fi
    fi
    echo "  OK: multiagent-setup $(multiagent-setup --version 2>/dev/null | awk '{print $2}')"
  fi
elif [ "$OS" = "Darwin" ] && has brew; then
  echo "  ..  Installing multiagent-setup via Homebrew (no .NET required)..."
  brew install Neftedollar/multiagent-template/multiagent-setup
  echo "  OK: multiagent-setup $(multiagent-setup --version 2>/dev/null | awk '{print $2}')"
else
  echo "  ..  Installing multiagent-setup via dotnet tool..."
  dotnet tool install -g multiagent-setup
  # Persist .dotnet/tools to PATH in shell rc (Linux only)
  if [[ "$OS" != "Darwin" ]]; then
    _dotnet_line='export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"'
    _added=false
    for _rc in "$HOME/.zshrc" "$HOME/.bashrc" "$HOME/.profile"; do
      if [[ -f "$_rc" ]] && ! grep -q '\.dotnet' "$_rc" 2>/dev/null; then
        echo "" >> "$_rc"
        echo "# multiagent-setup: .NET tools path" >> "$_rc"
        echo "$_dotnet_line" >> "$_rc"
        echo "  INFO: Added .dotnet to PATH in $_rc"
        _added=true
        break
      elif [[ -f "$_rc" ]] && grep -q '\.dotnet' "$_rc" 2>/dev/null; then
        _added=true
        break
      fi
    done
    if [[ "$_added" == false ]]; then
      echo "$_dotnet_line" >> "$HOME/.profile"
      echo "  INFO: Added .dotnet to PATH in ~/.profile"
    fi
  fi
  echo "  OK: multiagent-setup $(multiagent-setup --version 2>/dev/null | awk '{print $2}')"
fi

# Detect init vs new mode:
# init mode  — first arg is ".", an absolute path, a relative path, or an existing directory
# new mode   — first arg is a plain project name (no slashes, not ".", not an existing dir)
if [[ "$FIRST_ARG" == "." || "$FIRST_ARG" == /* || "$FIRST_ARG" == ./* || "$FIRST_ARG" == ../* ]] || \
   ( [[ -n "$FIRST_ARG" ]] && [[ -d "$FIRST_ARG" ]] ); then
  # init mode — inject into existing directory
  TARGET_DIR="$(cd "$FIRST_ARG" && pwd)"
  SETUP_ARGS=("init" "$TARGET_DIR")
  [ -n "$PROVIDER" ] && SETUP_ARGS+=("--provider" "$PROVIDER")
else
  # new mode — create a new workspace
  PROJECT_NAME="$FIRST_ARG"
  SETUP_ARGS=("new" "$PROJECT_NAME")
  [ -n "$GITHUB_ORG" ] && SETUP_ARGS+=("$GITHUB_ORG")
  [ -n "$PROVIDER" ]   && SETUP_ARGS+=("--provider" "$PROVIDER")
fi
multiagent-setup "${SETUP_ARGS[@]}"
