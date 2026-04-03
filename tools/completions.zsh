# Multi-Agent Workspace — zsh completions
# Source this file: source ./tools/completions.zsh
# Or add to ~/.zshrc: source /path/to/project/tools/completions.zsh

# --- sync-roles.sh ---
_sync_roles() {
  local -a opts
  opts=(
    '--clone:Clone agency-agents repo if missing, then sync'
    '--pull:Git pull latest roles before sync'
  )
  _describe 'option' opts
}
compdef _sync_roles sync-roles.sh
compdef _sync_roles ./tools/sync-roles.sh
compdef _sync_roles tools/sync-roles.sh

# --- setup.sh ---
_setup_sh() {
  case $CURRENT in
    2) _message 'project-name' ;;
    3) _message 'github-org (default: project-name)' ;;
    4) _path_files -/ -W "$(dirname "$0")/../" && _message 'agemcp-path' ;;
  esac
}
compdef _setup_sh setup.sh
compdef _setup_sh ./setup.sh

# --- /slash-commands via claude ---
# Completes role names from ~/.claude/commands/ and .claude/commands/
_claude_slash() {
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

# Bind to claude CLI if available
if (( $+commands[claude] )); then
  # Complete slash commands when typing / after claude
  compdef _claude_slash claude
fi

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

# --- gh shortcuts for multi-repo ---
_gh_korat() {
  local -a repos
  # Read repos from CLAUDE.md backlog section if available
  if [ -f CLAUDE.md ]; then
    while IFS= read -r line; do
      if [[ "$line" =~ '`([^`]+/[^`]+)`' ]]; then
        repos+=("${match[1]}")
      fi
    done < <(grep 'korat-ai/' CLAUDE.md 2>/dev/null)
  fi
  if [ ${#repos} -eq 0 ]; then
    repos=('owner/repo')
  fi
  _describe 'repository' repos
}
