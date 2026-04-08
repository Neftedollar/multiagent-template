# {{PROJECT_NAME}} — Multi-Agent Workspace

{{PROJECT_DESCRIPTION}}

**Founder:** {{FOUNDER}}. **Team:** AI agents. **Phase:** {{PHASE}}.

This file is automatically injected into every GitHub Copilot context in this workspace.

---

## Workspace structure

```
~/{{PROJECT_NAME}}/
├── code/{{PROJECT_NAME}}/   ← main code repo (has its own CLAUDE.md)
├── docs/
│   ├── process.md           ← pipeline operational manual (source of truth)
│   ├── role-capabilities.md ← role capability index
│   └── workflows/           ← pipeline specs (WORKFLOW-*.md)
├── .github/
│   └── copilot-instructions.md  ← this file
```

## How to work in this repo

**Always read:**
- This file (already loaded)
- `code/{{PROJECT_NAME}}/CLAUDE.md` — architecture, build, tests (when working with code)

**Operating modes:**

| If asked to... | Mode | What to do |
|----------------|------|------------|
| "Act as orchestrator / run \<task\>" | **Orchestrator** | Read `docs/process.md`, execute pipeline |
| "You are the \[role\]" | **Single Expert** | Answer as that role, no pipeline |
| General question | **Advisor** | Help, suggest next step |

## Multi-agent pipeline

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

5 types: `feature`, `bugfix` (skip PLAN), `infra`, `content`, `spike` (PLAN only).

Each step has a gate: `APPROVED` / `NEEDS WORK (reason)`. Retry: 3× → helper → 2× → human escalation.

## Orchestrator role

When acting as **Orchestrator**:
- **Never write code** — always delegate to the appropriate specialist role
- Select roles dynamically from `docs/role-capabilities.md` — don't hardcode assignments
- Validate each step before proceeding
- Deliver work as a PR (never merge directly)

### Escalate to human only for:
- Public-facing content approval
- API-breaking architecture decisions
- Infrastructure with cost impact
- 5+ consecutive failures

## Specialist roles

| Layer | Roles |
|-------|-------|
| Strategy | product-manager, product-trend-researcher |
| Management | orchestrator, testing-reality-checker, specialized-workflow-architect |
| Engineering | engineering-software-architect, engineering-backend-architect, engineering-frontend-developer, engineering-code-reviewer, engineering-devops-automator, engineering-security-engineer |
| Design | design-ux-researcher, design-ui-designer |
| GTM | specialized-developer-advocate, engineering-technical-writer, marketing-content-creator |

Full capability index: `docs/role-capabilities.md`.

## Rules

- All code changes go in `code/{{PROJECT_NAME}}/`
- Read `code/{{PROJECT_NAME}}/CLAUDE.md` before touching code
- Simple solution > complex. Working first, pretty later.
- Git: `git status` before commit, never `git init` in an existing repo
- Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`

## Backlog

GitHub Project in org `{{GITHUB_ORG}}`. Issues in `{{GITHUB_ORG}}/{{GITHUB_REPO}}`.
