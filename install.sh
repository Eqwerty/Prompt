#!/usr/bin/env sh
set -eu

# Install prompt from the latest GitHub release.
# Usage:
#   ./install.sh
# Optional:
#   INSTALL_DIR=/custom/bin ./install.sh

BINARY_BASENAME="${BIN_BASENAME:-gitprompt}"

OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"
CPU_ARCHITECTURE="$(uname -m)"

case "$OPERATING_SYSTEM" in
  linux) TARGET_OS="linux" ;;
  darwin) TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *)
    echo "Unsupported OS: $OPERATING_SYSTEM"
    echo "This installer currently supports Linux, macOS, and Windows (Git Bash)."
    exit 1
    ;;
esac

case "$CPU_ARCHITECTURE" in
  x86_64|amd64) TARGET_ARCHITECTURE="amd64" ;;
  *)
    echo "Unsupported architecture: $CPU_ARCHITECTURE"
    echo "This installer currently supports amd64 only."
    exit 1
    ;;
esac

if [ -z "${INSTALL_DIR:-}" ]; then
  if [ "$TARGET_OS" = "windows" ]; then
    INSTALL_DIR="$HOME/prompt"
  else
    INSTALL_DIR="$HOME/.local/bin"
  fi
fi

if [ "$TARGET_OS" = "windows" ]; then
  RELEASE_ASSET_NAME="prompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.zip"
  EXTRACTED_BINARY_NAME="prompt.exe"
  INSTALLED_BINARY_NAME="${INSTALL_NAME:-${BINARY_BASENAME}.exe}"
else
  RELEASE_ASSET_NAME="prompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.tar.gz"
  EXTRACTED_BINARY_NAME="prompt"
  INSTALLED_BINARY_NAME="${INSTALL_NAME:-${BINARY_BASENAME}}"
fi

RELEASE_ASSET_URL="https://github.com/Eqwerty/Prompt/releases/download/latest/${RELEASE_ASSET_NAME}"

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM

echo "Downloading ${RELEASE_ASSET_URL}"

DOWNLOAD_COMPLETED=0

if command -v curl >/dev/null 2>&1; then
  if [ "$TARGET_OS" = "windows" ]; then
    if curl --ssl-no-revoke -fsSL "$RELEASE_ASSET_URL" -o "$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME"; then
      DOWNLOAD_COMPLETED=1
    fi
  else
    if curl -fsSL "$RELEASE_ASSET_URL" -o "$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME"; then
      DOWNLOAD_COMPLETED=1
    fi
  fi
fi

if [ "$DOWNLOAD_COMPLETED" -eq 0 ] && command -v wget >/dev/null 2>&1; then
  if wget -qO "$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME" "$RELEASE_ASSET_URL"; then
    DOWNLOAD_COMPLETED=1
  fi
fi

if [ "$DOWNLOAD_COMPLETED" -eq 0 ] && [ "$TARGET_OS" = "windows" ] && command -v powershell.exe >/dev/null 2>&1; then
  if powershell.exe -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '$RELEASE_ASSET_URL' -OutFile '$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME'" >/dev/null; then
    DOWNLOAD_COMPLETED=1
  fi
fi

if [ "$DOWNLOAD_COMPLETED" -eq 0 ]; then
  echo "Failed to download release asset."
  echo "If this repository has only prereleases, latest/download will not work."
  echo "Push a new commit to publish a non-prerelease release."
  exit 1
fi

mkdir -p "$INSTALL_DIR"

if [ "$TARGET_OS" = "windows" ]; then
  if command -v unzip >/dev/null 2>&1; then
    unzip -q "$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME" -d "$TEMPORARY_DIRECTORY"
  elif command -v powershell.exe >/dev/null 2>&1; then
    powershell.exe -NoProfile -Command "Expand-Archive -Path '$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME' -DestinationPath '$TEMPORARY_DIRECTORY' -Force" >/dev/null
  else
    echo "Need unzip or powershell.exe to extract zip files."
    exit 1
  fi
else
  tar -xzf "$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME" -C "$TEMPORARY_DIRECTORY"
fi

FINAL_BINARY_PATH="$INSTALL_DIR/$INSTALLED_BINARY_NAME"
STAGED_BINARY_PATH="$INSTALL_DIR/.${INSTALLED_BINARY_NAME}.new.$$"

cp "$TEMPORARY_DIRECTORY/$EXTRACTED_BINARY_NAME" "$STAGED_BINARY_PATH"
chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

if [ "$TARGET_OS" = "windows" ]; then
  if mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
    :
  else
    rm -f "$STAGED_BINARY_PATH"
    echo "Failed to replace $FINAL_BINARY_PATH."
    echo "On Windows, a running .exe may be locked. Close shells using gitprompt and run the installer again."
    exit 1
  fi
else
  mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
fi

echo "Installed to $INSTALL_DIR/$INSTALLED_BINARY_NAME"
if [ "$TARGET_OS" = "windows" ]; then
  echo "Make sure your PS1 is updated: PS1='\$(~/prompt/gitprompt.exe)'"
else
  echo "Make sure your PS1 is updated: PS1='\$($HOME/.local/bin/gitprompt)'"
fi

