# TVBridge Architecture

## Overview

TVBridge is a Windows desktop application that receives TradingView alerts via webhook and routes them to multiple trading and notification channels. It is local-first, free-forever, MIT licensed.

## Data Flow

```
TradingView ──HTTPS──► Cloudflare Tunnel ──► Local C# WPF App (Kestrel)
                                                  │
                                                  ├──► SQLite (signals, rules, config, audit)
                                                  ├──► Python sidecar ──IPC──► MT5 Terminal
                                                  ├──► NinjaTrader 8 (ATI socket)
                                                  ├──► Telegram (Bot API)
                                                  └──► Discord (webhook URL)
```

1. TradingView fires an alert containing a JSON payload matching `docs/signal-schema.json`.
2. The alert hits the Cloudflare Tunnel public URL over HTTPS.
3. `cloudflared.exe` (bundled, managed by `TVBridge.Tunnel`) forwards to local Kestrel on a configurable port.
4. `TVBridge.Webhook` validates the JSON schema and webhook secret.
5. The validated `Signal` is persisted to SQLite and passed to the routing engine.
6. `TVBridge.Core.RuleEngine` evaluates rules by priority, matching on strategy_id, symbol, action, account_tag, and timeframe.
7. Matched rules produce a list of destination channels; each `IOutputChannel` implementation delivers the signal.

## Tech Stack

| Layer | Choice |
|---|---|
| UI | WPF + .NET 8 + CommunityToolkit.MVVM |
| Webhook listener | ASP.NET Core Kestrel hosted in-process |
| Tunnel | Cloudflare Tunnel via bundled `cloudflared.exe` |
| DB | SQLite via Dapper |
| Credential encryption | Windows DPAPI (`ProtectedData`) |
| MT5 | Python sidecar (`MetaTrader5` PyPI) over ZeroMQ (NetMQ ↔ pyzmq) |
| NinjaTrader | NT8 ATI TCP socket |
| Telegram | `Telegram.Bot` NuGet |
| Discord | `System.Net.Http` to webhook URL |
| Logging | Serilog → rolling file + in-app viewer |
| Tests | xUnit + FluentAssertions (C#), pytest (Python) |
| Installer | Inno Setup |

## Key Design Decisions

### MT5 via Python Sidecar (not EA)
The official MetaQuotes `MetaTrader5` Python package provides full API access without requiring an installed Expert Advisor. Communication between C# and Python uses ZeroMQ (NetMQ on C#, pyzmq on Python): REQ/REP for commands, PUB/SUB for account state streaming.

### DPAPI for Credentials
All secrets (webhook secret, bot tokens, API keys) are encrypted using Windows DPAPI before storage in SQLite. No plaintext secrets ever touch disk.

### Rule-Based Routing
Signals are matched against ordered rules. Each rule specifies match conditions (with wildcard support), destination channels, priority, and optional dry-run override. Rules with `continue_on_match=true` allow a signal to hit multiple destinations.

### Cloudflare Tunnel (No Port Forwarding)
Eliminates the need for router configuration. The bundled `cloudflared.exe` creates a secure tunnel. First-run wizard handles Cloudflare authentication.

### Dry-Run Mode
Global toggle plus per-rule override. When active, channels log "would have sent X" without executing. Essential for testing alert configurations.

## Project Structure

- `TVBridge.App` — WPF entry point, DI composition root
- `TVBridge.Core` — Signal/Rule models, rule engine, interfaces
- `TVBridge.Webhook` — Kestrel listener, signal validation
- `TVBridge.Storage` — SQLite + Dapper + migrations + DPAPI helper
- `TVBridge.Tunnel` — cloudflared process manager
- `TVBridge.Channels` — IOutputChannel implementations (MT5, Telegram, Discord, NinjaTrader)
- `sidecar/mt5_bridge` — Python MT5 bridge process
