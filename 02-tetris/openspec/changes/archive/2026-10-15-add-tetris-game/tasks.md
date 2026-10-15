# Tasks

## 1. Project setup

- [x] 1.1 Scaffold a vanilla TypeScript + Vite project (`package.json`, `tsconfig.json` strict, `index.html`, `src/main.ts`) and verify `npm install && npm run build` succeeds
- [x] 1.2 Add Vitest with a `test` script and a trivial passing test, and verify `npm test` runs it

## 2. Pieces, board and bag

- [x] 2.1 Implement the seven tetromino shapes with four rotations each in `src/core/pieces.ts`, with tests that every kind has 4 cells in every rotation
- [x] 2.2 Implement the 10x20 board, collision check and piece placement in `src/core/board.ts`, with tests for walls, floor and locked-cell collisions
- [x] 2.3 Implement the 7-bag with injectable rng in `src/core/bag.ts`, with a test that every group of 7 pieces from a bag boundary contains each kind once

## 3. Game rules

- [x] 3.1 Implement `newGame`, spawn, `left`/`right` and `tick` gravity in `src/core/game.ts`, with tests for movement, wall blocking, falling and locking on the floor
- [x] 3.2 Implement rotation with the left-then-right wall kick, with tests for open rotation, kick, and impossible rotation
- [x] 3.3 Implement soft drop and hard drop (no points), with tests for one-row soft drop, lock when blocked, and hard drop landing
- [x] 3.4 Implement line clearing and scoring (100/300/500/800), with tests for 0, 1, 2, 3 and 4 line clears and row shifting
- [x] 3.5 Implement next-piece preview promotion, game over on blocked spawn, input ignored after game over, and `restart`, with tests for each scenario in the spec

## 4. Browser shell

- [x] 4.1 Implement the canvas renderer for the board, current piece and next-piece preview, plus score and game over display in `src/render.ts` and `index.html`, and verify it visually in the browser at `npm run dev`
- [x] 4.2 Wire keyboard input (arrows, Space, Enter, `preventDefault` on handled keys) and the 800 ms gravity timer in `src/main.ts`, and verify in the browser that every control works, including restart after game over

## 5. Integration check

- [x] 5.1 Play a full game in the browser through a line clear and a game over, then verify `npm test` and `npm run build` both pass
