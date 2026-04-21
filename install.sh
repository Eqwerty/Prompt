#!/usr/bin/env sh
set -eu

die() {
  printf 'error: %s\n' "$1" >&2
  exit 1
}

download_release_asset() {
  if ! command -v curl >/dev/null 2>&1; then
    die "curl is required but not found."
  fi

  # shellcheck disable=SC2086
  curl $CURL_SSL_OPT -fsSL "$RELEASE_ASSET_URL" -o "$RELEASE_ASSET_PATH" \
    || die "Failed to download: $RELEASE_ASSET_URL"
}

extract_release_asset() {
  if [ "$TARGET_OS" = "windows" ]; then
    if command -v unzip >/dev/null 2>&1; then
      unzip -q "$RELEASE_ASSET_PATH" -d "$TEMPORARY_DIRECTORY"
    elif command -v powershell.exe >/dev/null 2>&1; then
      powershell.exe -NoProfile -Command "Expand-Archive -Path '$RELEASE_ASSET_PATH' -DestinationPath '$TEMPORARY_DIRECTORY' -Force" >/dev/null
    else
      die "Need unzip or powershell.exe to extract zip files."
    fi
  else
    tar -xzf "$RELEASE_ASSET_PATH" -C "$TEMPORARY_DIRECTORY"
  fi
}

install_binary() {
  mkdir -p "$BIN_DIR"
  cp "$EXTRACTED_BINARY_PATH" "$STAGED_BINARY_PATH"
  chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

  if [ "$TARGET_OS" = "windows" ]; then
    if ! mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
      rm -f "$STAGED_BINARY_PATH"
      printf 'Failed to replace %s.\n' "$FINAL_BINARY_PATH" >&2
      die "On Windows, a running .exe may be locked. Close shells using gitprompt and try again."
    fi
  else
    mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
  fi
}

OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"
CPU_ARCHITECTURE="$(uname -m)"

case "$OPERATING_SYSTEM" in
  linux)            TARGET_OS="linux" ;;
  darwin)           TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *) die "Unsupported OS: $OPERATING_SYSTEM. Supported: Linux, macOS, Windows (Git Bash)." ;;
esac

case "$CPU_ARCHITECTURE" in
  x86_64|amd64) TARGET_ARCHITECTURE="amd64" ;;
  *) die "Unsupported architecture: $CPU_ARCHITECTURE. Supported: amd64 only." ;;
esac

BIN_DIR="$HOME/.local/bin"

if [ "$TARGET_OS" = "windows" ]; then
  CURL_SSL_OPT="--ssl-no-revoke"
  RELEASE_ASSET_NAME="gitprompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.zip"
  BINARY_NAME="gitprompt.exe"
else
  CURL_SSL_OPT=""
  RELEASE_ASSET_NAME="gitprompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.tar.gz"
  BINARY_NAME="gitprompt"
fi

RELEASE_ASSET_URL="https://github.com/Eqwerty/GitPrompt/releases/download/latest/${RELEASE_ASSET_NAME}"

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM

RELEASE_ASSET_PATH="$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME"
EXTRACTED_BINARY_PATH="$TEMPORARY_DIRECTORY/$BINARY_NAME"
FINAL_BINARY_PATH="$BIN_DIR/$BINARY_NAME"
STAGED_BINARY_PATH="$BIN_DIR/.$BINARY_NAME.new.$$"

printf 'Downloading %s...\n' "$RELEASE_ASSET_NAME"
download_release_asset

printf 'Extracting...\n'
extract_release_asset

printf 'Installing to %s...\n' "$FINAL_BINARY_PATH"
install_binary

printf 'Done.\n'
printf '\n'
printf 'Next steps — add to your shell startup file:\n'
if [ "$TARGET_OS" = "windows" ]; then
  printf '  eval "$($HOME/.local/bin/gitprompt.exe init bash)"  # gitprompt\n'
else
  printf '  eval "$(gitprompt init bash)"  # gitprompt\n'
fi
