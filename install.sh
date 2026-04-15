#!/usr/bin/env sh
set -eu

# Install prompt from the latest GitHub release.
# Usage:
#   ./install.sh
#   ./install.sh --yes

TOTAL_STEPS=4
SCRIPT_STARTED_AT="$(date +%s)"
LOADER_INDEX=0
YES_MODE=0

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
  printf '%s%sPrompt installer%s\n' "$COLOR_BOLD" "$COLOR_BLUE" "$COLOR_RESET"
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

run_step() {
  step_number="$1"
  step_message="$2"
  log_file="$3"
  shift 3
  step_started_at="$(current_timestamp)"

  print_step_start "$step_number" "$step_message"

  if [ "$USE_ANSI" -eq 1 ]; then
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

print_usage() {
  cat <<EOF
Usage: sh ./install.sh [options]

Options:
  -y,  --yes                       Auto-configure shell without prompting
  -h,  --help                      Show this help text
EOF
}

parse_arguments() {
  while [ "$#" -gt 0 ]; do
    case "$1" in
      -y|--yes)
        YES_MODE=1
        ;;
      -h|--help)
        print_usage
        exit 0
        ;;
      *)
        print_usage >&2
        print_error_and_exit "Unknown option: $1"
        ;;
    esac
    shift
  done
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


download_release_asset() {
  download_completed=0

  if command -v curl >/dev/null 2>&1; then
    if [ "$TARGET_OS" = "windows" ]; then
      if curl --ssl-no-revoke -fsSL "$RELEASE_ASSET_URL" -o "$RELEASE_ASSET_PATH"; then
        download_completed=1
      fi
    else
      if curl -fsSL "$RELEASE_ASSET_URL" -o "$RELEASE_ASSET_PATH"; then
        download_completed=1
      fi
    fi
  fi

  if [ "$download_completed" -eq 0 ] && command -v wget >/dev/null 2>&1; then
    if wget -qO "$RELEASE_ASSET_PATH" "$RELEASE_ASSET_URL"; then
      download_completed=1
    fi
  fi

  if [ "$download_completed" -eq 0 ] && [ "$TARGET_OS" = "windows" ] && command -v powershell.exe >/dev/null 2>&1; then
    if powershell.exe -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '$RELEASE_ASSET_URL' -OutFile '$RELEASE_ASSET_PATH'" >/dev/null; then
      download_completed=1
    fi
  fi

  if [ "$download_completed" -eq 0 ]; then
    printf '%s\n' "Failed to download release asset." >&2
    printf '%s\n' "If this repository has only prereleases, latest/download will not work." >&2
    printf '%s\n' "Push a new commit to publish a non-prerelease release." >&2
    return 1
  fi
}

extract_release_asset() {
  if [ "$TARGET_OS" = "windows" ]; then
    if command -v unzip >/dev/null 2>&1; then
      unzip -q "$RELEASE_ASSET_PATH" -d "$TEMPORARY_DIRECTORY"
    elif command -v powershell.exe >/dev/null 2>&1; then
      powershell.exe -NoProfile -Command "Expand-Archive -Path '$RELEASE_ASSET_PATH' -DestinationPath '$TEMPORARY_DIRECTORY' -Force" >/dev/null
    else
      printf '%s\n' "Need unzip or powershell.exe to extract zip files." >&2
      return 1
    fi
  else
    tar -xzf "$RELEASE_ASSET_PATH" -C "$TEMPORARY_DIRECTORY"
  fi
}

install_binary() {
  mkdir -p "$INSTALL_DIR"
  cp "$EXTRACTED_BINARY_PATH" "$STAGED_BINARY_PATH"
  chmod +x "$STAGED_BINARY_PATH" 2>/dev/null || true

  if [ "$TARGET_OS" = "windows" ]; then
    if mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH" 2>/dev/null; then
      return 0
    fi

    rm -f "$STAGED_BINARY_PATH"
    printf '%s\n' "Failed to replace $FINAL_BINARY_PATH." >&2
    printf '%s\n' "On Windows, a running .exe may be locked. Close shells using gitprompt and run the installer again." >&2
    return 1
  fi

  mv -f "$STAGED_BINARY_PATH" "$FINAL_BINARY_PATH"
}

BINARY_BASENAME="gitprompt"
OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"
CPU_ARCHITECTURE="$(uname -m)"

parse_arguments "$@"

case "$OPERATING_SYSTEM" in
  linux) TARGET_OS="linux" ;;
  darwin) TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *)
    print_error_and_exit "Unsupported OS: $OPERATING_SYSTEM. Supported: Linux, macOS, Windows (Git Bash)."
    ;;
esac

case "$CPU_ARCHITECTURE" in
  x86_64|amd64) TARGET_ARCHITECTURE="amd64" ;;
  *)
    print_error_and_exit "Unsupported architecture: $CPU_ARCHITECTURE. Supported: amd64 only."
    ;;
esac

if [ "$TARGET_OS" = "windows" ]; then
  INSTALL_DIR="$HOME/prompt"
else
  INSTALL_DIR="$HOME/.local/bin"
fi

if [ "$TARGET_OS" = "windows" ]; then
  RELEASE_ASSET_NAME="prompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.zip"
  EXTRACTED_BINARY_NAME="prompt.exe"
  INSTALLED_BINARY_NAME="${BINARY_BASENAME}.exe"
else
  RELEASE_ASSET_NAME="prompt_${TARGET_OS}_${TARGET_ARCHITECTURE}.tar.gz"
  EXTRACTED_BINARY_NAME="prompt"
  INSTALLED_BINARY_NAME="${BINARY_BASENAME}"
fi

RELEASE_ASSET_URL="https://github.com/Eqwerty/Prompt/releases/download/latest/${RELEASE_ASSET_NAME}"

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM
LOG_DIRECTORY="$TEMPORARY_DIRECTORY/logs"
mkdir -p "$LOG_DIRECTORY"

RELEASE_ASSET_PATH="$TEMPORARY_DIRECTORY/$RELEASE_ASSET_NAME"
EXTRACTED_BINARY_PATH="$TEMPORARY_DIRECTORY/$EXTRACTED_BINARY_NAME"
FINAL_BINARY_PATH="$INSTALL_DIR/$INSTALLED_BINARY_NAME"
STAGED_BINARY_PATH="$INSTALL_DIR/.${INSTALLED_BINARY_NAME}.new.$$"

print_banner
print_status "$COLOR_DIM" "INFO" "Target: ${TARGET_OS}-${TARGET_ARCHITECTURE}"
print_status "$COLOR_DIM" "INFO" "Asset: $RELEASE_ASSET_NAME"
print_status "$COLOR_DIM" "INFO" "Install path: $FINAL_BINARY_PATH"

printf '\n'

run_step "1" "Downloading release asset" "$LOG_DIRECTORY/download.log" \
  download_release_asset

run_step "2" "Extracting release archive" "$LOG_DIRECTORY/extract.log" \
  extract_release_asset

run_step "3" "Installing to $FINAL_BINARY_PATH" "$LOG_DIRECTORY/install.log" \
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
  run_step "4" "Configuring shell (~/.bashrc)" "$LOG_DIRECTORY/configure.log" \
    configure_shell
  print_status "$COLOR_DIM" "INFO" "Run 'source ~/.bashrc' or open a new terminal to activate the prompt."
else
  print_status "$COLOR_YELLOW" "SKIP" "$(format_colored_step_label "4") Configuring shell (skipped)"
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
print_status "$COLOR_GREEN" "DONE" "Installed to $FINAL_BINARY_PATH $(format_duration_segment "$(format_duration "$OVERALL_DURATION")")"

