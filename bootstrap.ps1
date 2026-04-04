# Multi-Agent Workspace — Bootstrap for a clean Windows machine
# Installs all dependencies and creates the workspace in one command.
#
# Usage:
#   irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.ps1 | iex
#   # or locally:
#   .\bootstrap.ps1 MyProject [github-org]
param(
    [Parameter(Position=0, Mandatory=$true)][string]$ProjectName,
    [Parameter(Position=1)][string]$GithubOrg = ""
)

$ErrorActionPreference = "Stop"

function Has([string]$cmd) { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

function RefreshPath {
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("PATH", "User")
}

function Install-WinGet([string]$Id, [string]$Name) {
    Write-Host "  ..  Installing $Name..."
    winget install --id $Id -e --source winget --accept-source-agreements --accept-package-agreements
    RefreshPath
}

Write-Host "============================================"
Write-Host "  Multi-Agent Workspace Bootstrap"
Write-Host "  Project: $ProjectName"
Write-Host "  OS: Windows"
Write-Host "============================================"
Write-Host ""

$hasWinget = Has "winget"
if (-not $hasWinget) {
    Write-Warning "winget not found. Install App Installer from the Microsoft Store, then re-run."
}

# ─── git ─────────────────────────────────────────────────────────────────────

if (-not (Has "git")) {
    if ($hasWinget) { Install-WinGet "Git.Git" "git" }
    else { Write-Error "FAIL: git not found — install from https://git-scm.com" }
}
Write-Host "  OK: git"

# ─── jq ──────────────────────────────────────────────────────────────────────

if (-not (Has "jq")) {
    if ($hasWinget) { Install-WinGet "jqlang.jq" "jq" }
    else { Write-Warning "jq not found — install from https://jqlang.github.io/jq/" }
}
Write-Host "  OK: jq"

# ─── gh CLI ──────────────────────────────────────────────────────────────────

if (-not (Has "gh")) {
    if ($hasWinget) { Install-WinGet "GitHub.cli" "gh CLI" }
    else { Write-Warning "gh not found — install from https://cli.github.com" }
}
Write-Host "  OK: gh"

# ─── .NET SDK ────────────────────────────────────────────────────────────────

if (-not (Has "dotnet")) {
    Write-Host "  ..  Installing .NET SDK..."
    if ($hasWinget) {
        Install-WinGet "Microsoft.DotNet.SDK.9" ".NET SDK"
    } else {
        $script = "$env:TEMP\dotnet-install.ps1"
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $script -UseBasicParsing
        & $script -Channel LTS
        $env:PATH += ";$env:LOCALAPPDATA\Microsoft\dotnet"
    }
}
$dotnetVer = & dotnet --version 2>$null
Write-Host "  OK: dotnet $dotnetVer"

# ─── Claude Code ─────────────────────────────────────────────────────────────

if (-not (Has "claude")) {
    Write-Host "  ..  Installing Claude Code..."
    if (-not (Has "npm")) {
        if ($hasWinget) { Install-WinGet "OpenJS.NodeJS.LTS" "Node.js" }
        else { Write-Warning "Node.js not found — install from https://nodejs.org" }
    }
    if (Has "npm") { npm install -g @anthropic-ai/claude-code }
    else { Write-Warning "Install Claude Code manually: https://docs.anthropic.com/en/docs/claude-code" }
}
if (Has "claude") { Write-Host "  OK: claude" }

# ─── Create workspace ────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Creating workspace..."
Write-Host ""

$toolsPath = "$env:USERPROFILE\.dotnet\tools"
if ($env:PATH -notlike "*$toolsPath*") { $env:PATH += ";$toolsPath" }

$installed = & dotnet tool list -g 2>$null | Select-String "^multiagent-setup"
if (-not $installed) {
    dotnet tool install -g multiagent-setup
}

$argList = @($ProjectName)
if ($GithubOrg) { $argList += $GithubOrg }

& multiagent-setup @argList
exit $LASTEXITCODE
