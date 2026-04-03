#!/bin/bash
# Install my-mcps (agemcp + AGE graph database)
# Standalone installer — run from any project or via curl:
#
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash
#   # or with custom path:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash -s -- ~/my-mcps
#
# What it does:
#   1. Checks/installs deps (docker, uv)
#   2. Clones agemcp into <target>/agemcp
#   3. Starts AGE database (PostgreSQL + Apache AGE)
#   4. Adds agemcp to Claude Code MCP config (project-level if in a project, else global)

set -euo pipefail

TARGET_DIR="${1:-$(pwd)/my-mcps}"
AGEMCP_DIR="${TARGET_DIR}/agemcp"
AGEMCP_REPO="https://github.com/neftedollar/agemcp.git"
AGE_DSN="postgresql+asyncpg://agemcp:agemcp@localhost:5435/agemcp"
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

# ─── uv ──────────────────────────────────────────────────────

if ! has uv; then
  echo "  ..  uv not found, installing..."
  curl -LsSf https://astral.sh/uv/install.sh | sh
  export PATH="$HOME/.local/bin:$HOME/.cargo/bin:$PATH"
  if has uv; then
    echo "  OK: uv installed"
  else
    echo "FAIL: uv installation failed"
    ERRORS=$((ERRORS+1))
  fi
else
  echo "  OK: uv"
fi

# ─── Bail on errors ──────────────────────────────────────────

if [ $ERRORS -gt 0 ]; then
  echo ""
  echo "ABORT: $ERRORS issue(s) above. Fix and re-run."
  exit 1
fi

# ─── Clone agemcp ────────────────────────────────────────────

echo ""

if [ -d "$AGEMCP_DIR" ]; then
  if [ -f "$AGEMCP_DIR/pyproject.toml" ]; then
    echo "  OK: agemcp already at $AGEMCP_DIR"
    echo "  ..  pulling latest..."
    (cd "$AGEMCP_DIR" && git pull --ff-only 2>/dev/null) || echo "  WARN: git pull failed, using existing"
  else
    echo "FAIL: $AGEMCP_DIR exists but doesn't look like agemcp (missing pyproject.toml)"
    exit 1
  fi
else
  echo "  ..  Cloning agemcp..."
  mkdir -p "$(dirname "$AGEMCP_DIR")"
  git clone "$AGEMCP_REPO" "$AGEMCP_DIR"
  echo "  OK: agemcp cloned"
fi

# ─── Start AGE database ─────────────────────────────────────

echo ""

if docker ps --format '{{.Names}}' 2>/dev/null | grep -q 'agemcp.*db'; then
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
    echo "  WARN: no docker-compose.yml found in agemcp, skipping DB start"
  fi
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

# Build the agemcp server entry
AGEMCP_ENTRY=$(cat <<JSONEOF
{
  "command": "uv",
  "args": ["run", "agemcp", "run"],
  "cwd": "${AGEMCP_ABS}",
  "env": {
    "DB__DSN": "${AGE_DSN}"
  }
}
JSONEOF
)

# Merge into existing mcp.json or create new one
if [ -f "$MCP_FILE" ]; then
  # Check if agemcp already configured
  if grep -q '"agemcp"' "$MCP_FILE" 2>/dev/null; then
    echo "  OK: agemcp already in ${MCP_SCOPE} MCP config ($MCP_FILE)"
  else
    # Add agemcp to existing mcpServers
    if has python3; then
      python3 -c "
import json, sys
with open('$MCP_FILE') as f:
    cfg = json.load(f)
cfg.setdefault('mcpServers', {})['agemcp'] = json.loads('''$AGEMCP_ENTRY''')
with open('$MCP_FILE', 'w') as f:
    json.dump(cfg, f, indent=2)
    f.write('\n')
"
      echo "  OK: agemcp added to ${MCP_SCOPE} MCP config ($MCP_FILE)"
    else
      echo "  WARN: python3 not found, writing fresh MCP config"
      cat > "$MCP_FILE" <<MCPEOF
{
  "mcpServers": {
    "agemcp": ${AGEMCP_ENTRY}
  }
}
MCPEOF
      echo "  OK: agemcp written to ${MCP_SCOPE} MCP config ($MCP_FILE)"
    fi
  fi
else
  cat > "$MCP_FILE" <<MCPEOF
{
  "mcpServers": {
    "agemcp": ${AGEMCP_ENTRY}
  }
}
MCPEOF
  echo "  OK: agemcp written to ${MCP_SCOPE} MCP config ($MCP_FILE)"
fi

# ─── Done ────────────────────────────────────────────────────

echo ""
echo "================================"
echo "  my-mcps installed!"
echo "================================"
echo ""
echo "  agemcp:   ${AGEMCP_ABS}"
echo "  AGE DB:   localhost:${AGE_PORT}"
echo "  MCP config: ${MCP_FILE} (${MCP_SCOPE})"
echo ""
echo "  Start a Claude Code session — agemcp is ready."
echo "  To stop the database:  cd ${AGEMCP_ABS} && docker compose down"
echo "  To restart:            cd ${AGEMCP_ABS} && docker compose up -d"
echo ""
