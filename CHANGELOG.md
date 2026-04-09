# Changelog

All notable changes to `multiagent-setup` are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions use [Semantic Versioning](https://semver.org/).

---

## [1.30.0] — 2026-04-09

### Fixed
- **`block-dangerous` false positive on SQL keywords in text-tool context** — `DangerousPatterns` refactored from 2-tuple `(Regex, string)` to 3-tuple `(Regex, string, bool isSqlPattern)`. SQL-destructive patterns (`DROP TABLE/DATABASE`, `TRUNCATE TABLE`) now have an explicit `isSqlPattern = true` flag; the first-word context check uses the flag instead of fragile label-string matching. Fixes false positives in `grep`, `gh`, `git log`, etc.
- **`enforce-commit-msg` false positive on `gh` commands mentioning "git commit"** — regex anchored to `(?:^|[|&;]\s*)git\s+commit\b`; no longer fires when "git commit" appears inside a `--body` argument.
- **`ExtractCommitMessage` CRLF heredoc** — `\n` in heredoc boundary regex loosened to `\r?\n`; fixes Windows heredoc commit messages not being extracted.

Total tests: **127** (unchanged — 3-tuple refactor is a structural fix, not new functionality).

---

## [1.29.0] — 2026-04-09

### Fixed
- **`{{HOOK_EXEC}}` for Homebrew/binary installs** — hook commands in `.claude/settings.json` now resolve to the actual running binary path (`Process.MainModule.FileName`) instead of always assuming the dotnet global-tool path. Homebrew (`/opt/homebrew/bin/multiagent-setup`) and direct binary downloads now produce a working hook configuration. Falls back to `$HOME/.dotnet/tools/multiagent-setup` when the process name doesn't match.
- Applied fix to all three commands that write settings: `new`, `add-provider`, `update`.
- `TemplateResources.ResolveHookExec()` extracted as shared helper; 2 new tests added.

### Changed
- **`bootstrap.sh`** — on macOS with Homebrew present, installs `multiagent-setup` via tap (`brew install Neftedollar/multiagent-template/multiagent-setup`) instead of dotnet tool, skipping the .NET SDK requirement.

Total tests: **127** (was 125).

---

## [1.28.0] — 2026-04-09

### Added
- **Test coverage: DoctorCommand** — 12 tests covering exit codes, required/optional file checks, hook config validation, global roles, IDE provider detection (cursor, cline, aider), and MCP config parsing. `DoctorCommand` refactored to accept `workspaceRoot` and `homeDir` for testability.
- **Demo SVG updated** — `docs/demo.svg` now shows the interactive provider picker (v1.24.0 feature) instead of the old `--provider claude` flag flow.
- **Landing page updated** — Homebrew install and interactive picker shown in Quick Start.
- Total tests: **125** (was 113).

---

## [1.27.0] — 2026-04-09

### Added
- **Test coverage: ProviderRegistry** — 20 tests covering `All` count, `Find`, `ValidForNew/Add`, `AllExpansion`, per-provider contracts (nessy null prefix, cline/aider empty dirs, binary-name invariants). Acts as a regression guard when adding providers.
- **Test coverage: TemplateResources** — 18 tests covering text/binary classification for all supported extensions including `.clinerules` regression case.
- **`InternalsVisibleTo`** — test project now references the main assembly directly; no more pattern duplication needed for future tests.
- Total tests: **113** (was 65).

---

## [1.26.0] — 2026-04-09

### Added
- **Homebrew tap** — `brew install Neftedollar/multiagent-template/multiagent-setup` installs without requiring .NET SDK. Formula lives in `Formula/multiagent-setup.rb` in this repo; SHA256 and version updated automatically on each tagged release by the new `release.yml` CI workflow.
- **GitHub Releases with binaries** — each tagged version now ships self-contained binaries for `osx-arm64`, `osx-x64`, `linux-x64`, and `win-x64` (no .NET runtime required to run them).

---

## [1.25.0] — 2026-04-09

### Fixed
- **`block-dangerous` hook**: SQL-destructive patterns (`DROP TABLE/DATABASE`, `TRUNCATE TABLE`) no longer trigger false positives when the command is a text-manipulation tool (`grep`, `rg`, `gh`, `git`, `cat`, `echo`, etc.). Direct SQL execution (`DROP TABLE users`, `psql ... DROP TABLE`) is still blocked. Adds 6 new regression tests (total: 65).
- **`enforce-commit-msg` hook**: Added heredoc commit message extraction. The `<<'EOF'...EOF` pattern (recommended in the system prompt for multi-line commits) now correctly extracts the commit message body for validation instead of always passing silently.

---

## [1.24.0] — 2026-04-09

### Added
- **Interactive provider picker** — `multiagent-setup new <project>` now shows a numbered menu of all 12 providers when `--provider` is not specified and stdin is a terminal. Selecting by number or name replaces the previous silent default to `claude`. Non-interactive mode (CI, piped input) retains the `claude` default.

---

## [1.23.0] — 2026-04-09

### Changed
- **Internal: provider registry** — replaced growing `if/else` chains in `SetupCommand`, `AddProviderCommand`, and `UpdateCommand` with a single `ProviderRegistry` table (`ProviderRegistry.cs`). Adding a new provider now requires only one registry entry + template files. No user-facing behavior change; all 57 tests pass.

---

## [1.22.0] — 2026-04-09

### Fixed
- **GitHub Actions template**: prevent newline injection into `$GITHUB_OUTPUT` via issue title — switched to heredoc output pattern (`<<EOF`) per GitHub security guidance
- **Windows PowerShell completions**: removed unreliable `$PROFILE` env-var lookup; path now always derived from `SpecialFolder.MyDocuments`
- **`add-provider` / `update`**: emit visible `WARN` when GitHub org/repo can't be parsed from `CLAUDE.md` instead of silently using OS username as fallback
- **`TemplateResources.IsTextResource`**: tightened `.clinerules` match from `EndsWith("clinerules")` to `EndsWith(".clinerules")`

---
## [1.21.0] — 2026-04-09

### Fixed
- `update` command now detects and re-extracts templates for **Cline**, **Aider**, **Continue.dev**, and **Roo Code** providers (previously silently skipped)
- `update` command now re-extracts `.github/workflows/orchestrator.yml` for claude/nessy workspaces
- `doctor` command now recognizes Cline, Aider, Continue.dev, and Roo Code as valid provider configurations

---

## [1.20.0] — 2026-04-09

### Added
- **Roo Code provider** (`--provider roo` / `add-provider roo`) — generates `.roo/rules/workspace.md` for the Roo Code VS Code extension; rules load automatically per project from `.roo/rules/` directory (distinct format from Cline's `.clinerules`)
- Roo Code included in `--provider all`
- Shell completions (zsh + PowerShell) updated for `roo`

---

## [1.19.0] — 2026-04-09

### Added
- **GitHub Actions workflow** (`.github/workflows/orchestrator.yml`) scaffolded for claude/nessy workspaces — run the autonomous orchestrator in CI via `workflow_dispatch`, issue label trigger (`orchestrator`), or scheduled cron; uses environment variables for all GitHub context data to prevent injection (CWE-78)

---

## [1.18.0] — 2026-04-09

### Added
- **Continue.dev provider** (`--provider continue` / `add-provider continue`) — generates `.continue/config.yaml` with custom `/orchestrator` and `/expert` slash commands and workspace rules; works with VS Code and JetBrains via the Continue extension

---

## [1.17.0] — 2026-04-09

### Added
- **Cline provider** (`--provider cline` / `add-provider cline`) — generates `.clinerules` for VS Code Cline and Roo Code extensions; rules load automatically per project
- **Aider provider** (`--provider aider` / `add-provider aider`) — generates `.aider.conf.yml` (auto-reads `CLAUDE.md` + `docs/process.md`) and `AIDER.md`; includes conventional-commit prompt
- Both providers included in `--provider all`
- `TemplateResources.IsTextResource` extended with `.yml`, `.yaml`, and `.clinerules` support
- Shell completions (zsh + PowerShell) updated for new providers

---

## [1.16.0] — 2026-04-09

### Added
- `doctor` command — workspace health checker: validates required files (`CLAUDE.md`, `docs/process.md`, `role-capabilities.md`, `.claude/`), agent roles, hook configuration, required tools (`git`, `gh`, `dotnet`), and at least one AI agent CLI on PATH; exits 0 on warnings-only, 1 on errors
- `doctor` added to shell completions (zsh + PowerShell)

### Fixed
- `sync-roles`: default action changed from empty string to `--pull` (was silently a no-op)

---

## [1.15.3] — 2026-04-09

### Fixed
- `block-dangerous` hook: tightened force-push patterns to avoid false positives when PR body or commit message text contains "git push --force main" as documentation or test-plan text; patterns now require `--force`/`-f` and `main`/`master` to be adjacent git arguments

---

## [1.15.2] — 2026-04-09

### Added
- `auto-lint` hook: C# (`.cs`) support via `dotnet format --include <file>`
- `block-dangerous` hook: blocks `git push --force` / `git push -f` without a branch argument (which would force-push to the tracked branch)

---

## [1.15.1] — 2026-04-09

### Fixed
- `update`: Cursor `.mdc` files were being written as binary — `IsTextResource` was missing `.mdc` extension in `UpdateCommand`

### Changed
- `IsTextResource` extracted to shared `TemplateResources` helper — removes duplication across `SetupCommand`, `AddProviderCommand`, `UpdateCommand`

---

## [1.15.0] — 2026-04-09

### Fixed
- `install-mcps`: replaced `Environment.Exit(0)` inside `SetupDockerAsync` with proper `return false` — callers now receive a result instead of the process aborting mid-execution, making the method composable and testable
- `install-mcps`: connection strings are now masked in console output (`password=***`)

---

## [1.14.0] — 2026-04-09

### Added
- `add-provider all` — adds all providers to an existing workspace in one command
- README: `add-provider all` example in Quick Start
- bootstrap.sh + bootstrap.ps1: `--provider` flag support

---

## [1.13.0] — 2026-04-08

### Added
- `update` subcommand — updates an existing workspace with the latest templates
  - Auto-detects installed providers (claude, codex, qwen, cursor, windsurf, copilot, gemini)
  - Re-extracts `docs/`, `tools/`, hooks, and provider configs
  - Preserves user-customised files (`CLAUDE.md`, `GEMINI.md`, etc.) by default
  - `--force` flag to overwrite all files
- README: `update` command documented in Quick Start and CLI Reference

---

## [1.12.0] — 2026-04-08

### Fixed
- `add-provider`: `GITHUB_ORG` and `GITHUB_REPO` now parsed from existing `CLAUDE.md` instead of defaulting to `Environment.UserName` — provider files now contain the correct GitHub org
- `new`: `GitInitAsync` now checks exit codes for `git init`, `git add`, and `git commit`; errors are surfaced to the user instead of silently ignored
- `add-provider`: workspace detection now requires both `CLAUDE.md` and `docs/process.md` to avoid false-positive matches

---

## [1.11.0] — 2026-04-08

### Added
- `AGENTS.md` workspace context template for OpenAI Codex CLI — parallel to `CLAUDE.md`, `GEMINI.md`, `QWEN.md`
- README: "What you get in 5 minutes" summary block in Quick Start
- README: `> Recommended:` bootstrap callout, "Best for" column in provider table, existing-project clone example
- `docs/marketing/` — dev.to article draft, HN launch post, Twitter/X thread outline
- `docs/demo.svg` — animated terminal demo SVG embedded in README
- CI build status badge in README
- Corrected `add-provider` CLI syntax in `llms.txt` and `llms-full.txt`

---

## [1.10.0] — 2026-04-08

### Added
- `QWEN.md` workspace context template for Qwen Code — parallel to `GEMINI.md`
- `.qwen/settings.json` sets `contextFileName: QWEN.md`
- README: Gemini CLI row in providers table, Quick Start, and CLI reference

---

## [1.9.0] — 2026-04-08

### Added
- **Cursor** provider (`--provider cursor`) — `.cursor/rules/workspace.mdc` (alwaysApply) + `orchestrator.mdc`
- **Windsurf** provider (`--provider windsurf`) — `.windsurf/rules/workspace.md` + `orchestrator.md`
- **GitHub Copilot** provider (`--provider copilot`) — `.github/copilot-instructions.md`
- **Gemini CLI** provider (`--provider gemini`) — `.gemini/settings.json` + `GEMINI.md` context
- **Nessy** provider (`--provider nessy`) — Claude alias, `.claude/` config reused
- `add-provider` subcommand — add a new provider to an existing workspace without recreating
- `sync-roles` multi-provider: auto-detects `.qwen/commands/` and `.codex/skills/` in workspace
- MIT `LICENSE` file
- `CODE_OF_CONDUCT.md` (Contributor Covenant)
- `CONTRIBUTING.md` — setup instructions, how to add a new provider
- `llms.txt` — LLM crawler discoverability index
- `llms-full.txt` — comprehensive LLM reference (CLI, providers, pipeline, workspace)
- `examples/saas-starter.md` and `examples/open-source-maintainer.md`
- Animated terminal demo SVG (`docs/demo.svg`)
- Cross-platform build CI (GitHub Actions) — publishes to NuGet on tag push
- GitHub issue/PR templates
- `SECURITY.md`
- `--provider all` now includes cursor, windsurf, copilot, gemini (8 providers total)
- Zsh + PowerShell completions updated with all providers and `add-provider` subcommand

### Fixed
- `bootstrap.sh`/`bootstrap.ps1`: install `.NET 10` (channel `10.0`) instead of LTS
- `sync-roles`: defaults to `--pull` when called without an action flag

---

## [1.8.0] — 2025-04-01

### Added
- **OpenAI Codex CLI** provider (`--provider codex`)
  - `.codex/config.toml` with `codex_hooks = true`
  - `.codex/hooks.json` — pre/post/stop hooks wired to `multiagent-setup hook`
  - `.codex/skills/orchestrator.md` — orchestrator role pre-loaded
- **Qwen Code** provider (`--provider qwen`)
  - `.qwen/settings.json` with full hook configuration
- `--provider all` — scaffold all providers at once (claude + codex + qwen)

### Changed
- Orchestrator role: added **cardinal rule** — never writes code, always delegates to specialist roles
- Orchestrator role: clarified model tier assignments for all providers

---

## [1.7.0] — 2025-03-15

### Changed
- Restored live `settings.json` with real hook path (was using placeholder)
- Version bump housekeeping

---

## [1.6.0] — 2025-03-10

### Added
- All hooks compiled into `multiagent-setup` binary — no shell script wrappers needed
- `hook` subcommand: `multiagent-setup hook <name>`
- `{{HOOK_EXEC}}` template variable — resolves to correct binary path per OS at workspace creation
- Templates moved to `tools/setup-cli/Templates/` and embedded as resources

### Changed
- `settings.json` no longer references `.sh` wrapper scripts — uses dotnet tool path directly
- README updated to document hook system and subcommands

---

## [1.5.0] — 2025-02-28

### Added
- Cross-platform hook execution via dotnet global tool (Windows support)
- PowerShell completions (`tools/completions.ps1`)
- `age-mcp` wired into `stop-guard` hook
- `install-mcps.ps1` for Windows

### Fixed
- Windows hook path: use PowerShell env var syntax (`$env:USERPROFILE`)

---

## [1.4.0] — 2025-02-15

### Added
- `new`, `sync-roles`, `install-mcps` as proper CLI subcommands in the dotnet tool
- Thin wrapper scripts (`sync-roles.sh`, `sync-roles.ps1`) delegate to `multiagent-setup` CLI
- `sync-roles.ps1` for Windows

### Changed
- Bootstrapping now installs/updates the global dotnet tool rather than copying scripts

---

## [1.3.0] — 2025-01-30

### Added
- Initial multi-agent workspace scaffold as a dotnet global tool
- `CLAUDE.md` workspace context template with pipeline summary and role table
- `docs/process.md` — operational pipeline manual
- `docs/role-capabilities.md` — capability index for dynamic role selection
- Pipeline workflow specs (feature, bugfix, infra, content, spike)
- Hook system: `block-dangerous`, `enforce-commit-msg`, `auto-lint`, `log-agent`, `stop-guard`, `research-reminder`
- `.claude/settings.json` wired to all hooks
- `sync-roles` — clones and syncs [agency-agents](https://github.com/msitarzewski/agency-agents) roles to `~/.claude/commands/`
- `install-mcps` — Docker-based setup for AGE graph + O'Brien memory
- Zsh completions (`tools/completions.zsh`)
- `bootstrap.sh` / `bootstrap.ps1` for clean-machine setup

---

[Unreleased]: https://github.com/Neftedollar/multiagent-template/compare/v1.15.3...HEAD
[1.15.3]: https://github.com/Neftedollar/multiagent-template/compare/v1.15.2...v1.15.3
[1.15.2]: https://github.com/Neftedollar/multiagent-template/compare/v1.15.1...v1.15.2
[1.15.1]: https://github.com/Neftedollar/multiagent-template/compare/v1.15.0...v1.15.1
[1.15.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.14.0...v1.15.0
[1.14.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.13.0...v1.14.0
[1.13.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.12.0...v1.13.0
[1.12.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.11.0...v1.12.0
[1.11.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.10.0...v1.11.0
[1.10.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.9.0...v1.10.0
[1.9.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.8.0...v1.9.0
[1.8.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.7.0...v1.8.0
[1.7.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.6.0...v1.7.0
[1.6.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.5.0...v1.6.0
[1.5.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/Neftedollar/multiagent-template/releases/tag/v1.3.0
