# Prompt

This repository publishes a single prompt binary and auto-publishes GitHub Releases.

## What Happens On Push

The workflow in `.github/workflows/release.yml` runs when either is true:

- You push to `master`
- You trigger it manually with `workflow_dispatch`

When it runs, it:

- Builds cross-platform binaries
- Packages release artifacts
- Replaces a fixed `latest` release tag with new artifacts

Stable asset URL pattern:

- `https://github.com/Eqwerty/Prompt/releases/download/latest/<asset-name>`

Current build targets:

- Linux amd64: `prompt_linux_amd64.tar.gz`
- macOS amd64: `prompt_darwin_amd64.tar.gz`
- Windows amd64: `prompt_windows_amd64.zip`

## Install (Linux, macOS, and Windows Git Bash)

The installer downloads the latest GitHub release asset for your OS and installs the prompt executable with this default layout:

- Linux/macOS: `$HOME/.local/bin/gitprompt`
- Windows Git Bash: `$HOME/prompt/gitprompt.exe`

Install:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Notes:

- Installer supports Linux, macOS, and Windows Git Bash on amd64.
- On Linux and macOS, rerunning the installer replaces the binary atomically, so self-updates from your shell prompt work without `Text file busy` errors.

## Update

Run the same install command again to update to the newest release artifact:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Optional alias (Windows Git Bash, with schannel workaround):

    alias updateprompt='curl -fsSL --ssl-no-revoke https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh'

## Local Development Loop (No Release Needed)

For day-to-day prompt changes, you can test locally without pushing to `master` or publishing a release.

Run:

    sh ./dev-install-local.sh

What it does:

- Runs `dotnet test` (Release)
- Publishes a local Release binary for your OS
- Installs it to the same default location as `install.sh`
  - Linux/macOS: `$HOME/.local/bin/gitprompt`
  - Windows Git Bash: `$HOME/prompt/gitprompt.exe`

Optional (faster inner loop):

    SKIP_TESTS=1 sh ./dev-install-local.sh

## Bash Prompt Setup

After install, set `PS1` and you are done.

Linux/macOS:

    PS1='$($HOME/.local/bin/gitprompt)'

Windows Git Bash:

    PS1='$(~/prompt/gitprompt.exe)'
