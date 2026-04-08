# multiagent-template

Scaffold a multi-agent AI workspace where a team of specialized agents — orchestrator, architect, developer, reviewer, DevOps, designer, and more — autonomously drives software from backlog to merged PR. You set direction; the agents handle execution.

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

Already have git, gh, jq, and .NET 10 installed?

```bash
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject                       # Claude (default)
multiagent-setup new MyProject --provider nessy      # Nessy CLI (Claude alias)
multiagent-setup new MyProject --provider codex      # OpenAI Codex
multiagent-setup new MyProject --provider qwen       # Qwen Code
multiagent-setup new MyProject --provider cursor     # Cursor IDE
multiagent-setup new MyProject --provider windsurf   # Windsurf IDE
multiagent-setup new MyProject --provider copilot    # GitHub Copilot
multiagent-setup new MyProject --provider gemini     # Google Gemini CLI
multiagent-setup new MyProject --provider all        # all providers at once

# Add a provider to an existing workspace (no need to recreate)
multiagent-setup add-provider cursor
multiagent-setup add-provider gemini --force   # overwrite existing files
```

Then start working:

```bash
cd MyProject
claude          # or: nessy / codex / qwen-code / gemini (terminal agents)
                # or open in Cursor / Windsurf / VS Code (IDE agents)
/orchestrator Implement user authentication with JWT
```

---

## Why multiagent-template?

**The problem**: Asking a single AI agent to be architect, developer, reviewer, and DevOps all at once leads to context collapse, no accountability, and inconsistent quality.

**The solution**: A structured workspace where each agent plays a defined role in a gated pipeline:

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

- **Orchestrator** coordinates — never writes code itself
- **Architects** design before developers build
- **Reviewers** validate independently after each step
- **Safety hooks** prevent dangerous commands, enforce commit conventions, auto-lint

Each step has an approval gate. Failures retry (3×) → helper role (2×) → human escalation. The human only sees escalations, not every step.

---

## Supported Providers

| Provider | Binary / Tool | Notes |
|----------|---------------|-------|
| **claude** | `claude` | [Claude Code](https://docs.anthropic.com/en/docs/claude-code) by Anthropic — default |
| **nessy** | `nessy` | [Nessy CLI](https://nessy.ai) — Claude-compatible alias |
| **codex** | `codex` | [OpenAI Codex CLI](https://github.com/openai/codex) |
| **qwen** | `qwen-code` | [Qwen Code](https://github.com/QwenLM/qwen-code) by Alibaba |
| **cursor** | [Cursor](https://cursor.com) IDE | Rules placed in `.cursor/rules/` (MDC format) |
| **windsurf** | [Windsurf](https://windsurf.com) IDE | Rules placed in `.windsurf/rules/` (Wave 8+) |
| **copilot** | GitHub Copilot | Reads `.github/copilot-instructions.md` |
| **gemini** | `gemini` | [Gemini CLI](https://github.com/google-gemini/gemini-cli) by Google — creates `GEMINI.md` |

Use `--provider all` to scaffold all providers (claude + nessy + codex + qwen + cursor + windsurf + copilot + gemini).

---

## How It Works

One human (CEO) gives tasks. The **Orchestrator** agent breaks them into steps, picks the right specialist role, runs the pipeline, and delivers a PR. Human escalation is required only for: public content, breaking API changes, infra decisions with cost impact, or 5+ consecutive failures.

### Pipeline types

| Type | Steps | When |
|------|-------|------|
| `feature` | PLAN → BUILD → TEST → VERIFY → SHIP | New functionality |
| `bugfix` | BUILD → TEST → VERIFY → SHIP | Skip planning |
| `infra` | PLAN → BUILD → VERIFY → SHIP | No test step |
| `content` | PLAN → BUILD → VERIFY(CEO) | Docs / marketing |
| `spike` | PLAN | Research only |

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
├── CLAUDE.md                <- workspace context (read by AI on every session)
├── code/                    <- product repo (git-ignored)
├── docs/
│   ├── process.md           <- operational manual (pipeline source of truth)
│   ├── role-capabilities.md <- role index for dynamic orchestrator routing
│   └── workflows/           <- pipeline specs (WORKFLOW-*.md)
├── .claude/
│   ├── commands/            <- slash-command roles (synced from agency-agents)
│   ├── hooks/lint.json      <- auto-lint formatter config
│   ├── mcp.json             <- MCP server config
│   └── settings.json        <- hook configuration
├── .codex/                  <- Codex config (--provider codex)
│   └── skills/              <- orchestrator skill pre-loaded
└── tools/
    ├── completions.zsh      <- zsh completions
    └── completions.ps1      <- PowerShell completions
```

---

## Hook System

All hooks are compiled into the `multiagent-setup` binary — no shell scripts, no platform quirks.

| Hook | Trigger | Action |
|------|---------|--------|
| `block-dangerous` | PreToolUse (Bash) | Blocks `rm -rf /`, `push --force main`, `DROP TABLE`, etc. |
| `enforce-commit-msg` | PreToolUse (Bash) | Enforces conventional commits (`feat:`, `fix:`, etc.) |
| `auto-lint` | PostToolUse (Edit/Write) | Runs formatter on changed file (prettier, ruff, gofmt, rustfmt…) |
| `log-agent` | PreToolUse (Agent) | Logs sub-agent launches to `.claude/agent-log.jsonl` |
| `stop-guard` | Stop | Reminds to run tests and update knowledge graph |
| `research-reminder` | PostToolUse (WebSearch) | Reminds to persist research in O'Brien memory |

---

## Agent Roles

20+ specialist roles from [agency-agents](https://github.com/msitarzewski/agency-agents), installed at workspace creation time.

| Layer | Roles |
|-------|-------|
| Strategy | `/product-manager`, `/product-trend-researcher` |
| Management | `/orchestrator`, `/testing-reality-checker`, `/specialized-workflow-architect` |
| Engineering | `/engineering-software-architect`, `/engineering-backend-architect`, `/engineering-frontend-developer`, `/engineering-code-reviewer`, `/engineering-devops-automator`, `/engineering-security-engineer` |
| AI / ML | `/engineering-ai-engineer` |
| Design | `/design-ux-researcher`, `/design-ui-designer` |
| GTM | `/specialized-developer-advocate`, `/engineering-technical-writer`, `/marketing-content-creator` |

The orchestrator routes dynamically via `docs/role-capabilities.md` — no hardcoded assignments. If no role fits, it creates an ad-hoc role on the fly.

---

## Infrastructure (Optional)

### AGE Graph
Graph knowledge base on PostgreSQL + [Apache AGE](https://age.apache.org/), connected via [age-mcp](https://github.com/Neftedollar/age-mcp). Stores modules, pipelines, role bindings, security findings, code insights. Grows with every task.

### O'Brien
Semantic memory on pgvector — cross-session context, task locking, crash recovery.

```bash
multiagent-setup install-mcps             # interactive Docker setup
multiagent-setup install-mcps --manual    # enter connection strings manually
```

---

## Examples

See [`examples/`](examples/) for concrete workflows:
- [SaaS Starter](examples/saas-starter.md) — foundation, auth, billing, autonomous sessions
- [Open Source Maintainer](examples/open-source-maintainer.md) — bug triage, PR reviews, releases

---

## CLI Reference

```bash
multiagent-setup new <project> [org] [--provider claude|nessy|codex|qwen|cursor|windsurf|copilot|gemini|all]
multiagent-setup add-provider <provider> [--force]   # add provider to existing workspace
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

Templates live in [`tools/setup-cli/Templates/`](tools/setup-cli/Templates/). Each provider gets its own directory under `providers/`. See [CONTRIBUTING.md](CONTRIBUTING.md) for setup instructions and how to add a new provider.

---

## FAQ

**Does this work with projects that already have code?**  
Yes. The workspace wraps your existing repo: `multiagent-setup new MyProject` creates the workspace, then clone your repo into `code/MyProject/`.

**How do I use multiple providers?**  
Run `multiagent-setup new MyProject --provider all` to scaffold all providers at once.

**What does the orchestrator do when I'm not watching?**  
In CEO Mode it waits for your next task. In Autonomous mode (`claude -p`), it picks tasks from the GitHub Project backlog and escalates only for defined edge cases.

**Can I add custom roles?**  
Yes. Create a `.md` file in `.claude/commands/` with a `name:` frontmatter field. The orchestrator uses it automatically, and can also create ad-hoc roles on the fly.

---

[Русская версия](README_RU.md) | [Landing page](https://neftedollar.com/multiagent-template/)
