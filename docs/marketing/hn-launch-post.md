# HN Launch Post

**Title:** Show HN: multiagent-template – scaffold a gated multi-agent dev team in one command

---

**Post body:**

I built a .NET dotnet global tool that scaffolds a structured multi-agent AI workspace: `dotnet tool install -g multiagent-setup && multiagent-setup new MyProject`.

The problem I was trying to solve: single-agent coding tools collapse when given non-trivial tasks because they're forced to be architect, developer, reviewer, and DevOps simultaneously. The solution I landed on is explicit role separation with approval gates at each handoff — the same structure that makes human teams work. An Orchestrator coordinates and never writes code. Architects design before developers build. Reviewers validate independently. Each step either passes (`APPROVED`) or returns (`NEEDS WORK`) with a reason. After 3 retries a helper role is invoked; after 2 more the human is escalated. In practice, human escalation is rare.

The scaffold supports 8 providers (Claude, OpenAI Codex, Gemini CLI, Qwen Code, Cursor, Windsurf, GitHub Copilot, Nessy) and places the right config in the right location for each one. Safety hooks — block-dangerous-commands, enforce-commit-msg, auto-lint, sub-agent logging — are compiled into the binary rather than shell scripts, so they work the same across platforms. There's also an optional infrastructure layer: an Apache AGE graph for persistent knowledge (module boundaries, role bindings, security findings) and a pgvector semantic memory store for cross-session context.

The 20+ specialist roles come from the agency-agents project. The orchestrator routes dynamically via a capability index and creates ad-hoc roles when no standard role fits. In autonomous mode (`claude -p "/orchestrator"`), it pulls tasks from the GitHub Project backlog and runs the full pipeline without supervision, escalating only for defined edge cases.

Repo: https://github.com/Neftedollar/multiagent-template
NuGet: https://www.nuget.org/packages/multiagent-setup
Landing: https://neftedollar.com/multiagent-template/

Happy to discuss design decisions, particularly around the escalation policy and how the role routing works.
