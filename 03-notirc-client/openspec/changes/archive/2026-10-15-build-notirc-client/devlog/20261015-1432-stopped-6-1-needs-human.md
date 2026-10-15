# Stopped: 6.1 needs Emmz (two windows, a look at the UI, and a Docker server)

<!-- Section: 6 · Task: 6.1 · Base: 6837d31 -->

**What blocked:** task 6.1 is a live check with two client windows, and CLAUDE.md says that needs human verification. The Docker daemon is also not running (OrbStack's socket `/var/run/docker.sock` is missing), so the local server isn't up. I started nothing and changed no code. Tree is clean apart from this note; 6.1 and 6.2 are unticked.

**Question:** do all of these hold? Reply with PASS, or paste what went wrong.

## Recipe (run from the repo root; `!` runs it in the session)

1. Start OrbStack (open the app, or `! orb start`), then the server:
   `! docker run --rm -d --name notirc-local -e NOTIRC_TOKEN=local -p 8080:8080 ghcr.io/thatrendle/notirc:latest`
2. Two terminals, two windows:
   - `dotnet run --project NotIrc.App -- --server ws://localhost:8080 --token local --nick emmz`
   - `dotnet run --project NotIrc.App -- --server ws://localhost:8080 --token local --nick ann`
3. Check, in order:
   - Both show **Joined**. Each presence list shows both nicknames, and each window got a join notice for the other.
   - A message typed in either window appears in both, once, with a time. Your own is highlighted, and readable (dark theme).
   - Start a third client with `--nick emmz`: it prompts for a new nickname, pre-filled with `emmz`. Cancelling leaves it "Connected, not joined" with a "Choose nickname…" button.
   - Paste about 1100 characters (or 300 emoji) into the input: the counter goes past 1024, "Too long" shows, and Send is disabled. Trim it under the limit and it clears.
   - `! docker stop notirc-local`: both windows go to **Reconnecting…**. Typing and pressing Enter shows "Not connected" and keeps the text.
   - Restart the server (same `docker run` as step 1): both rejoin, show **Joined** and add the "may have been missed" notice. No notice on the first join.
   - Also try a wrong token (`--token nope`): "Disconnected: token rejected" and a red banner, and it does not retry.
4. Clean up: `! docker stop notirc-local` (it was started with `--rm`).

**On PASS:** tick 6.1, write a note with what you saw, then do 6.2 (the three gates) and move to the whole-change review.
**On FAIL:** treat each failed item as a bug; add a fix section to `tasks.md` and agree it with Emmz first.

## Next

- Wait for Emmz's PASS/FAIL on the recipe above. Don't decide anything for her.
- Unpushed: the §5 and §7 commits.
