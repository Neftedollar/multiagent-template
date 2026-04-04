#!/bin/bash
# Multi-Agent Workspace Setup — thin bootstrapper
# Installs the dotnet tool on first run, then delegates to it.
#
# Usage: ./setup.sh <project-name> [github-org]

set -euo pipefail

if ! command -v dotnet &>/dev/null; then
  echo "FAIL: .NET SDK not found."
  echo "  Install from: https://dotnet.microsoft.com/download"
  exit 1
fi

if ! dotnet tool list -g 2>/dev/null | grep -q '^multiagent-setup\b'; then
  echo "Installing multiagent-setup..."
  dotnet tool install -g multiagent-setup
fi

# Ensure ~/.dotnet/tools is on PATH (may be missing on first install)
export PATH="$PATH:$HOME/.dotnet/tools"

exec multiagent-setup "$@"
