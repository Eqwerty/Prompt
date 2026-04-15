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
#   ./dev-install-local.sh --skip-tests --verbose --yes
#   ./dev-install-local.sh -svy
#   INSTALL_DIR=/custom/path ./dev-install-local.sh
#   BIN_BASENAME=mygitprompt ./dev-install-local.sh
#   INSTALL_NAME=mygitprompt.exe ./dev-install-local.sh

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="${SCRIPT_DIRECTORY}"

SKIP_TESTS=0
VERBOSE_MODE=0
YES_MODE=0
TOTAL_STEPS=6
SCRIPT_STARTED_AT="$(date +%s)"
LOADER_INDEX=0

IS_TTY=0
USE_ANSI=0
USE_COLOR=0
ESCAPE_CHARACTER="$(printf '\033')"

if [ -t 1 ] && [ "${TERM:-}" != "dumb" ]; then
  IS_TTY=1
  USE_ANSI=1
fi

if [ "$USE_ANSI" -eq 1 ] && [ -z "${NO_COLOR:-}" ]; then
  USE_COLOR=1
fi

if [ "$USE_COLOR" -eq 1 ]; then
  COLOR_RESET="${ESCAPE_CHARACTER}[0m"
  COLOR_BOLD="${ESCAPE_CHARACTER}[1m"
  COLOR_DIM="${ESCAPE_CHARACTER}[2m"
  COLOR_RED="${ESCAPE_CHARACTER}[31m"
  COLOR_GREEN="${ESCAPE_CHARACTER}[32m"
  COLOR_YELLOW="${ESCAPE_CHARACTER}[33m"
  COLOR_BLUE="${ESCAPE_CHARACTER}[34m"
  COLOR_CYAN="${ESCAPE_CHARACTER}[36m"
else
  COLOR_RESET=""
  COLOR_BOLD=""
  COLOR_DIM=""
  COLOR_RED=""
  COLOR_GREEN=""
  COLOR_YELLOW=""
  COLOR_BLUE=""
  COLOR_CYAN=""
fi

print_status() {
  status_color="$1"
  status_label="$2"
  shift 2

  printf '%s[%s]%s %s\n' "$status_color" "$status_label" "$COLOR_RESET" "$*"
}

print_banner() {
  printf '%s%sPrompt local installer%s\n' "$COLOR_BOLD" "$COLOR_BLUE" "$COLOR_RESET"
  print_status "$COLOR_DIM" "INFO" "Repo: $REPOSITORY_ROOT"
}

format_step_label() {
  step_number="$1"
  printf '[%s/%s]' "$step_number" "$TOTAL_STEPS"
}

format_colored_step_label() {
  step_number="$1"
  printf '%s%s%s' "${COLOR_BOLD}${COLOR_CYAN}" "$(format_step_label "$step_number")" "$COLOR_RESET"
}

current_timestamp() {
  date +%s
}

format_duration() {
  total_seconds="$1"

  if [ "$total_seconds" -lt 60 ]; then
    printf '%ss' "$total_seconds"
    return
  fi

  total_minutes=$(( total_seconds / 60 ))
  remaining_seconds=$(( total_seconds % 60 ))

  if [ "$total_minutes" -lt 60 ]; then
    printf '%sm %02ss' "$total_minutes" "$remaining_seconds"
    return
  fi

  total_hours=$(( total_minutes / 60 ))
  remaining_minutes=$(( total_minutes % 60 ))
  printf '%sh %02sm %02ss' "$total_hours" "$remaining_minutes" "$remaining_seconds"
}

format_duration_segment() {
  duration_text="$1"
  printf '%s(%s)%s' "$COLOR_DIM" "$duration_text" "$COLOR_RESET"
}

next_loader_frame() {
  frame_index=$((LOADER_INDEX % 4))

  case "$frame_index" in
    0) frame='|' ;;
    1) frame='/' ;;
    2) frame='-' ;;
    *) frame='\\' ;;
  esac

  LOADER_INDEX=$((LOADER_INDEX + 1))
  printf '%s' "$frame"
}

render_step_loader() {
  step_number="$1"
  step_message="$2"
  elapsed_seconds="$3"
  loader_frame="$4"
  elapsed_clock="$(format_duration "$elapsed_seconds")"

  printf '\r%s%s%s %s %s %s%s%s' \
    "${COLOR_BOLD}${COLOR_CYAN}" "$(format_step_label "$step_number")" "$COLOR_RESET" "$loader_frame" \
    "$step_message" "$COLOR_DIM" "$elapsed_clock" "$COLOR_RESET"
}

clear_loader_line() {
  if [ "$USE_ANSI" -eq 1 ]; then
    printf '\r%s[2K' "$ESCAPE_CHARACTER"
  fi
}

print_step_start() {
  step_number="$1"
  step_message="$2"

  if [ "$USE_ANSI" -eq 1 ]; then
    printf '\r%s[2K' "$ESCAPE_CHARACTER"
  else
    printf '\r'
  fi

  printf '%s %s' "$(format_colored_step_label "$step_number")" "$step_message"
}

print_step_done() {
  step_number="$1"
  step_message="$2"
  step_duration="$3"

  if [ "$USE_ANSI" -eq 1 ]; then
    printf '\r%s[2K' "$ESCAPE_CHARACTER"
  else
    printf '\r'
  fi

  print_status "$COLOR_GREEN" "OK" "$(format_colored_step_label "$step_number") $step_message $(format_duration_segment "$(format_duration "$step_duration")")"
}

print_step_warning() {
  step_number="$1"
  step_message="$2"
  step_duration="$3"

  if [ "$USE_ANSI" -eq 1 ]; then
    printf '\r%s[2K' "$ESCAPE_CHARACTER"
  else
    printf '\r'
  fi

  print_status "$COLOR_YELLOW" "OK" "$(format_colored_step_label "$step_number") $step_message $(format_duration_segment "$(format_duration "$step_duration") skipped")"
}

print_step_failed() {
  step_number="$1"
  step_message="$2"
  step_duration="$3"

  if [ "$USE_ANSI" -eq 1 ]; then
    printf '\r%s[2K' "$ESCAPE_CHARACTER"
  else
    printf '\r'
  fi

  print_status "$COLOR_RED" "FAIL" "$(format_colored_step_label "$step_number") $step_message $(format_duration_segment "$(format_duration "$step_duration")")"
}

print_error_and_exit() {
  message="$1"
  print_status "$COLOR_RED" "ERROR" "$message"
  exit 1
}

print_usage() {
  cat <<EOF
Usage: sh ./dev-install-local.sh [options]

Options:
  -y,  --yes                       Auto-configure shell without prompting
  -s,  --skip-tests                Skip test execution only
  -v,  --verbose                   Show dotnet output while commands run
  -h,  --help                      Show this help text

Environment overrides:
  INSTALL_DIR=/custom/path
  BIN_BASENAME=mygitprompt
  INSTALL_NAME=mygitprompt.exe
EOF
}

parse_arguments() {
  while [ "$#" -gt 0 ]; do
    case "$1" in
      --yes)         YES_MODE=1 ;;
      --skip-tests)  SKIP_TESTS=1 ;;
      --verbose)     VERBOSE_MODE=1 ;;
      --help)        print_usage; exit 0 ;;
      --)            shift; break ;;
      --*)
        print_usage >&2
        print_error_and_exit "Unknown option: $1"
        ;;
      -*)
        flags="${1#-}"
        while [ -n "$flags" ]; do
          flag="${flags%"${flags#?}"}"
          flags="${flags#?}"
          case "$flag" in
            y) YES_MODE=1 ;;
            s) SKIP_TESTS=1 ;;
            v) VERBOSE_MODE=1 ;;
            h) print_usage; exit 0 ;;
            *)
              print_usage >&2
              print_error_and_exit "Unknown option: -$flag"
              ;;
          esac
        done
        ;;
      *)
        print_usage >&2
        print_error_and_exit "Unknown option: $1"
        ;;
    esac

    shift
  done
}

run_step() {
  step_number="$1"
  step_message="$2"
  log_file="$3"
  shift 3
  step_started_at="$(current_timestamp)"

  print_step_start "$step_number" "$step_message"

  if [ "$VERBOSE_MODE" -eq 1 ]; then
    status_file="${log_file}.status"
    rm -f "$status_file"

    (
      printf '\n'
      command_status=0
      "$@" || command_status=$?
      printf '%s\n' "$command_status" >"$status_file"
      exit 0
    ) 2>&1 | tee "$log_file"

    if [ -f "$status_file" ]; then
      step_status="$(cat "$status_file")"
      rm -f "$status_file"
    else
      step_status=1
    fi

    step_finished_at="$(current_timestamp)"
    step_duration=$((step_finished_at - step_started_at))

    if [ "$step_status" -eq 0 ]; then
      print_step_done "$step_number" "$step_message" "$step_duration"
    else
      print_step_failed "$step_number" "$step_message" "$step_duration"
      exit "$step_status"
    fi
  elif [ "$USE_ANSI" -eq 1 ]; then
    "$@" >"$log_file" 2>&1 &
    command_pid=$!

    while kill -0 "$command_pid" 2>/dev/null; do
      now_timestamp="$(current_timestamp)"
      elapsed_seconds=$((now_timestamp - step_started_at))
      render_step_loader "$step_number" "$step_message" "$elapsed_seconds" "$(next_loader_frame)"
      sleep 0.1
    done

    step_finished_at="$(current_timestamp)"
    step_duration=$((step_finished_at - step_started_at))

    clear_loader_line

    if wait "$command_pid"; then
      print_step_done "$step_number" "$step_message" "$step_duration"
    else
      step_status=$?
      print_step_failed "$step_number" "$step_message" "$step_duration"
      printf '%s\n' ""
      cat "$log_file"
      exit "$step_status"
    fi
  else
    if "$@" >"$log_file" 2>&1; then
      step_finished_at="$(current_timestamp)"
      step_duration=$((step_finished_at - step_started_at))
      print_step_done "$step_number" "$step_message" "$step_duration"
    else
      step_status=$?
      step_finished_at="$(current_timestamp)"
      step_duration=$((step_finished_at - step_started_at))
      print_step_failed "$step_number" "$step_message" "$step_duration"
      printf '%s\n' ""
      cat "$log_file"
      exit "$step_status"
    fi
  fi
}

install_binary() {
  if [ "$TARGET_OS" = "windows" ]; then
    if mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
      return 0
    fi

    rm -f "$STAGED_BINARY_PATH"
    printf '%s\n' "Failed to replace $FINAL_BINARY_PATH." >&2
    printf '%s\n' "Close shells/processes using gitprompt.exe and run again." >&2
    return 1
  fi

  mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
}

configure_shell() {
  PROMPT_RC_PATH="$INSTALL_DIR/.promptrc"
  SHELL_CONFIG="$HOME/.bashrc"

  if [ "$TARGET_OS" = "windows" ]; then
    cat > "$PROMPT_RC_PATH" <<EOF
# gitprompt
_GITPROMPT_BIN="$FINAL_BINARY_PATH"

__gitprompt_preexec_flag=0
__gitprompt_running=0

__gitprompt_debug_trap() {
  if [ "\$__gitprompt_running" -eq 0 ] && [ "\$BASH_COMMAND" != "_gitprompt_update_ps1" ]; then
    __gitprompt_preexec_flag=1
  fi
}

_gitprompt_update_ps1() {
  __gitprompt_running=1
  if [ "\$__gitprompt_preexec_flag" -eq 1 ]; then
    __gitprompt_preexec_flag=0
    "\$_GITPROMPT_BIN" --invalidate-status-cache >/dev/null 2>&1 || true
  fi
  if output="\$("\$_GITPROMPT_BIN" 2>/dev/null)" && [ -n "\$output" ]; then
    PS1="\$output"
  else
    PS1='\w > '
  fi
  __gitprompt_running=0
}

if [ -x "\$_GITPROMPT_BIN" ]; then
  trap '__gitprompt_debug_trap' DEBUG
  PROMPT_COMMAND="_gitprompt_update_ps1\${PROMPT_COMMAND:+; \$PROMPT_COMMAND}"
fi

alias updateprompt='curl -fsSL --ssl-no-revoke https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh -s -- --yes && source ~/.bashrc'
alias uninstallprompt='curl -fsSL --ssl-no-revoke https://raw.githubusercontent.com/Eqwerty/Prompt/master/uninstall.sh | sh && trap - DEBUG && PROMPT_COMMAND="" && PS1='"'"'\w > '"'"' && source ~/.bashrc'
EOF
  else
    cat > "$PROMPT_RC_PATH" <<EOF
# gitprompt
_GITPROMPT_BIN="$FINAL_BINARY_PATH"

__gitprompt_preexec_flag=0
__gitprompt_running=0

__gitprompt_debug_trap() {
  if [ "\$__gitprompt_running" -eq 0 ] && [ "\$BASH_COMMAND" != "_gitprompt_update_ps1" ]; then
    __gitprompt_preexec_flag=1
  fi
}

_gitprompt_update_ps1() {
  __gitprompt_running=1
  if [ "\$__gitprompt_preexec_flag" -eq 1 ]; then
    __gitprompt_preexec_flag=0
    "\$_GITPROMPT_BIN" --invalidate-status-cache >/dev/null 2>&1 || true
  fi
  if output="\$("\$_GITPROMPT_BIN" 2>/dev/null)" && [ -n "\$output" ]; then
    PS1="\$output"
  else
    PS1='\w \$ '
  fi
  __gitprompt_running=0
}

if [ -x "\$_GITPROMPT_BIN" ]; then
  trap '__gitprompt_debug_trap' DEBUG
  PROMPT_COMMAND="_gitprompt_update_ps1\${PROMPT_COMMAND:+; \$PROMPT_COMMAND}"
fi

alias updateprompt='curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh -s -- --yes && source ~/.bashrc'
alias uninstallprompt='curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/uninstall.sh | sh && trap - DEBUG && PROMPT_COMMAND="" && PS1='"'"'\w \$ '"'"' && source ~/.bashrc'
EOF
  fi

  EXPECTED_SOURCE_LINE="[ -f \"$PROMPT_RC_PATH\" ] && . \"$PROMPT_RC_PATH\"  # gitprompt"

  if [ ! -f "$SHELL_CONFIG" ]; then
    touch "$SHELL_CONFIG"
  fi

  if grep -qF "# gitprompt" "$SHELL_CONFIG" 2>/dev/null; then
    return 0
  fi

  shell_config_backup="${SHELL_CONFIG}.bak"
  if [ ! -f "$shell_config_backup" ]; then
    cp "$SHELL_CONFIG" "$shell_config_backup"
  fi

  printf '\n%s\n' "$EXPECTED_SOURCE_LINE" >> "$SHELL_CONFIG"
}


publish_binary() {
  if dotnet publish "$REPOSITORY_ROOT/src/Prompt/Prompt.csproj" \
      --configuration Release \
      --runtime "$RUNTIME_IDENTIFIER" \
      --nologo \
      --no-restore \
      -p:DebugType=None \
      -p:DebugSymbols=false \
      -o "$PUBLISH_DIRECTORY"; then
    printf 'aot' > "$AOT_STATUS_FILE"
    return 0
  fi

  rm -rf "$PUBLISH_DIRECTORY"
  mkdir -p "$PUBLISH_DIRECTORY"
  printf 'non-aot' > "$AOT_STATUS_FILE"

  dotnet publish "$REPOSITORY_ROOT/src/Prompt/Prompt.csproj" \
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
}

parse_arguments "$@"

BINARY_BASENAME="${BIN_BASENAME:-gitprompt}"
OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"
CPU_ARCHITECTURE="$(uname -m)"

print_banner

case "$OPERATING_SYSTEM" in
  linux) TARGET_OS="linux" ;;
  darwin) TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *)
    print_error_and_exit "Unsupported OS: $OPERATING_SYSTEM"
    ;;
esac

case "$CPU_ARCHITECTURE" in
  x86_64|amd64) TARGET_ARCHITECTURE="amd64" ;;
  *)
    print_status "$COLOR_RED" "ERROR" "Unsupported architecture: $CPU_ARCHITECTURE"
    print_error_and_exit "This script currently supports amd64 only."
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
  print_error_and_exit "dotnet CLI is required but was not found in PATH."
fi

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM
PUBLISH_DIRECTORY="$TEMPORARY_DIRECTORY/publish"
LOG_DIRECTORY="$TEMPORARY_DIRECTORY/logs"
AOT_STATUS_FILE="$TEMPORARY_DIRECTORY/aot_status"
mkdir -p "$PUBLISH_DIRECTORY" "$LOG_DIRECTORY"

print_status "$COLOR_DIM" "INFO" "Target runtime: $RUNTIME_IDENTIFIER"
print_status "$COLOR_DIM" "INFO" "Install path: $INSTALL_DIR/$INSTALLED_BINARY_NAME"
if [ "$VERBOSE_MODE" -eq 1 ]; then
  print_status "$COLOR_DIM" "INFO" "Output mode: verbose"
else
  print_status "$COLOR_DIM" "INFO" "Output mode: quiet (use -v or --verbose to stream dotnet output)"
fi

printf '\n'

run_step "1" "Restoring solution packages" "$LOG_DIRECTORY/restore.log" \
  dotnet restore "$REPOSITORY_ROOT/Prompt.slnx" --nologo

run_step "2" "Building solution (Release)" "$LOG_DIRECTORY/build.log" \
  dotnet build "$REPOSITORY_ROOT/Prompt.slnx" --configuration Release --nologo --no-restore

if [ "$SKIP_TESTS" -ne 1 ]; then
  run_step "3" "Running tests (Release)" "$LOG_DIRECTORY/test.log" \
    dotnet test "$REPOSITORY_ROOT/Prompt.slnx" --configuration Release --nologo --no-build --no-restore
else
  print_step_warning "3" "Running tests (Release)" "0"
fi

run_step "4" "Publishing binary ($RUNTIME_IDENTIFIER, AOT if available)" "$LOG_DIRECTORY/publish.log" \
  publish_binary

if [ "$(cat "$AOT_STATUS_FILE" 2>/dev/null)" = "aot" ]; then
  print_status "$COLOR_DIM" "INFO" "Binary type: native AOT"
else
  print_status "$COLOR_DIM" "INFO" "Binary type: single-file (AOT native toolchain not found)"
fi

SOURCE_BINARY_PATH="$PUBLISH_DIRECTORY/$PUBLISHED_BINARY_NAME"
if [ ! -f "$SOURCE_BINARY_PATH" ]; then
  print_error_and_exit "Published binary not found: $SOURCE_BINARY_PATH"
fi

mkdir -p "$INSTALL_DIR"
FINAL_BINARY_PATH="$INSTALL_DIR/$INSTALLED_BINARY_NAME"
STAGED_BINARY_PATH="$INSTALL_DIR/.${INSTALLED_BINARY_NAME}.new.$$"

cp "$SOURCE_BINARY_PATH" "$STAGED_BINARY_PATH"
chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

run_step "5" "Installing to $FINAL_BINARY_PATH" "$LOG_DIRECTORY/install.log" \
  install_binary

CONFIGURE_SHELL=1
if [ "$YES_MODE" -eq 0 ] && [ -e /dev/tty ]; then
  printf '\nConfigure shell automatically? (writes %s/.promptrc and sources it from ~/.bashrc) [Y/n] ' "$INSTALL_DIR" >/dev/tty
  read -r CONFIGURE_ANSWER </dev/tty
  case "$CONFIGURE_ANSWER" in
    [nN]*) CONFIGURE_SHELL=0 ;;
  esac
fi

if [ "$CONFIGURE_SHELL" -eq 1 ]; then
  run_step "6" "Configuring shell (~/.bashrc)" "$LOG_DIRECTORY/configure.log" \
    configure_shell
  print_status "$COLOR_DIM" "INFO" "Run 'source ~/.bashrc' or open a new terminal to activate the prompt."
else
  print_status "$COLOR_YELLOW" "SKIP" "$(format_colored_step_label "6") Configuring shell (skipped)"
  printf '\n'
  print_status "$COLOR_DIM" "INFO" "Add to your shell config manually:"
  if [ "$TARGET_OS" = "windows" ]; then
    print_status "$COLOR_DIM" "INFO" "  PS1='\$(\"$FINAL_BINARY_PATH\" 2>/dev/null || printf \"\\w > \")'"
  else
    print_status "$COLOR_DIM" "INFO" "  PS1='\$(\"$FINAL_BINARY_PATH\" 2>/dev/null || printf \"\\w \\$ \")'"
  fi
fi

SCRIPT_FINISHED_AT="$(current_timestamp)"
OVERALL_DURATION=$((SCRIPT_FINISHED_AT - SCRIPT_STARTED_AT))

printf '\n'
print_status "$COLOR_GREEN" "DONE" "Installed local build to: $FINAL_BINARY_PATH $(format_duration_segment "$(format_duration "$OVERALL_DURATION")")"
