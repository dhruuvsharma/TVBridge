---
name: db-migration
description: Create a new SQLite migration file for TVBridge database schema changes.
allowed-tools: Read, Write, Glob, Grep
---

# Database Migration

Use this skill when the SQLite schema needs to change.

## Steps

1. **Determine next version**: Check `src/TVBridge.Storage/Migrations/` for existing files. Next version = max + 1.
2. **Create migration file**: `src/TVBridge.Storage/Migrations/V{NNN}_{Description}.sql`
   - Include both UP and DOWN sections, delimited by `-- UP` and `-- DOWN` comments
   - UP: the forward migration (ALTER TABLE, CREATE TABLE, etc.)
   - DOWN: the rollback (DROP, ALTER TABLE DROP COLUMN, etc.)
3. **Register migration**: Ensure the migration runner picks up the new file (it should auto-discover by convention)
4. **Test**: Write a test that applies the migration and verifies the schema change
5. **Update**: If the migration adds columns to `Rule` or `Signal`, update the corresponding C# record and Dapper mappings
