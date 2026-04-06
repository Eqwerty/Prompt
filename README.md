# Prompt

This repository builds a single Go binary from `prompt.go` and auto-publishes GitHub Releases.

## What Happens On Push

The workflow in `.github/workflows/release.yml` runs only when both are true:

- You push to `master`
- The push includes changes to `prompt.go`

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

The installer downloads the latest GitHub release asset for your OS and installs the binary with this default layout:

- Linux/macOS: `$HOME/.local/bin/gitprompt`
- Windows Git Bash: `$HOME/promptgo/gitprompt.exe`

From a cloned repo:

    ./install.sh

Without cloning:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Optional custom install location:

    INSTALL_DIR=/usr/local/bin ./install.sh

Optional custom installed name:

    BIN_BASENAME=prompt ./install.sh

Notes:

- Installer supports Linux, macOS, and Windows Git Bash on amd64.
- Ensure your install directory is in `PATH`.

## Bash Prompt Setup

For Windows Git Bash, after running the installer with defaults, this works:

    PS1='$(~/promptgo/gitprompt.exe)'

For Linux/macOS, with default install path:

    PS1='$($HOME/.local/bin/gitprompt)'
