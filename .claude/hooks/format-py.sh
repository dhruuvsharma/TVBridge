#!/bin/bash
# PostToolUse hook for Edit|Write on *.py — run ruff check --fix and ruff format
input=$(cat)
file=$(echo "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null)

if [[ "$file" == *.py ]]; then
  ruff check --fix "$file" 2>/dev/null
  ruff format "$file" 2>/dev/null
fi

exit 0
