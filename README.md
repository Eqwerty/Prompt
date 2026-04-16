# GitPrompt

A fast cross-platform shell prompt binary for Git repositories.

`gitPrompt` prints a two-line prompt:

1. `user host path [git-status]`
2. prompt symbol (`$`, `#`, or `>`)

This repository contains the source code for that binary.

## Quick Install

Install latest release:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh | sh
```

Default install location:

- Linux/macOS: `$HOME/.gitPrompt/gitPrompt`
- Windows Git Bash: `$HOME/.gitPrompt/gitPrompt.exe`

Update is the same command.

## Uninstall

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/uninstall.sh | sh
```

This removes the binary, the generated `.gitPromptrc` shell config, and the source line from `~/.bashrc`.
Legacy manual `PS1` lines written before the automated setup was introduced are also removed.

## Bash Setup

Shell configuration is automated by the installer. After `install.sh` runs, it writes a `.gitPromptrc`
file co-located with the binary and adds a source line to `~/.bashrc`:

- Linux/macOS: `$HOME/.gitPrompt/.gitPromptrc`
- Windows Git Bash: `$HOME/.gitPrompt/.gitPromptrc`

The `.gitPromptrc` sets `PS1` and provides two convenience aliases:
- `updategitprompt` — re-runs the installer and reloads `~/.bashrc`
- `uninstallgitprompt` — runs the uninstaller and reloads `~/.bashrc`

If you skip automatic setup or need to configure manually, add one of the following to your shell config:

Linux/macOS:

```sh
PS1='$([ -x "$HOME/.gitPrompt/gitPrompt" ] && "$HOME/.gitPrompt/gitPrompt" || printf "\w \$ ")'
```

Windows Git Bash:

```sh
PS1='$([ -x "$HOME/.gitPrompt/gitPrompt.exe" ] && "$HOME/.gitPrompt/gitPrompt.exe" || printf "\w > ")'
```

The `&&`/`||` guard runs on every prompt render — if `gitPrompt` is removed, the prompt falls back to the current directory and prompt symbol (e.g. `~/repos$ `). Bash expands `\w` and `\$` before the command substitution runs.

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

`gitPrompt` optionally reads a `config.json` file co-located with the binary:

- Linux/macOS: `$HOME/.gitPrompt/config.json`
- Windows Git Bash: `$HOME/.gitPrompt/config.json`

If the file is absent or cannot be parsed, all settings fall back to their defaults. The parser is case-insensitive and accepts comments and trailing commas.

### Cache

Controls how long `gitPrompt` reuses cached results before re-running a Git command. TTL values are specified in **seconds**. Setting a value to `0` disables caching for that entry.

| Key | Default | Description |
|---|---|---|
| `cache.gitStatusTtl` | `5` | TTL for cached Git status results (staged, unstaged, untracked, etc.) |
| `cache.repositoryTtl` | `60` | TTL for cached repository detection results |

Example `config.json`:

```json
{
  "cache": {
    "gitStatusTtl": 3,
    "repositoryTtl": 30
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
sh ./dev-install-local.sh --yes
sh ./dev-install-local.sh -svy
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
