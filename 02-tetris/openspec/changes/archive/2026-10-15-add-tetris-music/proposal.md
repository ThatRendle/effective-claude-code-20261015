# Proposal

## Why

The game is silent. The traditional Tetris theme (Korobeiniki, "Theme A") is what players expect, and it should only sound while there is actually a game being played, not over a pause or game-over screen. Players also need a way to turn sound off, and a few well-chosen sound effects make key moments feel good.

## What Changes

- Play a looping rendition of Korobeiniki while `status === "playing"`.
- Silence it while paused or after game over; resume it on unpause; restart it from the top on a new game.
- Add sound effects for key moments only: hard drop, line clear (1-3 rows), four-line clear, and game over.
- Add a mute toggle (the `M` key) that silences music and effects, and remember the setting between visits.
- Synthesize all sound in the browser with the Web Audio API, so no audio file, dependency or licensing concern is added. Korobeiniki is a public-domain folk song.
- Start audio on the first key press, since browsers block audio before a user gesture.

## Capabilities

### New Capabilities

### Modified Capabilities
- `tetris`: adds Background music, Sound effects and Mute requirements.

## Impact

- New `src/audio.ts` (audio context, mute state, effects) and `src/music.ts` (tune data + scheduler); `src/main.ts` drives them from game state and key presses.
- `index.html`: add `M` to the Controls list and a small muted indicator.
- No new dependencies. Game core (`src/core/*`) is untouched.
- Out of scope: volume control, move/rotate/lock/pause sound effects, per-level tempo.
