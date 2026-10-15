# Design

## Context

Game logic is a pure reducer (`step(state, action)` in `src/core/game.ts`); `main.ts` maps keys to actions and runs a constant `setInterval` that dispatches `tick`; `render.ts` draws state and toggles the `#gameover` overlay. `status` is `"playing" | "gameOver"`. See specs for required behavior.

## Goals / Non-Goals

**Goals:**
- Keep pause in the pure core so it is unit-testable like the rest of the game.

**Non-Goals:**
- Auto-pause on tab blur, a pause button, or timer-accurate gravity resume.

## Decisions

- **Pause as a `status` value** (`"paused"`) plus a `togglePause` action, rather than a separate boolean in `main.ts`. `step` then ignores every other action while paused, using the same early-return shape as `gameOver`, so tick/input suppression needs no extra code in `main.ts`. Alternative (clearing the interval and gating keys in `main.ts`) puts game rules outside the tested core.
- **Escape toggles** pause/resume with one action; `togglePause` is a no-op on `gameOver`.
- **Keep the interval running**; ticks are simply ignored while paused. Alternative of stopping/restarting the timer adds state for no visible benefit.
- **Overlay**: a `#paused` element reusing the `.gameover` overlay styling, shown when `status === "paused"`. The falling piece stays drawn while paused (render condition becomes `status !== "gameOver"`).
- Add `Escape` to the Controls list in `index.html`.

## Risks / Trade-offs

- Resuming does not reset the gravity timer, so the first drop after resume may come sooner than 800 ms → acceptable for this game; noted rather than fixed.
- `restart` is currently a no-op while playing; it must stay a no-op while paused (covered by a scenario).
