# GitPrompt

A fast cross-platform shell prompt binary for Git repositories.

`gitprompt` prints a two-line prompt:

1. `user host path [duration] [git-status]`
2. prompt symbol (`$`, `#`, or `>`)

## Quick Install

Install the latest release:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh | sh
```

Default install location: `~/.local/bin/gitprompt` (Linux/macOS) or `~/.local/bin/gitprompt.exe` (Windows Git Bash).

## Bash Setup

After installing, add to your Bash startup file (`~/.bashrc`, or `~/.bash_profile` on macOS):

```sh
eval "$(gitprompt init bash)"
```

## Update

```sh
gitprompt update
```

Requires `curl` and network access. Alternatively, re-run the install script.

## Uninstall

```sh
gitprompt uninstall
```

Removes the binary, config, and cache files. Automatically cleans up `gitprompt init` lines from your shell config files, and warns about any remaining references for manual removal.

## Commands

| Command | Description |
|---|---|
| `gitprompt init bash` | Print the Bash shell integration script |
| `gitprompt config` | Open config.jsonc in `$EDITOR` or `$VISUAL` (fallback: vim) |
| `gitprompt config reset [-y]` | Reset config.jsonc to defaults (`-y` skips confirmation) |
| `gitprompt update` | Update to the latest release |
| `gitprompt uninstall` | Uninstall gitprompt |
| `gitprompt debug` | Show a diagnostic report for the current directory |
| `gitprompt --help` | Show help |

## Prompt Format Reference

### Overall Shape

Line 1:

`<user> <host> <path> [duration] [git-status]`

Line 2:

`$` on Unix, `#` for Unix root, `>` on Windows.

If you are outside a Git repo, the git-status segment is omitted.

### Context Segment

- `<user>`: current user (fallback `?`)
- `<host>`: machine name (fallback `?`)
- `<path>`: current working directory
- Home path is shortened to `~`
- If the working directory no longer exists, `<path>` is rendered in red and suffixed with `[missing]`

### Git Status Segment

General shape:

`(branch) ↑A ↓B +x ~y ...`

Render order:

1. Branch label
2. Ahead (`↑N`) if `N > 0`
3. Behind (`↓N`) if `N > 0`
4. Staged counts (`+ ~ → -`, non-zero only)
5. Unstaged counts (`+ ~ → -`, non-zero only)
6. Untracked (`?N`)
7. Conflicts (`!N`)
8. Stash (`@N`)

| Icon | Meaning |
|---|---|
| `↑` | Commits ahead |
| `↓` | Commits behind |
| `+` | Added |
| `~` | Modified |
| `→` | Renamed/copied |
| `-` | Deleted |
| `?` | Untracked |
| `!` | Conflicts |
| `@` | Stash entries |

Staged and unstaged share the same file-state icons (`+ ~ → -`) and are distinguished by color: staged in green, unstaged in red.

Example:

```text
(main) ↑2 ↓1 +1 ~2 +3 -1 ?4 !1 @1
```

In that example, `+1 ~2` is staged, and `+3 -1` is unstaged.

### Branch Labels

| Format | Meaning |
|---|---|
| `(main)` | Tracked branch |
| `*(feature)` | No upstream tracking branch |
| `(abc1234...)` | Detached HEAD |
| `(origin/main abc1234...)` | Detached HEAD matching one remote ref |

### Operation Markers

In-progress Git operations appear inside the branch label, e.g. `(main|MERGE)` or `*(feature|CHERRY-PICK)`. REBASE includes progress when available: `(main|REBASE 1/3)`.

Supported: `REBASE`, `MERGE`, `CHERRY-PICK`, `REVERT`, `BISECT`.

## Configuration

`gitprompt` optionally reads a `config.jsonc` file from the platform config directory:

- Linux/macOS: `$XDG_CONFIG_HOME/gitprompt/config.jsonc` (default: `~/.config/gitprompt/config.jsonc`)
- Windows Git Bash: `%APPDATA%/gitprompt/config.jsonc`

If the file is absent or cannot be parsed, all settings fall back to their defaults.

### Cache

Controls how long `gitprompt` reuses cached results before re-running a Git command (in seconds; `0` disables caching).

| Key | Default | Description |
|---|---|---|
| `cache.gitStatusTtl` | `5` | Git status results (staged, unstaged, untracked, etc.) |
| `cache.repositoryTtl` | `60` | Repository detection results |

### Timeout

Controls how long `gitprompt` waits for a Git subprocess before killing it and showing `[timeout]` in place of the git status segment. Set to `0` to disable the timeout.

| Key | Default | Description |
|---|---|---|
| `commandTimeoutMs` | `2000` | Git subprocess timeout in milliseconds |

### Command Duration

Shows the elapsed time of the last command in the prompt, rendered in pink between the path and the git status segment. Off by default.

| Key | Default | Description |
|---|---|---|
| `showCommandDuration` | `false` | Show last command duration (e.g. `42ms`) |

Requires bash 5+ (`EPOCHREALTIME`). Silently no-ops on older bash versions.

Example `config.jsonc`:

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

## Local Development

```sh
sh ./dev-install-local.sh
```
