# chat-messaging Specification

## Purpose

Defines how the client displays channel messages and lets the user send their own, including the byte limit, rejection feedback and behaviour while disconnected.

## Requirements

### Requirement: Display channel messages in arrival order
The client SHALL show each message received from the channel with its sender's nickname, its text and a timestamp set by the client when it was received, in the order received, with no limit on how many remain visible.

#### Scenario: Messages appear in order
- **WHEN** the server delivers messages from several senders
- **THEN** each appears once, in arrival order, with nickname, text and receive time

#### Scenario: Long sessions keep everything
- **WHEN** many hundreds of messages have arrived
- **THEN** none are removed from the visible log

#### Scenario: Unknown message types are ignored
- **WHEN** the server sends a message whose type the client does not recognise
- **THEN** nothing is shown and no error is raised

### Requirement: Distinguish own messages
The client SHALL present messages whose sender nickname equals the user's current nickname differently from other people's messages.

#### Scenario: Own message echoed back
- **WHEN** the server broadcasts the user's own message back to them
- **THEN** it appears once, styled as the user's own

#### Scenario: Nickname changes after a prompt
- **WHEN** the user joins under a replacement nickname
- **THEN** subsequent messages from that nickname are styled as own

### Requirement: Send messages to the channel
The client SHALL let the user send a message when joined, SHALL clear the input once it is sent, and SHALL show the message in the log only when the server delivers it back.

#### Scenario: Message sent and echoed
- **WHEN** the user submits non-empty text while joined
- **THEN** the text is sent, the input clears and the message appears when the server broadcasts it

#### Scenario: Empty input is not sent
- **WHEN** the user submits empty or whitespace-only text
- **THEN** nothing is sent

### Requirement: Warn before exceeding the byte limit
The client SHALL measure the input in UTF-8 bytes against the 1024-byte limit, SHALL show the user when the input is over the limit, and SHALL NOT send an over-limit message.

#### Scenario: Multibyte text counts as bytes
- **WHEN** the input holds 300 emoji (more than 1024 bytes but fewer than 1024 characters)
- **THEN** the client shows that the message is too long and does not send it

#### Scenario: Exactly at the limit
- **WHEN** the input is exactly 1024 UTF-8 bytes
- **THEN** it is accepted for sending

#### Scenario: Back under the limit
- **WHEN** the user edits an over-limit input until it is within the limit
- **THEN** the warning clears and sending is allowed

### Requirement: Show server rejection of a message
If the server rejects a sent message as too long, the client SHALL tell the user the message was rejected, SHALL keep the connection, and SHALL keep the user's text available to edit.

#### Scenario: Server reports too long
- **WHEN** the server answers a message with a too-long rejection
- **THEN** the user sees that the message was rejected and can edit and resend

### Requirement: Refuse sending while disconnected
When the client is not joined, it SHALL refuse to send, SHALL show a "not connected" notice, SHALL keep the user's text in the input and SHALL NOT queue the message for later.

#### Scenario: Send attempted while reconnecting
- **WHEN** the user submits text while the status is reconnecting
- **THEN** a "not connected" notice appears, nothing is sent and the text stays in the input

#### Scenario: Nothing is sent after rejoining
- **WHEN** the client rejoins after a refused send
- **THEN** the refused message is not sent automatically

### Requirement: Mark possible missed messages after reconnect
Because the server provides no history, the client SHALL add a visible notice to the log after each successful reconnect saying messages may have been missed.

#### Scenario: Gap marker after rejoin
- **WHEN** the client rejoins after a dropped connection
- **THEN** a notice appears in the log at that point saying messages may have been missed

#### Scenario: No marker on first join
- **WHEN** the client joins for the first time
- **THEN** no missed-messages notice appears

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
