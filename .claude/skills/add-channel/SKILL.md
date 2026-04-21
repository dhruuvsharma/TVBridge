---
name: add-channel
description: Step-by-step guide for adding a new output channel to TVBridge (e.g., a new broker or notification service).
allowed-tools: Read, Edit, Write, Glob, Grep, Bash
---

# Add Channel

Use this skill when adding a new output channel (broker, notification service, etc.) to TVBridge.

## Steps

1. **Create channel directory**: `src/TVBridge.Channels/<Name>/`
2. **Create channel class**: Implement `IOutputChannel` from `src/TVBridge.Channels/Abstractions/`
   - Constructor takes channel-specific config via DI
   - `SendAsync(Signal signal, CancellationToken ct)` — main delivery method
   - `ValidateConfigAsync()` — test connection / credentials
   - `Name` property — display name for UI
3. **Create config model**: `<Name>ChannelConfig.cs` — all settings needed, credentials as `byte[]` (DPAPI encrypted)
4. **Register in DI**: Add to the service collection in `src/TVBridge.App/` composition root
5. **Add config UI**: Create a WPF page in the Channels section for configuring this channel
   - Use MVVM pattern with CommunityToolkit.Mvvm
   - Include a "Test Connection" button that calls `ValidateConfigAsync()`
6. **Write tests**: Create test file in `tests/TVBridge.Channels.Tests/<Name>ChannelTests.cs`
   - Happy path send
   - Invalid config handling
   - Connection failure handling
   - Dry-run mode (should log, not send)
7. **Update CLAUDE.md**: Add the channel to the tech stack table and directory layout if needed
8. **Update docs/architecture.md**: Add to the data flow diagram if it's a new channel type
