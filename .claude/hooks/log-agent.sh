#!/bin/bash
# PreToolUse hook: log agent spawns for observability
# Logs to .claude/agent-log.jsonl

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // empty')

if [ "$TOOL_NAME" != "Agent" ]; then
  exit 0
fi

LOG_DIR="${CLAUDE_PROJECT_DIR:-.}/.claude"
LOG_FILE="$LOG_DIR/agent-log.jsonl"
mkdir -p "$LOG_DIR"

PROMPT=$(echo "$INPUT" | jq -r '.tool_input.prompt // empty' | head -c 500)
DESCRIPTION=$(echo "$INPUT" | jq -r '.tool_input.description // empty')
AGENT_TYPE=$(echo "$INPUT" | jq -r '.tool_input.subagent_type // "general-purpose"')
MODEL=$(echo "$INPUT" | jq -r '.tool_input.model // "default"')
SESSION_ID=$(echo "$INPUT" | jq -r '.session_id // "unknown"')

jq -n -c \
  --arg ts "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --arg sid "$SESSION_ID" \
  --arg type "$AGENT_TYPE" \
  --arg model "$MODEL" \
  --arg desc "$DESCRIPTION" \
  --arg prompt "$PROMPT" \
  '{timestamp: $ts, session: $sid, agent_type: $type, model: $model, description: $desc, prompt_preview: $prompt}' \
  >> "$LOG_FILE"

exit 0
