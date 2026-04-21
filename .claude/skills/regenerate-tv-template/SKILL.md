---
name: regenerate-tv-template
description: Regenerate the TradingView alert template from the canonical signal schema.
allowed-tools: Read, Write, Glob
---

# Regenerate TradingView Template

Use this skill after any change to `docs/signal-schema.json`.

## Steps

1. **Read** `docs/signal-schema.json`
2. **Generate** `docs/tradingview-template.txt`:
   - For each property in the schema, output the appropriate TradingView placeholder or a sensible default
   - `alert_id` → `"{{timenow}}"`
   - `symbol` → `"{{ticker}}"`
   - `timestamp` → `"{{timenow}}"`
   - `timeframe` → `"{{interval}}"`
   - `action` → `"{{strategy.order.action}}"` (user customizes)
   - `secret` → `"YOUR_WEBHOOK_SECRET_HERE"`
   - Nullable fields → `null`
   - String fields → placeholder string
3. **Update** the in-app template output (search for where the app generates/displays the template to users)
4. **Verify** the generated template is valid JSON
