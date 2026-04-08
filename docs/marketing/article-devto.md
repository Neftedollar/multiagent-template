# I Built a Multi-Agent Dev Team in One Command — Here's What I Learned

Most "agentic coding" tools are still just one model with a very long system prompt. You ask it to build a feature and it tries to be architect, developer, reviewer, and DevOps all at once. It context-collapses around step 3. The PR it opens would never survive a real code review.

I spent several months building a different model: a structured workspace where specialized agents operate in a gated pipeline, each with a defined role and accountability boundary. The result is [multiagent-template](https://github.com/Neftedollar/multiagent-template) — a .NET 10 dotnet global tool that scaffolds the whole thing in one command.

## The Core Idea: Role Separation + Gates

The insight is simple: the reason a single AI agent struggles with a full feature is the same reason a single human junior dev would — no specialization, no review, no checks. The fix is also the same: a team with defined roles and handoffs.

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

Each step is owned by a specialist role. The **Orchestrator** coordinates but never writes code. The **Architect** designs before the **Developer** builds. The **Reviewer** validates independently. Every step has a gate: `APPROVED` or `NEEDS WORK (reason)`. Failures retry 3×, then a helper role, then human escalation.

You only get paged for things that actually need a human: public content decisions, breaking API changes, infra with cost impact, or 5+ consecutive failures.

## Getting Started in 60 Seconds

```bash
# Full bootstrap on a clean machine (installs deps + scaffolds workspace)
curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject

# Or, if you already have git, gh, jq, and .NET 10:
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject --provider claude
cd MyProject
claude
```

Then give the orchestrator a task:

```
/orchestrator Implement user authentication with JWT
```

That's it. You'll get a PR.

## What the Orchestrator Actually Does

Here's what happens when you run that command against a Next.js + FastAPI project:

1. `/product-manager` writes an EPIC with acceptance criteria
2. `/engineering-software-architect` defines module boundaries and approach
3. `/engineering-backend-architect` implements auth endpoints
4. `/engineering-frontend-developer` builds the login/signup UI
5. `/engineering-devops-automator` updates CI and secrets config
6. `/engineering-code-reviewer` validates everything
7. PR opened: `feat/auth-jwt`

Your job: review and merge. Roughly 5 minutes of actual human time.

The orchestrator picks roles dynamically from `docs/role-capabilities.md` — it doesn't hardcode assignments. If no standard role fits the task, it creates an ad-hoc role on the fly.

## 8 Providers, One Tool

The workspace isn't locked to Claude. The same pipeline structure scaffolds for:

| Provider | How to use |
|----------|-----------|
| claude | `--provider claude` (default) |
| OpenAI Codex | `--provider codex` |
| Gemini CLI | `--provider gemini` |
| Qwen Code | `--provider qwen` |
| Cursor IDE | `--provider cursor` |
| Windsurf IDE | `--provider windsurf` |
| GitHub Copilot | `--provider copilot` |

```bash
# All providers at once
multiagent-setup new MyProject --provider all

# Add a provider to an existing workspace without recreating it
multiagent-setup add-provider cursor
```

The rules and pipeline are placed in the right location for each provider (`.cursor/rules/`, `.windsurf/rules/`, `GEMINI.md`, etc.).

## Safety Hooks Baked In

Every workspace ships with hooks compiled into the binary — no shell scripts, no platform differences:

- `block-dangerous` — blocks `rm -rf /`, `push --force main`, `DROP TABLE`, etc. at the PreToolUse level
- `enforce-commit-msg` — enforces conventional commits before any `git commit` runs
- `auto-lint` — runs the right formatter (prettier, ruff, gofmt, rustfmt) after every file edit
- `log-agent` — logs every sub-agent launch to `.claude/agent-log.jsonl`
- `stop-guard` — reminds agents to run tests before stopping

These aren't suggestions written in markdown. They're enforced at the tool-call layer.

## Autonomous Mode

Once your backlog is populated in the GitHub Project, you can let the orchestrator run unsupervised:

```bash
# Add tasks to GitHub Project backlog, then:
claude -p "/orchestrator"
```

The orchestrator picks the next task, runs the full pipeline, opens a PR, and moves to the next item. You check in when you want.

This is the mode I use for routine maintenance tasks: dependency updates, documentation gaps, minor bug fixes. The agents handle the queue overnight; I review PRs in the morning.

## What It Looks Like in Practice: Open Source Maintainer

```bash
multiagent-setup new MyLib my-github-org
cd MyLib
git clone https://github.com/my-github-org/my-library code/MyLib
claude

# Triage a week of issues at once:
/orchestrator Review open PRs and triage new issues. Label by type, draft responses for issues needing clarification.

# Prepare a release:
/orchestrator Prepare release 2.2.0. Changes since 2.1.x: issues #38, #40, #42, #45. Update CHANGELOG, bump version, tag.
```

The `/engineering-technical-writer` handles CHANGELOG and migration guides. The `/engineering-devops-automator` bumps versions and tags. You merge.

## What's Optional (but Powerful)

Two infrastructure pieces are optional but change how agents retain context across sessions:

**AGE Graph** — a knowledge base on PostgreSQL + Apache AGE. Stores modules, pipelines, role bindings, security findings. Grows with every task so the orchestrator doesn't re-discover your architecture on every session.

**O'Brien** — semantic memory on pgvector. Cross-session context, task locking, crash recovery. Agents remember prior decisions.

```bash
multiagent-setup install-mcps   # interactive Docker setup
```

Neither is required to start. Both become useful once the project has real history.

## The Design Principle I Keep Coming Back To

The hardest part of building this wasn't the tooling — it was figuring out what the human should and shouldn't be involved in. The current answer:

- **Agents decide**: implementation approach, file structure, test coverage, formatting, linting
- **Human decides**: public-facing content, breaking changes, infra with cost impact, anything after 5+ failures

Everything else runs without interruption. That boundary took a few iterations to get right, but it's what makes the autonomous mode actually usable rather than just a demo.

---

The project is MIT-licensed and installs as a standard dotnet global tool.

If this is interesting to you, the repo is at [github.com/Neftedollar/multiagent-template](https://github.com/Neftedollar/multiagent-template). A star helps more developers find it — and if you try it, open an issue with what you find. The rough edges are real and I'm actively working through them.
