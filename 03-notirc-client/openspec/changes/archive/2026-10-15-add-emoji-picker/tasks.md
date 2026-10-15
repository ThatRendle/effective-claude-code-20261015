# Tasks

## 1. Emoji catalogue and insertion

- [x] 1.1 Add `EmojiCatalogue` in `NotIrc.App` with categories of single-sequence emoji, plus a unit test asserting no duplicates, no empty categories and every entry non-empty
- [x] 1.2 Add a pure insertion helper (text, selection start/end, emoji → new text and caret index) and unit tests for caret insert, selection replace, start/end of text and non-BMP emoji (surrogate pair never split)

## 2. Picker UI

- [x] 2.1 Add the emoji button and category-grouped `Flyout` grid beside `InputBox` in `MainWindow.axaml`; verify `make build` passes with `-warnaserror` (compiled bindings)
- [x] 2.2 Wire the pick handler in `MainWindow.axaml.cs` to use the helper, set the caret, refocus `InputBox`; headless `WindowTests` assert text, caret, focus, and insertion while not joined
- [x] 2.3 Headless test: inserting a 4-byte emoji at 1022 bytes shows `1026/1024`, the "Too long" warning, and a disabled Send button; removing it clears them

## 3. Human verification and docs

- [x] 3.1 Stop for human verification of look and feel (emoji render, flyout placement, light/dark theme) with a copy-pasteable `dotnet run` recipe; record Emmz's result in the devlog
- [x] 3.2 Mention the emoji button in `README.md` and verify the text matches the running app

## Workflow follow-up

- Run `/code-review` on the change branch if requested, then archive with `/opsx:archive` once review fixes are done.
