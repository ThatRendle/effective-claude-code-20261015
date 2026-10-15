## ADDED Requirements

### Requirement: Background music
The game SHALL play the Tetris theme (Korobeiniki) as looping background music, and SHALL play it only while the game status is playing. The music SHALL be silent while the game is paused and after game over. Because browsers require a user gesture before audio can start, music SHALL begin on the player's first key press.

#### Scenario: Music plays during play
- **WHEN** a game is in progress and the player has pressed a key at least once
- **THEN** the theme is audible and loops when it reaches the end

#### Scenario: No music before first key press
- **WHEN** the page has loaded and the player has not yet pressed any key
- **THEN** no music plays and the game is otherwise unaffected

#### Scenario: Pause silences music
- **WHEN** the game is playing with music and the player presses Escape
- **THEN** the music stops

#### Scenario: Resume restarts music
- **WHEN** the game is paused and the player presses Escape
- **THEN** the music plays again

#### Scenario: Game over silences music
- **WHEN** the game ends
- **THEN** the music stops

#### Scenario: Restart starts music again
- **WHEN** the game is over and the player presses Enter
- **THEN** the music plays again from the beginning of the theme

### Requirement: Sound effects
The game SHALL play a distinct sound effect for each of: a hard drop, a line clear of 1 to 3 rows, a four-line clear, and game over. No other action SHALL produce a sound effect.

#### Scenario: Hard drop
- **WHEN** the player presses Space and the piece locks
- **THEN** the hard drop sound plays

#### Scenario: Line clear
- **WHEN** a piece locks and clears 1, 2 or 3 rows
- **THEN** the line clear sound plays

#### Scenario: Four-line clear
- **WHEN** a piece locks and clears 4 rows
- **THEN** the four-line clear sound plays instead of the line clear sound

#### Scenario: Game over
- **WHEN** the game ends
- **THEN** the game over sound plays once

#### Scenario: Movement is silent
- **WHEN** the player moves, rotates or soft-drops a piece without a line clear
- **THEN** no sound effect plays

### Requirement: Mute
Pressing the M key SHALL toggle mute at any time (playing, paused or game over). While muted, neither music nor sound effects SHALL be audible, and the game SHALL show that sound is muted. The mute setting SHALL be remembered between visits, defaulting to unmuted. Toggling mute SHALL NOT change game state.

#### Scenario: Mute silences everything
- **WHEN** music is playing and the player presses M
- **THEN** the music is silent and a muted indicator is shown
- **AND** the board, piece, score and game status are unchanged

#### Scenario: Unmute
- **WHEN** the game is muted and the player presses M
- **THEN** the indicator is hidden and music is audible again if the game is playing

#### Scenario: Effects are muted
- **WHEN** the game is muted and a line clear or game over occurs
- **THEN** no sound plays

#### Scenario: Mute while paused
- **WHEN** the game is paused and the player presses M
- **THEN** the mute state toggles and the game remains paused

#### Scenario: Setting persists
- **WHEN** the player mutes, then reloads the page and presses a key
- **THEN** the game starts muted with the indicator shown
