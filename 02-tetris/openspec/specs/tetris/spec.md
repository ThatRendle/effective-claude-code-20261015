# tetris Specification

## Purpose

A single-player Tetris game that runs in the browser: falling tetrominoes on a 10x20 board, keyboard play, line clears, scoring, a next-piece preview, and game over with restart.

## Requirements

### Requirement: Board and pieces
The game SHALL present a board 10 columns wide and 20 rows tall, and SHALL use exactly the seven standard tetrominoes (I, O, T, S, Z, J, L). Each piece kind SHALL be visually distinguishable.

#### Scenario: New game starts empty
- **WHEN** a new game starts
- **THEN** the board is 10x20 with no locked cells and score 0
- **AND** one piece is falling from the top of the board

#### Scenario: Piece locks when it cannot fall
- **WHEN** a falling piece cannot move down one row because of the floor or locked cells
- **THEN** the piece locks into the board at its current position
- **AND** the next piece spawns at the top

### Requirement: Piece sequence
Pieces SHALL be dealt from a 7-bag: each bag contains one of each of the seven kinds in random order, and a new bag is shuffled when the previous one is exhausted.

#### Scenario: Each bag contains every piece once
- **WHEN** 7 consecutive pieces are dealt starting at a bag boundary
- **THEN** they are the seven distinct kinds, each exactly once

### Requirement: Next piece preview
The game SHALL display the kind of the piece that will spawn after the current one.

#### Scenario: Preview matches the next spawn
- **WHEN** the current piece locks
- **THEN** the piece that spawns is the one that was shown in the preview
- **AND** the preview updates to the following piece

### Requirement: Horizontal movement
The Left and Right arrow keys SHALL move the falling piece one column in that direction. A move that would overlap a wall or locked cell SHALL be ignored.

#### Scenario: Move left
- **WHEN** the player presses Left and the cell to the left is free
- **THEN** the piece moves one column left

#### Scenario: Blocked by wall
- **WHEN** the player presses Left and the piece is against the left wall
- **THEN** the piece does not move

### Requirement: Rotation
The Up arrow key SHALL rotate the falling piece 90 degrees clockwise. If the rotated piece would collide, the game SHALL try shifting it one column left, then one column right; if both collide, the rotation SHALL be ignored.

#### Scenario: Rotate in open space
- **WHEN** the player presses Up and the rotated piece fits
- **THEN** the piece rotates 90 degrees clockwise in place

#### Scenario: Wall kick
- **WHEN** the player presses Up, the rotated piece collides in place, and fits after a one-column shift
- **THEN** the piece rotates and is shifted by that one column

#### Scenario: Rotation impossible
- **WHEN** the player presses Up and the rotated piece collides in place and with both one-column shifts
- **THEN** the piece is unchanged

### Requirement: Gravity
The falling piece SHALL descend one row at a fixed interval of about 800 ms. The rate SHALL NOT change during a game.

#### Scenario: Piece falls on its own
- **WHEN** the gravity interval elapses and the row below is free
- **THEN** the piece moves down one row

#### Scenario: Speed is constant
- **WHEN** the score or number of cleared lines increases
- **THEN** the gravity interval is unchanged

### Requirement: Soft drop
Holding the Down arrow key SHALL move the falling piece down one row per key press, without waiting for gravity. Soft drop SHALL NOT award points.

#### Scenario: Soft drop moves one row
- **WHEN** the player presses Down and the row below is free
- **THEN** the piece moves down one row and the score is unchanged

#### Scenario: Soft drop onto the stack
- **WHEN** the player presses Down and the row below is blocked
- **THEN** the piece locks

### Requirement: Hard drop
The Space key SHALL move the falling piece straight down to the lowest free position and lock it immediately. Hard drop SHALL NOT award points.

#### Scenario: Hard drop locks at the bottom
- **WHEN** the player presses Space
- **THEN** the piece lands at the lowest position it can reach in its column span and locks at once
- **AND** the next piece spawns

### Requirement: Line clears and scoring
When a piece locks, each completely filled row SHALL be removed and rows above it SHALL shift down. The score SHALL increase by 100, 300, 500 or 800 for 1, 2, 3 or 4 rows cleared by that lock. Locking without clearing a row SHALL NOT change the score.

#### Scenario: Single line
- **WHEN** a piece locks and completes exactly one row
- **THEN** that row is removed, rows above shift down, and the score increases by 100

#### Scenario: Four lines
- **WHEN** a piece locks and completes four rows
- **THEN** all four rows are removed and the score increases by 800

#### Scenario: No line
- **WHEN** a piece locks and completes no row
- **THEN** the score is unchanged

### Requirement: Game over
The game SHALL end when a newly spawned piece overlaps locked cells. After game over, gravity and movement input SHALL have no effect, and the game over state SHALL be visible to the player along with the final score.

#### Scenario: Spawn blocked
- **WHEN** a piece locks and the next piece's spawn position overlaps locked cells
- **THEN** the game ends and a game over message with the final score is shown

#### Scenario: Input after game over
- **WHEN** the game is over and the player presses a movement, rotation or drop key
- **THEN** nothing on the board changes

### Requirement: Restart
Pressing Enter after game over SHALL start a new game with an empty board and score 0.

#### Scenario: Restart after game over
- **WHEN** the game is over and the player presses Enter
- **THEN** a new game starts with an empty board and score 0

#### Scenario: Enter during play
- **WHEN** a game is in progress and the player presses Enter
- **THEN** the game continues unchanged

### Requirement: Pause
Pressing Escape during play SHALL pause the game, and pressing Escape while paused SHALL resume it. While paused, gravity and all movement, rotation and drop input SHALL have no effect, and a "Paused" message SHALL be visible. Escape SHALL have no effect after game over.

#### Scenario: Pause during play
- **WHEN** a game is in progress and the player presses Escape
- **THEN** the game is paused and a "Paused" message is shown
- **AND** the board, falling piece and score are unchanged

#### Scenario: Gravity while paused
- **WHEN** the game is paused and the gravity interval elapses
- **THEN** the falling piece does not move

#### Scenario: Input while paused
- **WHEN** the game is paused and the player presses a movement, rotation or drop key
- **THEN** nothing on the board changes

#### Scenario: Resume
- **WHEN** the game is paused and the player presses Escape
- **THEN** the "Paused" message is hidden and play continues from the same board, piece and score

#### Scenario: Escape after game over
- **WHEN** the game is over and the player presses Escape
- **THEN** the game stays over and no "Paused" message is shown

#### Scenario: Restart key while paused
- **WHEN** the game is paused and the player presses Enter
- **THEN** the game remains paused and unchanged

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

### Requirement: Runs with Node.js and a browser only
The game SHALL be runnable using only Node.js and a web browser, with no other tools, services or accounts.

#### Scenario: Start the game
- **WHEN** a developer with Node.js installed installs dependencies and runs the project's dev command
- **THEN** the game is playable in a browser at the printed local address
