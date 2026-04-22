#!/usr/bin/env sh
set -eu

ESC=$(printf '\033')
if [ -z "${NO_COLOR:-}" ] && [ "${TERM:-}" != "dumb" ]; then
  R="$ESC[0m"; BOLD="$ESC[1m"
  GREEN="$ESC[32m"; YELLOW="$ESC[33m"; RED="$ESC[31m"
else
  R=''; BOLD=''; GREEN=''; YELLOW=''; RED=''
fi

die() {
  printf "${RED}error:${R} %s\n" "$1" >&2
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
  if [ "$TARGET_OS" = "windows" ]; then
    OLD_BINARY_PATH="${FINAL_BINARY_PATH}.old"
    rm -f "$OLD_BINARY_PATH" 2>/dev/null || true

    # Rename the current binary before replacing — rename is allowed even when
    # the file is running (Windows only blocks overwrite/delete of running .exe).
    if [ -f "$FINAL_BINARY_PATH" ]; then
      if ! mv "$FINAL_BINARY_PATH" "$OLD_BINARY_PATH" 2>/dev/null; then
        rm -f "$STAGED_BINARY_PATH"
        die "Cannot rename ${FINAL_BINARY_PATH}. Close shells using gitprompt and try again."
      fi
    fi

    if ! mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
      mv -f "$OLD_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null || true
      rm -f "$STAGED_BINARY_PATH"
      die "Failed to install to ${FINAL_BINARY_PATH}."
    fi
  else
    mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
  fi
}

add_to_shell_config() {
  for config in "$HOME/.bashrc" "$HOME/.bash_profile" "$HOME/.profile"; do
    if [ -f "$config" ]; then
      if grep -q "^[^#]*gitprompt.* init bash" "$config" 2>/dev/null; then
        printf "${GREEN}✓${R} Shell config already set up (%s)\n" "$config"
        return
      fi
      printf '\n%s\n' "$EVAL_LINE" >> "$config"
      printf "${GREEN}✓${R} Added gitprompt init to %s\n" "$config"
      return
    fi
  done

  printf '\n'
  printf "${YELLOW}Next steps — add to your shell startup file:\n"
  printf "  %s\n" "$EVAL_LINE"
  printf "${R}"
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

if [ "$TARGET_OS" = "windows" ]; then
  EVAL_LINE='eval "$($HOME/.local/bin/gitprompt.exe init bash)"'
else
  EVAL_LINE='eval "$(gitprompt init bash)"'
fi

FINAL_BINARY_PATH="$BIN_DIR/$BINARY_NAME"
STAGED_BINARY_PATH="$BIN_DIR/.$BINARY_NAME.new.$$"

if [ -z "${_INSTALL_SOURCED:-}" ]; then
  RELEASE_ASSET_URL="https://github.com/Eqwerty/GitPrompt/releases/download/latest/${RELEASE_ASSET_NAME}"

  TEMPORARY_DIRECTORY="$(mktemp -d)"
  trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT
  trap 'printf "\n${RED}error:${R} Cancelled.\n" >&2; exit 130' INT TERM

  RELEASE_ASSET_PATH="$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME"
  EXTRACTED_BINARY_PATH="$TEMPORARY_DIRECTORY/$BINARY_NAME"

  printf "${YELLOW}●${R} Downloading %s..." "$RELEASE_ASSET_NAME"
  download_release_asset
  printf "\r${GREEN}✓${R} Downloading %s...\n" "$RELEASE_ASSET_NAME"

  printf "${YELLOW}●${R} Extracting..."
  extract_release_asset
  printf "\r${GREEN}✓${R} Extracting...\n"

  mkdir -p "$BIN_DIR"
  cp "$EXTRACTED_BINARY_PATH" "$STAGED_BINARY_PATH"
  chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

  printf "${YELLOW}●${R} Installing to %s..." "$FINAL_BINARY_PATH"
  install_binary
  printf "\r${GREEN}✓${R} Installing to %s...\n" "$FINAL_BINARY_PATH"

  add_to_shell_config
  printf '\nRestart your terminal or run: source ~/.bashrc\n'
  printf "Run 'gitprompt --help' to see available commands.\n"
fi
