# {{PROJECT_NAME}} — Business Workspace

{{PROJECT_DESCRIPTION}}

**Founder:** {{FOUNDER}}. **Team:** AI agents. **Phase:** {{PHASE}}.

This file is always loaded by Windsurf as workspace context.

---

## How to start

Determine your mode each session:

| If asked to... | Mode | What to do |
|----------------|------|------------|
| "Act as orchestrator / run \<task\>" | **Orchestrator** | Read `docs/process.md`, then execute |
| "You are the \[role\]" | **Single Expert** | Answer as that role, no pipeline |
| General question | **Advisor** | Help, suggest next step |

## Context to load

**Always read:** This file (already loaded).

**If task involves code:** `code/{{PROJECT_NAME}}/CLAUDE.md` — architecture, build, tests.

**As orchestrator:**
- `docs/process.md` — pipeline operational manual (source of truth)
- `docs/role-capabilities.md` — capability index for dynamic role selection

## Workspace structure

```
~/{{PROJECT_NAME}}/
├── code/{{PROJECT_NAME}}/   ← main code repo (has its own CLAUDE.md)
├── docs/
│   ├── process.md           ← pipeline operational manual
│   ├── role-capabilities.md ← role capability index
│   └── workflows/           ← pipeline specs (WORKFLOW-*.md)
├── .windsurf/rules/         ← Windsurf rules (this file + orchestrator.md)
```

## Pipeline summary

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

5 types: `feature`, `bugfix` (skip PLAN), `infra`, `content`, `spike` (PLAN only).

Gate at each step: `APPROVED` / `NEEDS WORK (reason)`.
Retry: 3× → helper role → 2× → human escalation.

## Roles

| Layer | Roles |
|-------|-------|
| Strategy | product-manager, product-trend-researcher |
| Management | orchestrator, testing-reality-checker, specialized-workflow-architect |
| Engineering | engineering-software-architect, engineering-backend-architect, engineering-frontend-developer, engineering-code-reviewer, engineering-devops-automator, engineering-security-engineer |
| Design | design-ux-researcher, design-ui-designer |
| GTM | specialized-developer-advocate, engineering-technical-writer, marketing-content-creator |

Roles selected dynamically via `docs/role-capabilities.md`.

## Rules

- All code changes go in `code/{{PROJECT_NAME}}/`
- Read `code/{{PROJECT_NAME}}/CLAUDE.md` before touching code
- Simple solution > complex. Working first, pretty later.
- Git: `git status` before commit, never `git init` in existing repo

## Backlog

GitHub Project in org `{{GITHUB_ORG}}`. Issues in `{{GITHUB_ORG}}/{{GITHUB_REPO}}`.
