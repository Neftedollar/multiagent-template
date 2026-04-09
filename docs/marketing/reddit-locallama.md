# Reddit Post Draft — r/LocalLLaMA

**Title:** I built a scaffold that turns Claude/Gemini/Codex into a full autonomous dev team (12 AI coding agents, gated pipeline, safety hooks)

---

**Body:**

The problem I kept running into: single-agent sessions collapse on real tasks. Context fills up, the agent drifts from the plan, and by the time it "finishes" you've got a PR that touches 30 files in ways you didn't ask for.

So I built [multiagent-template](https://github.com/Neftedollar/multiagent-template) — a dotnet global tool (one command, no YAML to write) that scaffolds a workspace where specialized agents actually have roles:

- **Orchestrator** — coordinates, never writes code
- **Architect** — designs before anyone codes
- **Developer(s)** — implement, in worktrees, with actual tests
- **Reviewer** — validates independently (uses a separate role from the builder)
- **DevOps, Security, UX** — involved at the right pipeline stage

The pipeline is `PLAN → BUILD → TEST → VERIFY → SHIP`. Each step has an approval gate. Failed gates auto-retry (3 attempts → helper role → human escalation). You only get paged when it actually matters.

**Install and go:**

```bash
# macOS (Homebrew, no .NET needed):
brew install Neftedollar/multiagent-template/multiagent-setup

multiagent-setup new MyProject    # interactive picker: choose your AI provider
cd MyProject && claude
/orchestrator Add OAuth2 login
```

**Supports 12 AI coding agents:**

Terminal: Claude Code, Nessy, OpenAI Codex CLI, Google Gemini CLI, Qwen Code, Aider  
IDE/Extensions: Cursor, Windsurf, GitHub Copilot, Cline, Continue.dev, Roo Code

Use `--provider all` to wire up all of them at once for a project.

**Safety hooks baked into the binary:**
- `block-dangerous` — stops `rm -rf /`, force pushes to main, `DROP TABLE` in direct SQL
- `enforce-commit-msg` — conventional commits only (including heredoc-style multi-line commits)
- `auto-lint` — runs prettier/ruff/gofmt/dotnet-format after every file edit
- Context-aware: `grep -n "DROP TABLE" schema.sql` passes; `psql -c "DROP TABLE users"` is blocked

**Autonomous mode** — connect to your GitHub Project backlog:
```bash
claude -p "/orchestrator"
```
Agents pick tasks overnight, deliver PRs, you review in the morning.

**Optional (but nice):** AGE graph (PostgreSQL + Apache AGE) for persistent codebase knowledge + O'Brien (pgvector) for cross-session memory. Not required for the basic workflow.

---

Source: [github.com/Neftedollar/multiagent-template](https://github.com/Neftedollar/multiagent-template) | MIT license

Happy to answer questions about the architecture, hook system, or provider support.

---

## Also consider posting to:

- **r/ClaudeAI** — Claude-specific users
- **r/MachineLearning** — broader ML/AI audience (title: "Structured multi-agent pipeline for autonomous code generation")
- **r/programming** — dev tools angle (title: "One command scaffolds a full autonomous dev pipeline — 12 AI coding agents, gated handoffs, safety hooks")
- **r/devops** — hook/pipeline angle
- **r/SideProject** — indie maker angle
