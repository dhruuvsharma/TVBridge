#!/bin/bash
# PreToolUse hook for Edit on secrets/, *.pfx, appsettings.Production.json — hard block
input=$(cat)
file=$(echo "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null)

if echo "$file" | grep -qiE '(secrets/|\.pfx$|appsettings\.Production\.json$)'; then
  echo "BLOCKED: Cannot edit sensitive file: $file"
  echo "This file is protected. Require explicit user override."
  exit 2
fi

exit 0
