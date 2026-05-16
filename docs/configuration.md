← [README](../README.md)

# Configuration

`gitprompt` optionally reads a `config.jsonc` file from the platform config directory:

- Linux/macOS: `$XDG_CONFIG_HOME/gitprompt/config.jsonc` (default: `~/.config/gitprompt/config.jsonc`)
- Windows Git Bash: `%APPDATA%/gitprompt/config.jsonc`

If the file is absent or cannot be parsed, all settings fall back to their defaults.

Open it in your editor with [`gitprompt config`](commands.md).

## Cache

Controls how long `gitprompt` reuses cached results before re-running a Git command (in seconds; `0` disables caching).

| Key | Default | Description |
|---|---|---|
| `cache.gitStatusTtl` | `5` | Git status results (staged, unstaged, untracked, etc.) |
| `cache.repositoryTtl` | `60` | Repository detection results |

## Timeout

Controls how long `gitprompt` waits for a Git subprocess before killing it and showing `[timeout]` in place of the git status segment. Set to `0` to disable.

| Key | Default | Description |
|---|---|---|
| `commandTimeoutMs` | `2000` | Git subprocess timeout in milliseconds |

## Command Duration

Shows the elapsed time of the last command in the prompt, rendered in pink between the path and the git status segment. Requires bash 5+ (`EPOCHREALTIME`).

| Key | Default | Description |
|---|---|---|
| `showCommandDuration` | `true` | Show last command duration (e.g. `42ms`) |
| `commandDurationMinMs` | *(none)* | Minimum duration in ms before the duration is shown. `null` means always show. |

## Context Segment

| Key | Default | Description |
|---|---|---|
| `showUser` | `true` | Show the username in the prompt |
| `showDomain` | `false` | Prepend the Windows domain to the username, e.g. `DOMAIN+user` (Windows only; no effect when `USER` env var is set) |
| `showHost` | `true` | Show the hostname in the prompt |
| `maxPathDepth` | `0` | Max directory segments shown in the path (`0` = full path) |

When `maxPathDepth` is set, paths deeper than the limit are truncated with `…`. Examples with `maxPathDepth: 2`:

- `~/repos/company/project/src` → `~/…/project/src`
- `/etc/nginx/conf.d` → `/…/nginx/conf.d`

## Prompt Layout

| Key | Default | Description |
|---|---|---|
| `multilinePrompt` | `true` | Put the prompt symbol (`$`, `#`, `❯`) on its own line |
| `newlineBeforePrompt` | `false` | Add a blank line before the prompt |
| `promptStartOfLine` | `true` | Move to column 0 if the cursor is not there when the prompt renders (e.g. after `printf text`) |
| `promptSymbol` | *(auto)* | Override the prompt symbol. Omit (or set to `null`) to keep automatic: `$` for regular users, `#` for root, `❯` on Windows. |

> **Note:** `promptStartOfLine` is baked into the bash integration script at `eval` time, not re-read on every render. Changing it in `config.jsonc` has no effect until you start a new shell or re-run `eval "$(gitprompt init bash)"`.

When `multilinePrompt: false`, the symbol appears at the end of the status line:

```
# two-line (default)
user host ~/repo (main)
$ 

# single-line
user host ~/repo (main) $ 
```

## Git Status Icons

Customise the icon characters shown in the git status segment. Set any key to a string to override it; `null` restores the default.

| Key | Default | Description |
|---|---|---|
| `icons.ahead` | `↑` | Commits ahead of upstream |
| `icons.behind` | `↓` | Commits behind upstream |
| `icons.added` | `+` | Added files (staged or unstaged) |
| `icons.modified` | `~` | Modified files (staged or unstaged) |
| `icons.renamed` | `→` | Renamed files (staged or unstaged) |
| `icons.deleted` | `-` | Deleted files (staged or unstaged) |
| `icons.untracked` | `?` | Untracked files |
| `icons.conflicts` | `!` | Merge conflicts |
| `icons.stash` | `@` | Stash entries |
| `icons.dirty` | `•` | Dirty indicator (compact mode only) |
| `icons.clean` | `✓` | Clean indicator (compact mode only) |
| `icons.noUpstreamMarker` | `*` | Prefix on branch name when there is no upstream |
| `icons.detachedHeadMarker` | `:` | Prefix on branch name when HEAD is detached |
| `icons.branchLabelOpen` | `(` | Opening bracket around the branch name (overrides all states when set) |
| `icons.branchLabelClose` | `)` | Closing bracket around the branch name (overrides all states when set) |
| `icons.branchLabelOpenNormal` | `(` | Opening bracket for a normal (tracked) branch |
| `icons.branchLabelCloseNormal` | `)` | Closing bracket for a normal (tracked) branch |
| `icons.branchLabelOpenNoUpstream` | `(` | Opening bracket when there is no upstream |
| `icons.branchLabelCloseNoUpstream` | `)` | Closing bracket when there is no upstream |
| `icons.branchLabelOpenDetached` | `[` | Opening bracket when HEAD is detached |
| `icons.branchLabelCloseDetached` | `]` | Closing bracket when HEAD is detached |
| `icons.branchOperationSeparator` | `\|` | Separator between branch name and operation (e.g. `REBASE`) inside the label |

## Colors

Customise the color of each prompt segment using `#RRGGBB` hex strings. Set any key to a hex color to override it; `null` restores the default. Use any color picker to choose a value.

| Key | Default | Description |
|---|---|---|
| `colors.user` | `#00BB00` | Username |
| `colors.host` | `#CB06B2` | Hostname |
| `colors.path` | `#D78700` | Working directory path |
| `colors.commandDuration` | `#CB06B2` | Last command duration |
| `colors.branch` | `#48A8CD` | Branch name (tracked upstream) |
| `colors.branchNoUpstream` | `#48A8CD` | Branch name (no upstream) |
| `colors.branchDetached` | `#DDDD00` | Branch name (detached HEAD) |
| `colors.ahead` | `#48A8CD` | Commits ahead indicator |
| `colors.behind` | `#48A8CD` | Commits behind indicator |
| `colors.staged` | `#00BB00` | Staged changes |
| `colors.unstaged` | `#CC0000` | Unstaged changes |
| `colors.untracked` | `#CC0000` | Untracked files |
| `colors.stash` | `#CB06B2` | Stash entries |
| `colors.conflict` | `#FF5555` | Merge conflicts |
| `colors.dirty` | `#D78700` | Dirty indicator (compact mode only) |
| `colors.clean` | `#00BB00` | Clean indicator (compact mode only) |
| `colors.missingPath` | `#FF5555` | Missing working directory |
| `colors.timeout` | `#FFA002` | Git timeout indicator |
| `colors.promptSymbol` | `#AAAAAA` | Prompt symbol (`$`, `#`, `❯`) |

> **Note:** `colors.promptSymbol` controls the *color* of the prompt symbol. To change the symbol character itself, use [`promptSymbol`](#prompt-layout).

## Compact Mode

When `compact: true`, the git status segment shows only the branch name, ahead/behind counts, and a single dirty/clean indicator instead of the full breakdown of file counts.

| Key | Default | Description |
|---|---|---|
| `compact` | `false` | Show dirty/clean icon only instead of full staged/unstaged counts |
| `showStash` | `true` | Show stash entry count in the prompt (applies to both full and compact mode) |

Example compact output: `(main) ↑2 •` — dirty repo, 2 commits ahead. Clean: `(main) ✓`.

The dirty/clean icons and their colors can be customised with `icons.dirty`, `icons.clean`, `colors.dirty`, and `colors.clean`.

## Example

```jsonc
{
  "cache": {
    "gitStatusTtl": 3,   // git status cache TTL in seconds (0 = disabled)
    "repositoryTtl": 30  // repository location cache TTL in seconds (0 = disabled)
  },
  "commandTimeoutMs": 500,  // kill git subprocess after 500ms (0 = disabled)
  "showCommandDuration": true  // show last command duration in the prompt
}
```
