# Proposal

## Why

There is no way to stop the game mid-play: gravity keeps dropping the piece while the player answers the door or checks something else. Escape is the conventional "stop everything" key.

## What Changes

- Pressing Escape during play pauses the game: gravity stops and board-changing input is ignored.
- Pressing Escape while paused resumes play.
- A "Paused" overlay (styled like the game over overlay) is shown while paused, and the Controls panel lists Escape.
- Escape has no effect after game over.

Assumption: Escape toggles pause/resume (one key for both), rather than needing a separate resume key.

## Capabilities

### New Capabilities

### Modified Capabilities
- `tetris`: adds a Pause requirement (pause, resume, overlay, input and gravity suspended while paused). No existing requirement text changes.

## Impact

- `src/core/game.ts`: new `paused` status and a pause-toggle action handled by `step`.
- `src/main.ts`: map `Escape` to that action.
- `src/render.ts`, `index.html`, `src/style.css`: pause overlay and controls list entry.
- `src/core/game.test.ts`: tests for the new behavior.
