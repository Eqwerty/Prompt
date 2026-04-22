#!/usr/bin/env sh
set -eu

# Local development installer: builds and installs a local binary.
# Usage:
#   sh ./dev-install-local.sh

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="$SCRIPT_DIRECTORY"

_INSTALL_SOURCED=1
. "$SCRIPT_DIRECTORY/install.sh"

run_step() {
  step_message="$1"
  log_file="$2"
  shift 2

  printf "${YELLOW}●${R} %s..." "$step_message"
  if "$@" >"$log_file" 2>&1; then
    printf "\r${GREEN}✓${R} %s...\n" "$step_message"
  else
    step_status=$?
    printf '\n'
    printf "${RED}error:${R} " >&2
    cat "$log_file" >&2
    exit "$step_status"
  fi
}

try_step() {
  step_message="$1"
  log_file="$2"
  shift 2

  printf "${YELLOW}●${R} %s..." "$step_message"
  if "$@" >"$log_file" 2>&1; then
    printf "\r${GREEN}✓${R} %s...\n" "$step_message"
    return 0
  else
    printf "\r${YELLOW}○${R} %s...\n" "$step_message"
    return 1
  fi
}

if ! command -v dotnet >/dev/null 2>&1; then
  die "dotnet CLI is required but was not found in PATH."
fi

if [ "$TARGET_OS" = "windows" ]; then
  RUNTIME_IDENTIFIER="win-x64"
  PUBLISHED_BINARY_NAME="GitPrompt.exe"
elif [ "$TARGET_OS" = "darwin" ]; then
  RUNTIME_IDENTIFIER="osx-x64"
  PUBLISHED_BINARY_NAME="GitPrompt"
else
  RUNTIME_IDENTIFIER="linux-x64"
  PUBLISHED_BINARY_NAME="GitPrompt"
fi

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT
trap 'printf "\n${RED}error:${R} Cancelled.\n" >&2; exit 130' INT TERM

PUBLISH_DIRECTORY="$TEMPORARY_DIRECTORY/publish"
mkdir -p "$PUBLISH_DIRECTORY"

run_step "Restoring packages" "$TEMPORARY_DIRECTORY/restore.log" \
  dotnet restore "$REPOSITORY_ROOT/GitPrompt.slnx" --nologo

run_step "Building" "$TEMPORARY_DIRECTORY/build.log" \
  dotnet build "$REPOSITORY_ROOT/GitPrompt.slnx" --configuration Release --nologo --no-restore

if ! try_step "Publishing AOT ($RUNTIME_IDENTIFIER)" "$TEMPORARY_DIRECTORY/publish_aot.log" \
    dotnet publish "$REPOSITORY_ROOT/src/GitPrompt/GitPrompt.csproj" \
      --configuration Release \
      --runtime "$RUNTIME_IDENTIFIER" \
      --nologo \
      --no-restore \
      -p:DebugType=None \
      -p:DebugSymbols=false \
      -o "$PUBLISH_DIRECTORY"; then
  rm -rf "$PUBLISH_DIRECTORY"
  mkdir -p "$PUBLISH_DIRECTORY"
  run_step "Publishing ($RUNTIME_IDENTIFIER)" "$TEMPORARY_DIRECTORY/publish.log" \
    dotnet publish "$REPOSITORY_ROOT/src/GitPrompt/GitPrompt.csproj" \
      --configuration Release \
      --runtime "$RUNTIME_IDENTIFIER" \
      --nologo \
      --no-restore \
      -p:PublishAot=false \
      -p:PublishSingleFile=true \
      -p:SelfContained=false \
      -p:DebugType=None \
      -p:DebugSymbols=false \
      -o "$PUBLISH_DIRECTORY"
fi

SOURCE_BINARY_PATH="$PUBLISH_DIRECTORY/$PUBLISHED_BINARY_NAME"
[ -f "$SOURCE_BINARY_PATH" ] || die "Published binary not found: $SOURCE_BINARY_PATH"

mkdir -p "$BIN_DIR"
cp "$SOURCE_BINARY_PATH" "$STAGED_BINARY_PATH"
chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

run_step "Installing to $FINAL_BINARY_PATH" "$TEMPORARY_DIRECTORY/install.log" \
  install_binary

add_to_shell_config
printf '\nRestart your terminal or run: source ~/.bashrc\n'
printf "Run 'gitprompt --help' to see available commands.\n"