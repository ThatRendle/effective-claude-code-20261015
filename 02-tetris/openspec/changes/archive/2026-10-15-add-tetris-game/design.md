# Design

## Context

Greenfield: the directory contains no code. See proposal.md for motivation and specs/tetris/spec.md for behaviour. Constraints: vanilla TypeScript and CSS, Vite for building, no UI framework, and the game must run with just Node.js and a browser.

## Goals / Non-Goals

**Goals:**
- A game core that is pure and deterministic enough to unit test without a DOM or timers.
- A thin shell (input, timer, rendering) that holds no game rules.

**Non-Goals:**
- Full SRS kick tables, lock delay, pause, hold, ghost, levels (see proposal).
- Touch or gamepad input, sound, persistence of high scores.

## Decisions

**Pure core, impure shell.** `src/core/` exports types and pure functions: `newGame(rng)`, `step(state, action)` where action is one of `left | right | rotate | softDrop | hardDrop | tick | restart`, returning a new state. State holds board, current piece, bag queue, next piece, score and status (`playing | gameOver`). The shell in `src/main.ts` maps key events and a gravity `setInterval` to actions, and calls the renderer with the new state. Alternative: a class with mutable fields. Rejected because immutable transitions make tests simple assertions on input and output.

**Injectable randomness.** The bag shuffle takes a `rng: () => number`, so tests can pass a seeded or scripted one. Production uses `Math.random`.

**Board representation.** A 20x10 array of cell values (`null` or piece kind). Pieces are a kind, rotation index (0-3) and an (x, y) origin; shapes are looked up from a table of four precomputed rotations per kind. Alternative: rotate matrices at runtime. Rejected because a table is simpler to verify against the spec.

**Rotation and kicks.** Rotate clockwise; if colliding, try dx = -1, then +1; else no-op. This is the "guideline-lite" decision. Alternative: SRS tables, rejected as out of scope.

**Gravity and drops.** `tick` moves down one row or locks if blocked. Soft drop is one row per keydown (relying on key repeat for hold). Hard drop computes the landing row and locks immediately. Locking is therefore always immediate when a downward move is blocked (no lock delay). Gravity interval is a constant of 800 ms.

**Lock, clear, spawn.** One `lock` routine merges the piece, clears full rows, adds the score from a table `[0, 100, 300, 500, 800]`, promotes the preview piece to current, draws a new preview, and sets `gameOver` if the spawn overlaps.

**Rendering.** One `<canvas>` for the board and a small second canvas for the preview, with score and game over text in DOM elements. Redraw the whole thing on every state change; the board is tiny.

**Tooling.** Vite + TypeScript (strict), Vitest for unit tests of `src/core/`. Scripts: `dev`, `build`, `test`. Rejected: no-build plain JS, since the project preference is Vite + TypeScript and `npm run dev` still needs only Node.js.

**Layout.**
```
index.html
src/
  core/   pieces.ts  bag.ts  board.ts  game.ts   (+ *.test.ts)
  render.ts
  main.ts
  style.css
```

## Risks / Trade-offs

- [Key repeat varies by OS, so soft drop speed differs] → Accept; soft drop is defined as one row per key press.
- [Timer drift or background-tab throttling affects gravity] → Acceptable for a workshop game; a throttled tab simply plays slower.
- [Simple kicks differ from the official game] → Documented as a deliberate scope choice.
- [Browser-default scrolling on arrow and Space keys] → Call `preventDefault` for the handled keys.
