# Multi-Agent Workspace — zsh completions for multiagent-setup
# Source this file: source ./tools/completions.zsh
# Or add to ~/.zshrc: source /path/to/project/tools/completions.zsh

# --- multiagent-setup ---
_multiagent_setup() {
  local -a subcommands
  subcommands=(
    'new:Create a new multi-agent workspace'
    'add-provider:Add a provider to an existing workspace'
    'sync-roles:Sync agent roles to .claude/commands/'
    'install-mcps:Install age-mcp and o-brien MCP servers'
    'hook:Run a built-in hook (cross-platform)'
  )

  local -a providers
  providers=(claude nessy gemini codex qwen all)

  local -a hooks
  hooks=(block-dangerous enforce-commit-msg auto-lint log-agent stop-guard research-reminder)

  case $CURRENT in
    2)
      _describe 'subcommand' subcommands
      ;;
    *)
      case ${words[2]} in
        new|add-provider)
          case $CURRENT in
            3) _message 'project-name or provider' ;;
            *) compadd -- --provider ${providers[@]} --force ;;
          esac
          ;;
        sync-roles)
          compadd -- --clone --pull --agency-dir --workspace-root
          ;;
        install-mcps)
          compadd -- --docker --manual --age-conn --obrien-conn --target
          ;;
        hook)
          compadd -- ${hooks[@]}
          ;;
      esac
      ;;
  esac
}
compdef _multiagent_setup multiagent-setup

# --- /slash-commands via claude / nessy / gemini ---
# Completes role names from ~/.claude/commands/ and .claude/commands/
_ai_slash_commands() {
  local -a roles
  local dir

  # Global roles
  if [ -d "$HOME/.claude/commands" ]; then
    for f in "$HOME/.claude/commands"/*.md(N); do
      roles+=("/${${f:t}%.md}")
    done
  fi

  # Project-level roles
  if [ -d ".claude/commands" ]; then
    for f in .claude/commands/*.md(N); do
      roles+=("/${${f:t}%.md}")
    done
  fi

  # Deduplicate
  roles=(${(u)roles})
  _describe 'slash-command' roles
}

# Bind slash-command completion to supported agent CLIs
for _agent_cmd in claude nessy gemini; do
  if (( $+commands[$_agent_cmd] )); then
    compdef _ai_slash_commands $_agent_cmd
  fi
done
unset _agent_cmd

# --- orchestrator pipeline types ---
_orchestrator_pipelines() {
  local -a pipelines
  pipelines=(
    'feature:Full pipeline — PLAN→BUILD→TEST→VERIFY→SHIP'
    'bugfix:Skip PLAN — BUILD→TEST→VERIFY→SHIP'
    'infra:Skip TEST — PLAN→BUILD→VERIFY→SHIP'
    'content:No SHIP — PLAN→BUILD→VERIFY(CEO)'
    'spike:Research only — PLAN'
  )
  _describe 'pipeline' pipelines
}
