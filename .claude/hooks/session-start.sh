#!/bin/bash
# SessionStart hook — print project status
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo .)"

echo "=== TVBridge Session Start ==="

# Current branch
branch=$(git branch --show-current 2>/dev/null || echo "no git")
echo "Branch: $branch"

# Uncommitted files
uncommitted=$(git status --porcelain 2>/dev/null | wc -l)
echo "Uncommitted files: $uncommitted"

# Current build phase
if [ -f docs/BUILD_PLAN.md ]; then
  phase=$(grep "^## Current Phase:" docs/BUILD_PLAN.md 2>/dev/null || echo "Unknown")
  echo "$phase"
fi

# Last test status
if [ -f TestResults/.last-run ]; then
  echo "Last test run: $(cat TestResults/.last-run)"
fi

echo "=============================="
