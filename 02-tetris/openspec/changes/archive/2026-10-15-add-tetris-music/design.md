# Design

## Context

`main.ts` holds the current `GameState`, maps keys to actions and dispatches them through the pure `step` reducer; `status` is `"playing" | "paused" | "gameOver"`. Rendering is a function of state. There is no audio today. Browsers keep an `AudioContext` suspended until a user gesture.

## Goals / Non-Goals

**Goals:**
- Music is a pure function of `state.status` and the mute flag (audible iff `"playing"` and not muted), so pause/resume/game-over/restart need no special cases.
- Sound effects are derived from state transitions in `main.ts`; the core stays untouched.
- No assets, no dependencies.

**Non-Goals:**
- Volume control, effects for move/rotate/lock/pause, tempo changes with score (gravity is constant by spec).

## Decisions

- **Synthesize with Web Audio** (square-wave oscillators, short gain envelope) from a note table in `src/music.ts`, rather than shipping an mp3/midi. Avoids binary assets, licensing questions about recordings, and Vite asset handling. Korobeiniki is a public-domain folk melody. Trade-off: chip-tune sound, which suits Tetris.
- **Lookahead scheduler**: schedule notes a short window ahead using `AudioContext.currentTime`, driven by a `setInterval` (~100 ms), looping the melody. `setTimeout` per note drifts and stutters under load.
- **Shared audio module** (`src/audio.ts`): owns the single `AudioContext`, a master gain, and the `muted` flag. Music and effects connect to the master gain; muting sets the gain to 0 and music additionally stops its scheduler, so no work is done while muted.
- **Music API**: `createMusic(audio)` returns `{ sync(status) }`. `main.ts` calls it after each state change. It starts when status becomes `"playing"` (and not muted) and stops otherwise; starting always begins at the top of the theme, so resume after pause and unmute restart the tune. Simpler than tracking position; accepted.
- **Effects from transitions**: in `dispatch`, compare `state` and `next`. Hard drop: action is `hardDrop` and the piece changed. Line clears: score delta of 100/300/500 means 1-3 rows, 800 means four (the spec's scoring table makes this unambiguous, and nothing else awards points). Game over: status changed to `"gameOver"`. A line-clear or four-line sound replaces the hard-drop thud when both happen on the same action. `audio.effects` exposes `hardDrop()`, `lineClear()`, `tetris()`, `gameOver()`.
- **Mute key** `M` is handled in `main.ts`'s keydown handler before the `KEY_ACTIONS` lookup, since it is not a game action and must not go through `step`. It works in every status.
- **Persistence**: `localStorage` key such as `tetris.muted`, read at startup and written on toggle, both wrapped in try/catch (storage can be unavailable); failures fall back to unmuted.
- **Autoplay**: the first `keydown` calls `audioContext.resume()` and then syncs. Until then audio is a no-op.
- **Indicator**: a `#muted` element styled like the existing overlays/status text, shown when muted. `M` added to the Controls list in `index.html`.
- **Tests**: tune data and the transition-to-effect mapping are pure and can be unit-tested next to the existing tests; actual sound is verified manually.

## Risks / Trade-offs

- Resume and unmute restart the tune from the beginning rather than mid-phrase.
- A player who never presses a key hears nothing (browser policy; stated in the spec). A persisted mute applies from first key press.
- Inferring line clears from score delta couples `main.ts` to the scoring table; if scoring changes, the mapping must change with it. Alternative is returning events from `step`, which changes the core's contract for little gain here.
- Transcription errors in the melody are possible; checked by ear during manual verification.
