---
name: security-audit
description: Pre-release security audit for TVBridge — checks for hardcoded secrets, DPAPI usage, and sensitive files.
allowed-tools: Read, Grep, Glob, Bash
---

# Security Audit

Run this before any release build.

## Checks

1. **Hardcoded secrets**: Grep for patterns like `password`, `token`, `secret`, `apikey`, `connectionstring` in `src/` and `sidecar/` — flag any that aren't reading from config/DPAPI
2. **DPAPI usage**: Verify all credential reads in `TVBridge.Storage` go through the DPAPI helper. No raw credential strings in memory longer than necessary
3. **Publish output clean**: Verify these files are NOT in `dist/` or publish output:
   - `.env`
   - `appsettings.Development.json`
   - `appsettings.Production.json`
   - `*.pfx`
   - Any file in `secrets/`
4. **Webhook secret validation**: Verify the webhook endpoint checks the `secret` field against stored DPAPI-encrypted secret
5. **SQLite file permissions**: Note if the DB file has overly permissive ACLs (informational)
6. **No telemetry**: Verify no outbound HTTP calls except to configured channels and Cloudflare
7. **Report**: Output a pass/fail summary for each check
