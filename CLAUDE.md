# TVBridge

Local-first Windows app that receives TradingView webhook alerts via Cloudflare Tunnel and routes them to MT5 (Python sidecar), NinjaTrader (ATI), Telegram, and Discord. Free forever, MIT licensed.

## Tech Stack

| Layer | Choice |
|---|---|
| UI | WPF + .NET 8 + CommunityToolkit.MVVM |
| Webhook | ASP.NET Core Kestrel (in-process) |
| Tunnel | Cloudflare Tunnel (`cloudflared.exe`) |
| DB | SQLite via Dapper |
| Secrets | Windows DPAPI (`ProtectedData`) |
| MT5 | Python sidecar (`MetaTrader5` PyPI) over ZeroMQ |
| NinjaTrader | NT8 ATI TCP socket |
| Telegram | `Telegram.Bot` NuGet |
| Discord | `System.Net.Http` → webhook URL |
| Logging | Serilog → rolling file + in-app viewer |
| Tests | xUnit + FluentAssertions (C#), pytest (Python) |

## Directory Layout

```
src/TVBridge.App/          WPF entry point, DI composition root
src/TVBridge.Core/         Signal/Rule models, rule engine, interfaces
src/TVBridge.Webhook/      Kestrel listener, signal validation
src/TVBridge.Storage/      SQLite + Dapper + migrations + DPAPI helper
src/TVBridge.Tunnel/       cloudflared process manager
src/TVBridge.Channels/     IOutputChannel implementations
sidecar/mt5_bridge/        Python MT5 bridge
tests/                     xUnit + pytest tests
docs/                      Architecture, schema, build plan, ADRs
installer/                 Inno Setup scripts + assets
tools/cloudflared/         Bundled binary (gitignored)
```

## Build / Test / Run

```bash
dotnet build                                      # build all
dotnet test                                       # run all tests
dotnet run --project src/TVBridge.App              # launch app
python sidecar/mt5_bridge/main.py                  # run MT5 sidecar standalone
```

## Coding Conventions

- **C#**: nullable refs ON, `record` for DTOs, `sealed` by default, async all the way, `ConfigureAwait(false)` in library code (not WPF), DI everywhere, no static state except logging
- **Python**: type hints everywhere, `ruff` clean, no global state, `asyncio` for I/O, `pydantic` models
- **Naming**: C# PascalCase public / _camelCase private. Python snake_case
- **Tests**: AAA pattern, one assert per test, FluentAssertions (C#), pytest parametrize (Python). 80%+ coverage on Core and Channels
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `test:`)
- **Logging**: Serilog structured, never log secrets, log signal IDs not contents at Info level
- **Secrets**: ALL credentials encrypted with DPAPI before SQLite. No plaintext ever

## Before Committing

1. `dotnet build` passes with no warnings
2. `dotnet test` all green
3. No hardcoded secrets (run `security-audit` skill before releases)
4. CLAUDE.md updated if structure changed

## Current Build Phase

See `docs/BUILD_PLAN.md` — currently **Phase 1: Foundation**.

## Skills

- **`add-channel`** — Adding a new output channel (class, interface impl, DI, UI, tests)
- **`add-rule-condition`** — Adding a new routing rule match condition
- **`regenerate-tv-template`** — Regenerate TradingView alert template from schema
- **`db-migration`** — Create a new SQLite migration
- **`release-build`** — Build installer for distribution
- **`security-audit`** — Pre-release security check

## Token Efficiency

- Default model: Sonnet. Use Haiku for boilerplate/formatting. Opus for architecture/concurrency
- Use plan mode for tasks touching 3+ files
- Spawn subagents for tests (`test-writer`), reviews (`code-reviewer`), UI (`wpf-ui`), MT5 (`python-mt5`)
- Batch parallel tool calls. Never re-read unchanged files
- `/compact` when context > 60%
