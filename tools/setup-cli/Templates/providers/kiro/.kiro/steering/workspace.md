---
inclusion: always
---

# {{PROJECT_NAME}} — Business Workspace

{{PROJECT_DESCRIPTION}}

**Founder:** {{FOUNDER}}. **Team:** AI agents. **Phase:** {{PHASE}}.

---

## How to start

Determine your mode each session:

| If asked to... | Mode | What to do |
|----------------|------|------------|
| "Act as orchestrator / run \<task\>" | **Orchestrator** | Read `docs/process.md`, execute pipeline |
| "You are the \[role\]" | **Single Expert** | Answer as that role, no pipeline |
| General question | **Advisor** | Help, suggest next step |

## Context to load

**Always read:** This file (already loaded).

**If task involves code:** `code/{{PROJECT_NAME}}/CLAUDE.md` — architecture, build, tests.

**If orchestrator:** `docs/process.md` — operational manual (pipelines, gates, escalation).

## Workspace structure

```
~/{{PROJECT_NAME}}/
├── code/
│   └── {{PROJECT_NAME}}/   ← main code repo
├── docs/
│   ├── process.md           ← pipeline source of truth
│   ├── role-capabilities.md ← capability index for role selection
│   └── workflows/           ← workflow specs
├── .kiro/
│   └── steering/            ← Kiro steering documents (always loaded)
```

## Pipeline (summary)

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

5 pipeline types: feature, bugfix (skip PLAN), infra, content, spike (PLAN only).

Each step has a gate: `APPROVED` / `NEEDS WORK (reason)`. Retry: 3× → helper → 2× → CEO escalation.

## Rules

- **Confirm intent**: on ambiguous requests — clarify before acting.
- **Code**: all code changes in `code/{{PROJECT_NAME}}/`, read its CLAUDE.md.
- **Don't overengineer**: simple solution > complex. Working first, pretty later.
- **Git discipline**: `git status` before commit, never `git init` in existing repo.

## Backlog

GitHub Project in org `{{GITHUB_ORG}}`. Issues in `{{GITHUB_ORG}}/{{GITHUB_REPO}}`.
