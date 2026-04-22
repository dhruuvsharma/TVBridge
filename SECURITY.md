# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in TVBridge, please report it responsibly:

1. **Do NOT open a public GitHub issue**
2. Email the maintainers privately (see repository contacts)
3. Include: description of the vulnerability, steps to reproduce, potential impact
4. We will acknowledge within 48 hours and provide a fix timeline

## Security Design

TVBridge takes security seriously:

- **Credentials**: All secrets (API keys, bot tokens, webhook secrets) are encrypted with Windows DPAPI before storage. Plaintext secrets never touch disk or logs
- **Webhook authentication**: Every incoming webhook is validated against a shared secret
- **Local-only**: No data is sent to external servers except through user-configured channels
- **No telemetry**: TVBridge collects zero usage data. The update checker only fetches the latest release tag from GitHub's public API
- **Crash reports**: Stored locally only (`%LocalAppData%\TVBridge\crashes\`). Never uploaded automatically
- **Network**: The only outbound connections are: Cloudflare Tunnel (user-initiated), configured channels (Telegram Bot API, Discord webhook, MT5 terminal, NinjaTrader ATI), and GitHub Releases API (update check)

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | Yes       |

## Scope

In scope:
- Authentication bypass (webhook secret validation)
- Credential exposure (DPAPI bypass, log leakage)
- Remote code execution
- SQL injection in signal/rule storage
- Process injection via sidecar or tunnel manager

Out of scope:
- Attacks requiring physical access to the machine
- Social engineering
- Denial of service against the local webhook endpoint
- Vulnerabilities in third-party dependencies (report upstream)
