#!/bin/bash
# Stop hook — quick build check before ending turn
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo .)"

if [ -f TVBridge.sln ]; then
  result=$(dotnet build --no-restore --verbosity quiet 2>&1)
  if [ $? -ne 0 ]; then
    echo "WARNING: Build failed! Fix before ending session."
    echo "$result" | tail -5
    exit 1
  fi
fi

exit 0
