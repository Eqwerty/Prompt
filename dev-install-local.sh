#!/usr/bin/env sh
set -eu

# Local development installer:
# - runs tests
# - publishes a local single-file binary (non-AOT) for the current OS/arch
# - installs it to the same default path used by install.sh
#
# Usage:
#   ./dev-install-local.sh
# Optional:
#   INSTALL_DIR=/custom/path ./dev-install-local.sh
#   BIN_BASENAME=mygitprompt ./dev-install-local.sh
#   INSTALL_NAME=mygitprompt.exe ./dev-install-local.sh
#   SKIP_TESTS=1 ./dev-install-local.sh

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="${SCRIPT_DIRECTORY}"

BINARY_BASENAME="${BIN_BASENAME:-gitprompt}"
OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"
CPU_ARCHITECTURE="$(uname -m)"

case "$OPERATING_SYSTEM" in
  linux) TARGET_OS="linux" ;;
  darwin) TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *)
    echo "Unsupported OS: $OPERATING_SYSTEM"
    exit 1
    ;;
esac

case "$CPU_ARCHITECTURE" in
  x86_64|amd64) TARGET_ARCHITECTURE="amd64" ;;
  *)
    echo "Unsupported architecture: $CPU_ARCHITECTURE"
    echo "This script currently supports amd64 only."
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
  RUNTIME_IDENTIFIER="win-x64"
  INSTALLED_BINARY_NAME="${INSTALL_NAME:-${BINARY_BASENAME}.exe}"
  PUBLISHED_BINARY_NAME="Prompt.exe"
elif [ "$TARGET_OS" = "darwin" ]; then
  RUNTIME_IDENTIFIER="osx-x64"
  INSTALLED_BINARY_NAME="${INSTALL_NAME:-${BINARY_BASENAME}}"
  PUBLISHED_BINARY_NAME="Prompt"
else
  RUNTIME_IDENTIFIER="linux-x64"
  INSTALLED_BINARY_NAME="${INSTALL_NAME:-${BINARY_BASENAME}}"
  PUBLISHED_BINARY_NAME="Prompt"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required but was not found in PATH."
  exit 1
fi

if [ "${SKIP_TESTS:-0}" != "1" ]; then
  echo "[1/3] Running tests..."
  dotnet test "$REPOSITORY_ROOT/Prompt.slnx" --configuration Release --nologo
else
  echo "[1/3] Skipping tests (SKIP_TESTS=1)."
fi

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM
PUBLISH_DIRECTORY="$TEMPORARY_DIRECTORY/publish"
mkdir -p "$PUBLISH_DIRECTORY"

echo "[2/3] Publishing local single-file binary ($RUNTIME_IDENTIFIER)..."
dotnet publish "$REPOSITORY_ROOT/src/Prompt/Prompt.csproj" \
  --configuration Release \
  --runtime "$RUNTIME_IDENTIFIER" \
  --nologo \
  -p:PublishAot=false \
  -p:PublishSingleFile=true \
  -p:SelfContained=false \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$PUBLISH_DIRECTORY"

SOURCE_BINARY_PATH="$PUBLISH_DIRECTORY/$PUBLISHED_BINARY_NAME"
if [ ! -f "$SOURCE_BINARY_PATH" ]; then
  echo "Published binary not found: $SOURCE_BINARY_PATH"
  exit 1
fi

mkdir -p "$INSTALL_DIR"
FINAL_BINARY_PATH="$INSTALL_DIR/$INSTALLED_BINARY_NAME"
STAGED_BINARY_PATH="$INSTALL_DIR/.${INSTALLED_BINARY_NAME}.new.$$"

cp "$SOURCE_BINARY_PATH" "$STAGED_BINARY_PATH"
chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

echo "[3/3] Installing to $FINAL_BINARY_PATH"
if [ "$TARGET_OS" = "windows" ]; then
  if mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
    :
  else
    rm -f "$STAGED_BINARY_PATH"
    echo "Failed to replace $FINAL_BINARY_PATH."
    echo "Close shells/processes using gitprompt.exe and run again."
    exit 1
  fi
else
  mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
fi

echo "Done. Installed local build to: $FINAL_BINARY_PATH"
if [ "$TARGET_OS" = "windows" ]; then
  echo "PS1 example: PS1='\$(~/prompt/gitprompt.exe)'"
else
  echo "PS1 example: PS1='\$($HOME/.local/bin/gitprompt)'"
fi

