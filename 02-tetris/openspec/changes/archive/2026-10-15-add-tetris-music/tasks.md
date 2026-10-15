# Tasks

## 1. Audio foundation

- [x] 1.1 Add `src/audio.ts`: single `AudioContext` created lazily, master gain, `unlock()` (resume on first key), `muted` state with `toggleMute()`
- [x] 1.2 Persist mute in `localStorage` (try/catch, default unmuted) and load it at startup

## 2. Music

- [x] 2.1 Add `src/music.ts` with the Korobeiniki melody as a note/duration table (frequency + beats) and a tempo constant
- [x] 2.2 Implement a Web Audio lookahead scheduler (square oscillator + gain envelope) that loops the melody
- [x] 2.3 Export `createMusic(audio)` returning `sync(status)`: play from the top when status is `"playing"` and not muted, stop and cancel queued notes otherwise

## 3. Sound effects

- [x] 3.1 Add synthesized effects to `src/audio.ts`: `hardDrop()`, `lineClear()`, `tetris()`, `gameOver()`; all silent when muted
- [x] 3.2 Add a pure helper mapping `(prevState, nextState, action)` to an effect name (hard drop; score delta 100/300/500 → line clear; 800 → four-line clear; status → `"gameOver"`), with the line-clear sound taking priority over the thud

## 4. Wire into the game

- [x] 4.1 In `src/main.ts`, handle `M` before the `KEY_ACTIONS` lookup to toggle mute without calling `step`
- [x] 4.2 Unlock audio on the first keydown; call `music.sync(state.status)` after each state change, unlock and mute toggle
- [x] 4.3 In `dispatch`, play the effect returned by the helper from 3.2
- [x] 4.4 Add a `#muted` indicator (shown while muted) and an `M` entry in Controls in `index.html`, with matching styles in `src/style.css`

## 5. Verify

- [x] 5.1 Unit-test the melody table and the effect-mapping helper next to the existing tests
- [x] 5.2 Run the existing tests and type-check/build
- [x] 5.3 Manually check in the browser: silent before first key; music plays while playing; stops on Escape and returns on resume; stops on game over with the game-over sound; restarts from the top on Enter; hard drop, line clear and four-line clear sounds; `M` mutes everything in every status without changing the game; mute survives a reload
