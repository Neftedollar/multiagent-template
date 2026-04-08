# Example: SaaS Starter

Building a new SaaS product from scratch? This shows a typical first-week orchestrator session.

## Setup

```bash
multiagent-setup new MySaaS my-github-org --provider claude
cd MySaaS
git clone https://github.com/my-github-org/my-saas-repo code/MySaaS
claude
```

## Session 1: Foundation

```
/orchestrator Set up the project foundation: Next.js frontend, FastAPI backend, PostgreSQL. 
Auth via OAuth2 (Google + GitHub). Deploy target: Railway.
```

**What happens:**
1. `/product-manager` writes an EPIC with acceptance criteria
2. `/engineering-software-architect` defines the tech stack and module boundaries
3. `/engineering-backend-architect` scaffolds FastAPI with auth endpoints
4. `/engineering-frontend-developer` scaffolds Next.js with OAuth flow
5. `/engineering-devops-automator` adds Railway config + GitHub Actions CI
6. `/engineering-code-reviewer` validates all changes
7. PR created: `feat/foundation`

**Human action:** Review and merge the PR (5 min).

## Session 2: First Feature

```
/orchestrator Implement subscription billing with Stripe. 
Free tier: 3 projects. Pro: $19/mo unlimited. 
```

The orchestrator picks up from where session 1 left off (reads graph knowledge from AGE), adds billing models, Stripe webhook handlers, billing UI, and creates a PR.

## Session 3: Autonomous Mode

```bash
# Add tasks to the GitHub Project backlog, then:
claude -p "/orchestrator"
```

The orchestrator picks tasks from the backlog, executes them, and reports back. You check in at your convenience.

---

## Key files in this workspace

```
MySaaS/
├── CLAUDE.md           <- add your product context here (mission, stack, rules)
├── docs/
│   ├── process.md      <- pipeline config (retry counts, escalation rules)
│   └── workflows/      <- per-pipeline specs
└── .claude/commands/   <- 20+ specialist roles, ready to use
```

## Tips

- Update `CLAUDE.md` with your tech stack specifics — the orchestrator reads it on every session
- Add a `## Architecture` section to `CLAUDE.md` describing your module boundaries
- Use `/engineering-security-engineer` before any auth/payment work
- Use `/testing-reality-checker` when something feels off
