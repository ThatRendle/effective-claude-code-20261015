# Stopped: human verification of the emoji picker (task 3.1)

<!-- Section: 3 · Tasks: 3.1 · Base: 9e72a79 -->

§1 and §2 are committed (78ce554, 9e72a79), gates green. Task 3.1 needs Emmz's eyes: how the picker looks and feels
cannot be checked headlessly. Question pending: do the emoji render, does the flyout sit sensibly above the input, and
does it look right in light and dark themes?

Recipe given to Emmz (a local `notirc.json` already exists in the repo root):
`dotnet run --project NotIrc.App -- --server ws://localhost:8080 --token any-token --nick emmz`
Check: click the 😀 button beside the input; pick several emoji (flyout stays open); select text then pick (replaces);
caret and focus return to the input; type until near 1024 bytes and pick (counter goes red/"Too long"); toggle OS
light/dark.

## Next

- Wait for Emmz's result. Tick 3.1 only when she reports it good; record her answer in a devlog note. Fix any look issues
  before ticking.
- Then 3.2: add a README.md line about the emoji button, verify against the running app.
- Then whole-change review (`git diff 5437b38..HEAD`), change-done note, offer `code-review`, then offer `/opsx:archive`.
