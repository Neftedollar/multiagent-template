#!/bin/bash
# Install my-mcps (age-mcp + AGE graph database)
# Standalone installer — run from any project or via curl:
#
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash
#   # or with custom path:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash -s -- ~/my-mcps
#
# What it does:
#   1. Checks/installs deps (docker, dotnet)
#   2. Clones age-mcp into <target>/age-mcp
#   3. Starts AGE database (PostgreSQL + Apache AGE)
#   4. Installs age-mcp dotnet global tool
#   5. Adds age-mcp to Claude Code MCP config (project-level if in a project, else global)

set -euo pipefail

TARGET_DIR="${1:-$(pwd)/my-mcps}"
AGEMCP_DIR="${TARGET_DIR}/age-mcp"
AGEMCP_REPO="https://github.com/Neftedollar/age-mcp.git"
AGE_CONN="Host=localhost;Port=5435;Database=agemcp;Username=agemcp;Password=agemcp"
AGE_PORT=5435

OS="$(uname -s)"
has() { command -v "$1" &>/dev/null; }

echo "================================"
echo "  my-mcps installer"
echo "  Target: ${TARGET_DIR}"
echo "  OS: ${OS} $(uname -m)"
echo "================================"
echo ""

ERRORS=0

# ─── Docker ──────────────────────────────────────────────────

if ! has docker; then
  echo "  ..  docker not found, installing..."
  if [ "$OS" = "Darwin" ]; then
    if has brew; then
      brew install --cask docker
      echo ""
      echo "  >>  Docker Desktop installed. Open it from Applications to start the daemon."
      echo "  >>  Then re-run this script."
      exit 0
    else
      echo "FAIL: install Docker Desktop from https://docker.com/products/docker-desktop"
      ERRORS=$((ERRORS+1))
    fi
  elif [ -f /etc/debian_version ]; then
    curl -fsSL https://get.docker.com | sh
    sudo systemctl enable docker && sudo systemctl start docker
    sudo usermod -aG docker "$USER" 2>/dev/null || true
    echo "  OK: docker installed (may need logout/login for group permissions)"
  else
    echo "FAIL: install docker — https://docs.docker.com/engine/install/"
    ERRORS=$((ERRORS+1))
  fi
elif ! docker info &>/dev/null 2>&1; then
  echo "  >>  Docker installed but daemon not running."
  if [ "$OS" = "Darwin" ]; then
    echo "  >>  Opening Docker Desktop..."
    open -a Docker 2>/dev/null || true
    echo "  >>  Wait for it to start, then re-run."
    exit 0
  else
    sudo systemctl start docker 2>/dev/null \
      && echo "  OK: docker daemon started" \
      || { echo "FAIL: could not start docker daemon"; ERRORS=$((ERRORS+1)); }
  fi
else
  echo "  OK: docker"
fi

# ─── .NET SDK ────────────────────────────────────────────────

if ! has dotnet; then
  echo "  ..  dotnet not found, installing..."
  if [ "$OS" = "Darwin" ] && has brew; then
    brew install dotnet
  elif [ -f /etc/debian_version ]; then
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
    export PATH="$HOME/.dotnet:$PATH"
  else
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
    export PATH="$HOME/.dotnet:$PATH"
  fi
  if has dotnet; then
    echo "  OK: dotnet installed"
  else
    echo "FAIL: dotnet installation failed — https://dotnet.microsoft.com/download"
    ERRORS=$((ERRORS+1))
  fi
else
  echo "  OK: dotnet $(dotnet --version 2>/dev/null)"
fi

# ─── Bail on errors ──────────────────────────────────────────

if [ $ERRORS -gt 0 ]; then
  echo ""
  echo "ABORT: $ERRORS issue(s) above. Fix and re-run."
  exit 1
fi

# ─── Clone age-mcp ───────────────────────────────────────────

echo ""

if [ -d "$AGEMCP_DIR" ]; then
  if [ -f "$AGEMCP_DIR/age-mcp.fsproj" ]; then
    echo "  OK: age-mcp already at $AGEMCP_DIR"
    echo "  ..  pulling latest..."
    (cd "$AGEMCP_DIR" && git pull --ff-only 2>/dev/null) || echo "  WARN: git pull failed, using existing"
  else
    echo "FAIL: $AGEMCP_DIR exists but doesn't look like age-mcp (missing age-mcp.fsproj)"
    exit 1
  fi
else
  echo "  ..  Cloning age-mcp..."
  mkdir -p "$(dirname "$AGEMCP_DIR")"
  git clone "$AGEMCP_REPO" "$AGEMCP_DIR"
  echo "  OK: age-mcp cloned"
fi

# ─── Start AGE database ─────────────────────────────────────

echo ""

if docker ps --format '{{.Names}}' 2>/dev/null | grep -q 'age.*db\|agemcp.*db'; then
  echo "  OK: AGE database already running"
else
  if [ -f "$AGEMCP_DIR/docker-compose.yml" ] || [ -f "$AGEMCP_DIR/compose.yml" ]; then
    echo "  ..  Starting AGE database..."
    (cd "$AGEMCP_DIR" && docker compose up -d)
    echo "  ..  Waiting for PostgreSQL..."
    for i in $(seq 1 10); do
      if nc -z localhost $AGE_PORT 2>/dev/null; then
        break
      fi
      sleep 1
    done
    if nc -z localhost $AGE_PORT 2>/dev/null; then
      echo "  OK: AGE database running on :${AGE_PORT}"
    else
      echo "  WARN: PostgreSQL not reachable on :${AGE_PORT} yet (may need more time)"
    fi
  else
    echo "  WARN: no docker-compose.yml found in age-mcp, skipping DB start"
  fi
fi

# ─── Install age-mcp dotnet tool ─────────────────────────────

echo ""

if has age-mcp; then
  echo "  OK: age-mcp tool already installed"
else
  echo "  ..  Installing age-mcp dotnet global tool..."
  dotnet tool install --global AgeMcp 2>/dev/null \
    && echo "  OK: age-mcp installed" \
    || dotnet tool update --global AgeMcp 2>/dev/null \
    && echo "  OK: age-mcp updated" \
    || echo "  WARN: could not install age-mcp globally, will use from source"
fi

# ─── Configure Claude Code MCP ───────────────────────────────

echo ""
AGEMCP_ABS="$(cd "$AGEMCP_DIR" && pwd)"

# Determine where to write MCP config
if [ -d ".claude" ]; then
  MCP_FILE=".claude/mcp.json"
  MCP_SCOPE="project"
else
  MCP_FILE="$HOME/.claude/mcp.json"
  MCP_SCOPE="global"
  mkdir -p "$HOME/.claude"
fi

# Build the age-mcp server entry
# Use global tool if available, otherwise run from source
if has age-mcp; then
  AGEMCP_ENTRY=$(cat <<JSONEOF
{
  "type": "stdio",
  "command": "age-mcp",
  "env": {
    "AGE_CONNECTION_STRING": "${AGE_CONN}",
    "TENANT_ID": "default"
  }
}
JSONEOF
  )
else
  AGEMCP_ENTRY=$(cat <<JSONEOF
{
  "type": "stdio",
  "command": "dotnet",
  "args": ["run", "--project", "${AGEMCP_ABS}"],
  "env": {
    "AGE_CONNECTION_STRING": "${AGE_CONN}",
    "TENANT_ID": "default"
  }
}
JSONEOF
  )
fi

# Merge into existing mcp.json or create new one
if [ -f "$MCP_FILE" ]; then
  if grep -q '"age-mcp"' "$MCP_FILE" 2>/dev/null; then
    echo "  OK: age-mcp already in ${MCP_SCOPE} MCP config ($MCP_FILE)"
  else
    if has python3; then
      python3 -c "
import json, sys
with open('$MCP_FILE') as f:
    cfg = json.load(f)
cfg.setdefault('mcpServers', {})['age-mcp'] = json.loads('''$AGEMCP_ENTRY''')
with open('$MCP_FILE', 'w') as f:
    json.dump(cfg, f, indent=2)
    f.write('\n')
"
      echo "  OK: age-mcp added to ${MCP_SCOPE} MCP config ($MCP_FILE)"
    else
      echo "  WARN: python3 not found, writing fresh MCP config"
      cat > "$MCP_FILE" <<MCPEOF
{
  "mcpServers": {
    "age-mcp": ${AGEMCP_ENTRY}
  }
}
MCPEOF
      echo "  OK: age-mcp written to ${MCP_SCOPE} MCP config ($MCP_FILE)"
    fi
  fi
else
  cat > "$MCP_FILE" <<MCPEOF
{
  "mcpServers": {
    "age-mcp": ${AGEMCP_ENTRY}
  }
}
MCPEOF
  echo "  OK: age-mcp written to ${MCP_SCOPE} MCP config ($MCP_FILE)"
fi

# ─── Done ────────────────────────────────────────────────────

echo ""
echo "================================"
echo "  my-mcps installed!"
echo "================================"
echo ""
echo "  age-mcp:  ${AGEMCP_ABS}"
echo "  AGE DB:   localhost:${AGE_PORT}"
echo "  MCP config: ${MCP_FILE} (${MCP_SCOPE})"
echo ""
echo "  Start a Claude Code session — age-mcp is ready."
echo "  To stop the database:  cd ${AGEMCP_ABS} && docker compose down"
echo "  To restart:            cd ${AGEMCP_ABS} && docker compose up -d"
echo ""
