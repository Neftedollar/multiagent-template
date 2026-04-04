#!/bin/bash
# Install my-mcps: age-mcp (AGE graph) + o-brien (semantic memory)
# Standalone installer — run from any project or via curl:
#
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash
#   # or with custom path:
#   curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.sh | bash -s -- ~/my-mcps
#
# What it does:
#   1. Installs dotnet tools: age-mcp (NuGet: AgeMcp) + o-brien (NuGet: OBrienMcp)
#   2. Optionally starts local Docker databases (AGE + pgvector)
#      OR prompts for custom connection strings (remote server, existing DB, etc.)
#   3. Writes both servers to Claude Code MCP config

set -euo pipefail

TARGET_DIR="${1:-$(pwd)/my-mcps}"
AGEMCP_DIR="${TARGET_DIR}/age-mcp"
AGEMCP_REPO="https://github.com/Neftedollar/age-mcp.git"

# Docker defaults (used only if Docker mode is chosen)
AGE_PORT=5435
AGE_CONN_DOCKER="Host=localhost;Port=${AGE_PORT};Database=agemcp;Username=agemcp;Password=agemcp"

OBRIEN_PORT=5433
OBRIEN_DB_URL_DOCKER="Host=localhost;Port=${OBRIEN_PORT};Database=obrien;Username=postgres;Password=postgres"
OBRIEN_CONTAINER="o-brien-db"

OS="$(uname -s)"
export PATH="$PATH:$HOME/.dotnet/tools"
has() { command -v "$1" &>/dev/null; }

echo "================================"
echo "  my-mcps installer"
echo "  Target: ${TARGET_DIR}"
echo "  OS: ${OS} $(uname -m)"
echo "================================"
echo ""

ERRORS=0

# ─── .NET SDK ────────────────────────────────────────────────

if ! has dotnet; then
  echo "  ..  dotnet not found, installing..."
  if [ "$OS" = "Darwin" ] && has brew; then
    brew install dotnet
  else
    curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
    export PATH="$HOME/.dotnet:$PATH"
  fi
  if has dotnet; then
    echo "  OK: dotnet installed"
  else
    echo "FAIL: dotnet installation failed — https://dotnet.microsoft.com/download"
    exit 1
  fi
else
  echo "  OK: dotnet $(dotnet --version 2>/dev/null)"
fi

# ─── Ask: Docker or custom connection strings ─────────────────

echo ""
echo "Database setup:"
echo "  [1] Start local Docker containers (default, recommended for local dev)"
echo "  [2] Enter connection strings manually (remote server, existing DB, etc.)"
echo ""
read -r -p "Choose [1/2, default=1]: " DB_MODE
DB_MODE="${DB_MODE:-1}"

if [ "$DB_MODE" = "2" ]; then

  # ── Manual connection strings ──────────────────────────────

  echo ""
  echo "AGE graph (age-mcp):"
  echo "  Format: Host=...;Port=...;Database=...;Username=...;Password=..."
  echo "  Default (local Docker): ${AGE_CONN_DOCKER}"
  read -r -p "  AGE connection string [Enter to use default]: " AGE_CONN_INPUT
  AGE_CONN="${AGE_CONN_INPUT:-${AGE_CONN_DOCKER}}"

  echo ""
  echo "O'Brien memory (o-brien):"
  echo "  Format: Host=...;Port=...;Database=...;Username=...;Password=..."
  echo "  Default (local Docker): ${OBRIEN_DB_URL_DOCKER}"
  read -r -p "  O'Brien connection string [Enter to use default]: " OBRIEN_INPUT
  OBRIEN_DB_URL="${OBRIEN_INPUT:-${OBRIEN_DB_URL_DOCKER}}"

  echo ""
  echo "  OK: using custom connection strings"

else

  # ── Docker mode ────────────────────────────────────────────

  AGE_CONN="$AGE_CONN_DOCKER"
  OBRIEN_DB_URL="$OBRIEN_DB_URL_DOCKER"

  # Check/install Docker
  if ! has docker; then
    echo "  ..  docker not found, installing..."
    if [ "$OS" = "Darwin" ]; then
      if has brew; then
        brew install --cask docker
        echo "  >>  Docker Desktop installed. Open it from Applications, then re-run."
        exit 0
      else
        echo "FAIL: install Docker Desktop from https://docker.com/products/docker-desktop"
        exit 1
      fi
    elif [ -f /etc/debian_version ]; then
      curl -fsSL https://get.docker.com | sh
      sudo systemctl enable docker && sudo systemctl start docker
      sudo usermod -aG docker "$USER" 2>/dev/null || true
    else
      echo "FAIL: install docker — https://docs.docker.com/engine/install/"
      exit 1
    fi
  elif ! docker info &>/dev/null 2>&1; then
    echo "  >>  Docker installed but not running."
    if [ "$OS" = "Darwin" ]; then
      open -a Docker 2>/dev/null || true
      echo "  >>  Wait for Docker Desktop to start, then re-run."
      exit 0
    else
      sudo systemctl start docker 2>/dev/null \
        && echo "  OK: docker daemon started" \
        || { echo "FAIL: could not start docker"; exit 1; }
    fi
  else
    echo "  OK: docker"
  fi

  echo ""
  echo "── age-mcp (AGE graph) ─────────────────────────────────"
  echo ""

  # Clone for docker-compose
  if [ -d "$AGEMCP_DIR" ]; then
    echo "  OK: age-mcp repo at $AGEMCP_DIR"
    (cd "$AGEMCP_DIR" && git pull --ff-only 2>/dev/null) || echo "  WARN: git pull failed"
  else
    echo "  ..  Cloning age-mcp (for docker-compose)..."
    mkdir -p "$(dirname "$AGEMCP_DIR")"
    git clone "$AGEMCP_REPO" "$AGEMCP_DIR"
    echo "  OK: age-mcp cloned"
  fi

  # Start AGE database
  if docker ps --format '{{.Names}}' 2>/dev/null | grep -q 'age.*db\|agemcp.*db'; then
    echo "  OK: AGE database already running"
  else
    if [ -f "$AGEMCP_DIR/docker-compose.yml" ] || [ -f "$AGEMCP_DIR/compose.yml" ]; then
      echo "  ..  Starting AGE database..."
      (cd "$AGEMCP_DIR" && docker compose up -d)
      for i in $(seq 1 10); do nc -z localhost $AGE_PORT 2>/dev/null && break || sleep 1; done
      nc -z localhost $AGE_PORT 2>/dev/null \
        && echo "  OK: AGE database running on :${AGE_PORT}" \
        || echo "  WARN: PostgreSQL not reachable on :${AGE_PORT} yet"
    else
      echo "  WARN: no docker-compose.yml in age-mcp repo, skipping DB start"
    fi
  fi

  echo ""
  echo "── o-brien (semantic memory) ───────────────────────────"
  echo ""

  # Start o-brien postgres
  if docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^${OBRIEN_CONTAINER}$"; then
    echo "  OK: o-brien database already running"
  else
    if docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${OBRIEN_CONTAINER}$"; then
      echo "  ..  Starting existing o-brien container..."
      docker start "$OBRIEN_CONTAINER"
    else
      echo "  ..  Creating o-brien postgres container..."
      docker run -d \
        --name "$OBRIEN_CONTAINER" \
        -e POSTGRES_USER=postgres \
        -e POSTGRES_PASSWORD=postgres \
        -e POSTGRES_DB=obrien \
        -p "${OBRIEN_PORT}:5432" \
        pgvector/pgvector:pg17
    fi
    echo "  ..  Waiting for PostgreSQL..."
    for i in $(seq 1 15); do nc -z localhost $OBRIEN_PORT 2>/dev/null && break || sleep 1; done
    nc -z localhost $OBRIEN_PORT 2>/dev/null \
      && echo "  OK: o-brien database running on :${OBRIEN_PORT}" \
      || echo "  WARN: o-brien postgres not reachable on :${OBRIEN_PORT} yet"
  fi

fi  # end Docker mode

# ─── Install dotnet tools ─────────────────────────────────────

echo ""
echo "── Installing MCP tools ────────────────────────────────"
echo ""

if has age-mcp; then
  echo "  OK: age-mcp already installed"
else
  echo "  ..  Installing age-mcp (AgeMcp from NuGet)..."
  if dotnet tool install --global AgeMcp; then
    echo "  OK: age-mcp installed"
  elif dotnet tool update --global AgeMcp; then
    echo "  OK: age-mcp updated"
  else
    echo "FAIL: could not install AgeMcp from NuGet"
    ERRORS=$((ERRORS+1))
  fi
fi

if has obrien-mcp; then
  echo "  OK: obrien-mcp already installed"
else
  echo "  ..  Installing o-brien (OBrienMcp from NuGet)..."
  if dotnet tool install --global OBrienMcp; then
    echo "  OK: obrien-mcp installed"
  elif dotnet tool update --global OBrienMcp; then
    echo "  OK: obrien-mcp updated"
  else
    echo "  WARN: could not install obrien-mcp globally"
  fi
fi

if [ $ERRORS -gt 0 ]; then
  echo ""
  echo "ABORT: $ERRORS critical error(s) above."
  exit 1
fi

# ─── Configure Claude Code MCP ───────────────────────────────

echo ""

if [ -d ".claude" ]; then
  MCP_FILE=".claude/mcp.json"
  MCP_SCOPE="project"
else
  MCP_FILE="$HOME/.claude/mcp.json"
  MCP_SCOPE="global"
  mkdir -p "$HOME/.claude"
fi

AGE_ENTRY="{\"type\":\"stdio\",\"command\":\"age-mcp\",\"env\":{\"AGE_CONNECTION_STRING\":\"${AGE_CONN}\",\"TENANT_ID\":\"default\"}}"
OBRIEN_ENTRY="{\"type\":\"stdio\",\"command\":\"obrien-mcp\",\"env\":{\"DATABASE_URL\":\"${OBRIEN_DB_URL}\"}}"

if [ -f "$MCP_FILE" ] && has python3; then
  python3 -c "
import json
with open('$MCP_FILE') as f:
    cfg = json.load(f)
servers = cfg.setdefault('mcpServers', {})
changed = False
if 'age-mcp' not in servers:
    servers['age-mcp'] = json.loads('$AGE_ENTRY')
    changed = True
if 'o-brien' not in servers:
    servers['o-brien'] = json.loads('$OBRIEN_ENTRY')
    changed = True
with open('$MCP_FILE', 'w') as f:
    json.dump(cfg, f, indent=2)
    f.write('\n')
if changed:
    print('  OK: MCP config updated')
else:
    print('  OK: both servers already in MCP config')
"
else
  cat > "$MCP_FILE" <<MCPEOF
{
  "mcpServers": {
    "age-mcp": ${AGE_ENTRY},
    "o-brien": ${OBRIEN_ENTRY}
  }
}
MCPEOF
  echo "  OK: MCP config written to $MCP_FILE"
fi

echo "  MCP scope: ${MCP_SCOPE} ($MCP_FILE)"

# ─── Done ────────────────────────────────────────────────────

echo ""
echo "================================"
echo "  my-mcps installed!"
echo "================================"
echo ""
echo "  age-mcp connection: ${AGE_CONN}"
echo "  o-brien connection: ${OBRIEN_DB_URL}"
echo "  MCP config:         ${MCP_FILE} (${MCP_SCOPE})"
echo ""
echo "  Start a Claude Code session — both MCPs are ready."
echo ""
if [ "$DB_MODE" != "2" ]; then
  AGEMCP_ABS="$(cd "$AGEMCP_DIR" && pwd)"
  echo "  To stop databases:"
  echo "    cd ${AGEMCP_ABS} && docker compose down   # age-mcp"
  echo "    docker stop ${OBRIEN_CONTAINER}            # o-brien"
  echo ""
fi
