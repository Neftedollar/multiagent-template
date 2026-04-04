# multiagent-template

Шаблон рабочего пространства, в котором команда AI-агентов на базе [Claude Code](https://docs.anthropic.com/en/docs/claude-code) автономно выполняет задачи по разработке — от планирования до деплоя — с минимальным участием человека.

## Идея

Один человек (CEO) ставит задачи. Всё остальное делает команда AI-агентов, каждый из которых играет конкретную роль: продакт-менеджер, архитектор, разработчик, ревьюер, девопс, дизайнер, техписатель и т.д.

Центральное звено — **Оркестратор**: автономный агент, который получает задачу, разбивает её на шаги, подбирает роли, запускает пайплайн и доводит до готового PR. Человек подключается только в точках эскалации: публичный контент, breaking changes, инфрарешения с затратами, или 5+ провалов подряд.

## Пайплайн

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

Пять типов пайплайнов: `feature`, `bugfix`, `infra`, `content`, `spike`. Каждый шаг заканчивается гейтом: `APPROVED` — идём дальше, `NEEDS WORK` — агент исправляет (до 3 попыток → helper → 2 попытки → эскалация на CEO).

## Быстрый старт

**Создать воркспейс:**

```bash
# Через обёртку (устанавливает тул если нет, затем запускает)
./setup.sh MyProject

# Или напрямую через dotnet
dotnet tool install -g multiagent-setup
multiagent-setup MyProject
```

GitHub org берётся автоматически из `gh auth` — если не авторизован, запустит `gh auth login`.  
Явно задать org: `multiagent-setup MyProject my-org`

**Начать работу:**

```bash
cd ../MyProject
# (опционально) установить MCP-серверы
./tools/install-mcps.sh

claude
/orchestrator Реализовать авторизацию по OAuth2
```

## Структура воркспейса

```
MyProject/
├── code/                    ← репозиторий продукта (в .gitignore)
├── docs/
│   ├── process.md           ← операционный мануал (источник истины)
│   ├── role-capabilities.md ← индекс ролей для оркестратора
│   └── workflows/           ← спецификации пайплайнов (WORKFLOW-*.md)
├── .claude/
│   ├── commands/            ← слэш-команды (роли агентов)
│   ├── hooks/               ← хуки безопасности и автоматизации
│   ├── mcp.json             ← MCP-конфигурация
│   └── settings.json        ← конфигурация хуков
└── tools/
    ├── sync-roles.sh        ← синхронизация ролей из agency-agents
    ├── install-mcps.sh      ← установка age-mcp и o-brien
    └── completions.zsh      ← zsh-автодополнения
```

## Режимы работы

| Режим | Запуск | Описание |
|-------|--------|----------|
| **CEO Mode** | `/orchestrator <задача>` | Человек даёт задачу, оркестратор выполняет |
| **Single Expert** | `/<роль> <вопрос>` | Прямой вызов эксперта без пайплайна |
| **Autonomous** | `claude -p "/orchestrator ..."` | Оркестратор сам берёт задачи из бэклога |

## Инфраструктура

### AGE Graph

Графовая база знаний на PostgreSQL + [Apache AGE](https://age.apache.org/), подключается к Claude Code через [age-mcp](https://github.com/Neftedollar/age-mcp) (F#/.NET, dotnet global tool).

Хранит: модули и зависимости, пайплайны и шаги, привязки ролей, security findings, code insights. Оркестратор запрашивает граф на каждом шаге пайплайна и обновляет его по завершении задачи — база знаний растёт с каждой итерацией.

### O'Brien

Семантическое хранилище агентов на pgvector — для координации и памяти. Устанавливается как dotnet global tool (`OBrienMcp`).

Используется для: блокировки задач (оптимистичный lock), тегирования прогресса (`code-done` → `pr-created` → `completed-work`), хранения результатов исследований, crash recovery (lock старше 24ч → `stale-work`).

Установка обоих инструментов: `./tools/install-mcps.sh`

## Хуки

`.claude/settings.json` подключает набор хуков, которые работают без участия человека:

| Хук | Триггер | Действие |
|-----|---------|----------|
| `block-dangerous.sh` | PreToolUse (Bash) | Блокирует `rm -rf /`, `push --force main`, `DROP TABLE` и т.д. |
| `enforce-commit-msg.sh` | PreToolUse (Bash) | Требует conventional commits (`feat:`, `fix:` и т.д.) |
| `auto-lint.sh` | PostToolUse (Edit/Write) | Запускает форматтер для изменённого файла |
| `log-agent.sh` | PreToolUse (Agent) | Логирует запуск субагентов в `.claude/agent-log.jsonl` |
| `stop-guard.sh` | Stop | Напоминает запустить тесты и проставить теги в O'Brien |

## Роли

Роли подключаются как слэш-команды из [agency-agents](https://github.com/msitarzewski/agency-agents) — устанавливаются глобально в `~/.claude/commands/` при создании воркспейса.

Роли по слоям:

| Слой | Роли |
|------|------|
| Стратегия | `/product-manager`, `/product-trend-researcher` |
| Управление | `/orchestrator`, `/testing-reality-checker`, `/specialized-workflow-architect` |
| Инженерия | `/engineering-software-architect`, `/engineering-backend-architect`, `/engineering-frontend-developer`, `/engineering-code-reviewer`, `/engineering-devops-automator`, `/engineering-security-engineer` |
| Дизайн | `/design-ux-researcher`, `/design-ui-designer` |
| GTM | `/specialized-developer-advocate`, `/engineering-technical-writer`, `/marketing-content-creator` |

Оркестратор подбирает роли **динамически** по сигналам задачи (файлы, ключевые слова, лейблы) через индекс `docs/role-capabilities.md`. Если ни одна роль не подходит — создаётся ad-hoc роль на лету.

## Модели по уровням

| Уровень | Модель | Роли |
|---------|--------|------|
| Стратегический | opus | PM, архитекторы, безопасность, оркестратор |
| Исполнительный | sonnet | Разработчики, DevOps, техписатель, дизайн |
| Валидация | opus | Ревьюер, Reality Checker |
| Рутина | haiku | Сбор данных, форматирование |

## multiagent-setup (dotnet tool)

Тул, который создаёт воркспейс из шаблона. Все шаблонные файлы встроены в бинарник как embedded resources — никаких внешних зависимостей.

```bash
# Установить глобально
dotnet tool install -g multiagent-setup

# Создать воркспейс
multiagent-setup <project-name> [github-org]

# Обновить
dotnet tool update -g multiagentsetup
```

Исходник: [`tools/setup-cli/`](tools/setup-cli/)

## Требования

- [Claude Code](https://docs.anthropic.com/en/docs/claude-code) (`claude`)
- [GitHub CLI](https://cli.github.com/) (`gh`) + авторизация
- `git`, `jq`
- [.NET SDK](https://dotnet.microsoft.com/download) 10+ (для `multiagent-setup` и MCP-серверов)
- Docker (для AGE graph и O'Brien — опционально)
