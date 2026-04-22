# GitPrompt

A fast cross-platform shell prompt binary for Git repositories.

`gitprompt` prints a two-line prompt:

1. `user host path [git-status]`
2. prompt symbol (`$`, `#`, or `>`)

This repository contains the source code for that binary.

## Quick Install

Install the latest release:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh | sh
```

Default install location: `~/.local/bin/gitprompt` (Linux/macOS) or `~/.local/bin/gitprompt.exe` (Windows Git Bash).

## Bash Setup

After installing, add to your Bash startup file (`~/.bashrc`, or `~/.bash_profile` on macOS):

```sh
export PATH="$HOME/.local/bin:$PATH"  # skip if already set
eval "$(gitprompt init bash)"         # gitprompt
```

This generates and sources the shell integration at startup. The integration sets `PROMPT_COMMAND` and a `DEBUG` trap to update `PS1` on every prompt. If `gitprompt` is not on `PATH`, the integration silently does nothing.

If you prefer to manage `PS1` manually, you can call the binary directly:

Linux/macOS:

```sh
PS1='$([ -x "$HOME/.local/bin/gitprompt" ] && "$HOME/.local/bin/gitprompt" || printf "\w \$ ")'
```

Windows Git Bash:

```sh
PS1='$([ -x "$HOME/.local/bin/gitprompt.exe" ] && "$HOME/.local/bin/gitprompt.exe" || printf "\w > ")'
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

Removes the binary, config, and cache files, and prints any gitprompt references found in your shell config files for manual removal. Requires `curl` and network access.

Alternatively, without the binary:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/uninstall.sh | sh
```

## Commands

| Command | Description |
|---|---|
| `gitprompt init bash` | Print the Bash shell integration script |
| `gitprompt config` | Open the config file in `$EDITOR` (or a default editor) |
| `gitprompt update` | Update to the latest release |
| `gitprompt uninstall` | Uninstall gitprompt |
| `gitprompt --help` | Show help |

## Prompt Format Reference

### Overall Shape

Line 1:

`<user> <host> <path> [git-status]`

Line 2:

`$` on Unix, `#` for Unix root, `>` on Windows.

If you are outside a Git repo, the git-status segment is omitted.

### Context Segment

- `<user>`: current user (fallback `?`)
- `<host>`: machine name (fallback `?`)
- `<path>`: current working directory
- Home path is shortened to `~`
- If the working directory no longer exists but a shell fallback path is available, `<path>` is rendered in red and suffixed with `[missing]`

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

### Branch Labels

- Tracked branch: `(main)`
- No upstream: `*(feature)`
  - `*` means no upstream tracking branch.
- Detached HEAD commit: `(abc1234...)`
- Detached HEAD with one matching remote ref: `(origin/main abc1234...)`

### Operation Markers

If Git has an in-progress operation, it appears inside the branch label:

- `(main|MERGE)`
- `*(feature|CHERRY-PICK)`
- `(feature|REBASE)`

Supported markers: `REBASE`, `MERGE`, `CHERRY-PICK`, `REVERT`, `BISECT`.

### Icons

- `↑` ahead commits
- `↓` behind commits
- `+` added
- `~` modified
- `→` renamed/copied
- `-` deleted
- `?` untracked
- `!` conflicts
- `@` stash entries

Staged and unstaged share the same file-state icons (`+ ~ → -`) and are distinguished by color:

- staged: green
- unstaged: red

Example:

```text
(main) ↑2 ↓1 +1 ~2 +3 -1 ?4 !1 @1
```

In that example, `+1 ~2` is staged, and `+3 -1` is unstaged.

## Configuration

`gitprompt` optionally reads a `config.jsonc` file from the platform config directory:

- Linux/macOS: `$XDG_CONFIG_HOME/gitprompt/config.jsonc` (default: `~/.config/gitprompt/config.jsonc`)
- Windows Git Bash: `%APPDATA%/gitprompt/config.jsonc`

If the file is absent or cannot be parsed, all settings fall back to their defaults. The parser is case-insensitive and accepts comments and trailing commas.

### Cache

Controls how long `gitprompt` reuses cached results before re-running a Git command. TTL values are specified in **seconds**. Setting a value to `0` disables caching for that entry.

| Key | Default | Description |
|---|---|---|
| `cache.gitStatusTtl` | `5` | TTL for cached Git status results (staged, unstaged, untracked, etc.) |
| `cache.repositoryTtl` | `60` | TTL for cached repository detection results |

Example `config.jsonc`:

```jsonc
{
  "cache": {
    "gitStatusTtl": 3,   // git status cache TTL in seconds (0 = disabled)
    "repositoryTtl": 30  // repository location cache TTL in seconds (0 = disabled)
  }
}
```

## Local Development

Run the local dev install script:

```sh
sh ./dev-install-local.sh
```

Useful flags:

```sh
sh ./dev-install-local.sh --verbose
sh ./dev-install-local.sh --skip-tests
sh ./dev-install-local.sh -sv
```

### Native AOT prerequisites

The dev script attempts native AOT compilation (same as the release build) and automatically falls
back to a non-AOT single-file binary if the required toolchain is not present. To enable AOT
locally, install the appropriate toolchain for your OS:

| OS | Requirement |
|---|---|
| **Windows** | [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **"Desktop development with C++"** workload |
| **Linux** | `clang` and `zlib1g-dev` — e.g. `sudo apt install clang zlib1g-dev` on Debian/Ubuntu |
| **macOS** | Xcode Command Line Tools — run `xcode-select --install` |

If these are absent the script falls back silently; no extra steps required.
