---
name: release-build
description: Build the TVBridge release installer including app, Python sidecar, and cloudflared.
allowed-tools: Read, Bash, Glob, Grep
---

# Release Build

Use this skill to create a distributable installer.

## Steps

1. **Run security audit first**: Invoke the `security-audit` skill
2. **Build C# app**:
   ```bash
   dotnet publish src/TVBridge.App -c Release -r win-x64 --self-contained -o dist/app
   ```
3. **Copy Python embeddable**: Download/copy Python embeddable to `dist/python/`
4. **Copy sidecar**: Copy `sidecar/mt5_bridge/` to `dist/sidecar/` and install requirements into embeddable Python
5. **Copy cloudflared**: Copy `tools/cloudflared/cloudflared.exe` to `dist/cloudflared/`
6. **Run Inno Setup**:
   ```bash
   iscc installer/tvbridge.iss
   ```
7. **Output**: Installer written to `installer/output/TVBridge_Setup.exe`
8. **Verify**: Check installer output exists and is > 10MB (sanity check)
