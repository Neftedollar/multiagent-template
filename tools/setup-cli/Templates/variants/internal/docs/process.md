# Operational Process

> **Template**: Internal Tools. Used by your team only — optimize for speed over ceremony. Lightweight pipeline with relaxed gates.

> **MCPs are optional.** If you haven't run `install-mcps`, skip all graph/Cypher sections below.
> Use GitHub Issues as your backlog. The pipeline works without AGE or O'Brien.

> **Process v1**. Source of truth: AGE graph `{{GRAPH_NAME}}` + workflow specs.

## Source of truth

| What | Where |
|------|-------|
| Pipelines, Steps, Roles | AGE graph `{{GRAPH_NAME}}` |
| Role capabilities | `docs/role-capabilities.md` |
| Workflow specs | `docs/workflows/REGISTRY.md` |
| Issues | GitHub Project `{{GITHUB_ORG}}/{{GITHUB_REPO}}` |

**Pipeline**: PLAN → BUILD → TEST → VERIFY → SHIP. Internal tools may skip PLAN and VERIFY for small changes (see rules below).

---

## Three entry points

| Mode | Launch | Description |
|------|--------|-------------|
| CEO Mode | `/orchestrator <task>` | CEO gives a specific task |
| Single Expert | `/<role> <question>` | Direct role call, no pipeline |
| Autonomous | `claude -p "/orchestrator ..." --max-turns 50` | Headless, from backlog |

---

## Models by role

| Tier | Model | Roles |
|------|-------|-------|
| Strategic | opus | PM, Architects, Security, Orchestrator |
| Execution | sonnet | Coder, Frontend, DevOps, Tech Writer |
| Validation | opus | Code Reviewer, Reality Checker |
| Routine | haiku | Data gathering, formatting, lookups |

---

## CEO control points

CEO **does not participate** in day-to-day operations. Escalate only for:

| Situation | How to notify |
|-----------|---------------|
| New infrastructure cost >$100/month | `needs-ceo` Issue |
| Data deletion affecting >1000 records | `needs-ceo` Issue |
| External integrations or API keys | `needs-ceo` Issue |
| 5+ failures on a single step | `needs-ceo` Issue with dossier |

No escalation needed for: design decisions, architecture choices, feature scope, minor performance changes.

---

## Dynamic role selection

Orchestrator selects roles dynamically via `docs/role-capabilities.md`:

1. **Signals**: labels, files, keywords, task domain
2. **Match**: signals → capability index → Primary + Secondary roles
3. **Fallback**: if no role fits → create ad-hoc role

---

## Helper mechanism on blockers

3 retry → helper → 2 retry → CEO escalation (for escalation criteria above only).

---

## Simplified pipeline for internal tools

| Pipeline type | Required steps | Steps you may skip |
|---------------|---------------|-------------------|
| `feature` | BUILD → TEST → SHIP | PLAN (for <1 day tasks), VERIFY (for low-risk changes) |
| `bugfix` | BUILD → SHIP | TEST (for trivial fixes), VERIFY |
| `infra` | BUILD → SHIP | PLAN, VERIFY |
| `content` | BUILD → SHIP | PLAN, VERIFY |
| `spike` | PLAN | — |

**When to keep VERIFY**: changes to shared infrastructure, auth/permissions, data pipelines, or anything used by >5 people regularly.

---

## Operational rules

### Worktree isolation

All agents modifying code **must** work in a git worktree:

```bash
git worktree add ../<project>-wt-<issue> -b <branch-name> main
```

### RED CI — fix before new PR

CI RED on PR → fix **in the same PR**. Max 3 attempts, then helper.

---

## Task-level locking

Optimistic lock via O'Brien to prevent two orchestrators from taking the same task:

```
1. o-brien.search(tags: ["active-work", "issue-NNN"]) → if found → SKIP
2. o-brien.store(content: "LOCK: issue #NNN", tags: ["active-work", "issue-NNN", "lock"])
3. Wait 2 sec → re-check → if >1 records → delete own lock, SKIP
4. Otherwise → task locked, continue
```

---

## Autonomous run

```bash
claude -p "/orchestrator Take next P0+P1 tasks from backlog. \
  Execute autonomously." \
  --allowedTools "Bash,Read,Edit,Write,Agent,Glob,Grep,Skill" \
  --max-turns 100
```

Morning check:
```bash
gh issue list --label needs-ceo --repo {{GITHUB_ORG}}/{{GITHUB_REPO}}
```

---

## Internal tools rules

### Pragmatic quality bar

Code quality standard: working and maintainable > perfect. Internal tools that work reliably beat beautifully designed tools that take 3x longer to build.

- Skip formal design docs for tasks under 1 day
- README is sufficient documentation for most internal tools
- Tests are important for anything that runs on a schedule or handles data

### Access and secrets

All secrets must go in environment variables or a secrets manager (never hardcoded). Internal does not mean insecure — log access, track who uses what.

### Deployment

Prefer simple deployment: single binary, Docker Compose, or a cron job over complex orchestration. The person on-call should be able to deploy a fix in under 10 minutes.
