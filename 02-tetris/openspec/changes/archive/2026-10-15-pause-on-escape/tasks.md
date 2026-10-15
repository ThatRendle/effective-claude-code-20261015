# Tasks

## 1. Core pause logic

- [x] 1.1 Add `"paused"` status and `togglePause` action to `src/core/game.ts`; `step` toggles playing↔paused, ignores all other actions while paused, and ignores `togglePause` on game over. Verify with new unit tests in `src/core/game.test.ts` covering each spec scenario (pause, tick while paused, input while paused, resume, Escape after game over, Enter while paused) and `npm test` passing.

## 2. UI

- [x] 2.1 Map `Escape` → `togglePause` in `src/main.ts`; add `#paused` overlay to `index.html`, style it with the `.gameover` look in `src/style.css`, show/hide it in `src/render.ts`, keep the piece drawn while paused, and add Escape to the Controls list. Verify by running the app: Escape shows "Paused" and freezes the piece, Escape again resumes, and Escape on game over does nothing.
- [x] 2.2 Run `npm run build` (typecheck) and `npm test`; verify both succeed.
