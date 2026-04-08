# Changelog

All notable changes to `multiagent-setup` are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions use [Semantic Versioning](https://semver.org/).

---

## [Unreleased] — v1.9.0

### Added
- **Cursor** provider (`--provider cursor`) — `.cursor/rules/workspace.mdc` (alwaysApply) + `orchestrator.mdc`
- **Windsurf** provider (`--provider windsurf`) — `.windsurf/rules/workspace.md` + `orchestrator.md`
- **GitHub Copilot** provider (`--provider copilot`) — `.github/copilot-instructions.md`
- **Gemini CLI** provider (`--provider gemini`) — `.claude/` config reused, `GEMINI.md` context
- **Nessy** provider (`--provider nessy`) — Claude alias, `.claude/` config reused
- `add-provider` subcommand — add a new provider to an existing workspace
- `sync-roles` multi-provider: auto-detects `.qwen/commands/` and `.codex/skills/` in workspace
- `AGENTS.md` template for Codex — correct context file (previously used `CLAUDE.md`)
- `QWEN.md` template for Qwen — correct context file
- Qwen orchestrator slash command in `.qwen/commands/`
- `.csx` scripts replace platform-specific `.sh`/`.ps1` hooks
- MIT `LICENSE` file
- `CODE_OF_CONDUCT.md` (Contributor Covenant)
- `CONTRIBUTING.md` — setup instructions, how to add a new provider
- `llms.txt` — LLM crawler discoverability index
- `llms-full.txt` — comprehensive LLM reference (CLI, providers, pipeline, workspace)
- `examples/saas-starter.md` and `examples/open-source-maintainer.md`
- Animated terminal demo SVG (`docs/demo.svg`)
- Cross-platform build CI (GitHub Actions)
- GitHub issue/PR templates
- `SECURITY.md`
- `--provider all` now includes cursor, windsurf, copilot (8 providers total)
- Zsh + PowerShell completions updated with `--provider` values

### Fixed
- bootstrap.sh/ps1: `.NET 10` channel instead of `LTS` (was installing .NET 8)
- completions scripts: removed stale `.sh` wrapper references
- sync-roles: defaults to `--pull` when called without an action flag

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

[Unreleased]: https://github.com/Neftedollar/multiagent-template/compare/v1.8.0...HEAD
[1.8.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.7.0...v1.8.0
[1.7.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.6.0...v1.7.0
[1.6.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.5.0...v1.6.0
[1.5.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/Neftedollar/multiagent-template/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/Neftedollar/multiagent-template/releases/tag/v1.3.0
