# multiagent-template

A scaffold for AI-powered development workspaces where a team of specialized agents — orchestrator, architect, developer, reviewer, DevOps, designer, and more — autonomously drives software from backlog to merged PR. You set direction; the agents handle execution.

[![NuGet](https://img.shields.io/nuget/v/multiagent-setup)](https://www.nuget.org/packages/multiagent-setup)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Quick Start

```bash
# One-liner bootstrap (macOS / Linux — installs all deps + creates workspace)
curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject

# Windows (PowerShell)
irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.ps1 -OutFile bootstrap.ps1
.\bootstrap.ps1 MyProject
```

Already have git, gh, jq, and .NET installed?

```bash
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject                      # Claude (default)
multiagent-setup new MyProject --provider gemini    # Gemini CLI
multiagent-setup new MyProject --provider nessy     # Nessy (Claude-compatible)
multiagent-setup new MyProject --provider codex     # OpenAI Codex
multiagent-setup new MyProject --provider qwen      # Qwen Code
multiagent-setup new MyProject --provider all       # all providers at once
```

GitHub org is auto-detected from `gh auth`. Override: `multiagent-setup new MyProject my-org`.

---

## Supported Providers

| Provider | Binary | Notes |
|----------|--------|-------|
| **claude** | `claude` | [Claude Code](https://docs.anthropic.com/en/docs/claude-code) by Anthropic — default |
| **nessy** | `nessy` | Claude-compatible agent; reuses `.claude/` config |
| **gemini** | `gemini` | [Gemini CLI](https://github.com/google-gemini/gemini-cli) by Google |
| **codex** | `codex` | [OpenAI Codex CLI](https://github.com/openai/codex) |
| **qwen** | `qwen-code` | [Qwen Code](https://github.com/QwenLM/qwen-code) by Alibaba |

---

## How It Works

One human (CEO) gives tasks. An **Orchestrator** agent breaks them into steps, picks the right specialist role for each step, runs the pipeline, and delivers a PR. Human escalation is required only for: public content, breaking changes, infra decisions with cost impact, or 5+ consecutive failures.

### Pipeline

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

Five pipeline types: `feature`, `bugfix` (skips PLAN), `infra`, `content`, `spike` (PLAN only).

Each step ends with a gate: `APPROVED` — proceed; `NEEDS WORK` — agent retries (up to 3×, then a helper agent, then 2× more, then CEO escalation).

### Modes

| Mode | How to trigger | Description |
|------|----------------|-------------|
| **CEO Mode** | `/orchestrator <task>` | Human gives task, orchestrator executes |
| **Single Expert** | `/<role> <question>` | Direct expert call, no pipeline |
| **Autonomous** | `claude -p "/orchestrator ..."` | Orchestrator self-selects tasks from backlog |

---

## Workspace Structure

```
MyProject/
├── code/                    <- product repo (git-ignored)
├── docs/
│   ├── process.md           <- operational manual (pipeline source of truth)
│   ├── role-capabilities.md <- role index for orchestrator
│   └── workflows/           <- pipeline specs (WORKFLOW-*.md)
├── .claude/                 <- Claude / Nessy config
│   ├── commands/            <- slash-command roles
│   ├── hooks/lint.json      <- auto-lint formatter config
│   ├── mcp.json             <- MCP server config
│   └── settings.json        <- hook configuration
├── .gemini/                 <- Gemini CLI config (--provider gemini)
│   └── settings.json
├── .codex/                  <- Codex config (--provider codex)
└── tools/
    ├── sync-roles.csx       <- sync roles from agency-agents
    ├── install-mcps.csx     <- install MCP servers
    ├── completions.zsh      <- zsh completions
    └── completions.ps1      <- PowerShell completions
```

---

## Hook System

Hooks run automatically via `settings.json` (or `.gemini/settings.json` / `.codex/hooks.json`). All hooks are built into the `multiagent-setup` binary — no separate shell scripts, fully cross-platform.

| Hook | Trigger | Action |
|------|---------|--------|
| `block-dangerous` | PreToolUse (Bash) | Blocks `rm -rf /`, `push --force main`, `DROP TABLE`, etc. |
| `enforce-commit-msg` | PreToolUse (Bash) | Enforces conventional commits (`feat:`, `fix:`, etc.) |
| `auto-lint` | PostToolUse (Edit/Write) | Runs formatter on changed file |
| `log-agent` | PreToolUse (Agent) | Logs sub-agent launches to `.claude/agent-log.jsonl` |
| `stop-guard` | Stop | Reminds to run tests and update O'Brien + graph |
| `research-reminder` | PostToolUse (WebSearch/WebFetch) | Reminds to persist research in O'Brien and graph |

---

## Agent Roles

Roles ship as slash commands from [agency-agents](https://github.com/msitarzewski/agency-agents), installed to the project-local `.claude/commands/` directory at workspace creation time.

| Layer | Roles |
|-------|-------|
| Strategy | `/product-manager`, `/product-trend-researcher` |
| Management | `/orchestrator`, `/testing-reality-checker`, `/specialized-workflow-architect` |
| Engineering | `/engineering-software-architect`, `/engineering-backend-architect`, `/engineering-frontend-developer`, `/engineering-code-reviewer`, `/engineering-devops-automator`, `/engineering-security-engineer` |
| AI / ML | `/engineering-ai-engineer` |
| Design | `/design-ux-researcher`, `/design-ui-designer` |
| GTM | `/specialized-developer-advocate`, `/engineering-technical-writer`, `/marketing-content-creator` |

The orchestrator selects roles **dynamically** via `docs/role-capabilities.md` based on task signals (files touched, keywords, labels). No hardcoded routing. If no role fits, an ad-hoc role is created on the fly.

---

## Infrastructure (Optional)

### AGE Graph
Graph knowledge base on PostgreSQL + [Apache AGE](https://age.apache.org/), connected via [age-mcp](https://github.com/Neftedollar/age-mcp). Stores modules, pipelines, role bindings, security findings, and code insights. Grows with every completed task.

### O'Brien
Semantic memory store on pgvector — for agent coordination and recall. Used for optimistic task locking, progress tagging, research storage, and crash recovery.

```bash
multiagent-setup install-mcps          # interactive Docker setup
multiagent-setup install-mcps --manual # enter connection strings manually
```

---

## CLI Reference

```bash
multiagent-setup new <project> [org] [--provider <name>]
multiagent-setup sync-roles [--clone|--pull] [--agency-dir <path>]
multiagent-setup install-mcps [--docker|--manual] [--age-conn <str>] [--obrien-conn <str>]
multiagent-setup hook <name>
multiagent-setup -v | --version
```

---

## Requirements

| Tool | macOS/Linux | Windows |
|------|-------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) 10+ | `brew install dotnet` | `winget install Microsoft.DotNet.SDK.10` |
| [GitHub CLI](https://cli.github.com/) | `brew install gh` | `winget install GitHub.cli` |
| git, jq | brew / apt | `winget install Git.Git jqlang.jq` |
| Agent CLI | see provider table above | same |
| Docker | optional, for AGE/O'Brien | `winget install Docker.DockerDesktop` |

`bootstrap.sh` / `bootstrap.ps1` install everything automatically on a clean machine.

---

## Contributing

Templates live in [`tools/setup-cli/Templates/`](tools/setup-cli/Templates/). Each provider gets its own subdirectory under `providers/`. See [`tools/setup-cli/`](tools/setup-cli/) for the CLI source.

---

[Русская версия](README_RU.md)
