# Spec Delta

## Purpose

Defines how the client shows who is in the channel, and how arrivals and departures are announced, based solely on the server's presence events.

## ADDED Requirements

### Requirement: Show who is in the channel
The client SHALL show a list of the nicknames currently in the channel, initialised from the server's list when the join is accepted and updated on each join and leave event, and SHALL include the user's own nickname.

#### Scenario: Initial list on join
- **WHEN** the join is accepted and the server lists other connected nicknames
- **THEN** the presence list shows those nicknames and the user's own

#### Scenario: Someone arrives
- **WHEN** the server reports that a nickname has joined
- **THEN** that nickname is added to the list

#### Scenario: Someone leaves
- **WHEN** the server reports that a nickname has left
- **THEN** that nickname is removed from the list

### Requirement: Rebuild presence on every join
The client SHALL replace its presence list with the one supplied on each accepted join, including a rejoin after a reconnect, rather than patching the previous list.

#### Scenario: People left during a drop
- **WHEN** the client rejoins and the server's list no longer contains someone shown before the drop
- **THEN** the presence list no longer shows them

#### Scenario: People arrived during a drop
- **WHEN** the client rejoins and the server's list contains someone not shown before
- **THEN** the presence list shows them

### Requirement: Announce arrivals and departures in the chat
The client SHALL add a notice to the message log, with a timestamp, whenever another user joins or leaves, visually distinct from chat messages.

#### Scenario: Join notice
- **WHEN** the server reports a nickname has joined
- **THEN** the log shows a join notice for that nickname

#### Scenario: Leave notice
- **WHEN** the server reports a nickname has left
- **THEN** the log shows a leave notice for that nickname

#### Scenario: No notices for the initial list
- **WHEN** the user joins and receives the list of existing nicknames
- **THEN** no individual join notices are added for them
