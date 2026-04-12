#!/usr/bin/env bash
set -euo pipefail

RUNTIME_IDENTIFIER="${1:-}"
TARGET_OS="${2:-}"
TARGET_ARCHITECTURE="${3:-}"
EXTENSION="${4:-}"

if [[ -z "$RUNTIME_IDENTIFIER" || -z "$TARGET_OS" || -z "$TARGET_ARCHITECTURE" ]]; then
  echo "Usage: $0 <runtime_identifier> <target_os> <target_architecture> [extension]" >&2
  exit 1
fi

DIST_DIR="dist"
PUBLISH_DIR="$DIST_DIR/publish"
BIN_NAME="prompt${EXTENSION}"
ARCHIVE_BASENAME="prompt_${TARGET_OS}_${TARGET_ARCHITECTURE}"

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

dotnet publish src/Prompt/Prompt.csproj \
  -c Release \
  -r "$RUNTIME_IDENTIFIER" \
  --no-restore \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$PUBLISH_DIR"

cp "$PUBLISH_DIR/Prompt${EXTENSION}" "$DIST_DIR/$BIN_NAME"

if [[ "$TARGET_OS" == "windows" ]]; then
  (
    cd "$DIST_DIR"
    7z a "${ARCHIVE_BASENAME}.zip" "$BIN_NAME" > /dev/null
  )
else
  tar -C "$DIST_DIR" -czf "$DIST_DIR/${ARCHIVE_BASENAME}.tar.gz" "$BIN_NAME"
fi

