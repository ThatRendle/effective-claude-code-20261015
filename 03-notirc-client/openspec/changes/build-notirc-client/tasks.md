# Tasks

## 1. Solution scaffold and gates

- [x] 1.1 Add `global.json` pinning SDK 10.0.x (`rollForward: latestFeature`, `allowPrerelease: false`); verify `dotnet --version` prints a 10.0.x version in the repo root
- [x] 1.2 Create `NotIrc.slnx` with `NotIrc.Core` (class library), `NotIrc.App` (Avalonia 12 app, compiled bindings on) and `NotIrc.Tests` (xUnit v3 with `Avalonia.Headless.XUnit`), plus `.gitignore` entries for build output and `notirc.json`; verify `dotnet build NotIrc.slnx --configuration Release -warnaserror` succeeds
- [x] 1.3 Enable analyzers in build (`EnforceCodeStyleInBuild`, `.editorconfig`) and add one trivial passing test; verify `dotnet test --solution NotIrc.slnx --configuration Release` runs it and exits non-zero when the test is made to fail. If the command needs a `global.json` runner setting or differs, fix it and update the gate table in design.md
- [x] 1.4 Verify `dotnet format NotIrc.slnx --verify-no-changes` exits 0 on the scaffold and non-zero on a deliberately misformatted file

## 2. Protocol and transport (Core)

- [x] 2.1 Define the protocol message types and JSON encode/decode for join, message and the server messages in `NOTIRC.md`, with unknown types parsed as "ignored"; verify unit tests round-trip every documented message and ignore an unknown one
- [x] 2.2 Add the transport abstraction and the `ClientWebSocket` implementation, with HTTP 401 on connect reported as a distinct unauthorized failure; verify unit tests against a scripted fake cover send, receive and close, and verify the unauthorized path with a loopback test server
- [x] 2.3 Add the UTF-8 byte-limit check (1024) as a pure function; verify tests cover ASCII, multibyte, exactly 1024 bytes and 1025 bytes

## 3. Session state machine (Core)

- [x] 3.1 Implement connect, join and the status stream (connecting, joined, reconnecting, stopped with reason); verify tests for the "Connect and join on launch" and "Visible connection status" scenarios, including no send before join is accepted
- [x] 3.2 Implement nick-taken handling through the nickname-prompt callback (re-prompt, null result leaves it connected but not joined); verify tests for the "Prompt for another nickname when taken" scenarios
- [x] 3.3 Implement reconnect with 1s/2s/4s/8s/15s delays via `TimeProvider`, rejoin, 401 stopping the loop, and 3 quiet same-nick retries 1s apart before prompting; verify tests with fake transport and fake time for every scenario in "Reconnect automatically after a drop", "Quietly retry the same nickname on reconnect" and "Bad token stops the client"
- [x] 3.4 Implement presence (initialised and rebuilt from each join, updated on joined and left) and the chat, notice and gap-marker event stream (no marker on first join, none for the initial list); verify tests for every scenario in `channel-presence` and "Mark possible missed messages after reconnect"
- [x] 3.5 Implement send rules (refuse when not joined without queueing, empty input ignored, over-limit blocked, server too-long rejection surfaced); verify tests for the send scenarios in `chat-messaging`

## 4. Configuration

- [x] 4.1 Implement layered settings (optional `notirc.json`, `NOTIRC_` environment variables, `--server`/`--token`/`--nick`), server address normalisation to a connection URL, missing-setting errors and token redaction; verify tests for every `client-configuration` scenario, including precedence order
- [x] 4.2 Add `notirc.example.json` with a placeholder token and document launch options in `README.md`; verify the documented `dotnet run` command line works as written against the settings loader

## 5. Avalonia app

- [x] 5.1 Build the main window and view-models (status bar, message log with timestamps, notices and own-message styling, presence list, input with byte counter and send command) bound to the session; verify headless tests render the window, show over-limit state and disable sending when over the limit or not joined
- [x] 5.2 Add the nickname prompt dialog (pre-filled with the rejected nickname) wired to the session callback, and the startup error view for missing settings and rejected token; verify headless tests for the prompt result path and for the error view text
- [x] 5.3 Wire startup: load settings, create the session, start connecting, and shut down cleanly on close; verify `dotnet run --project NotIrc.App` starts with valid settings and reports a missing setting without connecting

## 6. Integration checks

- [x] 6.1 Run the client against the local Docker server (`ghcr.io/thatrendle/notirc`) with two instances; verify messages flow both ways, presence updates, a taken nickname prompts, an over-limit message is blocked, and stopping and restarting the server triggers reconnect with the gap notice
- [x] 6.2 Run all three gates (build, test, format) from the repository root; verify each exits 0

## 7. Fix: own-message contrast

- [x] 7.1 Replace the opaque light accent background on own messages with a translucent accent tint so the text stays readable in light and dark themes; verify a headless test that an own row has a translucent (not opaque) background and a non-own row has none

## Workflow follow-up

- Run `/emmz:setup` to add the Makefile gate targets, `make deps` and the workflow section of `CLAUDE.md` before starting.
- Archive the change after review and a run against the live server in the room.
