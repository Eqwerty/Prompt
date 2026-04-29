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
| `showCommandDuration` | `false` | Show last command duration (e.g. `42ms`) |

## Context Segment

| Key | Default | Description |
|---|---|---|
| `showUser` | `true` | Show the username in the prompt |
| `showHost` | `true` | Show the hostname in the prompt |
| `maxPathDepth` | `0` | Max directory segments shown in the path (`0` = full path) |

When `maxPathDepth` is set, paths deeper than the limit are truncated with `…`. Examples with `maxPathDepth: 2`:

- `~/repos/company/project/src` → `~/…/project/src`
- `/etc/nginx/conf.d` → `/…/nginx/conf.d`

## Prompt Layout

| Key | Default | Description |
|---|---|---|
| `multilinePrompt` | `true` | Put the prompt symbol (`$`, `#`, `>`) on its own line |
| `newlineBeforePrompt` | `false` | Add a blank line before the prompt |

When `multilinePrompt: false`, the symbol appears at the end of the status line:

```
# two-line (default)
user host ~/repo (main)
$ 

# single-line
user host ~/repo (main) $ 
```

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
