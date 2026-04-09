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

### AI / LLM
| Capability | Primary | Secondary |
|------------|---------|-----------|
| LLM integration, RAG, embeddings, prompt engineering | `/engineering-ai-engineer` | `/engineering-backend-architect` |
| ML model development, training, evaluation | `/engineering-ai-engineer` | `/engineering-data-engineer` |
| AI feature architecture, system design with AI | `/engineering-software-architect` | `/engineering-ai-engineer` |
| MLOps, model deployment, inference API, monitoring | `/engineering-devops-automator` | `/engineering-ai-engineer` |
| AI security: prompt injection, data leakage, model safety | `/engineering-security-engineer` | `/engineering-ai-engineer` |
| AI output validation, hallucination checks | `/testing-reality-checker` | `/engineering-ai-engineer` |

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
| Performance review, bottlenecks | `/testing-performance-benchmarker` | `/engineering-backend-architect` |
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

### Agent Prompt Engineering
| Capability | Primary | Secondary |
|------------|---------|-----------|
| System prompts, role definitions, slash commands, agentic pipeline design | `/engineering-agent-prompt-engineer` | `/engineering-ai-engineer` |

---

## Default role per pipeline step

Use these when no specific signal matches. Always run the default role; conditional roles add to it.

| Step   | Default role(s) | Notes |
|--------|-----------------|-------|
| PLAN   | `/product-manager` | + `/engineering-software-architect` if task involves code changes |
| BUILD  | determined by domain mapping above | no single default — must match signals |
| TEST   | `/testing-reality-checker` | checks that the feature works end-to-end as intended |
| VERIFY | `/testing-reality-checker` + `/engineering-code-reviewer` | both always run — reality checker validates end-to-end correctness, code reviewer checks code quality + patterns; + conditional roles below based on what BUILD touched |
| SHIP   | orchestrator (no agent role) | git branch, commit, PR creation — orchestrator does this directly |

**VERIFY is never empty.** Minimum: `/testing-reality-checker` + `/engineering-code-reviewer`. Always both.

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
| `*.ipynb`, `*.onnx`, `*.pt`, `*.pth`, `*.pkl`, `*.safetensors` | `/engineering-ai-engineer` |
| `models/`, `embeddings/`, `vectorstore/`, `prompts/`, `chains/` | `/engineering-ai-engineer` |
| `*llm*`, `*rag*`, `*embedding*`, `*openai*`, `*anthropic*`, `*langchain*` | `/engineering-ai-engineer` |
| `*onnx*`, `*mlnet*`, `*ml.net*`, `*torchsharp*`, `*tensorflow*` | `/engineering-ai-engineer` |

### By keywords in task description
| Keywords | Role |
|----------|------|
| deploy, ci, cd, docker, pipeline | `/engineering-devops-automator` |
| query, index, cache | `/engineering-database-optimizer` |
| performance, bottleneck, slow, latency, n+1, memory leak, bundle size, optimize | `/testing-performance-benchmarker` (at VERIFY step) |
| auth, policy, security, rbac | `/engineering-security-engineer` |
| dashboard, ui, page, component, form, layout | `/engineering-frontend-developer` |
| dashboard, ui, page, component, form, layout, landing, website, onboarding, flow | `/design-ux-researcher` + `/design-ui-designer` (at PLAN step) |
| dashboard, ui, page, component, form, layout, landing, website | `/testing-evidence-collector` (at TEST step) |
| browser, e2e, visual, screenshot, responsive, mobile view | `/testing-evidence-collector` |
| slo, monitoring, alert, incident | `/engineering-sre` |
| llm, rag, embedding, vector, prompt, inference, fine-tune, ai agent, openai, anthropic, langchain, huggingface, claude api, gpt, gemini, model, ml, machine learning, recommendation, classification | `/engineering-ai-engineer` |
| onnx, ml.net, mlnet, torchsharp, tensorflow.net, microsoft.ml, semantic kernel, kernel memory, microsoft.extensions.ai | `/engineering-ai-engineer` |

### Conditional roles at PLAN step

| Condition | Roles added to PLAN | Sequence |
|-----------|-------------------|----------|
| Task keywords match UI signals (see above) | `/design-ux-researcher` → `/design-ui-designer` | After PM, before Architect |
| Task touches UI files (from issue or description) | `/design-ux-researcher` → `/design-ui-designer` | After PM, before Architect |
| Task keywords match AI signals (see above) | `/engineering-ai-engineer` | After Architect — validates model/API choice, estimates cost and latency, flags feasibility risks |

UX Researcher outputs user flows and screen states. UI Designer outputs layout spec and responsive notes. Architect receives both as input.

AI Engineer at PLAN: reviews the proposed architecture for AI components, selects model/API, estimates token cost and inference latency, identifies data privacy constraints, and flags risks (hallucination, non-determinism, vendor lock-in).

### Conditional roles at VERIFY step

| Condition | Role added to VERIFY | Focus |
|-----------|---------------------|-------|
| BUILD changed backend code (routes, queries, services) | `/testing-performance-benchmarker` | N+1 queries, unoptimized loops, missing indexes, memory allocation |
| BUILD changed frontend code (components, pages, bundles) | `/testing-performance-benchmarker` | Bundle size, render performance, unnecessary re-renders, lazy loading |
| Task keywords match performance signals (see above) | `/testing-performance-benchmarker` | Full performance audit |
| BUILD touched AI/LLM code (see file/keyword signals) | `/engineering-ai-engineer` | Model behavior, prompt quality, hallucination risk, cost per call, output determinism |
| BUILD touched AI/LLM code | `/engineering-security-engineer` | Prompt injection, data leakage through context, PII in prompts/responses, model output sanitization |

Performance Benchmarker reviews code for bottlenecks, runs parallel with Code Reviewer + Security Engineer.

AI Engineer at VERIFY: checks that prompts are robust (no injection vectors), outputs are validated before use, error handling covers model failures, cost per call is within budget, and the feature degrades gracefully when the model returns unexpected output.

### Conditional roles at TEST step

| Condition | Role added to TEST | Tool |
|-----------|-------------------|------|
| BUILD changed UI files (`*.html`, `*.css`, `*.tsx`, `*.jsx`, `*.vue`, `*.blade.php`) | `/testing-evidence-collector` | Playwright MCP |
| BUILD changed API routes | `/testing-api-tester` | Bash (curl/httpie) |
| BUILD touched AI/LLM code | `/testing-reality-checker` | Runs the feature end-to-end, checks outputs are coherent and non-empty, verifies graceful degradation on model error |

Evidence Collector uses Playwright MCP to navigate pages, click elements, fill forms, and take screenshots. Requires the app to be running (dev server).

Reality Checker at TEST for AI: calls the AI feature with real inputs (including edge cases and adversarial prompts), verifies that responses are non-empty, coherent, and safe, and confirms that model errors (timeout, rate limit, invalid response) are handled without crashing the app.

### Combined tasks

If a task spans multiple domains, the orchestrator can:
1. **Sequential**: architect → developer (if one depends on the other)
2. **Parallel**: frontend + backend (if no data dependency)
3. **Composite**: create a merged prompt from two roles (see Ad-Hoc Role Creation)

---

## Project focus → standing roles

Orchestrator reads `CLAUDE.md` at startup and derives standing roles — roles that participate in every relevant pipeline step for this project, regardless of individual task signals.

| Focus keyword in CLAUDE.md | Standing roles |
|-----------------------------|----------------|
| `ui`, `frontend`, `design`, `UX`, `landing`, `dashboard` | `/design-ux-researcher` (PLAN), `/testing-evidence-collector` (TEST), `/testing-accessibility-auditor` (VERIFY) |
| `security`, `auth`, `compliance`, `GDPR`, `SOC2`, `zero-trust` | `/engineering-security-engineer` (PLAN + VERIFY), `/compliance-auditor` (VERIFY) |
| `performance`, `SLO`, `SLI`, `latency`, `scale` | `/testing-performance-benchmarker` (VERIFY), `/engineering-sre` (VERIFY) |
| `AI`, `LLM`, `ML`, `RAG`, `embeddings`, `Claude`, `GPT`, `Gemini` | `/engineering-ai-engineer` (PLAN + BUILD + VERIFY) |
| `mobile`, `iOS`, `Android`, `React Native`, `Flutter` | `/engineering-mobile-app-builder` (BUILD) |
| `blockchain`, `smart contract`, `Web3`, `DeFi` | `/engineering-solidity-smart-contract-engineer` (BUILD), `/blockchain-security-auditor` (VERIFY) |

Standing roles are additive — they combine with default step roles and task-specific conditional roles.

---

## Vetted roles for software pipeline

The global `~/.claude/commands/` directory contains ~150 roles. Most are domain-specific (game engines, Chinese social media, academic research, etc.) and **not relevant** for a software product pipeline.

Orchestrator should prioritize roles from this vetted list. For anything outside it — create an ad-hoc role via `/engineering-agent-prompt-engineer`.

### Strategy & Product
| Role | Use when |
|------|----------|
| `/product-manager` | PLAN: scope, acceptance criteria, user stories |
| `/product-sprint-prioritizer` | PLAN: backlog prioritization, scope negotiation |
| `/product-trend-researcher` | Spike: market research, competitive analysis |
| `/specialized-workflow-architect` | PLAN: process design, pipeline specs |

### Architecture & Backend
| Role | Use when |
|------|----------|
| `/engineering-software-architect` | PLAN: system design, ADRs, trade-offs |
| `/engineering-backend-architect` | BUILD: API design, service architecture |
| `/engineering-database-optimizer` | BUILD/VERIFY: schema, queries, indexes |
| `/engineering-data-engineer` | BUILD: data pipelines, ETL, warehousing |
| `/engineering-senior-developer` | BUILD: complex implementation, cross-cutting concerns |

### Frontend & Design
| Role | Use when |
|------|----------|
| `/design-ux-researcher` | PLAN: user flows, interaction design |
| `/design-ui-designer` | PLAN: layouts, design system, responsive |
| `/design-ux-architect` | PLAN: large-scale UX systems |
| `/engineering-frontend-developer` | BUILD: UI implementation |

### AI / LLM
| Role | Use when |
|------|----------|
| `/engineering-ai-engineer` | PLAN/BUILD/VERIFY: LLM integration, RAG, embeddings, prompt engineering |
| `/engineering-agent-prompt-engineer` | Ad-hoc: new role creation, prompt quality review |
| `/specialized-mcp-builder` | BUILD: MCP server/tool development |
| `/engineering-autonomous-optimization-architect` | BUILD: self-improving agent systems |
| `/agentic-identity-trust` | BUILD/VERIFY: agent auth, trust, identity |
| `/backend-architect-with-memory` | BUILD: memory systems for agents |
| `/automation-governance-architect` | VERIFY: governance, audit trails, compliance for automated systems |

### Infrastructure & Reliability
| Role | Use when |
|------|----------|
| `/engineering-devops-automator` | BUILD: CI/CD, Docker, deployment |
| `/engineering-sre` | VERIFY: SLO/SLI, monitoring, incident response |
| `/engineering-incident-response-commander` | Incident: on-call, postmortem |
| `/engineering-git-workflow-master` | BUILD: branch strategy, merge conflicts |

### Security & Compliance
| Role | Use when |
|------|----------|
| `/engineering-security-engineer` | PLAN/VERIFY: auth, OWASP, threat modeling |
| `/engineering-threat-detection-engineer` | VERIFY: advanced threat analysis |
| `/compliance-auditor` | VERIFY: regulatory, policy review |
| `/blockchain-security-auditor` | VERIFY: smart contract audits (blockchain projects only) |

### Quality & Testing
| Role | Use when |
|------|----------|
| `/testing-reality-checker` | VERIFY (default): production readiness, end-to-end validation |
| `/engineering-code-reviewer` | VERIFY (default): code quality, patterns, tech debt |
| `/testing-evidence-collector` | TEST: Playwright E2E, visual evidence (UI projects) |
| `/testing-api-tester` | TEST: API contract testing |
| `/testing-performance-benchmarker` | VERIFY: N+1, bundle size, latency (performance projects) |
| `/testing-accessibility-auditor` | VERIFY: WCAG, a11y (UI projects) |
| `/testing-workflow-optimizer` | Retro: pipeline efficiency analysis |
| `/specialized-model-qa` | VERIFY: AI model output quality, eval harness |

### Documentation & Advocacy
| Role | Use when |
|------|----------|
| `/engineering-technical-writer` | BUILD/SHIP: API docs, README, tutorials |
| `/specialized-developer-advocate` | SHIP: developer-facing content, DX review |

### Support (situational)
| Role | Use when |
|------|----------|
| `/identity-graph-operator` | BUILD: AGE graph queries, knowledge graph maintenance |
| `/data-consolidation-agent` | BUILD: data aggregation, deduplication |
| `/engineering-rapid-prototyper` | BUILD: fast proof-of-concept |
| `/engineering-mobile-app-builder` | BUILD: mobile apps (mobile projects only) |
| `/engineering-solidity-smart-contract-engineer` | BUILD: smart contracts (blockchain only) |
