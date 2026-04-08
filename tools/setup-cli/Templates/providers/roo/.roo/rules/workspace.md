# {{PROJECT_NAME}} — Multi-Agent Workspace

{{PROJECT_DESCRIPTION}}

**Founder:** {{FOUNDER}}. **Phase:** {{PHASE}}.

---

## How to start

### Determine mode

| Request type | What to do |
|---|---|
| Need full pipeline | Switch to **Orchestrator** mode → describe the task |
| Need expert answer | Switch to **Architect** or **Code** mode → ask your question |
| Quick question | Stay in current mode → ask directly |

### Load context (all modes)

- This file is auto-loaded by Roo Code from `.roo/rules/`
- For code work: read `code/{{PROJECT_NAME}}/` (own CLAUDE.md inside)
- For pipeline: read `docs/process.md` — operational manual

---

## Workspace structure

```
{{PROJECT_NAME}}/
├── CLAUDE.md               ← workspace overview
├── code/{{PROJECT_NAME}}/  ← main code repo
├── docs/
│   ├── process.md          ← pipeline rules (PLAN→BUILD→TEST→VERIFY→SHIP)
│   ├── role-capabilities.md ← specialist roles index
│   └── workflows/          ← workflow specs
└── .roo/
    └── rules/              ← Roo Code rules (this file)
```

## Orchestrator pipeline

The `/orchestrator` command runs the full multi-agent pipeline:

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

5 types: **feature** (full), **bugfix** (skip PLAN), **infra**, **content**, **spike** (PLAN only).

Each step has a gate: `APPROVED` / `NEEDS WORK`. Retry: 3× → helper → 2× → CEO escalation.

## Agent roles (specialist modes)

Use these with Roo Code's custom modes or as prompts:

| Domain | Role |
|---|---|
| Strategy | Product Manager, Trend Researcher |
| Architecture | Software Architect, Backend Architect |
| Engineering | Frontend Developer, Code Reviewer, DevOps, Security |
| Design | UX Researcher, UI Designer |
| GTM | Dev Advocate, Tech Writer, Marketing |

Full capability index: `docs/role-capabilities.md`

## Rules

- **Code changes**: work only in `code/{{PROJECT_NAME}}/`
- **Git**: `git status` before every commit; never `git init` in existing repo
- **Confirm intent**: clarify ambiguous requests before acting
- **Don't overengineer**: working solution > elegant solution

## GitHub

Backlog: `{{GITHUB_ORG}}/{{GITHUB_REPO}}`
