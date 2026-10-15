# Proposal

## Why

The workshop room runs a notIRC server, a WebSocket group chat with one shared channel, and there is no client for it. This change builds a desktop client so a person can join the channel, read what others say and reply, in a client that is comfortable to use and keeps working when the connection drops.

## What Changes

- Add a desktop client that connects to a notIRC server using an address, token and nickname supplied through config or launch settings.
- Join the channel with the configured nickname. When the nickname is taken, prompt for another one at runtime.
- Show channel messages in arrival order with client-side timestamps and no cap on how many stay visible. Own messages are visually distinguished.
- Let the user send messages, with a byte-based (UTF-8, 1024-byte) warning before sending and clear handling of server rejections.
- Show a presence list kept current from the protocol events, plus join and leave notices in the chat.
- Reconnect automatically after a drop and re-join. Retry the same nickname quietly a couple of times before prompting. Mark the gap with a "you may have missed messages" notice.
- Stop with a clear error on a bad token (HTTP 401) instead of retrying. Refuse sending while disconnected, with no queueing.
- Show the connection status at all times: connecting, joined, reconnecting.
- Ignore unknown server message types silently.

Out of scope: history or scrollback, multiple channels, private messages, `/commands`, a persisted local log, notifications, sounds and themes, edit, delete and reactions, and multiple simultaneous sessions.

## Capabilities

### New Capabilities
- `channel-session`: Connecting to the server, joining with a nickname, nick-taken retry, auto-reconnect with re-join, bad-token handling and the visible connection status.
- `chat-messaging`: Displaying channel messages (order, timestamps, own-message distinction, missed-messages marker) and sending messages (byte limit, rejection feedback, refusal while disconnected).
- `channel-presence`: The presence list and the join and leave notices, driven by the protocol's presence events.
- `client-configuration`: Supplying server address, token and nickname at launch from a config file, environment variables and command-line arguments.

### Modified Capabilities
<!-- None: the project has no existing specs. -->

## Impact

- New code only, in a new solution: a protocol and connection library, an Avalonia desktop app and a test project. The stack and structure are recorded in `docs/adrs/ADR-0001-avalonia-dotnet-desktop-client.md`.
- Depends on `NOTIRC.md` as the sole source of truth for server behaviour. It adds no server-side work.
- New developer dependencies are logged in `docs/dependencies.md` (.NET 10 SDK, Avalonia templates, optional Docker for a local server).
- Nothing is persisted and nothing is deployed. The client runs locally.
