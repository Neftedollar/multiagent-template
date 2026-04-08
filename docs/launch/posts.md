# Launch Content — multiagent-template

## Hacker News "Show HN"

**Show HN: multiagent-setup – one command to scaffold a full AI dev team for your project**

I got tired of prompting a single AI agent to "now do the architecture" then "now write the code" then "now review it" — so I built a .NET global tool that scaffolds a structured multi-agent workspace in one command. `dotnet tool install -g multiagent-setup && multiagent-setup new MyProject` creates a workspace with a full team of specialized agents (orchestrator, architect, backend dev, frontend dev, reviewer, DevOps, designer), a PLAN → BUILD → TEST → VERIFY → SHIP pipeline with approval gates, and pre-wired safety hooks that block destructive shell commands, enforce conventional commits, and auto-lint — all compiled into the binary so there are no platform-specific shell scripts to maintain. It supports five AI coding agents as providers: Claude, Gemini, Codex, Qwen, and Nessy. The honest trade-off: this adds real structure and ceremony, so if you just want to ask an AI to fix a bug, this is overkill — it's aimed at people running a sustained build effort where you want repeatability and guardrails. Semantic memory is handled via AGE graph + pgvector for agents that need cross-session context. Cross-platform (Windows, macOS, Linux).

GitHub: https://github.com/Neftedollar/multiagent-template
NuGet: https://www.nuget.org/packages/multiagent-setup

---

## Twitter/X Thread

**Tweet 1 (hook)**
You're not bottlenecked by AI capability anymore. You're bottlenecked by having one agent doing everything with no memory, no roles, no gates. I built a fix.

**Tweet 2 (how it works)**
`multiagent-setup new MyProject` scaffolds a full AI dev team: orchestrator, architect, backend dev, frontend dev, reviewer, DevOps, designer. Each agent has a defined role. The pipeline is PLAN → BUILD → TEST → VERIFY → SHIP with approval gates between steps.

**Tweet 3 (key feature)**
Safety hooks are compiled into the binary — no bash scripts, no platform quirks. Blocks dangerous destructive commands, enforces conventional commits, auto-lints on commit. Works the same on Windows, macOS, Linux. Works with Claude, Gemini, Codex, Qwen, or Nessy as your AI provider.

**Tweet 4 (code example)**
Two commands, full team:
```
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject
```
Pick your provider: `--provider claude` or `--provider gemini`. Done. Your agents are ready. Drop a task in the backlog and run `/orchestrator`.

**Tweet 5 (CTA)**
If you've used Claude Code or Gemini CLI and thought "I want more structure than this" — this might be what you're looking for. Open source, MIT.

GitHub: https://github.com/Neftedollar/multiagent-template
NuGet: https://www.nuget.org/packages/multiagent-setup
Docs: https://neftedollar.com/multiagent-template/

---

## dev.to Article Outline

**Title: I Wanted a Dev Team, Not a Single AI Agent — So I Built One**  
**Subheading**: How `multiagent-setup` brings pipeline structure and specialized roles to AI-assisted development

### The problem with "just ask the AI"
- A single agent context-switches constantly: architect, coder, reviewer, all in one chat session
- No memory between sessions means re-explaining the project every time
- No guardrails means one bad prompt can make a mess of your repo

### What a structured multi-agent workspace looks like
- Specialized roles: each agent has a defined scope (architect doesn't write code, reviewer doesn't design)
- A real pipeline: PLAN → BUILD → TEST → VERIFY → SHIP, with gates that require approval before moving forward
- Semantic memory via AGE graph + pgvector so context survives across sessions

### One command to set it up
- `dotnet tool install -g multiagent-setup && multiagent-setup new MyProject`
- Cross-platform: same binary on Windows, macOS, Linux — no shell scripts
- Five provider options: Claude, Gemini, Codex, Qwen, Nessy; switch with `--provider`

### Safety without configuration
- Hooks are baked into the binary, not shell scripts you have to maintain
- Blocks dangerous shell commands, enforces conventional commits, auto-lints on commit
- Works out of the box — nothing to wire up manually

### Who this is (and isn't) for
- Good fit: sustained build efforts where repeatability and role clarity matter
- Not the right tool: quick one-off fixes or exploratory prompting sessions
- Open source, MIT license — contributions welcome
