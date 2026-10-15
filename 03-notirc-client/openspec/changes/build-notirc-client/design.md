# Design

## Context

Greenfield: the repository holds only `NOTIRC.md` (the protocol, and the sole source of truth for server behaviour), `PROMPT.md` and this change. See proposal.md for motivation. Foundational choices are in `docs/adrs/ADR-0001-avalonia-dotnet-desktop-client.md` and the tool list in `docs/dependencies.md`.

Protocol facts the design leans on:
- A 401 arrives before the WebSocket upgrade, so a bad token is distinguishable from a network drop.
- `join_ok` carries the full user list (excluding the joiner), so presence can always be rebuilt.
- The server broadcasts a sender's own message back to them, with the nickname attached.
- There is no history and no application-level keepalive.
- The message limit is 1024 UTF-8 bytes, not characters.

The machine has both a 10.0 SDK and an 11 RC SDK installed, so an unpinned `dotnet` would silently use the RC.

## Goals / Non-Goals

**Goals:**
- Keep every behaviour that is hard to get right (reconnect, nick retry, 401 versus drop, byte counting, presence rebuild) in a UI-free library that can be tested with a fake transport and fake time.
- Keep the Avalonia project thin: views, view-models and wiring only.
- Make gates runnable from the repository root with one command each.

**Non-Goals:**
- Any server-side change, persistence, packaging or installers.
- Cross-platform release builds. The app should run where .NET 10 and Avalonia run, but only the developer's machine is exercised.

## Decisions

### Platform, language and structure
Avalonia 12.x desktop app, C# on .NET 10, solution `NotIrc.slnx`. Three projects: `NotIrc.Core` (no UI references), `NotIrc.App` (references Core), `NotIrc.Tests` (references both). Rationale and rejected alternatives are in ADR-0001. `global.json` pins SDK 10.0.x with `rollForward: latestFeature` and `allowPrerelease: false` so the 11 RC is never selected.

### Transport behind an interface
Core defines a small transport abstraction (connect with URL, send text, receive text, close) implemented over `ClientWebSocket` and `System.Text.Json`. The connection logic depends on the abstraction, so tests drive it with a scripted fake. An HTTP 401 on connect surfaces as a distinct "unauthorized" failure, separate from other connect failures. Alternative rejected: a third-party WebSocket library, which adds nothing the protocol needs.

### Session as an explicit state machine
One `ChannelSession` type in Core owns the lifecycle: Connecting, Joining, Joined, Reconnecting, Stopped (with reason). It raises events or exposes an observable stream of: status changes, chat messages, presence changes, notices, and a "nickname needed" request. Moves between states are the only place connection behaviour lives. Alternative rejected: putting reconnect logic in view-models, which would make it untestable without UI.

### Nickname prompt as a callback
When a nickname is rejected and the user must choose, the session awaits a delegate supplied by the app (`Func<string, Task<string?>>`, rejected nickname in, new nickname or null out). The app shows a dialog; tests supply a canned answer. Returning null leaves the session connected but not joined; the user can ask for the prompt again.

### Reconnect timing (resolves the open detail)
- Reconnect delays: 1s, 2s, 4s, 8s, then 15s for every later attempt. No jitter, since there is one client per user.
- Quiet nickname retries on reconnect: 3 attempts, 1 second apart, then prompt.
- Delays use `TimeProvider`, so tests advance time instead of waiting.
A 401 at any point moves to Stopped and ends the loop.

### Byte counting
The 1024 limit is measured with `Encoding.UTF8.GetByteCount` on the exact text to be sent, with no trimming surprises: the check and the send use the same string. The input shows used bytes against the limit once the text is within a warning margin, and blocks sending when over.

### Presence and messages in the view-model
View-models hold an `ObservableCollection` for the log (messages and notices, one ordered list) and another for presence. Presence is replaced wholesale on each `join_ok`. Own-message styling compares the sender with the session's current nickname. Timestamps come from `TimeProvider` at receipt. The log is unbounded, as specified.

### MVVM
CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`), with compiled bindings (`x:DataType` on every bound view), which is the Avalonia 12 default. Alternatives rejected: ReactiveUI (heavier than needed) and hand-written `INotifyPropertyChanged` (boilerplate).

### Configuration
`Microsoft.Extensions.Configuration` with providers added lowest to highest priority: optional `notirc.json`, environment variables with prefix `NOTIRC_`, then command-line arguments (`--server`, `--token`, `--nick`). `notirc.json` is listed in `.gitignore`; a checked-in `notirc.example.json` shows the shape without a real token. The server setting accepts a host with optional port or a full `ws://` / `wss://` URL; the client builds `<scheme>://<host>/ws?token=<token>`, defaulting to `wss://` for a bare host and `ws://` only when asked for. The token is never written to the UI or logs. Missing settings are reported before any connection attempt.

### Testing
- Core: xUnit v3 unit tests against the fake transport and fake time, covering each spec scenario for session, messaging and presence.
- App: a few `Avalonia.Headless.XUnit` smoke tests (main window renders, send disabled when over the limit, nickname prompt wiring).
- Manual and optional: a run against the local Docker server (`docker run -e NOTIRC_TOKEN=<token> -p 8080:8080 ghcr.io/thatrendle/notirc:latest`, then `--server ws://localhost:8080`).

### Gate commands
All run from the repository root, non-interactively, and exit non-zero on failure. There is one stack, so no prefix.

| Gate | Command |
|---|---|
| build | `dotnet build NotIrc.slnx --configuration Release -warnaserror` |
| test | `dotnet test --solution NotIrc.slnx --configuration Release` |
| format | `dotnet format NotIrc.slnx --verify-no-changes` |
| lint | none: Roslyn analyzers run inside build via `-warnaserror` and `EnforceCodeStyleInBuild` |

The test command is verified (task 1.3). xUnit v3 runs on Microsoft.Testing.Platform, and `dotnet test --solution` fails with `MSB1001: Unknown switch` unless `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`, which it now does. The command itself is unchanged.

## Risks / Trade-offs

- [Avalonia 12 is new, so documentation and package versions differ from older examples] → Use the installed 12.0.4 templates as the baseline and check the docs when a pattern is unfamiliar.
- [Test gate command may need extra setup for Microsoft.Testing.Platform] → Prove it in task 1 before any real tests depend on it.
- [Quiet retry plus backoff can leave the user waiting several seconds with no feedback] → The status shows reconnecting throughout, and a notice marks each rejoin.
- [A very rapid drop and reconnect loop could flood the log with gap notices] → One notice per successful rejoin, not per attempt.
- [The server could free nicknames more slowly than assumed] → The quiet retry count is a constant in one place and falls back to the prompt, so the worst case is a prompt.
- [Unbounded log grows without limit in a very long session] → Accepted by the user for a workshop room.

## Open Questions

- Exact visual design (layout, colours, fonts) is left to implementation, within the scenarios in the specs.
