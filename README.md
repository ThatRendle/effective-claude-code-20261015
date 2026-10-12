# Effective Claude Code — exercises (15 October 2026)

Exercises for the Effective Claude Code workshop, presented by Emmz Rendle and Dylan Beattie.

| Directory | Exercise | When |
|---|---|---|
| [`01-vibe-todo`](01-vibe-todo) | Vibe-coding: a to-do app with no spec at all | Morning, Module 1 |
| [`02-tetris`](02-tetris) | A browser Tetris game, built from a spec with OpenSpec | Morning, Module 6 |
| [`03-notirc-client`](03-notirc-client) | A client for the notIRC chat server, built with a build loop | All afternoon |

## Getting started

```bash
git clone https://github.com/ThatRendle/effective-claude-code-20261015.git
cd effective-claude-code-20261015
```

Do each exercise in its own `claude` session, started in that exercise's directory:

```bash
cd 01-vibe-todo
claude
```

Each directory has a `PROMPT.md`. Instead of typing the prompt, type this into the session and press Enter:

```
@PROMPT.md
```

Facilitators will tell you when to run OpenSpec commands such as `/opsx:explore`.

## Ground rules

- Everything here is a learning exercise. None of the code is meant to be used after the workshop ends.
- Each directory's `CLAUDE.md` tells Claude this too. You'll add to the notIRC client's `CLAUDE.md` during the afternoon.
