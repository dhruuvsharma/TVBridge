#!/bin/bash
# PreToolUse hook for Bash — block destructive commands
# Reads the command from stdin (JSON with tool_input)

input=$(cat)
command=$(echo "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('command',''))" 2>/dev/null)

if echo "$command" | grep -qiE '(rm -rf|del /s|format |git push --force|git push -f)'; then
  echo "BLOCKED: Destructive command detected: $command"
  echo "Use explicit user confirmation to override."
  exit 2
fi

if echo "$command" | grep -qiE '(dist/|installer/output/)' && ! echo "$command" | grep -qiE '(mkdir|ls)'; then
  echo "BLOCKED: Command touches dist/ or installer/output/: $command"
  echo "Use explicit user confirmation to override."
  exit 2
fi

exit 0
