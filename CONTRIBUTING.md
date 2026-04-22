# Contributing to TVBridge

Thanks for your interest in contributing! TVBridge is a community-driven project and we welcome all contributions.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/your-username/tvbridge.git`
3. Create a branch: `git checkout -b feat/my-feature`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit using [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `chore:`, `docs:`, `test:`
7. Push and open a Pull Request

## Development Setup

- .NET 8 SDK
- Python 3.11+ (for sidecar tests)
- An IDE: Visual Studio 2022, VS Code, or Rider

```bash
dotnet build          # Build all projects
dotnet test           # Run all C# tests
cd sidecar/mt5_bridge && pytest  # Run Python tests
```

## Code Conventions

- **C#**: Nullable refs ON, `record` for DTOs, `sealed` by default, async all the way, `ConfigureAwait(false)` in library code
- **Python**: Type hints everywhere, `ruff` for linting, `asyncio` for I/O, `pydantic` models
- **Tests**: AAA pattern, one assert per test, FluentAssertions (C#), pytest parametrize (Python)
- **Commits**: Conventional Commits format
- **Secrets**: NEVER log or store credentials in plaintext

## Adding a New Output Channel

Use the `add-channel` Claude Code skill, or follow the pattern in `src/TVBridge.Channels/`:

1. Create a folder under `src/TVBridge.Channels/YourChannel/`
2. Implement `IOutputChannel` (defined in `TVBridge.Core`)
3. Add a config class
4. Register in DI (`src/TVBridge.App/App.xaml.cs`)
5. Add tests in `tests/TVBridge.Channels.Tests/YourChannel/`

## Reporting Issues

- Use GitHub Issues
- Include: steps to reproduce, expected vs actual behavior, TVBridge version, Windows version
- For crashes: attach the crash report from `%LocalAppData%\TVBridge\crashes\`
