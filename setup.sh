#!/bin/bash
# Multi-Agent Workspace Setup — thin bootstrapper
# Installs the dotnet tool on first run, then delegates to it.
#
# Usage: ./setup.sh <project-name> [github-org]

set -euo pipefail

OS="$(uname -s)"
has() { command -v "$1" &>/dev/null; }

if ! has dotnet; then
  echo "  ..  Installing .NET SDK..."
  if [ "$OS" = "Darwin" ] && has brew; then
    brew install dotnet
  else
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
    export PATH="$HOME/.dotnet:$PATH"
  fi
  has dotnet || { echo "FAIL: dotnet install failed — https://dotnet.microsoft.com/download"; exit 1; }
  echo "  OK: dotnet installed"
fi

if ! dotnet tool list -g 2>/dev/null | grep -q '^multiagent-setup\b'; then
  echo "Installing multiagent-setup..."
  dotnet tool install -g multiagent-setup
fi

# Ensure ~/.dotnet/tools is on PATH (may be missing on first install)
export PATH="$PATH:$HOME/.dotnet/tools"

exec multiagent-setup "$@"
