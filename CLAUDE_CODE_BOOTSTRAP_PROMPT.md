# TVBridge — Claude Code Bootstrap Prompt

> Copy everything below the `---` line into Claude Code as your first message in a fresh project directory. Run it with `claude` in an empty folder. Recommended start: `claude --model sonnet` (we'll downshift to Haiku for routine work via `/model`).

---

You are bootstrapping a brand-new project called **TVBridge**. Your job in this first session is **not to write the application code yet**. Your job is to:

1. Set up the Claude Code project infrastructure (CLAUDE.md, skills, hooks, subagents, settings) so that all future sessions are token-efficient and consistent.
2. Create the full directory scaffold and empty project files.
3. Produce a phased BUILD_PLAN.md.
4. Enter **plan mode** and present the Phase 1 plan for my approval before writing any feature code.

Read this entire document end-to-end before doing anything. Do not skip ahead. When you finish reading, summarize the plan back to me in 5 bullet points, then begin Step 0.

---

## 1. Project overview

**TVBridge** is a Windows desktop application that receives TradingView alerts via webhook and routes them to multiple trading and notification channels (MT5, NinjaTrader, Telegram, Discord, with more pluggable later). It is local-first, free-forever for end users, MIT licensed, and will be distributed as a Windows installer.

### Architecture (locked, do not redesign)

```
TradingView ──HTTPS──► Cloudflare Tunnel ──► Local C# WPF App
                                                  │
                                                  ├──► SQLite (signals, rules, config, audit)
                                                  ├──► Python sidecar ──IPC──► MT5 Terminal (open & logged in)
                                                  ├──► NinjaTrader 8 (ATI socket)
                                                  ├──► Telegram (Bot API)
                                                  └──► Discord (webhook URL)
```

### Tech stack (locked)

| Layer | Choice |
|---|---|
| UI | WPF + .NET 8 + CommunityToolkit.MVVM |
| Webhook listener | ASP.NET Core Kestrel hosted in-process |
| Tunnel | Cloudflare Tunnel via bundled `cloudflared.exe` |
| DB | SQLite via Dapper |
| Credential encryption | Windows DPAPI (`ProtectedData`) |
| MT5 | Python sidecar (`MetaTrader5` PyPI package, official MetaQuotes) over ZeroMQ (NetMQ on C# side, pyzmq on Python side) |
| NinjaTrader | NT8 ATI TCP socket interface |
| Telegram | `Telegram.Bot` NuGet |
| Discord | `System.Net.Http` to webhook URL |
| Logging | Serilog → rolling file + in-app viewer |
| Tests | xUnit + FluentAssertions |
| Installer | Inno Setup |
| License | MIT |

### Non-negotiable constraints

- **No paid services, no trial-limited services.** Everything end-to-end must be free forever.
- **MT5 must work without an installed EA.** Use the official MetaQuotes `MetaTrader5` Python package via a bundled Python sidecar. The user opens MT5 and logs in normally; that is the only requirement on their side.
- **No plaintext secrets ever.** All credentials encrypted with DPAPI before hitting SQLite.
- **Strict JSON only** for TradingView alerts. Generate the alert template from the schema; do not hand-maintain a separate template.
- **Two-way MT5**: orders out + account/positions/balance/history pulled back into the UI.
- **Dry-run mode**: global toggle + per-rule override. When on for a destination, log "would have sent X" without actually sending.

### Signal schema (canonical, treat as the contract)

```json
{
  "alert_id": "uuid",
  "strategy_id": "string",
  "account_tag": "string",
  "symbol": "string",
  "action": "BUY | SELL | CLOSE | MODIFY",
  "order_type": "MARKET | LIMIT | STOP",
  "entry_price": "number | null",
  "stop_loss": "number | null",
  "take_profit": "number | null",
  "lot_size": "number | null",
  "risk_percent": "number | null",
  "timeframe": "string",
  "timestamp": "ISO 8601 UTC",
  "comment": "string | null",
  "secret": "string"
}
```

Define this once in `docs/signal-schema.json` and have all code generate from / validate against it.

### Routing model

Rule-based engine. Each `Rule` has:
- `name`
- match conditions: `strategy_id`, `symbol`, `action`, `account_tag`, `timeframe` (any combination, wildcards allowed)
- `destinations`: list of channel IDs
- `priority` (lower = evaluated first)
- `continue_on_match` (bool — if true, keep evaluating subsequent rules)
- `dry_run_override` (nullable bool)
- `lot_multiplier` (default 1.0)
- `enabled` (bool)

Stored in SQLite, edited via UI, exportable to JSON for backup.

---

## 2. Step 0 — Set up Claude Code infrastructure FIRST

Before writing any feature code, create the following inside the project root. This is the most important step for token efficiency over the lifetime of this project.

### 2.1 `CLAUDE.md` (project root)

Concise, reference-style. Include:
- 1-paragraph project summary
- Tech stack table (mirror Section 1)
- Directory layout with one-line description per folder
- Coding conventions (see Section 3)
- Build/test/run commands (`dotnet build`, `dotnet test`, `dotnet run --project src/TVBridge.App`, `python sidecar/mt5_bridge/main.py`)
- "Before committing" checklist
- Pointer to `docs/BUILD_PLAN.md` for the current build phase
- Pointer to skills: "When you need to add a channel, use the `add-channel` skill. When you need to add a routing rule type, use `add-rule`. When you change the signal schema, use `regenerate-tv-template` afterward."

Keep CLAUDE.md under 200 lines. It's a reference card, not documentation.

### 2.2 `.claude/settings.json`

Configure:
- Hooks (see 2.4)
- Default permissions (allow Read/Grep/Glob always; require approval for Bash, Edit on `installer/`, anything in `secrets/`)
- Default model: `sonnet` (we'll override per-task with `/model`)

### 2.3 Skills — `.claude/skills/<name>/SKILL.md`

Create these skills with proper YAML frontmatter (`name`, `description`, optionally `allowed-tools`). Each skill description must be specific enough that Claude auto-invokes it correctly.

- **`add-channel`** — Step-by-step for adding a new output channel: create class in `src/TVBridge.Channels/<Name>/`, implement `IOutputChannel`, register in DI, add config UI page in WPF, write xUnit tests, update CLAUDE.md channel list.
- **`add-rule-condition`** — Adding a new match condition to the routing engine. Touches `Rule.cs`, `RuleEvaluator.cs`, the rule editor XAML, the SQLite schema (with migration), and tests.
- **`regenerate-tv-template`** — Reads `docs/signal-schema.json`, regenerates `docs/tradingview-template.txt` (the Pine Script `alert()` payload users paste into TradingView), and updates the in-app "Copy template" button output.
- **`db-migration`** — Create a new SQLite migration file in `src/TVBridge.Storage/Migrations/`, version it, write up + down, and add to the migration runner.
- **`release-build`** — Run `dotnet publish -c Release -r win-x64 --self-contained`, copy Python embeddable + sidecar, copy `cloudflared.exe`, run Inno Setup, output installer to `dist/`.
- **`security-audit`** — Run before any release: grep for hardcoded secrets, verify all credential reads use DPAPI, check that no `.env` or `appsettings.Development.json` is in the publish output, verify webhook secret validation is enabled.

### 2.4 Hooks — `.claude/hooks/`

Configure in `settings.json` and create the scripts:

- **PreToolUse / Bash** — Block destructive commands: `rm -rf`, `del /s`, `format`, `git push --force`, anything touching `dist/` or `installer/output/` without explicit confirmation.
- **PostToolUse / Edit|Write on `*.cs`** — Run `dotnet format` on the changed file.
- **PostToolUse / Edit|Write on `*.py`** — Run `ruff check --fix` and `ruff format`.
- **PostToolUse / Edit|Write on `*.xaml`** — Validate XAML parses (use `xmllint` or a small dotnet script).
- **PreToolUse / Edit on anything in `secrets/` or `*.pfx` or `appsettings.Production.json`** — Hard block, require user override.
- **SessionStart** — Print: current git branch, uncommitted file count, current build phase from `docs/BUILD_PLAN.md`, last test run status if cached.
- **Stop** — Run `dotnet build --no-restore` quickly and warn if it fails before ending the turn.

### 2.5 Subagents — `.claude/agents/`

Each is a focused specialist with its own context. Use them aggressively to keep main context clean.

- **`code-reviewer`** — Reviews any diff for: hardcoded secrets, missing null checks, missing DPAPI usage on credential paths, unhandled `IDisposable`, missing `ConfigureAwait(false)` in library code, missing tests for public methods. Read-only: `Read`, `Grep`, `Glob`.
- **`test-writer`** — Given a C# class or Python module, writes xUnit / pytest tests covering happy path, edge cases, error paths. Has `Read`, `Grep`, `Edit`, `Write`, `Bash(dotnet test:*)`, `Bash(pytest:*)`.
- **`wpf-ui`** — XAML/WPF specialist. Knows MVVM patterns, Material Design, data binding, async UI. Use for any view/viewmodel work.
- **`python-mt5`** — Python sidecar specialist. Knows the official `MetaTrader5` package quirks, ZeroMQ patterns, async Python. Use for anything in `sidecar/`.
- **`docs-scribe`** — Updates `CLAUDE.md`, `README.md`, and `docs/` after features land. Run after every completed phase.

### 2.6 Token efficiency rules (put these in CLAUDE.md too)

- **Default model is Sonnet.** Switch to Haiku (`/model haiku`) for: file moves, formatting fixes, simple renames, dependency bumps, doc typo fixes, test stub generation.
- **Switch to Opus** (`/model opus`) only for: architectural changes, debugging tricky concurrency issues, the routing engine, the Python↔C# IPC protocol design.
- **Use plan mode** (`shift+tab` or `/plan`) for any task that touches more than 3 files. Get approval, then execute.
- **Spawn subagents** for: writing tests (use `test-writer`), reviewing diffs (use `code-reviewer`), exploring unfamiliar code (use `Explore`). Subagents have their own context — use them to keep main context lean.
- **Compact proactively** when context > 60%. Run `/compact` with a focus instruction, e.g. `/compact keep the routing engine design and current task, drop file reads`.
- **Never re-read large files you've already read this session** unless they've been edited. Refer to your prior reading.
- **Batch tool calls** when you need to read multiple files — use parallel `view` calls in one message.

---

## 3. Coding conventions (put in CLAUDE.md)

- **C#**: nullable reference types ON, `record` for DTOs, `sealed` by default, async all the way down, `ConfigureAwait(false)` in libraries (not in WPF code-behind), DI everywhere, no static state except logging.
- **Python**: type hints everywhere, `ruff` clean, no global state, `asyncio` for I/O, use `pydantic` for the signal schema model.
- **Naming**: C# PascalCase for public, _camelCase for private fields, no Hungarian. Python snake_case.
- **Tests**: AAA pattern, one assert per test where reasonable, FluentAssertions on C# side, `pytest` parametrize on Python side. Aim for 80%+ on `Core` and `Channels` projects.
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `test:`).
- **Logging**: Serilog structured logging, never log secrets, log signal IDs not signal contents at Info level.

---

## 4. Directory scaffold to create

```
TVBridge/
├── .claude/
│   ├── settings.json
│   ├── hooks/
│   ├── skills/
│   └── agents/
├── .gitignore                 (Visual Studio + Python + Inno Setup)
├── CLAUDE.md
├── README.md
├── LICENSE                    (MIT, current year, your name placeholder)
├── TVBridge.sln
├── docs/
│   ├── architecture.md
│   ├── signal-schema.json
│   ├── tradingview-template.txt    (generated)
│   ├── BUILD_PLAN.md
│   └── adr/                   (architecture decision records)
├── src/
│   ├── TVBridge.App/          (WPF entry point, DI composition root)
│   ├── TVBridge.Core/         (signal models, rule engine, interfaces)
│   ├── TVBridge.Webhook/      (Kestrel listener, signal validator)
│   ├── TVBridge.Storage/      (SQLite + Dapper + migrations + DPAPI helper)
│   ├── TVBridge.Tunnel/       (cloudflared process manager)
│   └── TVBridge.Channels/
│       ├── Abstractions/      (IOutputChannel)
│       ├── MT5/               (talks to Python sidecar)
│       ├── Telegram/
│       ├── Discord/
│       └── NinjaTrader/
├── sidecar/
│   └── mt5_bridge/
│       ├── main.py
│       ├── pyproject.toml
│       ├── requirements.txt
│       └── README.md
├── tests/
│   ├── TVBridge.Core.Tests/
│   ├── TVBridge.Channels.Tests/
│   ├── TVBridge.Webhook.Tests/
│   └── sidecar_tests/
├── installer/
│   ├── tvbridge.iss
│   └── assets/                (icon, license rtf)
└── tools/
    └── cloudflared/           (binary downloaded at first build, gitignored)
```

---

## 5. Build plan — write this into `docs/BUILD_PLAN.md`

Each phase ends with: green tests, updated CLAUDE.md (via `docs-scribe`), and a git commit. Do not start a phase without my approval.

**Phase 1 — Foundation**
- Solution + all projects scaffolded
- `IOutputChannel` interface, `Signal` record, `Rule` record
- SQLite schema v1 + migration runner + DPAPI helper
- Serilog wired up
- Empty WPF shell with navigation skeleton (no real pages)
- xUnit smoke test runs green

**Phase 2 — Webhook + routing core**
- Kestrel listener on configurable local port
- Signal JSON validation against schema
- Rule engine with full match/priority/continue logic
- Dry-run mode plumbing
- 90%+ test coverage on `TVBridge.Core` and `TVBridge.Webhook`

**Phase 3 — Cloudflare Tunnel**
- `cloudflared.exe` lifecycle manager (start/stop/restart/health)
- First-run wizard: opens browser to Cloudflare login, captures tunnel URL
- URL displayed prominently in UI with copy button

**Phase 4 — Python MT5 sidecar (two-way)**
- Python project with `MetaTrader5`, `pyzmq`, `pydantic`
- ZeroMQ REQ/REP for commands, PUB/SUB for account state stream
- Commands: connect, disconnect, place_order, modify, close, get_positions, get_balance, get_history
- C# `Mt5Channel` implementing `IOutputChannel` + a `Mt5AccountService` for the pull side
- Sidecar process lifecycle managed by C# app
- Integration tests against MT5 demo account (skip if env var `MT5_DEMO_LOGIN` not set)

**Phase 5 — Telegram + Discord channels**
- Telegram: bot token + chat ID config, formatted message templates
- Discord: webhook URL config, embed formatting
- Per-channel message templates editable in UI

**Phase 6 — NinjaTrader channel**
- NT8 ATI socket client
- Symbol mapping (TV → NT8 instrument)
- Order routing

**Phase 7 — UI completion**
- Connection page (tunnel status, webhook URL, secret)
- Channels page (per-channel enable/config/test-send button)
- Rules page (visual editor, drag to reorder priority)
- Signals page (live log, filter, replay button)
- Settings page (DPAPI key rotation, log level, autostart, dry-run global)
- TradingView template generator page

**Phase 8 — Packaging**
- `release-build` skill end-to-end
- Inno Setup installer with: app, Python embeddable, sidecar, cloudflared.exe
- Code-signing placeholder (note: real cert is the one paid item — leave a TODO)
- SmartScreen warning documentation in README

**Phase 9 — Polish**
- Crash reporter (local file, optional upload)
- Auto-update check (GitHub Releases API, no telemetry)
- README with screenshots, GIFs, install guide
- LICENSE + CONTRIBUTING + SECURITY.md

---

## 6. What I want you to do RIGHT NOW

1. Confirm you've read everything by replying with a 5-bullet summary of the plan and any clarifying questions (max 3 questions).
2. Create the full directory scaffold from Section 4 (empty/stub files are fine for now, but `.csproj` files should be valid and `dotnet sln add` everything).
3. Create all `.claude/` infrastructure from Section 2 (CLAUDE.md, settings.json, every skill, every hook, every subagent).
4. Write `docs/BUILD_PLAN.md` from Section 5.
5. Write `docs/signal-schema.json` from Section 1.
6. Write `docs/architecture.md` summarizing this prompt (so future sessions can reload it without me re-pasting).
7. Initial git commit: `chore: bootstrap project structure and Claude Code infrastructure`.
8. Enter **plan mode** and present your Phase 1 plan for my approval. Do not start Phase 1 until I approve.

Use `/model haiku` for the scaffold creation (it's mostly file boilerplate). Switch back to `sonnet` before presenting the Phase 1 plan.

If anything in this prompt conflicts or is unclear, **ask before assuming**. Do not invent requirements.

Begin.
