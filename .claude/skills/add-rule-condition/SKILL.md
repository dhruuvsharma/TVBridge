---
name: add-rule-condition
description: Guide for adding a new match condition to the TVBridge routing rule engine.
allowed-tools: Read, Edit, Write, Glob, Grep, Bash
---

# Add Rule Condition

Use this skill when adding a new match condition field to the routing engine.

## Steps

1. **Update `Rule` record** in `src/TVBridge.Core/`: Add the new condition property (nullable, so existing rules aren't broken)
2. **Update `RuleEvaluator`** in `src/TVBridge.Core/`: Add matching logic for the new condition
   - Null = wildcard (matches anything)
   - Support glob/wildcard patterns if the field is string-based
3. **Create SQLite migration**: Use the `db-migration` skill to add the column
4. **Update rule editor XAML** in `src/TVBridge.App/`: Add input field for the new condition
5. **Update rule editor ViewModel**: Bind the new field
6. **Write tests** in `tests/TVBridge.Core.Tests/`:
   - Rule with new condition matches correctly
   - Rule with null (wildcard) for new condition still matches
   - Rule with non-matching value rejects correctly
7. **Update `docs/signal-schema.json`** if the condition maps to a new signal field
