# {{PROJECT_NAME}} — Using Aider

This workspace is configured for [Aider](https://aider.chat) — AI pair programming in the terminal.

## Quick start

```bash
# From workspace root — CLAUDE.md and docs/process.md are loaded automatically
aider

# Work on code repo directly
cd code/{{PROJECT_NAME}}
aider
```

## Workspace layout

```
{{PROJECT_NAME}}/
├── CLAUDE.md               ← workspace context (auto-loaded by .aider.conf.yml)
├── docs/
│   ├── process.md          ← pipeline rules (auto-loaded)
│   └── role-capabilities.md
├── code/{{PROJECT_NAME}}/  ← main code repo
└── .aider.conf.yml         ← Aider config (this provider)
```

## Multi-agent pipeline

Aider handles **BUILD** and **TEST** phases. Use the full pipeline for larger features:

```
PLAN → BUILD (aider) → TEST (aider) → VERIFY → SHIP
```

**PLAN** with Claude/orchestrator, **BUILD** with Aider, **VERIFY** with code reviewer.

## Tips

- Use `/add docs/role-capabilities.md` to include the capability index
- Use `/architect` mode for architecture decisions before coding
- Use `/ask` for questions without code changes
- Keep commits conventional: `feat:`, `fix:`, `chore:`, etc.

## Agent roles

Roles live in `.claude/commands/` as slash commands for other agents (Claude, Gemini, etc.).
Aider doesn't use slash commands — use Aider for code, other agents for strategy and review.

## GitHub org

`{{GITHUB_ORG}}/{{GITHUB_REPO}}`
