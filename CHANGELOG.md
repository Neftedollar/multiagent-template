# Changelog

All notable changes to `multiagent-setup` are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions use [Semantic Versioning](https://semver.org/).

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
