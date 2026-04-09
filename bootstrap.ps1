# Multi-Agent Workspace — Bootstrap for a clean Windows machine
# Installs all dependencies and creates the workspace in one command.
#
# Usage:
#   irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.ps1 -OutFile bootstrap.ps1
#   .\bootstrap.ps1 MyProject                        # create new workspace
#   .\bootstrap.ps1 MyProject --provider gemini      # with specific provider
#   .\bootstrap.ps1 MyProject my-org --provider all  # with GitHub org
#   .\bootstrap.ps1 .                                # inject into current git repo (init mode)
#   .\bootstrap.ps1 C:\path\to\repo                  # inject into existing git repo (init mode)
param(
    [Parameter(Position=0, Mandatory=$true)][string]$ProjectName,
    [Parameter(Position=1)][string]$GithubOrg = "",
    [string]$Provider = ""
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

# Detect init vs new mode:
# init mode — arg is ".", an absolute path, a relative .\ path, or an existing directory
# new mode  — arg is a plain project name (no path separators)
$isPath = ($ProjectName -eq ".") -or
          ($ProjectName -match '^[A-Za-z]:\\') -or
          ($ProjectName -match '^\\\\') -or
          ($ProjectName -match '^\.\\') -or
          ($ProjectName -match '^\.\.\\') -or
          (Test-Path $ProjectName -PathType Container)
$_Mode = if ($isPath) { "init" } else { "new" }
$_Target = if ($isPath) { (Resolve-Path $ProjectName -ErrorAction SilentlyContinue).Path ?? $ProjectName } else { $ProjectName }

Write-Host "============================================"
Write-Host "  Multi-Agent Workspace Bootstrap"
Write-Host "  Mode:     $_Mode"
Write-Host "  Target:   $_Target"
Write-Host "  Provider: $(if ($Provider) { $Provider } else { 'claude (default)' })"
Write-Host "  OS:       Windows"
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
        Install-WinGet "Microsoft.DotNet.SDK.10" ".NET SDK"
    } else {
        $script = "$env:TEMP\dotnet-install.ps1"
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $script -UseBasicParsing
        & $script -Channel 10.0
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
    & dotnet tool install -g multiagent-setup 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & dotnet tool update -g multiagent-setup
    }
} else {
    & dotnet tool update -g multiagent-setup 2>&1 | Out-Null
}

# Build argument list based on mode
if ($_Mode -eq "init") {
    $argList = @("init", $_Target)
    if ($Provider) { $argList += "--provider"; $argList += $Provider }
} else {
    $argList = @("new", $ProjectName)
    if ($GithubOrg) { $argList += $GithubOrg }
    if ($Provider)  { $argList += "--provider"; $argList += $Provider }
}

& multiagent-setup @argList
exit $LASTEXITCODE
