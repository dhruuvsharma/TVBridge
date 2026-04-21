# ADR 001: Python Sidecar for MT5 Communication

## Status
Accepted

## Context
MetaTrader 5 provides an official Python package (`MetaTrader5` on PyPI) by MetaQuotes for programmatic access. The alternative is writing a custom Expert Advisor (EA), which requires MQL5 development and user installation.

## Decision
Use a bundled Python sidecar process communicating with the C# host via ZeroMQ (REQ/REP for commands, PUB/SUB for streaming). The user only needs MT5 open and logged in.

## Consequences
- **Pro**: No EA installation required for users
- **Pro**: Official, maintained API by MetaQuotes
- **Pro**: Two-way communication (orders + account data)
- **Con**: Must bundle Python embeddable in installer (~30MB)
- **Con**: IPC adds latency vs in-process (mitigated by ZeroMQ speed)
