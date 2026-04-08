# {{PROJECT_NAME}} — Using Continue.dev

This workspace is configured for [Continue.dev](https://continue.dev) — the open-source AI code assistant for VS Code and JetBrains.

## Quick start

1. Install [Continue extension](https://marketplace.visualstudio.com/items?itemName=Continue.continue) in VS Code or JetBrains
2. Open this workspace folder
3. Open Continue sidebar (`Ctrl+L` / `Cmd+L`) — workspace rules load automatically

## Custom slash commands

| Command | Description |
|---|---|
| `/orchestrator <task>` | Run the full multi-agent pipeline |
| `/expert <role>: <question>` | Ask a specialist role directly |

### Example: run a feature pipeline

```
/orchestrator Add user authentication with JWT tokens to the API
```

### Example: ask an architect

```
/expert architect: What's the best way to structure the database layer?
```

## Workspace layout

```
{{PROJECT_NAME}}/
├── CLAUDE.md               ← workspace context (auto-loaded via rules)
├── docs/
│   ├── process.md          ← pipeline rules (PLAN→BUILD→TEST→VERIFY→SHIP)
│   └── role-capabilities.md ← specialist roles index
├── code/{{PROJECT_NAME}}/  ← main code repo
└── .continue/
    └── config.yaml         ← Continue config (this provider)
```

## Adding context manually

In the Continue chat, use `@` to add files:
- `@CLAUDE.md` — workspace overview
- `@docs/process.md` — pipeline rules
- `@docs/role-capabilities.md` — role index

## Pipeline

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

Use `/orchestrator` for full pipeline execution. Use Continue's inline edit (`Ctrl+I`) for BUILD/TEST steps.

## GitHub

`{{GITHUB_ORG}}/{{GITHUB_REPO}}`
