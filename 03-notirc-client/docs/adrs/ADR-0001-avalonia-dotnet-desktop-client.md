# ADR-0001: Avalonia desktop client on .NET 10

- Status: Accepted
- Date: 2026-10-15

## Context

The notIRC client must connect to a WebSocket + JSON chat server (`NOTIRC.md` is the only source of truth),
show the shared channel, let the user send messages, keep a presence list, retry a taken nick, warn about the
1024-byte limit, and auto-reconnect. It should be comfortable to use. Server address, token and nickname come
from config or launch settings. No persistence, no history.

## Decision

- **Platform:** Avalonia 12.x desktop application.
- **Language / runtime:** C# on .NET 10, pinned by `global.json` (10.0.x, `allowPrerelease: false`) because the
  machine also has an 11 RC SDK that would otherwise be selected.
- **Structure:** `NotIrc.Core` (protocol, connection and reconnect state machine, nick and byte-limit logic, no UI),
  `NotIrc.App` (Avalonia views and view-models), `NotIrc.Tests`.
- **Transport:** built-in `ClientWebSocket` and `System.Text.Json`; no third-party networking library.

## Why

- Avalonia was the user's choice; it gives a native-feeling layout for the log, presence pane and input line.
- Keeping protocol logic out of the UI project makes the hard behaviour (401 versus drop, quiet nick retry on
  reconnect, byte counting) unit-testable against a fake transport.
- The protocol is plain WebSocket and JSON, which the base class library already covers.

## Alternatives rejected

- **Browser app (vanilla TypeScript + Vite):** the recommended default, but the user chose Avalonia.
- **.NET terminal app:** needs a TUI library for a polished layout and is fiddlier for prompts and live input.
- **Electron/Tauri wrapper:** adds a runtime and installers for no requirement.
- **Single app project:** protocol logic would sit beside UI code and be harder to test in isolation.

## Consequences

- Avalonia 12 uses compiled bindings by default (`x:DataType` required) and xUnit v3 for headless tests.
- Runs locally only; there is no hosting, datastore or deployment.
