# Spec Delta

## ADDED Requirements

### Requirement: Insert emoji from a picker
The client SHALL offer an emoji picker beside the input field, and choosing an emoji SHALL insert it into the input at the caret, replacing any selected text.

#### Scenario: Insert at the caret
- **WHEN** the input holds "hello world" with the caret after "hello" and the user chooses an emoji from the picker
- **THEN** the emoji is inserted after "hello" and the rest of the text is unchanged

#### Scenario: Replace a selection
- **WHEN** part of the input is selected and the user chooses an emoji
- **THEN** the selection is replaced by the emoji

#### Scenario: Focus returns to the input
- **WHEN** the user chooses an emoji
- **THEN** the input has keyboard focus with the caret just after the inserted emoji

#### Scenario: Picker works while disconnected
- **WHEN** the client is not joined and the user chooses an emoji
- **THEN** the emoji is inserted into the input like any typed text

### Requirement: Inserted emoji obey the byte limit
Emoji inserted from the picker SHALL be counted, warned about and refused for sending under the same UTF-8 byte rules as typed text; the picker itself SHALL NOT block insertion.

#### Scenario: Insertion pushes the input over the limit
- **WHEN** the input is 1022 bytes and the user inserts a 4-byte emoji
- **THEN** the emoji is inserted, the counter shows 1026/1024, the "Too long" warning appears and sending is not allowed
