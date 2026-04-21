# TVBridge

**Route TradingView alerts to MT5, NinjaTrader, Telegram, and Discord — locally, for free.**

TVBridge is a Windows desktop application that receives TradingView webhook alerts via a secure Cloudflare Tunnel and routes them to multiple trading and notification channels.

## Features

- Receive TradingView alerts via HTTPS webhook (no port forwarding needed)
- Route to MetaTrader 5 (via official Python API — no EA required)
- Route to NinjaTrader 8 (ATI socket)
- Notify via Telegram and Discord
- Rule-based routing with wildcards and priorities
- Dry-run mode for safe testing
- All credentials encrypted with Windows DPAPI
- 100% free, no paid services required

## Status

**Under development** — See `docs/BUILD_PLAN.md` for current progress.

## License

MIT — see [LICENSE](LICENSE).
