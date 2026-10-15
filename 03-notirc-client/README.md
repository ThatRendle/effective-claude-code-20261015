# NotIrc client

A desktop chat client for the NotIrc server, built with Avalonia and .NET 10.

## Settings

The client needs three settings: the server address, the shared token and your nickname.
It reads them from three places. Later ones win:

1. `notirc.json` in the working directory (optional, ignored by git)
2. Environment variables `NOTIRC_SERVER`, `NOTIRC_TOKEN`, `NOTIRC_NICK`
3. Command-line arguments `--server`, `--token`, `--nick`

If any setting is missing, the client says which one and does not connect.
The token is never shown in the window or in error text.

The server address is a host with an optional port (`chat.example.com`, `localhost:8080`), or a
full `ws://` or `wss://` URL. A bare host uses secure `wss://`. Use `ws://` explicitly for a local server.

To use a file, copy `notirc.example.json` to `notirc.json` and fill in the token.

## Run

From the repository root:

```sh
dotnet run --project NotIrc.App -- --server chat.example.com --token <token> --nick <nickname>
```

Against a local server (see `NOTIRC.md` for the Docker command; any token works if it matches the server's):

```sh
dotnet run --project NotIrc.App -- --server ws://localhost:8080 --token any-token --nick emmz
```

With a `notirc.json` or environment variables set, no arguments are needed:

```sh
NOTIRC_SERVER=ws://localhost:8080 NOTIRC_TOKEN=any-token NOTIRC_NICK=emmz dotnet run --project NotIrc.App
```

## Checks

`make gates` runs build, test, format and spec validation.
