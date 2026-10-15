# channel-session Specification

## Purpose

Defines how the client connects to a notIRC server, joins the shared channel with a nickname, recovers from dropped connections, and tells the user what state the connection is in.

## Requirements

### Requirement: Connect and join on launch
The client SHALL open a connection to the configured server using the configured token, then send a join request with the configured nickname as the first message, and SHALL NOT send chat messages until the server accepts the join.

#### Scenario: Successful join
- **WHEN** the client launches with a valid server address, token and free nickname
- **THEN** the client connects, sends the join request, receives acceptance and shows the status as joined

#### Scenario: Chat is unavailable before join is accepted
- **WHEN** the join has been sent but not yet accepted
- **THEN** the user cannot send a chat message

### Requirement: Bad token stops the client
The client SHALL treat a rejected token (HTTP 401 on connect) as a permanent failure, SHALL show a clear message that the token was rejected, and SHALL NOT retry automatically.

#### Scenario: Token rejected on first connect
- **WHEN** the server answers the connection attempt with HTTP 401
- **THEN** the client shows a "token rejected" error, stops attempting to connect and does not enter the reconnecting state

#### Scenario: Token rejected during a reconnect
- **WHEN** a reconnect attempt is answered with HTTP 401
- **THEN** the client stops reconnecting and shows the same "token rejected" error

### Requirement: Prompt for another nickname when taken
When the server reports the nickname is taken, the client SHALL prompt the user for a different nickname, pre-filled with the nickname that was rejected, and SHALL send a new join request on the same connection with the user's choice.

#### Scenario: Configured nickname taken on first join
- **WHEN** the server answers the join with a nick-taken rejection
- **THEN** the client prompts for a new nickname and, once the user confirms one, sends a new join request without reconnecting

#### Scenario: Replacement nickname also taken
- **WHEN** the replacement nickname is also rejected
- **THEN** the client prompts again

#### Scenario: User declines to choose
- **WHEN** the user dismisses the nickname prompt
- **THEN** the client stays connected but not joined and shows that status, and offers the prompt again on request

### Requirement: Reconnect automatically after a drop
When an established connection closes for any reason other than a rejected token or the user quitting, the client SHALL show the reconnecting status and keep trying to reconnect and re-join until it succeeds, the token is rejected or the user quits.

#### Scenario: Connection drops while joined
- **WHEN** the connection closes unexpectedly
- **THEN** the status becomes reconnecting and the client begins reconnection attempts

#### Scenario: Server unreachable for a while
- **WHEN** reconnect attempts keep failing
- **THEN** the client keeps retrying at a bounded interval and the status stays reconnecting

#### Scenario: Reconnect succeeds
- **WHEN** a reconnect attempt succeeds and the join is accepted
- **THEN** the status returns to joined and the user can send messages again

### Requirement: Quietly retry the same nickname on reconnect
After a reconnect, if the join is rejected as nick-taken, the client SHALL retry the same nickname a small fixed number of times without prompting, and SHALL prompt for a new nickname only if those retries are also rejected.

#### Scenario: Own stale nickname frees up
- **WHEN** the rejoin is rejected as nick-taken and a quiet retry then succeeds
- **THEN** the client rejoins with the same nickname and the user is never prompted

#### Scenario: Nickname stays taken
- **WHEN** every quiet retry is rejected as nick-taken
- **THEN** the client prompts the user for a new nickname

### Requirement: Visible connection status
The client SHALL show the current connection status at all times, as one of: connecting, joined, reconnecting, or disconnected with a reason.

#### Scenario: Status follows the connection
- **WHEN** the connection moves between connecting, joined and reconnecting
- **THEN** the displayed status changes to match without user action

#### Scenario: Permanent failure shows its reason
- **WHEN** the client stops because of a rejected token
- **THEN** the status shows disconnected with that reason
