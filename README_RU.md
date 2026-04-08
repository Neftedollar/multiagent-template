# multiagent-template

**Одна команда. Полная команда AI-инженеров. Делайте больше.**

Шаблон рабочего пространства, в котором команда специализированных AI-агентов — оркестратор, архитектор, разработчик, ревьюер, DevOps, дизайнер и другие — автономно ведёт разработку от идеи до слитого PR. Вы задаёте направление; агенты берут выполнение на себя.

[![NuGet](https://img.shields.io/nuget/v/multiagent-setup)](https://www.nuget.org/packages/multiagent-setup)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/Neftedollar/multiagent-template?style=social)](https://github.com/Neftedollar/multiagent-template)

---

## Зачем multiagent-template?

Большинство AI-инструментов для кодинга дают одного агента, который пишет код по запросу. multiagent-template даёт **скоординированную команду**:

- **Оркестратор** разбивает задачи на шаги и подбирает нужного специалиста для каждого
- **Гейты пайплайна** ловят проблемы до того, как они накапливаются (`PLAN → BUILD → TEST → VERIFY → SHIP`)
- **5 AI-агентов** из коробки: Claude, Gemini, Codex, Qwen, Nessy
- **Хуки безопасности** — блокировка опасных команд, conventional commits, автолинт, логирование агентов
- **Семантическая память** через AGE-граф + O'Brien pgvector — агенты помнят контекст между сессиями
- **Без платформо-специфичных скриптов** — все хуки работают через кросс-платформенный бинарник `multiagent-setup`

Участие человека: постановка задач и финальное одобрение PR. Всё остальное — автономно.

---

## Быстрый старт

```bash
# Один лайнер (macOS / Linux) — устанавливает все зависимости и создаёт воркспейс
curl -fsSL https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.sh | bash -s -- MyProject

# Windows (PowerShell)
irm https://raw.githubusercontent.com/Neftedollar/multiagent-template/main/bootstrap.ps1 -OutFile bootstrap.ps1
.\bootstrap.ps1 MyProject
```

Если git, gh, jq и .NET 10 уже установлены:

```bash
dotnet tool install -g multiagent-setup
multiagent-setup new MyProject                      # Claude (по умолчанию)
multiagent-setup new MyProject --provider gemini    # Gemini CLI
multiagent-setup new MyProject --provider nessy     # Nessy (совместим с Claude)
multiagent-setup new MyProject --provider codex     # OpenAI Codex
multiagent-setup new MyProject --provider qwen      # Qwen Code
multiagent-setup new MyProject --provider all       # все провайдеры сразу
```

Начать работу:
```bash
cd MyProject
claude          # или: gemini / codex / nessy / qwen-code
/orchestrator Сделай REST API с авторизацией
```

GitHub org определяется автоматически из `gh auth`. Явно: `multiagent-setup new MyProject my-org`

---

## Поддерживаемые провайдеры

| Провайдер | Бинарник | Описание |
|-----------|----------|----------|
| **claude** | `claude` | [Claude Code](https://docs.anthropic.com/en/docs/claude-code) от Anthropic — по умолчанию |
| **nessy** | `nessy` | Claude-совместимый агент; переиспользует конфиг `.claude/` |
| **gemini** | `gemini` | [Gemini CLI](https://github.com/google-gemini/gemini-cli) от Google |
| **codex** | `codex` | [OpenAI Codex CLI](https://github.com/openai/codex) |
| **qwen** | `qwen-code` | [Qwen Code](https://github.com/QwenLM/qwen-code) от Alibaba |

Добавить провайдер в существующий воркспейс:
```bash
multiagent-setup add-provider gemini    # добавляет Gemini в существующий Claude-воркспейс
multiagent-setup add-provider all       # добавляет все недостающие провайдеры
```

---

## Как это работает

Один человек (CEO) ставит задачи. **Оркестратор** разбивает их на шаги, подбирает нужную роль для каждого шага, запускает пайплайн и доводит до готового PR. Эскалация на человека нужна только для: публичного контента, breaking changes, инфрарешений с затратами или 5+ провалов подряд.

### Пайплайн

```
PLAN → BUILD → TEST → VERIFY → SHIP
```

Пять типов: `feature`, `bugfix` (без PLAN), `infra`, `content`, `spike` (только PLAN).

Каждый шаг заканчивается гейтом: `APPROVED` — идём дальше; `NEEDS WORK` — агент исправляет (до 3 попыток, затем помощник, затем ещё 2, затем эскалация на CEO).

### Режимы работы

| Режим | Запуск | Описание |
|-------|--------|----------|
| **CEO Mode** | `/orchestrator <задача>` | Человек даёт задачу, оркестратор выполняет |
| **Single Expert** | `/<роль> <вопрос>` | Прямой вызов эксперта без пайплайна |
| **Autonomous** | `claude -p "/orchestrator ..."` | Оркестратор сам берёт задачи из бэклога |

---

## Структура воркспейса

```
MyProject/
├── code/                    ← репозиторий продукта (в .gitignore)
├── docs/
│   ├── process.md           ← операционный мануал (источник истины для пайплайна)
│   ├── role-capabilities.md ← индекс ролей для оркестратора
│   └── workflows/           ← спецификации пайплайнов (WORKFLOW-*.md)
├── .claude/                 ← конфиг Claude / Nessy
│   ├── commands/            ← слэш-команды (роли из agency-agents)
│   ├── hooks/lint.json      ← конфиг авто-линтера
│   ├── mcp.json             ← конфигурация MCP-серверов
│   └── settings.json        ← конфигурация хуков
├── .gemini/                 ← конфиг Gemini CLI (--provider gemini)
│   └── settings.json
├── .codex/                  ← конфиг Codex (--provider codex)
└── tools/
    ├── completions.zsh      ← автодополнения для zsh
    └── completions.ps1      ← автодополнения для PowerShell
```

---

## Система хуков

Хуки запускаются автоматически через `settings.json`. Все реализованы внутри бинарника `multiagent-setup` — без отдельных скриптов, полностью кросс-платформенно.

| Хук | Триггер | Действие |
|-----|---------|----------|
| `block-dangerous` | PreToolUse (Bash) | Блокирует `rm -rf /`, `push --force main`, `DROP TABLE` и т.д. |
| `enforce-commit-msg` | PreToolUse (Bash) | Требует conventional commits (`feat:`, `fix:` и т.д.) |
| `auto-lint` | PostToolUse (Edit/Write) | Запускает форматтер для изменённого файла |
| `log-agent` | PreToolUse (Agent) | Логирует запуск субагентов в `.claude/agent-log.jsonl` |
| `stop-guard` | Stop / SessionEnd | Напоминает запустить тесты и обновить O'Brien + граф |
| `research-reminder` | PostToolUse (WebSearch/WebFetch) | Напоминает сохранить результаты исследований |

---

## Роли агентов

Роли устанавливаются как слэш-команды из [agency-agents](https://github.com/msitarzewski/agency-agents) в проектный `.claude/commands/` при создании воркспейса.

| Слой | Роли |
|------|------|
| Стратегия | `/product-manager`, `/product-trend-researcher` |
| Управление | `/orchestrator`, `/testing-reality-checker`, `/specialized-workflow-architect` |
| Инженерия | `/engineering-software-architect`, `/engineering-backend-architect`, `/engineering-frontend-developer`, `/engineering-code-reviewer`, `/engineering-devops-automator`, `/engineering-security-engineer` |
| AI / ML | `/engineering-ai-engineer` |
| Дизайн | `/design-ux-researcher`, `/design-ui-designer` |
| GTM | `/specialized-developer-advocate`, `/engineering-technical-writer`, `/marketing-content-creator` |

Оркестратор подбирает роли **динамически** по сигналам задачи через `docs/role-capabilities.md`. Если ни одна роль не подходит — создаётся ad-hoc роль на лету.

Обновить роли:
```bash
multiagent-setup sync-roles --pull
```

---

## Инфраструктура (опционально)

### AGE-граф
База знаний на PostgreSQL + [Apache AGE](https://age.apache.org/), подключается через [age-mcp](https://github.com/Neftedollar/age-mcp). Хранит модули, пайплайны, привязки ролей, security findings и code insights. Растёт с каждой выполненной задачей.

### O'Brien
Семантическое хранилище на pgvector — для координации агентов и памяти. Используется для оптимистичной блокировки задач, тегирования прогресса, хранения результатов исследований и crash recovery.

```bash
multiagent-setup install-mcps          # интерактивный Docker-режим
multiagent-setup install-mcps --manual # ввести строки подключения вручную
```

---

## Справка по CLI

```bash
# Создать воркспейс
multiagent-setup new <project> [org] [--provider <name>]

# Добавить провайдер в существующий воркспейс
multiagent-setup add-provider <provider> [--workspace-dir <path>] [--force]

# Синхронизировать роли из agency-agents
multiagent-setup sync-roles [--clone|--pull] [--agency-dir <path>] [--workspace-root <path>]

# Установить MCP-серверы (AGE + O'Brien)
multiagent-setup install-mcps [--docker|--manual] [--age-conn <str>] [--obrien-conn <str>]

# Запустить хук вручную
multiagent-setup hook <name>

multiagent-setup -v | --version
```

Провайдеры: `claude` (по умолчанию), `nessy`, `gemini`, `codex`, `qwen`, `all`

---

## Требования

| Инструмент | macOS/Linux | Windows |
|------------|-------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) 10+ | `brew install dotnet` | `winget install Microsoft.DotNet.SDK.10` |
| [GitHub CLI](https://cli.github.com/) | `brew install gh` | `winget install GitHub.cli` |
| git, jq | brew / apt | `winget install Git.Git jqlang.jq` |
| AI-агент | см. таблицу провайдеров | то же |
| Docker | опционально, для AGE/O'Brien | `winget install Docker.DockerDesktop` |

`bootstrap.sh` / `bootstrap.ps1` устанавливают всё автоматически на чистой машине.

---

## FAQ

**Можно использовать несколько провайдеров в одном воркспейсе?**  
Да. `multiagent-setup new MyProject --provider all` создаёт конфиг для всех провайдеров сразу, или `multiagent-setup add-provider <name>` добавляет один позже. Каждый провайдер получает свою директорию (`.gemini/`, `.codex/`, и т.д.), разделяя общие `docs/` и `code/`.

**Нужен ли Docker?**  
Нет. Docker нужен только для опциональных компонентов AGE-граф + O'Brien. Базовый воркспейс и все хуки работают без него.

**Что такое Nessy?**  
Nessy — Claude-совместимый AI-агент. Поскольку он использует те же соглашения CLI, что и Claude Code (слэш-команды, хуки settings.json), `--provider nessy` просто переиспользует директорию `.claude/`. Отдельный конфиг не нужен.

**Как обновить роли агентов?**  
Роли берутся из репозитория сообщества [agency-agents](https://github.com/msitarzewski/agency-agents). Запустите `multiagent-setup sync-roles --pull`. Проектные файлы ролей (без маркера автогенерации) не перезаписываются.

**Можно добавить свои роли?**  
Да. Создайте `.md` файл в `.claude/commands/` с полем `name:` во frontmatter. Оркестратор подхватит его автоматически. Также оркестратор создаёт ad-hoc роли на лету, если ни одна существующая не подходит.

**Работает ли без Claude?**  
Да. Используйте `--provider gemini`, `--provider codex` или `--provider qwen`. Каждый провайдер получает преднастроенный settings.json с хуками. Пайплайн, документы процессов и система ролей работают одинаково независимо от агента.

---

## Контрибьюция

Шаблоны находятся в [`tools/setup-cli/Templates/`](tools/setup-cli/Templates/). Каждый провайдер — в поддиректории `providers/<name>/`. Исходник CLI: [`tools/setup-cli/`](tools/setup-cli/).

PR приветствуются. Для добавления нового провайдера:
1. Добавьте директорию `tools/setup-cli/Templates/providers/<name>/`
2. Подключите в `SetupCommand.cs` (`CreateDirectories`, `ResolveOutputPath`, `CheckTools`)
3. Добавьте имя провайдера в `validProviders` в `Program.cs`

---

[English version](README.md)
