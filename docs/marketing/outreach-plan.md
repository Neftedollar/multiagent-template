# Outreach Plan: DX Audit + Twitter/X Thread

---

## DX Audit — Top 5 Friction Points

**1. .NET 10 as a prerequisite is a surprise**
Most AI tool users don't have .NET installed. The README lists it in requirements but doesn't explain why the tool is built on it or how fast the install is.
Fix: Add a one-liner to the Quick Start that installs .NET 10 inline, and add a note: "You don't need to know .NET — it's just the packaging mechanism."

**2. The bootstrap URL is long but there's no "copy" hint**
The curl one-liner is the fastest path but it's easy to mistype. There's no "recommended starting point" callout making it obvious this is the happy path.
Fix: Add a visual callout (e.g. `> Recommended: use the bootstrap`) above the manual install steps.

**3. No 5-minute outcome promise**
The README explains what the tool does well, but a new visitor can't quickly answer "what will I have after 5 minutes?" without reading several sections.
Fix: Add a "What you get in 5 minutes" summary block right after the Quick Start — one sentence each for workspace, pipeline, roles, and hooks.

**4. The workspace structure diagram doesn't show a real project**
The tree shows template placeholders (`MyProject/`). A visitor who already has a codebase needs to see one extra step: cloning their existing repo into `code/`.
Fix: Add a second "existing project" code block after the workspace diagram showing `git clone <your-repo> code/MyProject/`.

**5. Provider comparison table lacks a "best for" column**
8 providers with no guidance on which to pick first is choice paralysis. A developer with Cursor already installed might not realize they can use that.
Fix: Add a "Best for" column to the provider table (e.g. "terminal-first workflows", "IDE users", "OpenAI API key holders").

---

## Twitter/X Thread Outline

**Tweet 1 (hook)**
I got tired of AI coding tools that "help" by being architect, developer, and reviewer all at once and collapsing halfway through. So I built a structured alternative. Thread:

**Tweet 2 (the problem)**
Single-agent tools fail on real tasks because there's no role separation, no accountability, and no checkpoints. The context window fills up, the plan drifts, and the PR is a mess.

**Tweet 3 (the solution)**
multiagent-template scaffolds a pipeline with 20+ specialist roles and approval gates at every step:
PLAN → BUILD → TEST → VERIFY → SHIP
Each gate is APPROVED or NEEDS WORK (with a reason).

**Tweet 4 (one command)**
```bash
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject --provider claude
cd MyProject && claude
/orchestrator Implement JWT auth
```
You'll have a PR in minutes. Your job: review and merge.

**Tweet 5 (role separation)**
The Orchestrator coordinates but never writes code. Architects design before developers build. Reviewers validate independently. Failures retry 3× → helper role → human escalation. You only get paged when it actually matters.

**Tweet 6 (8 providers)**
Not locked to one AI tool. Supports:
- Claude, Codex, Gemini CLI, Qwen Code
- Cursor, Windsurf, GitHub Copilot
One command adds any provider to an existing workspace.

**Tweet 7 (safety hooks)**
Safety hooks compiled into the binary — not shell scripts:
- block-dangerous: stops rm -rf /, force pushes, DROP TABLE
- enforce-commit-msg: conventional commits only
- auto-lint: prettier/ruff/gofmt after every edit
No platform quirks.

**Tweet 8 (autonomous mode)**
Add tasks to your GitHub Project backlog, then:
```bash
claude -p "/orchestrator"
```
Agents run the full pipeline overnight. You review PRs in the morning. Human escalation is for edge cases only.

**Tweet 9 (real use case)**
Open source maintainer? One command triages a week of issues, drafts responses, bumps versions, writes CHANGELOG entries, and opens the release PR. You just merge.

**Tweet 10 (optional infra)**
Optional but powerful:
- AGE graph (PostgreSQL + Apache AGE) — persistent knowledge of your codebase that grows with every task
- O'Brien (pgvector) — cross-session memory, task locking, crash recovery

**Tweet 11 (provenance)**
20+ roles from the agency-agents project. The orchestrator routes dynamically via a capability index — no hardcoded assignments. Can create ad-hoc roles on the fly when nothing fits.

**Tweet 12 (CTA)**
MIT license. Installs as a standard dotnet global tool. Bootstrap script handles all deps on a clean machine.

Repo: https://github.com/Neftedollar/multiagent-template
If it's useful to you, a star helps other developers find it.
