#!/bin/bash
# PostToolUse hook for Edit|Write on *.xaml — validate XAML parses as XML
input=$(cat)
file=$(echo "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null)

if [[ "$file" == *.xaml ]]; then
  python -c "
import xml.etree.ElementTree as ET
try:
    ET.parse('$file')
    print('XAML validation passed: $file')
except ET.ParseError as e:
    print(f'XAML validation FAILED: $file - {e}')
    exit(1)
" 2>/dev/null
fi

exit 0
