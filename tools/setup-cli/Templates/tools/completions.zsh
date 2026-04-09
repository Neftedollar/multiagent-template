# Multi-Agent Workspace — zsh completions for multiagent-setup
# Source this file: source ./tools/completions.zsh
# Or add to ~/.zshrc: source /path/to/project/tools/completions.zsh

# --- /slash-commands via claude / nessy / gemini ---
# Completes role names from ~/.claude/commands/ and .claude/commands/
_ai_slash_commands() {
  local -a roles
  if [ -d "$HOME/.claude/commands" ]; then
    for f in "$HOME/.claude/commands"/*.md(N); do
      roles+=("/${${f:t}%.md}")
    done
  fi
  if [ -d ".claude/commands" ]; then
    for f in .claude/commands/*.md(N); do
      roles+=("/${${f:t}%.md}")
    done
  fi
  roles=(${(u)roles})
  _describe 'slash-command' roles
}

for _agent_cmd in claude nessy gemini; do
  if (( $+commands[$_agent_cmd] )); then
    compdef _ai_slash_commands $_agent_cmd
  fi
done
unset _agent_cmd

# --- multiagent-setup CLI ---
_multiagent_setup() {
  local -a subcommands providers templates shells hooks
  subcommands=(
    'new:Create a new multi-agent workspace'
    'init:Add workspace files to an existing git repo'
    'add-provider:Add a provider to an existing workspace'
    'remove-provider:Remove a provider from an existing workspace'
    'list-providers:List providers configured in current workspace'
    'update:Update workspace templates to latest version'
    'sync-roles:Sync agent roles to ~/.claude/commands/'
    'install-mcps:Install age-mcp and o-brien MCP servers'
    'hook:Run a built-in hook (cross-platform)'
    'doctor:Check workspace health — tools, files, hooks, roles'
    'completions:Print shell completion script'
  )
  providers=(
    'claude:Claude Code by Anthropic (default)'
    'nessy:Nessy CLI (Claude-compatible alias)'
    'gemini:Google Gemini CLI'
    'codex:OpenAI Codex CLI'
    'qwen:Qwen Code by Alibaba'
    'cursor:Cursor IDE'
    'windsurf:Windsurf IDE by Codeium'
    'copilot:GitHub Copilot in VS Code'
    'cline:Cline extension for VS Code'
    'aider:Aider AI pair programmer'
    'continue:Continue.dev VS Code/JetBrains extension'
    'roo:Roo Code VS Code extension'
    'kiro:Amazon Kiro VS Code extension'
    'all:All providers at once'
  )
  templates=(
    'default:Generic workspace'
    'saas:SaaS product (user-impact gates, SLO review)'
    'oss:Open source (CHANGELOG, semver gates)'
    'internal:Internal tools (lighter pipeline)'
  )
  shells=(
    'zsh:zsh completion script'
    'pwsh:PowerShell completion script'
  )
  hooks=(
    'block-dangerous' 'enforce-commit-msg' 'auto-lint'
    'log-agent' 'stop-guard' 'research-reminder'
  )

  case $CURRENT in
    2) _describe 'subcommand' subcommands ;;
    *) case ${words[2]} in
      new|init)
        case ${words[CURRENT-1]} in
          --provider) _describe 'provider' providers ;;
          --template) _describe 'template' templates ;;
          *) compadd -- --provider --template ;;
        esac ;;
      add-provider)
        case $CURRENT in
          3) _describe 'provider' providers ;;
          *) compadd -- --force ;;
        esac ;;
      remove-provider)
        case $CURRENT in
          3) _describe 'provider' providers ;;
          *) compadd -- --force --dry-run ;;
        esac ;;
      list-providers) ;; # no args
      update)
        compadd -- --force --dry-run ;;
      sync-roles)
        compadd -- --clone --pull --global --agency-dir ;;
      install-mcps)
        compadd -- --docker --manual --age-conn --obrien-conn --target ;;
      hook)
        _describe 'hook' hooks ;;
      doctor)
        compadd -- --for ;;
      completions)
        _describe 'shell' shells ;;
    esac ;;
  esac
}
compdef _multiagent_setup multiagent-setup
