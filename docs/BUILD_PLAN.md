# TVBridge Build Plan

> Each phase ends with: green tests, updated CLAUDE.md (via `docs-scribe`), and a git commit.
> Do not start a phase without explicit approval.

## Current Phase: 4 — Python MT5 Sidecar (Two-Way)

---

## Phase 1 — Foundation

**Goal:** Buildable solution with core types, storage layer, logging, and an empty WPF shell.

- [ ] Solution + all projects scaffolded and building
- [ ] `IOutputChannel` interface defined in `TVBridge.Channels/Abstractions/`
- [ ] `Signal` record in `TVBridge.Core` (validated against `docs/signal-schema.json`)
- [ ] `Rule` record in `TVBridge.Core` with all routing fields
- [ ] SQLite schema v1 + migration runner in `TVBridge.Storage`
- [ ] DPAPI credential helper in `TVBridge.Storage`
- [ ] Serilog wired up (rolling file + console)
- [ ] Empty WPF shell with navigation skeleton (sidebar, frame, no real pages)
- [ ] xUnit smoke tests run green

---

## Phase 2 — Webhook + Routing Core

**Goal:** Working webhook endpoint that validates signals and routes them through the rule engine.

- [ ] Kestrel listener on configurable local port
- [ ] Signal JSON validation against schema
- [ ] Webhook secret validation
- [ ] Rule engine with full match/priority/continue logic
- [ ] Dry-run mode plumbing (global toggle + per-rule override)
- [ ] 90%+ test coverage on `TVBridge.Core` and `TVBridge.Webhook`

---

## Phase 3 — Cloudflare Tunnel

**Goal:** Expose local webhook to the internet via Cloudflare Tunnel.

- [ ] `cloudflared.exe` lifecycle manager (start/stop/restart/health)
- [ ] First-run wizard: opens browser to Cloudflare login, captures tunnel URL
- [ ] Tunnel URL displayed prominently in UI with copy button
- [ ] Health monitoring and auto-restart

---

## Phase 4 — Python MT5 Sidecar (Two-Way) ✅

**Goal:** Full two-way communication with MetaTrader 5 via Python sidecar.

- [x] Python project with `MetaTrader5`, `pyzmq`, `pydantic`
- [x] ZeroMQ REQ/REP for commands, PUB/SUB for account state stream
- [x] Commands: connect, disconnect, place_order, modify, close, get_positions, get_balance, get_history
- [x] C# `Mt5Channel` implementing `IOutputChannel`
- [x] `Mt5AccountService` for pulling positions/balance/history into UI
- [x] Sidecar process lifecycle managed by C# app
- [x] Integration tests (skip if `MT5_DEMO_LOGIN` env var not set)

---

## Phase 5 — Telegram + Discord Channels

**Goal:** Notification channels for Telegram and Discord.

- [ ] Telegram: bot token + chat ID config, formatted message templates
- [ ] Discord: webhook URL config, embed formatting
- [ ] Per-channel message templates editable in UI
- [ ] Tests with mocked HTTP

---

## Phase 6 — NinjaTrader Channel

**Goal:** Order routing to NinjaTrader 8 via ATI.

- [ ] NT8 ATI TCP socket client
- [ ] Symbol mapping (TradingView → NT8 instrument names)
- [ ] Order routing through ATI
- [ ] Tests with mocked socket

---

## Phase 7 — UI Completion

**Goal:** Full WPF UI for all features.

- [ ] Connection page (tunnel status, webhook URL, secret management)
- [ ] Channels page (per-channel enable/config/test-send button)
- [ ] Rules page (visual editor, drag to reorder priority)
- [ ] Signals page (live log, filter, replay button)
- [ ] Settings page (DPAPI key rotation, log level, autostart, dry-run global toggle)
- [ ] TradingView template generator page

---

## Phase 8 — Packaging

**Goal:** Distributable Windows installer.

- [ ] `release-build` skill end-to-end
- [ ] Inno Setup installer with: app, Python embeddable, sidecar, cloudflared.exe
- [ ] Code-signing placeholder (TODO: real cert is the one paid item)
- [ ] SmartScreen warning documentation in README

---

## Phase 9 — Polish

**Goal:** Production readiness.

- [ ] Crash reporter (local file, optional upload)
- [ ] Auto-update check (GitHub Releases API, no telemetry)
- [ ] README with screenshots, GIFs, install guide
- [ ] LICENSE + CONTRIBUTING + SECURITY.md
