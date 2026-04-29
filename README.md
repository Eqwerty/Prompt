# GitPrompt

A fast cross-platform shell prompt binary for Git repositories.

## Quick Install

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh | sh
```

Default install location: `~/.local/bin/gitprompt` (Linux/macOS) or `~/.local/bin/gitprompt.exe` (Windows Git Bash).

## Bash Setup

Add to your Bash startup file (`~/.bashrc`, or `~/.bash_profile` on macOS):

```sh
eval "$(gitprompt init bash)"
```

## Maintenance

```sh
gitprompt update      # update to the latest release
gitprompt uninstall   # remove binary, config, and cache files
```

See [Commands](docs/commands.md) for the full reference.

## Git Status

`gitprompt` shows a git status segment on the right side of the prompt:

`(branch) ↑A ↓B +x ~y ...`

Render order:

1. Branch label
2. Ahead (`↑N`) / Behind (`↓N`)
3. Staged counts (`+ ~ → -`, non-zero only) — shown in green
4. Unstaged counts (`+ ~ → -`, non-zero only) — shown in red
5. Untracked (`?N`), Conflicts (`!N`), Stash (`@N`)

| Icon | Meaning |
|---|---|
| `↑` / `↓` | Commits ahead / behind |
| `+` | Added |
| `~` | Modified |
| `→` | Renamed/copied |
| `-` | Deleted |
| `?` | Untracked |
| `!` | Conflicts |
| `@` | Stash entries |

Example: `(main) ↑2 ↓1 +1 ~2 +3 -1 ?4 !1 @1` — `+1 ~2` is staged, `+3 -1` is unstaged.

### Branch Labels

| Format | Meaning |
|---|---|
| `(main)` | Tracked branch |
| `*(feature)` | No upstream tracking branch |
| `(abc1234...)` | Detached HEAD |
| `(origin/main abc1234...)` | Detached HEAD matching one remote ref |

### Operation Markers

In-progress operations appear inside the branch label: `(main|MERGE)`, `*(feature|CHERRY-PICK)`, `(main|REBASE 1/3)`.

Supported: `REBASE`, `MERGE`, `CHERRY-PICK`, `REVERT`, `BISECT`.

## Git Aliases

The installer sets up a collection of Git aliases and shell functions (e.g. `gs`, `gco`, `gc`, `gpl`, `gr`, `glog`). They are loaded automatically by the `eval "$(gitprompt init bash)"` line — no extra config needed.

See [`git_aliases.sh`](git_aliases.sh) for the full list.

To get the latest validated aliases without reinstalling:

```sh
gitprompt update aliases
```

To install the binary only, without aliases:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh | sh -s -- --no-aliases
```

## Configuration

See [Configuration](docs/configuration.md) for all settings (cache TTLs, timeout, command duration, path depth, prompt layout).
