# Changelog

All notable changes to `multiagent-setup` are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/). Versioning: [SemVer](https://semver.org/).

---

## [1.9.0] — Unreleased

### Added
- **Gemini provider** (`--provider gemini`): `GEMINI.md`, `.gemini/settings.json` with full hooks wired
- **Nessy provider** (`--provider nessy`): Claude-compatible alias; reuses `.claude/` config and all slash commands
- **Codex `AGENTS.md`**: Workspace context file for Codex workspaces (Codex CLI uses `AGENTS.md`, not `CLAUDE.md`); model tiers updated to o3/gpt-4o/gpt-4o-mini
- **Qwen provider** — fully fleshed out: `QWEN.md` workspace context, `.qwen/commands/orchestrator.md` skill, complete hooks in `.qwen/settings.json` (PreToolUse/PostToolUse/SessionEnd)
- **`add-provider` subcommand**: Add a new AI provider to an existing workspace without recreating it (`multiagent-setup add-provider <provider>`)
- **Auto GitHub Project board**: `multiagent-setup new` creates a GitHub Project board automatically and injects the URL into `CLAUDE.md`
- **`llms.txt`**: LLM crawler discoverability file at repo root
- **Animated terminal demo** (`docs/demo.svg`): Pure-SVG animated terminal recording showing full workspace setup, embedded in README and landing page
- **GitHub community files**: Issue templates (bug report, feature request), PR template, `SECURITY.md`
- **CI workflow** (`.github/workflows/build.yml`): Cross-platform build matrix (Ubuntu, Windows, macOS)

### Fixed
- `sync-roles` defaults to `--pull` when called without an explicit action flag
- `enforce-commit-msg` now matches `--message` long form in addition to `-m`
- `stop-guard` respects `stop_hook_active` flag to avoid infinite recursion
- `InstallMcpsCommand`: `WaitForPortAsync` result checked on docker-start path; connection strings redact passwords in output
- Path traversal guard in `ExtractTemplates` using `GetFullPath` prefix check
- Project name validation rejects path separators and `..` traversal attempts
- Bootstrap scripts updated to .NET 10 channel

### Changed
- Shell scripts (`setup.sh`, `sync-roles.sh`, `install-mcps.sh`, `*.ps1`) replaced with cross-platform `.csx` scripts (dotnet-script); only `bootstrap.sh/ps1` remain platform-specific
- `.claude/settings.json` completions updated for new subcommands and providers

---

## [1.8.0] — 2026-03-xx

### Added
- **Gemini + Nessy providers** (initial): provider flag, directory scaffolding, GEMINI.md, nessy alias
- `.csx` replacement for all shell wrapper scripts
- `--workspace-root` flag on `sync-roles` for non-cwd workspaces

### Fixed
- `SessionEnd` hook for `stop-guard` in Gemini settings

---

## [1.7.0] — 2026-03-xx

### Added
- **Codex provider** (`--provider codex`): `.codex/skills/orchestrator.md`, hooks config
- **Qwen provider** (`--provider qwen`): initial scaffold
- Orchestrator command updated: cardinal rule "never write code, always delegate"

### Changed
- `--provider` flag (default: `claude`); `--provider all` installs all providers

---

## [1.6.0] — 2026-02-xx

### Added
- All template files embedded in binary via `EmbeddedResource` (`Templates/` directory)
- `multiagent-setup hook <name>` subcommand for cross-platform hook execution
- Auto-lint hook: runs formatter on edited files (prettier, eslint, fantomas, ruff, gofmt, rustfmt, rubocop, php-cs-fixer)
- Research-reminder hook on WebSearch/WebFetch

### Changed
- README updated for new hook architecture

---

## [1.5.0] — 2026-02-xx

### Added
- Cross-platform hooks via `multiagent-setup` dotnet global tool
- PowerShell completions (`completions.ps1`)
- `stop-guard` hook checks for code changes before session end

### Fixed
- Hook executable path resolved per OS at workspace creation time via `{{HOOK_EXEC}}` template variable
- Windows uses `$env:USERPROFILE` syntax; macOS/Linux uses `$HOME`

---

## [1.4.0] — 2026-01-xx

### Added
- `multiagent-setup new <project>` subcommand (replaces `setup.sh`)
- `multiagent-setup sync-roles` — clone/pull agency-agents and sync to `~/.claude/commands/`
- `multiagent-setup install-mcps` — interactive Docker + MCP server setup (age-mcp, O'Brien)
- `block-dangerous` hook: blocks `rm -rf /`, force-push to main, `DROP TABLE`, etc.
- `enforce-commit-msg` hook: enforces conventional commits format

---

## [1.3.0] and earlier

Initial versions: basic workspace scaffolding with `setup.sh`, role sync via shell script, manual MCP setup.
