# TVBridge

**Route TradingView alerts to MT5, NinjaTrader, Telegram, and Discord — locally, for free.**

TVBridge is a Windows desktop application that receives TradingView webhook alerts via a secure Cloudflare Tunnel and routes them to multiple trading and notification channels. No port forwarding, no cloud servers, no subscription fees.

## Features

- **Webhook receiver** — HTTPS endpoint via Cloudflare Tunnel (no port forwarding needed)
- **MetaTrader 5** — Full two-way bridge via official Python API (no EA required). Place orders, modify, close, stream account state
- **NinjaTrader 8** — Order routing via ATI TCP socket with symbol mapping
- **Telegram** — Bot notifications with customizable message templates
- **Discord** — Webhook notifications with color-coded embeds
- **Rule engine** — Route signals based on symbol, strategy, action, account tag, timeframe. Priority ordering, wildcards, continue-on-match
- **Dry-run mode** — Test everything safely before going live (global toggle + per-rule override)
- **Encrypted secrets** — All credentials protected with Windows DPAPI. No plaintext ever stored
- **TradingView template generator** — Auto-generates the alert JSON with your webhook secret
- **Local-first** — Everything runs on your machine. No data leaves your network except through your configured channels

## Quick Start

### Prerequisites

- Windows 10/11 (64-bit)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (included in installer)
- Python 3.11+ (included in installer, needed for MT5 sidecar)
- MetaTrader 5 terminal (if using MT5 channel)
- NinjaTrader 8 (if using NinjaTrader channel)

### Install

1. Download the latest installer from [Releases](../../releases)
2. Run `TVBridge_Setup_x.x.x.exe`
3. See [SmartScreen note](docs/SMARTSCREEN.md) if Windows shows a warning

### Setup

1. Launch TVBridge
2. Go to **Settings** → click **Generate** to create a webhook secret
3. Copy the **TradingView Alert Template** from the Settings page
4. In TradingView, create an alert and paste the template into the **Message** field
5. Set the alert webhook URL to your tunnel URL (shown in the status bar)
6. Go to **Rules** → create a rule to route signals to your desired channels
7. Test with **dry-run mode** enabled first

### Build from source

```bash
git clone https://github.com/user/tvbridge.git
cd tvbridge
dotnet build
dotnet test
dotnet run --project src/TVBridge.App
```

### Release build

```powershell
.\scripts\release-build.ps1
# Then run Inno Setup on installer\tvbridge.iss
```

## Architecture

```
TradingView → Cloudflare Tunnel → Kestrel Webhook → Rule Engine → Channels
                                                                     ├── MT5 (Python sidecar via ZeroMQ)
                                                                     ├── NinjaTrader 8 (ATI TCP socket)
                                                                     ├── Telegram (Bot API)
                                                                     └── Discord (Webhook)
```

| Layer | Technology |
|---|---|
| UI | WPF + .NET 8 + CommunityToolkit.MVVM |
| Webhook | ASP.NET Core Kestrel (in-process) |
| Tunnel | Cloudflare Tunnel (cloudflared.exe) |
| Database | SQLite via Dapper |
| Secrets | Windows DPAPI |
| MT5 | Python sidecar (MetaTrader5 PyPI) over ZeroMQ |
| NinjaTrader | NT8 ATI TCP socket |
| Telegram | Telegram.Bot NuGet |
| Discord | System.Net.Http webhook |
| Logging | Serilog (rolling file + console) |
| Tests | xUnit + FluentAssertions (C#), pytest (Python) |

## Project Structure

```
src/TVBridge.App/          WPF entry point, DI composition root, ViewModels, Views
src/TVBridge.Core/         Signal/Rule models, rule engine, interfaces, crash reporter, update checker
src/TVBridge.Webhook/      Kestrel listener, signal validation
src/TVBridge.Storage/      SQLite + Dapper + migrations + DPAPI helper
src/TVBridge.Tunnel/       cloudflared process manager
src/TVBridge.Channels/     IOutputChannel implementations (MT5, Telegram, Discord, NinjaTrader)
sidecar/mt5_bridge/        Python MT5 bridge (ZeroMQ server)
tests/                     xUnit + pytest tests
docs/                      Architecture, schema, build plan
installer/                 Inno Setup script
scripts/                   Build automation
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

See [SECURITY.md](SECURITY.md) for reporting vulnerabilities.

## License

MIT — see [LICENSE](LICENSE).
