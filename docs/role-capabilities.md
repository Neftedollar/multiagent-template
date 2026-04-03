# Role Capability Index

> Orchestrator uses this index to match roles to tasks.
> Update when adding new roles or discovering new capabilities.

## Mapping: domain → roles (priority order)

### Backend / System Design
| Capability | Primary | Secondary |
|------------|---------|-----------|
| System architecture, ADR, trade-offs | `/engineering-software-architect` | `/engineering-backend-architect` |
| API design, REST/gRPC/GraphQL | `/engineering-backend-architect` | `/engineering-software-architect` |
| SQL, DB schema, migrations, indexes | `/engineering-database-optimizer` | `/engineering-backend-architect` |
| Caching, performance | `/engineering-database-optimizer` | `/engineering-backend-architect` |
| Data pipelines, ETL | `/engineering-data-engineer` | `/engineering-backend-architect` |

### Frontend / UI
| Capability | Primary | Secondary |
|------------|---------|-----------|
| User flows, interaction patterns | `/design-ux-researcher` | `/design-ui-designer` |
| Layout, visual spec, responsive | `/design-ui-designer` | `/engineering-frontend-developer` |
| UI implementation | `/engineering-frontend-developer` | `/engineering-senior-developer` |
| Design system, components | `/design-ui-designer` | `/engineering-frontend-developer` |

### Security
| Capability | Primary | Secondary |
|------------|---------|-----------|
| Auth, authz, OWASP, policy model | `/engineering-security-engineer` | `/engineering-backend-architect` |
| Threat modeling, compliance | `/engineering-security-engineer` | `/compliance-auditor` |

### Infrastructure
| Capability | Primary | Secondary |
|------------|---------|-----------|
| CI/CD, Docker, deployment | `/engineering-devops-automator` | `/engineering-sre` |
| SLO/SLI, monitoring, incident | `/engineering-sre` | `/engineering-devops-automator` |

### Quality
| Capability | Primary | Secondary |
|------------|---------|-----------|
| Code review, quality patterns | `/engineering-code-reviewer` | `/engineering-software-architect` |
| E2E testing, visual evidence | `/testing-evidence-collector` | `/testing-reality-checker` |
| Production readiness | `/testing-reality-checker` | `/engineering-sre` |

### Documentation
| Capability | Primary | Secondary |
|------------|---------|-----------|
| API docs, tutorials, README | `/engineering-technical-writer` | `/specialized-developer-advocate` |

### Product / Strategy
| Capability | Primary | Secondary |
|------------|---------|-----------|
| Scope, acceptance criteria | `/product-manager` | `/product-sprint-prioritizer` |
| Market research, competitors | `/product-trend-researcher` | `/product-manager` |
| Process design | `/specialized-workflow-architect` | `/testing-workflow-optimizer` |

---

## Signals for role selection

### By file patterns
| Pattern | Role |
|---------|------|
| `*.css`, `*.js`, `*.tsx`, `*.html` | `/engineering-frontend-developer` |
| `*.html`, `*.css`, `*.tsx`, `*.jsx`, `*.vue`, `*.blade.php`, `templates/` | `/testing-evidence-collector` (at TEST step) |
| `Dockerfile`, `docker-compose*`, `.github/`, `scripts/` | `/engineering-devops-automator` |
| `*.sql`, `*Migration*` | `/engineering-database-optimizer` |
| `*.md` in `docs/` | `/engineering-technical-writer` |

### By keywords in task description
| Keywords | Role |
|----------|------|
| deploy, ci, cd, docker, pipeline | `/engineering-devops-automator` |
| query, index, performance, cache | `/engineering-database-optimizer` |
| auth, policy, security, rbac | `/engineering-security-engineer` |
| dashboard, ui, page, component, form, layout | `/engineering-frontend-developer` |
| dashboard, ui, page, component, form, layout, landing, website, onboarding, flow | `/design-ux-researcher` + `/design-ui-designer` (at PLAN step) |
| dashboard, ui, page, component, form, layout, landing, website | `/testing-evidence-collector` (at TEST step) |
| browser, e2e, visual, screenshot, responsive, mobile view | `/testing-evidence-collector` |
| slo, monitoring, alert, incident | `/engineering-sre` |

### Conditional roles at PLAN step

| Condition | Roles added to PLAN | Sequence |
|-----------|-------------------|----------|
| Task keywords match UI signals (see above) | `/design-ux-researcher` → `/design-ui-designer` | After PM, before Architect |
| Task touches UI files (from issue or description) | `/design-ux-researcher` → `/design-ui-designer` | After PM, before Architect |

UX Researcher outputs user flows and screen states. UI Designer outputs layout spec and responsive notes. Architect receives both as input.

### Conditional roles at TEST step

| Condition | Role added to TEST | Tool |
|-----------|-------------------|------|
| BUILD changed UI files (`*.html`, `*.css`, `*.tsx`, `*.jsx`, `*.vue`, `*.blade.php`) | `/testing-evidence-collector` | Playwright MCP |
| BUILD changed API routes | `/testing-api-tester` | Bash (curl/httpie) |

Evidence Collector uses Playwright MCP to navigate pages, click elements, fill forms, and take screenshots. Requires the app to be running (dev server).

### Combined tasks

If a task spans multiple domains, the orchestrator can:
1. **Sequential**: architect → developer (if one depends on the other)
2. **Parallel**: frontend + backend (if no data dependency)
3. **Composite**: create a merged prompt from two roles (see Ad-Hoc Role Creation)
