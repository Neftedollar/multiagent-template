# Install my-mcps: age-mcp (AGE graph) + o-brien (semantic memory)
# Windows PowerShell installer — run from any project or via iwr:
#
#   irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/tools/install-mcps.ps1 | iex
#   # or with custom path:
#   .\install-mcps.ps1 C:\my-mcps
#
# What it does:
#   1. Installs dotnet tools: age-mcp (NuGet: AgeMcp) + o-brien (NuGet: OBrienMcp)
#   2. Optionally starts local Docker databases (AGE + pgvector)
#      OR prompts for custom connection strings (remote server, existing DB, etc.)
#   3. Writes both servers to Claude Code MCP config
param(
    [Parameter(Position=0)][string]$TargetDir = "$PWD\my-mcps"
)

$ErrorActionPreference = "Stop"

$AgemcpDir   = Join-Path $TargetDir "age-mcp"
$AgemcpRepo  = "https://github.com/Neftedollar/age-mcp.git"

$AgePort          = 5435
$AgeConnDocker    = "Host=localhost;Port=$AgePort;Database=agemcp;Username=agemcp;Password=agemcp"

$ObrienPort          = 5433
$ObrienDbUrlDocker   = "Host=localhost;Port=$ObrienPort;Database=obrien;Username=postgres;Password=postgres"
$ObrienContainer     = "o-brien-db"

function Has([string]$cmd) { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

function RefreshPath {
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("PATH", "User")
}

function WaitForPort([int]$port, [int]$retries = 15) {
    for ($i = 0; $i -lt $retries; $i++) {
        try {
            $tcp = [System.Net.Sockets.TcpClient]::new()
            $tcp.Connect("localhost", $port)
            $tcp.Close()
            return $true
        } catch { Start-Sleep 1 }
    }
    return $false
}

Write-Host "================================"
Write-Host "  my-mcps installer (Windows)"
Write-Host "  Target: $TargetDir"
Write-Host "================================"
Write-Host ""

# ─── .NET SDK ────────────────────────────────────────────────────────────────

if (-not (Has "dotnet")) {
    Write-Host "  ..  dotnet not found, installing..."
    if (Has "winget") {
        winget install --id Microsoft.DotNet.SDK.9 -e --source winget --accept-source-agreements --accept-package-agreements
        RefreshPath
    } else {
        $script = "$env:TEMP\dotnet-install.ps1"
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $script -UseBasicParsing
        & $script -Channel LTS
        $env:PATH += ";$env:LOCALAPPDATA\Microsoft\dotnet"
    }
    if (-not (Has "dotnet")) {
        Write-Error "FAIL: dotnet installation failed — https://dotnet.microsoft.com/download"
        exit 1
    }
    Write-Host "  OK: dotnet installed"
} else {
    $v = & dotnet --version 2>$null
    Write-Host "  OK: dotnet $v"
}

$toolsPath = "$env:USERPROFILE\.dotnet\tools"
if ($env:PATH -notlike "*$toolsPath*") { $env:PATH += ";$toolsPath" }

# ─── Database setup ───────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Database setup:"
Write-Host "  [1] Start local Docker containers (default, recommended for local dev)"
Write-Host "  [2] Enter connection strings manually (remote server, existing DB, etc.)"
Write-Host ""
$dbMode = Read-Host "Choose [1/2, default=1]"
if ([string]::IsNullOrEmpty($dbMode)) { $dbMode = "1" }

if ($dbMode -eq "2") {

    Write-Host ""
    Write-Host "AGE graph (age-mcp):"
    Write-Host "  Format: Host=...;Port=...;Database=...;Username=...;Password=..."
    Write-Host "  Default (local Docker): $AgeConnDocker"
    $in = Read-Host "  AGE connection string [Enter to use default]"
    $AgeConn = if ([string]::IsNullOrEmpty($in)) { $AgeConnDocker } else { $in }

    Write-Host ""
    Write-Host "O'Brien memory (o-brien):"
    Write-Host "  Format: Host=...;Port=...;Database=...;Username=...;Password=..."
    Write-Host "  Default (local Docker): $ObrienDbUrlDocker"
    $in = Read-Host "  O'Brien connection string [Enter to use default]"
    $ObrienDbUrl = if ([string]::IsNullOrEmpty($in)) { $ObrienDbUrlDocker } else { $in }

    Write-Host ""
    Write-Host "  OK: using custom connection strings"

} else {

    $AgeConn     = $AgeConnDocker
    $ObrienDbUrl = $ObrienDbUrlDocker

    # ── Docker ───────────────────────────────────────────────────────────────

    if (-not (Has "docker")) {
        Write-Host "  ..  Docker not found, installing..."
        if (Has "winget") {
            winget install --id Docker.DockerDesktop -e --source winget --accept-source-agreements --accept-package-agreements
            Write-Host "  >>  Docker Desktop installed. Launch it from the Start Menu, then re-run."
            exit 0
        } else {
            Write-Error "FAIL: install Docker Desktop from https://docker.com/products/docker-desktop"
        }
    }

    & docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  >>  Docker installed but not running. Starting Docker Desktop..."
        Start-Process "Docker Desktop" -ErrorAction SilentlyContinue
        Start-Sleep 3
        Write-Host "  >>  Wait for Docker Desktop to finish starting, then re-run."
        exit 0
    }
    Write-Host "  OK: docker"

    Write-Host ""
    Write-Host "── age-mcp (AGE graph) ──────────────────────────────────"
    Write-Host ""

    # Clone for docker-compose
    if (Test-Path $AgemcpDir) {
        Write-Host "  OK: age-mcp repo at $AgemcpDir"
        Push-Location $AgemcpDir
        & git pull --ff-only 2>$null
        Pop-Location
    } else {
        Write-Host "  ..  Cloning age-mcp (for docker-compose)..."
        New-Item -ItemType Directory -Force -Path (Split-Path $AgemcpDir) | Out-Null
        & git clone $AgemcpRepo $AgemcpDir
        Write-Host "  OK: age-mcp cloned"
    }

    # Start AGE database
    $running = & docker ps --format "{{.Names}}" 2>$null
    if ($running -match "age.*db|agemcp.*db") {
        Write-Host "  OK: AGE database already running"
    } else {
        $composeYml    = Join-Path $AgemcpDir "docker-compose.yml"
        $composeAlt    = Join-Path $AgemcpDir "compose.yml"
        if ((Test-Path $composeYml) -or (Test-Path $composeAlt)) {
            Write-Host "  ..  Starting AGE database..."
            Push-Location $AgemcpDir
            & docker compose up -d
            Pop-Location
            if (WaitForPort $AgePort) { Write-Host "  OK: AGE database running on :$AgePort" }
            else { Write-Warning "AGE database not reachable on :$AgePort yet" }
        } else {
            Write-Warning "no docker-compose.yml in age-mcp repo, skipping DB start"
        }
    }

    Write-Host ""
    Write-Host "── o-brien (semantic memory) ────────────────────────────"
    Write-Host ""

    $running = & docker ps --format "{{.Names}}" 2>$null
    if ($running -match "(?m)^$ObrienContainer$") {
        Write-Host "  OK: o-brien database already running"
    } else {
        $all = & docker ps -a --format "{{.Names}}" 2>$null
        if ($all -match "(?m)^$ObrienContainer$") {
            Write-Host "  ..  Starting existing o-brien container..."
            & docker start $ObrienContainer
        } else {
            Write-Host "  ..  Creating o-brien postgres container..."
            & docker run -d `
                --name $ObrienContainer `
                -e POSTGRES_USER=postgres `
                -e POSTGRES_PASSWORD=postgres `
                -e POSTGRES_DB=obrien `
                -p "${ObrienPort}:5432" `
                pgvector/pgvector:pg17
        }
        Write-Host "  ..  Waiting for PostgreSQL..."
        if (WaitForPort $ObrienPort) { Write-Host "  OK: o-brien database running on :$ObrienPort" }
        else { Write-Warning "o-brien postgres not reachable on :$ObrienPort yet" }
    }
}

# ─── Install dotnet tools ─────────────────────────────────────────────────────

Write-Host ""
Write-Host "── Installing MCP tools ─────────────────────────────────"
Write-Host ""

$errors = 0

if (Has "age-mcp") {
    Write-Host "  OK: age-mcp already installed"
} else {
    Write-Host "  ..  Installing age-mcp (AgeMcp from NuGet)..."
    & dotnet tool install --global AgeMcp 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { & dotnet tool update --global AgeMcp 2>&1 | Out-Null }
    if ($LASTEXITCODE -eq 0) { Write-Host "  OK: age-mcp installed" }
    else { Write-Error "FAIL: could not install AgeMcp from NuGet"; $errors++ }
}

if (Has "obrien-mcp") {
    Write-Host "  OK: obrien-mcp already installed"
} else {
    Write-Host "  ..  Installing o-brien (OBrienMcp from NuGet)..."
    & dotnet tool install --global OBrienMcp 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { & dotnet tool update --global OBrienMcp 2>&1 | Out-Null }
    if ($LASTEXITCODE -eq 0) { Write-Host "  OK: obrien-mcp installed" }
    else { Write-Warning "could not install obrien-mcp globally" }
}

if ($errors -gt 0) {
    Write-Error "ABORT: $errors critical error(s) above."
    exit 1
}

# ─── Configure Claude Code MCP ───────────────────────────────────────────────

Write-Host ""

if (Test-Path ".claude") {
    $mcpFile  = ".claude\mcp.json"
    $mcpScope = "project"
} else {
    $mcpFile  = "$env:USERPROFILE\.claude\mcp.json"
    $mcpScope = "global"
    New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.claude" | Out-Null
}

$ageEntry = [PSCustomObject]@{
    type    = "stdio"
    command = "age-mcp"
    env     = [PSCustomObject]@{ AGE_CONNECTION_STRING = $AgeConn; TENANT_ID = "default" }
}
$obrienEntry = [PSCustomObject]@{
    type    = "stdio"
    command = "obrien-mcp"
    env     = [PSCustomObject]@{ DATABASE_URL = $ObrienDbUrl }
}

if (Test-Path $mcpFile) {
    $cfg = Get-Content $mcpFile -Raw | ConvertFrom-Json
    if ($null -eq $cfg.mcpServers) {
        $cfg | Add-Member -NotePropertyName mcpServers -NotePropertyValue ([PSCustomObject]@{}) -Force
    }
    $changed = $false
    if ($null -eq $cfg.mcpServers.'age-mcp') {
        $cfg.mcpServers | Add-Member -NotePropertyName "age-mcp" -NotePropertyValue $ageEntry -Force
        $changed = $true
    }
    if ($null -eq $cfg.mcpServers.'o-brien') {
        $cfg.mcpServers | Add-Member -NotePropertyName "o-brien" -NotePropertyValue $obrienEntry -Force
        $changed = $true
    }
    $cfg | ConvertTo-Json -Depth 10 | Set-Content $mcpFile -Encoding UTF8NoBOM
    if ($changed) { Write-Host "  OK: MCP config updated" }
    else { Write-Host "  OK: both servers already in MCP config" }
} else {
    [PSCustomObject]@{
        mcpServers = [PSCustomObject]@{
            "age-mcp" = $ageEntry
            "o-brien" = $obrienEntry
        }
    } | ConvertTo-Json -Depth 10 | Set-Content $mcpFile -Encoding UTF8NoBOM
    Write-Host "  OK: MCP config written to $mcpFile"
}

Write-Host "  MCP scope: $mcpScope ($mcpFile)"

# ─── Done ────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "================================"
Write-Host "  my-mcps installed!"
Write-Host "================================"
Write-Host ""
Write-Host "  age-mcp connection: $AgeConn"
Write-Host "  o-brien connection: $ObrienDbUrl"
Write-Host "  MCP config:         $mcpFile ($mcpScope)"
Write-Host ""
Write-Host "  Start a Claude Code session — both MCPs are ready."
Write-Host ""
if ($dbMode -ne "2") {
    $agemcpAbs = if (Test-Path $AgemcpDir) { (Resolve-Path $AgemcpDir).Path } else { $AgemcpDir }
    Write-Host "  To stop databases:"
    Write-Host "    cd $agemcpAbs; docker compose down   # age-mcp"
    Write-Host "    docker stop $ObrienContainer            # o-brien"
    Write-Host ""
}
