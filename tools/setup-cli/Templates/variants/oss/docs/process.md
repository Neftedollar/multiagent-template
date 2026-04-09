# Operational Process

> **Template**: Open Source (OSS). Public codebase — every commit is potentially reviewed by the community. Optimize for clarity, contributor experience, and backward compatibility.

> **MCPs are optional.** If you haven't run `install-mcps`, skip all graph/Cypher sections below.
> Use GitHub Issues as your backlog. The pipeline works without AGE or O'Brien.

> **Process v1**. Source of truth: AGE graph `{{GRAPH_NAME}}` + workflow specs.

## Source of truth

| What | Where |
|------|-------|
| Pipelines, Steps, Roles | AGE graph `{{GRAPH_NAME}}` (Pipeline→HAS_STEP→Step→PERFORMED_BY→Role) |
| Modules, code dependencies | AGE graph `{{GRAPH_NAME}}` (Module→DEPENDS_ON→Module, Module→BELONGS_TO→Repo) |
| Security findings, Code insights | AGE graph `{{GRAPH_NAME}}` (SecurityFinding, CodeInsight nodes) |
| Role capabilities (extended) | `docs/role-capabilities.md` |
| Workflow specs | `docs/workflows/REGISTRY.md` → individual WORKFLOW-*.md |
| Issues, dependencies | GitHub Project + graph (issue→DEPENDS_ON) <!-- requires AGE MCP --> |
| Coordination | O'Brien (active-work, bugs, suggestions) <!-- requires AGE MCP --> |

**Pipeline**: PLAN → BUILD → TEST → VERIFY → SHIP (5 steps, 5 pipelines: feature, bugfix, infra, content, spike).

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
| Execution | sonnet | Coder, Frontend, DevOps, Tech Writer, Marketing, Designer |
| Validation | opus | Code Reviewer, Reality Checker |
| Routine | haiku | Data gathering, formatting, lookups |

---

## CEO control points

CEO **does not participate** in operations. Needed only when:

| Situation | How to notify |
|-----------|---------------|
| Ambiguous classification | `needs-ceo` Issue |
| Public content | PR with `needs-ceo-review` |
| Breaking API change | `needs-ceo` Issue |
| Infra decisions with costs | `needs-ceo` Issue |
| 5+ failures (3 role + 2 helper) | `needs-ceo` Issue with dossier |

---

## Dynamic role selection

Orchestrator selects roles dynamically via `docs/role-capabilities.md` + graph:

1. **Signals**: labels, files, keywords, task domain
2. **Match**: signals → capability index + graph → Primary + Secondary roles
3. **Composition**: sequential (A → B), parallel (A + B), or composite prompt
4. **Fallback**: if no role fits → create ad-hoc role

---

## Helper mechanism on blockers

3 retry → helper → 2 retry with recommendation → CEO escalation.

Helper MUST NOT be the same role that failed.

**CEO escalation**: create GitHub Issue with `needs-ceo` label. Orchestrator does not block — moves to next task.

---

## Operational rules

### Worktree isolation — required for code

All agents modifying code **must** work in a git worktree:

```bash
git worktree add ../<project>-wt-<issue> -b <branch-name> main
```

Each bugfix/feature creates its own worktree. After merge — `git worktree remove`.

### RED CI — fix before new PR

CI RED on PR → fix **in the same PR**. Max 3 attempts, then helper.

---

## Task-level locking

Optimistic lock via O'Brien to prevent two orchestrators from taking the same task:

```
1. o-brien.search(tags: ["active-work", "issue-NNN"]) → if found → SKIP
2. o-brien.store(content: "LOCK: issue #NNN", tags: ["active-work", "issue-NNN", "lock"])
3. Wait 2 sec → re-check → if >1 records → race → delete own lock, SKIP
4. Otherwise → task locked, continue
```

---

## Autonomous run

```bash
claude -p "/orchestrator Take next P0+P1 tasks from backlog. \
  Load process from graph {{GRAPH_NAME}}. Execute autonomously." \
  --allowedTools "Bash,Read,Edit,Write,Agent,Glob,Grep,Skill" \
  --max-turns 100
```

Morning CEO check:
```bash
gh issue list --label needs-ceo --repo {{GITHUB_ORG}}/{{GITHUB_REPO}}
gh pr list --repo {{GITHUB_ORG}}/{{GITHUB_REPO}}
```

---

## OSS-specific rules

### CHANGELOG — required in SHIP

Every SHIP step must include an update to `CHANGELOG.md` following [Keep a Changelog](https://keepachangelog.com) format:
- `Added` — new features
- `Changed` — changes to existing functionality
- `Deprecated` — soon-to-be removed features
- `Removed` — removed features
- `Fixed` — bug fixes
- `Security` — vulnerabilities fixed

### Semantic versioning — VERIFY gate

VERIFY must classify the change and confirm version bump intent:
- Breaking public API change → major version bump required (`needs-ceo`)
- New public API (backward-compatible) → minor version bump
- Bug fixes, docs, internal changes → patch version bump

### Backward compatibility — escalation required

Breaking changes require CEO escalation (`needs-ceo`) and must include:
1. Clear description of what breaks and why
2. Migration guide in `docs/` or PR description
3. Deprecation notice in the previous release (if feasible)

### Contributor-friendly output

- PR titles and commit messages must be clear enough for external contributors
- PR descriptions must explain **why** the change is made, not just what
- New public APIs must include docstrings or inline documentation
- `/engineering-technical-writer` must review docs in the VERIFY step for any user-facing change

### Security disclosure

Security issues reported via GitHub Security Advisories must bypass the normal pipeline:
→ fix in private fork → coordinated disclosure → release → public announcement.
Assign `/engineering-security-engineer` immediately. Escalate to CEO.
