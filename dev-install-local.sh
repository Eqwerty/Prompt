#!/usr/bin/env sh
set -eu

# Local development installer: builds and installs a local binary.
# Usage:
#   sh ./dev-install-local.sh

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="$SCRIPT_DIRECTORY"

ESC=$(printf '\033')
if [ -z "${NO_COLOR:-}" ] && [ "${TERM:-}" != "dumb" ]; then
  R="$ESC[0m"
  GREEN="$ESC[32m"; YELLOW="$ESC[33m"; RED="$ESC[31m"
else
  R=''; GREEN=''; YELLOW=''; RED=''
fi

die() {
  printf "${RED}error:${R} %s\n" "$1" >&2
  exit 1
}

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

install_binary() {
  if [ "$TARGET_OS" = "windows" ]; then
    if ! mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
      rm -f "$STAGED_BINARY_PATH"
      printf 'Failed to replace %s.\n' "$FINAL_BINARY_PATH" >&2
      die "Close shells using gitprompt.exe and run again."
    fi
  else
    mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
  fi
}

add_to_shell_config() {
  for config in "$HOME/.bashrc" "$HOME/.bash_profile" "$HOME/.profile"; do
    if [ -f "$config" ]; then
      if grep -qF "gitprompt init bash" "$config" 2>/dev/null; then
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

if ! command -v dotnet >/dev/null 2>&1; then
  die "dotnet CLI is required but was not found in PATH."
fi

if [ "$TARGET_OS" = "windows" ]; then
  RUNTIME_IDENTIFIER="win-x64"
  BINARY_NAME="gitprompt.exe"
  PUBLISHED_BINARY_NAME="GitPrompt.exe"
elif [ "$TARGET_OS" = "darwin" ]; then
  RUNTIME_IDENTIFIER="osx-x64"
  BINARY_NAME="gitprompt"
  PUBLISHED_BINARY_NAME="GitPrompt"
else
  RUNTIME_IDENTIFIER="linux-x64"
  BINARY_NAME="gitprompt"
  PUBLISHED_BINARY_NAME="GitPrompt"
fi

BIN_DIR="$HOME/.local/bin"
FINAL_BINARY_PATH="$BIN_DIR/$BINARY_NAME"
STAGED_BINARY_PATH="$BIN_DIR/.$BINARY_NAME.new.$$"

if [ "$TARGET_OS" = "windows" ]; then
  EVAL_LINE='eval "$($HOME/.local/bin/gitprompt.exe init bash)"'
else
  EVAL_LINE='eval "$(gitprompt init bash)"'
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

cp "$SOURCE_BINARY_PATH" "$STAGED_BINARY_PATH"
chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

run_step "Installing to $FINAL_BINARY_PATH" "$TEMPORARY_DIRECTORY/install.log" \
  install_binary

printf '\n'
add_to_shell_config
printf '\nRestart your terminal or run: source ~/.bashrc\n'