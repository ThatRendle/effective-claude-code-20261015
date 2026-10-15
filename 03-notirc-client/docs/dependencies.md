# Dependencies

<!-- Maintained by /emmz:architecture; verified by `make deps`. -->

| Tool | Why | Version | Check | Auth check | Install / auth |
|---|---|---|---|---|---|
| dotnet | build, test, format, run (pinned by `global.json`; the installed 11 RC SDK must not be picked) | >=10.0 <11 | `dotnet --list-sdks \| grep -q '^10\.0\.'` | — | `brew install --cask dotnet-sdk` (macOS) |
| avalonia templates | scaffold the Avalonia 12 app project | >=12.0 | `dotnet new list avalonia 2>/dev/null \| grep -q Avalonia` | — | `dotnet new install Avalonia.Templates` |
| openspec | change tracking | any | `openspec --version` | — | `npm i -g @fission-ai/openspec` |
| gh | PRs, releases | any | `gh --version` | `gh auth status` | `gh auth login` |
| docker | optional: run a local notIRC server for integration checks (`ghcr.io/thatrendle/notirc`) | any | `docker --version` | — | `brew install orbstack` (macOS) |
