# Proposal

## Why

The workshop exercise calls for a playable Tetris game that runs in the browser with nothing more than Node.js and a browser. The directory is empty, so this change builds the game from scratch.

## What Changes

- Add a browser Tetris game: 10x20 board, the seven tetrominoes, a next-piece preview.
- Keyboard controls: move left/right, rotate, soft drop, hard drop; Enter restarts after game over.
- Gravity at a fixed rate, line clears, and a score (100/300/500/800 for 1/2/3/4 lines).
- Game over when a new piece cannot spawn.
- 7-bag randomizer and collision-checked rotation with simple wall kicks.
- A pure, DOM-free game core with unit tests, separate from a canvas renderer.
- Out of scope: hold piece, ghost piece, levels, speed-up, pause, drop-distance points, lock delay.

## Capabilities

### New Capabilities
- `tetris`: the playable game: board and pieces, controls, gravity, line clears, scoring, next-piece preview, game over and restart.

### Modified Capabilities

None. There are no existing specs.

## Impact

- New code in this directory: a vanilla TypeScript + Vite project (`package.json`, `index.html`, `src/`), plus a unit test setup for the game core.
- New dev dependencies: Vite, TypeScript and a unit test runner (Vitest). No runtime dependencies.
- Runs with `npm install` and `npm run dev`; no other tooling.
