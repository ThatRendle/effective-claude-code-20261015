# Proposal

## Why

Emoji already travel through the client as plain text and count correctly against the 1024-byte limit, but the user has no way to enter one except an OS-level shortcut that varies by platform and is awkward on some. A picker beside the input makes emoji a first-class way to compose a message.

## What Changes

- Add an emoji button beside the input field that opens a picker of common emoji grouped by category.
- Choosing an emoji inserts it at the input's caret (replacing any selection), returns focus to the input and leaves the picker ready to close or pick again.
- The byte counter, "Too long" warning and send rules apply to inserted emoji exactly as to typed text; nothing is refused by the picker itself.
- No new protocol behaviour: emoji are sent as ordinary UTF-8 text.

## Capabilities

### New Capabilities

### Modified Capabilities
- `chat-messaging`: adds a requirement that the user can insert emoji into the input from a picker.

## Impact

- `NotIrc.App/MainWindow.axaml` and `.axaml.cs`: new button and picker popup beside `InputBox`.
- A small static emoji catalogue in `NotIrc.App` (no new package dependency).
- `NotIrc.Tests/App/WindowTests.cs`: headless window tests for the picker.
- Out of scope: `:shortcode:` expansion, emoji search, skin-tone variants, recent/favourite emoji, and bundling an emoji font (rendering relies on the OS font fallback).
