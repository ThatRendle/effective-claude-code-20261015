# Spec Delta

## ADDED Requirements

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
