# client-configuration Specification

## Purpose

Defines how the user supplies the server address, token and nickname when launching the client, and how missing or invalid settings are reported.

## Requirements

### Requirement: Accept settings from layered sources
The client SHALL read the server address, token and nickname from a local settings file, from environment variables and from command-line arguments, with command-line arguments overriding environment variables and environment variables overriding the file.

#### Scenario: File only
- **WHEN** the settings file supplies all three values and nothing else does
- **THEN** the client uses the file's values

#### Scenario: Argument overrides file
- **WHEN** the file and a command-line argument both supply the server address
- **THEN** the command-line value is used

#### Scenario: Environment overrides file
- **WHEN** the file and an environment variable both supply the nickname and no argument does
- **THEN** the environment value is used

### Requirement: Report missing settings clearly
If the server address, token or nickname is missing after all sources are read, the client SHALL NOT attempt to connect and SHALL tell the user which setting is missing and how to supply it.

#### Scenario: Token missing
- **WHEN** no source supplies a token
- **THEN** the client shows that the token is missing and names the ways to provide it

### Requirement: Keep the token out of the interface and version control
The client SHALL NOT display the token in the interface or logs, and the repository SHALL ignore the local settings file so it is not committed.

#### Scenario: Token not shown
- **WHEN** the client is running and connected
- **THEN** the token appears nowhere in the interface, status or error text

#### Scenario: Settings file ignored by version control
- **WHEN** a local settings file is created in the repository
- **THEN** version control does not offer it for commit

### Requirement: Accept local development servers
The client SHALL accept both secure and non-secure WebSocket addresses, so it can connect to a locally run server as well as the live one.

#### Scenario: Local server
- **WHEN** the server address is a non-secure address on the local machine
- **THEN** the client connects to it using that address
