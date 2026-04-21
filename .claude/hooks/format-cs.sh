#!/bin/bash
# PostToolUse hook for Edit|Write on *.cs — run dotnet format on the changed file
input=$(cat)
file=$(echo "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null)

if [[ "$file" == *.cs ]]; then
  project_dir=$(dirname "$file")
  while [[ "$project_dir" != "/" && ! -f "$project_dir"/*.csproj ]]; do
    project_dir=$(dirname "$project_dir")
  done
  if ls "$project_dir"/*.csproj 1>/dev/null 2>&1; then
    dotnet format "$project_dir"/*.csproj --include "$file" --no-restore 2>/dev/null
  fi
fi

exit 0
