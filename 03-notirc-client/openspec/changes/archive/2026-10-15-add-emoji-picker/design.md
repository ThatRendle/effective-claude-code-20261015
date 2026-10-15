# Design

## Context

`MainWindow.axaml` has a `TextBox` (`InputBox`) bound to `MainViewModel.InputText`; the byte counter, over-limit warning and `CanSend` all derive from that property via `MessageLimit` (UTF-8 bytes, ADR-0001). Emoji already flow through this path unchanged. Avalonia 12 ships no emoji picker, and the log and input render emoji through the OS font fallback. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Insert an emoji at the caret with one click, without touching the byte-limit or send logic.
- Keep caret and selection handling in the view, where the `TextBox` state lives.

**Non-Goals:**
- Shortcodes, search, skin tones, recents, custom emoji, or shipping an emoji font.
- Changing `NotIrc.Core`.

## Decisions

- **Popup via `Button` + `Flyout`.** An emoji `Button` beside the input opens a `Flyout` holding a category-grouped grid of emoji buttons. Alternative: a separate `Window` — heavier, steals focus, awkward in headless tests.
- **Curated static catalogue, no package.** A small `EmojiCatalogue` in `NotIrc.App` lists a few dozen common emoji per category (smileys, gestures, hearts, objects, symbols), restricted to single-codepoint-sequence emoji that render without ZWJ support. Alternative: a NuGet emoji dataset — new dependency (needs a `docs/dependencies.md` row) and thousands of glyphs to render, for no stated requirement.
- **Insert in code-behind, not the view-model.** The caret and selection belong to the `TextBox`; the handler replaces `SelectionStart..SelectionEnd` using `TextBox.Text`, sets `CaretIndex` after the insertion, and focuses `InputBox`. The two-way binding then updates `InputText`, so the counter and `CanSend` follow with no new view-model code. Alternative: push caret position into the view-model — extra state for one feature.
- **Insert whole strings only.** Emoji are inserted as complete `string` values, so surrogate pairs are never split; the caret moves by `emoji.Length` UTF-16 units.
- **Keep the picker open after a pick.** Lets the user add several emoji; it closes on click-away or Escape (the default `Flyout` behaviour).
- **No gating on connection state.** The picker is always enabled, matching typed text, which is also editable while disconnected.

## Risks / Trade-offs

- [Emoji render as boxes on a machine without an emoji font] → Out of scope; same limitation as received messages. Note it in the devlog if seen.
- [Picker look and feel needs a human eye] → Per CLAUDE.md, UI appearance is human verification: the tasks stop with a copy-pasteable recipe.
- [Headless tests cannot prove glyph rendering] → Tests assert inserted text, caret, focus and byte counter, not pixels.
