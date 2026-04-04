# Syncs agent roles from agency-agents repo into ~/.claude/commands/ (global)
# so they're available as /slash-commands in any Claude Code session.
#
# Source: https://github.com/msitarzewski/agency-agents
#
# Usage:
#   .\tools\sync-roles.ps1          # sync from existing clone
#   .\tools\sync-roles.ps1 --pull   # git pull before sync
#   .\tools\sync-roles.ps1 --clone  # clone repo if missing, then sync
#
# Roles are installed GLOBALLY (~/.claude/commands/) so all projects can use them.
# Project-specific ad-hoc roles go into .claude/commands/ (project-level).
param(
    [Parameter(Position=0)][string]$Action = ""
)

$ErrorActionPreference = "Stop"

$AgencyRepo  = "https://github.com/msitarzewski/agency-agents.git"
$ScriptDir   = Split-Path $PSScriptRoot -Resolve
$Workspace   = Split-Path $ScriptDir -Resolve
$AgencyDir   = if ($env:AGENCY_DIR) { $env:AGENCY_DIR } else { Join-Path (Split-Path $Workspace) "agency-agents" }
$CommandsDir = "$env:USERPROFILE\.claude\commands"

$SkipFiles = @("README.md","CONTRIBUTING.md","LICENSE","PULL_REQUEST_TEMPLATE.md","EXECUTIVE-BRIEF.md","QUICKSTART.md")
$SkipDirs  = @("strategy","examples","integrations",".github")

# ─── Clone or pull ────────────────────────────────────────────────────────────

if ($Action -eq "--clone") {
    if (-not (Test-Path $AgencyDir)) {
        Write-Host "Cloning agency-agents..."
        & git clone $AgencyRepo $AgencyDir
    } else {
        Write-Host "agency-agents already exists at $AgencyDir"
    }
    $Action = "--pull"
}

if ($Action -eq "--pull") {
    if (Test-Path (Join-Path $AgencyDir ".git")) {
        Write-Host "Pulling latest roles..."
        Push-Location $AgencyDir
        & git pull --ff-only 2>$null
        if ($LASTEXITCODE -ne 0) { Write-Warning "git pull failed, using existing" }
        Pop-Location
    }
}

if (-not (Test-Path $AgencyDir)) {
    Write-Error "Error: agency-agents not found at $AgencyDir`nRun: .\tools\sync-roles.ps1 --clone"
    exit 1
}

# ─── Sync ─────────────────────────────────────────────────────────────────────

$Marker = "<!-- auto-generated from agency-agents -->"

# Clean previous auto-generated commands
if (Test-Path $CommandsDir) {
    Get-ChildItem $CommandsDir -Filter "*.md" | Where-Object {
        (Get-Content $_.FullName -TotalCount 1) -eq $Marker
    } | Remove-Item -Force
}

New-Item -ItemType Directory -Force -Path $CommandsDir | Out-Null

$count   = 0
$skipped = 0

Get-ChildItem $AgencyDir -Filter "*.md" -Recurse | Sort-Object FullName | ForEach-Object {
    $file     = $_
    $basename = $file.Name

    # Skip non-role files
    if ($SkipFiles -contains $basename) { return }

    # Skip non-role directories
    $relPath = $file.FullName.Substring($AgencyDir.Length).TrimStart('\','/')
    $topDir  = $relPath.Split([IO.Path]::DirectorySeparatorChar)[0]
    if ($SkipDirs -contains $topDir) { return }

    # Must have frontmatter with name:
    $head = Get-Content $file.FullName -TotalCount 20 -ErrorAction SilentlyContinue
    if (-not ($head -match "^name:")) { return }

    $cmdName = [IO.Path]::GetFileNameWithoutExtension($basename)

    # Don't overwrite project-level commands
    $projectCmd = Join-Path $Workspace ".claude\commands\$cmdName.md"
    if (Test-Path $projectCmd) { $skipped++; return }

    # Extract content after frontmatter (skip first --- block)
    $lines      = Get-Content $file.FullName -Raw
    $dashCount  = 0
    $content    = ($lines -split "`n" | ForEach-Object {
        if ($_ -match "^---$") { $dashCount++; return }
        if ($dashCount -ge 2) { $_ }
    }) -join "`n"

    $output = @"
$Marker

Adopt the following expert role for this conversation. Apply this role's full knowledge, methodology, and communication style to the task below.

<role>
$content
</role>

Now, using the expertise above, help with the following:

`$ARGUMENTS
"@

    Set-Content -Path (Join-Path $CommandsDir "$cmdName.md") -Value $output -Encoding UTF8NoBOM
    $count++
}

Write-Host ""
Write-Host "Synced $count roles to $CommandsDir"
if ($skipped -gt 0) { Write-Host "Skipped $skipped (project-level override exists)" }
Write-Host ""
Write-Host "Check for new roles periodically:"
Write-Host "  .\tools\sync-roles.ps1 --pull"
