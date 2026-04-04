# Multi-Agent Workspace Setup — Windows bootstrapper
# Installs the dotnet tool on first run, then delegates to it.
#
# Usage: .\setup.ps1 <project-name> [github-org]
#   or run remotely:
#   irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/setup.ps1 | iex
param(
    [Parameter(Position=0)][string]$ProjectName = "",
    [Parameter(Position=1)][string]$GithubOrg = ""
)

$ErrorActionPreference = "Stop"

function Has([string]$cmd) { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

function RefreshPath {
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("PATH", "User")
}

# ─── .NET SDK ────────────────────────────────────────────────────────────────

if (-not (Has "dotnet")) {
    Write-Host "  ..  Installing .NET SDK..."
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
        Write-Error "FAIL: .NET SDK install failed — https://dotnet.microsoft.com/download"
        exit 1
    }
    Write-Host "  OK: dotnet installed"
} else {
    $v = & dotnet --version 2>$null
    Write-Host "  OK: dotnet $v"
}

# Ensure ~/.dotnet/tools is on PATH
$toolsPath = "$env:USERPROFILE\.dotnet\tools"
if ($env:PATH -notlike "*$toolsPath*") { $env:PATH += ";$toolsPath" }

# ─── multiagent-setup tool ───────────────────────────────────────────────────

$installed = & dotnet tool list -g 2>$null | Select-String "^multiagent-setup"
if (-not $installed) {
    Write-Host "Installing multiagent-setup..."
    & dotnet tool install -g multiagent-setup 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        dotnet tool update -g multiagent-setup
    }
} else {
    & dotnet tool update -g multiagent-setup 2>&1 | Out-Null
}

# Verify tool is at the standard path expected by .claude/settings.json hooks.
$toolExe = "$env:USERPROFILE\.dotnet\tools\multiagent-setup.exe"
if (-not (Test-Path $toolExe)) {
    Write-Warning "multiagent-setup not found at $toolExe"
    Write-Warning "The generated workspace hooks expect the tool at that path."
    Write-Warning "If you used a custom DOTNET_ROOT, update .claude/settings.json in the new workspace manually."
}

if ([string]::IsNullOrEmpty($ProjectName)) {
    Write-Host "Usage: .\setup.ps1 <project-name> [github-org]"
    exit 1
}

$argList = @($ProjectName)
if ($GithubOrg) { $argList += $GithubOrg }

& multiagent-setup @argList
exit $LASTEXITCODE
