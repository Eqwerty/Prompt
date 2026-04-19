#!/usr/bin/env sh
set -eu

# Uninstall gitprompt binary.
# Usage:
#   ./uninstall.sh

TOTAL_STEPS=2
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
  printf '%s%sGitPrompt uninstaller%s\n' "$COLOR_BOLD" "$COLOR_BLUE" "$COLOR_RESET"
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

scan_shell_configs() {
  for config_file in \
    "$HOME/.bashrc" \
    "$HOME/.bash_aliases" \
    "$HOME/.bash_profile" \
    "$HOME/.bash_login" \
    "$HOME/.profile" \
    "$HOME/.zshenv" \
    "$HOME/.zshrc" \
    "$HOME/.zprofile"; do
    if [ ! -f "$config_file" ]; then
      continue
    fi

    matches="$(grep -in "gitprompt" "$config_file" 2>/dev/null || true)"
    if [ -z "$matches" ]; then
      continue
    fi

    printf '%s\n' "$matches" | while IFS= read -r line; do
      printf '%s:%s\n' "$config_file" "$line" >> "$MATCHES_FILE"
    done
  done
}


remove_binary() {
  if [ -f "$FINAL_BINARY_PATH" ]; then
    rm -f "$FINAL_BINARY_PATH"
  else
    printf 'Binary not found at %s — already removed.\n' "$FINAL_BINARY_PATH"
  fi

  # Remove the entire install directory (binary, .gitpromptrc, config.json, cache folders).
  rm -rf "$INSTALL_DIR"
}

OPERATING_SYSTEM="$(uname -s | tr '[:upper:]' '[:lower:]')"

case "$OPERATING_SYSTEM" in
  linux) TARGET_OS="linux" ;;
  darwin) TARGET_OS="darwin" ;;
  msys*|mingw*|cygwin*) TARGET_OS="windows" ;;
  *)
    print_error_and_exit "Unsupported OS: $OPERATING_SYSTEM. Supported: Linux, macOS, Windows (Git Bash)."
    ;;
esac

if [ "$TARGET_OS" = "windows" ]; then
  INSTALL_DIR="$HOME/.gitprompt"
  INSTALLED_BINARY_NAME="gitprompt.exe"
else
  INSTALL_DIR="$HOME/.gitprompt"
  INSTALLED_BINARY_NAME="gitprompt"
fi

FINAL_BINARY_PATH="$INSTALL_DIR/$INSTALLED_BINARY_NAME"
GITPROMPT_RC_PATH="$INSTALL_DIR/.gitpromptrc"

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMPORARY_DIRECTORY"' EXIT INT TERM
LOG_DIRECTORY="$TEMPORARY_DIRECTORY/logs"
MATCHES_FILE="$TEMPORARY_DIRECTORY/matches"
mkdir -p "$LOG_DIRECTORY"
touch "$MATCHES_FILE"

print_banner
print_status "$COLOR_DIM" "INFO" "Binary: $FINAL_BINARY_PATH"

printf '\n'

run_step "1" "Scanning shell configs for gitprompt references" "$LOG_DIRECTORY/scan.log" \
  scan_shell_configs

if [ -s "$MATCHES_FILE" ]; then
  print_status "$COLOR_YELLOW" "WARN" "Found gitprompt references — remove these lines from your shell config manually:"
  while IFS= read -r match; do
    printf '  %s\n' "$match"
  done < "$MATCHES_FILE"
fi

GITPROMPT_RC_EXISTED=0
[ -f "$GITPROMPT_RC_PATH" ] && GITPROMPT_RC_EXISTED=1

run_step "2" "Removing $FINAL_BINARY_PATH" "$LOG_DIRECTORY/remove.log" \
  remove_binary

if [ "$GITPROMPT_RC_EXISTED" -eq 1 ]; then
  print_status "$COLOR_GREEN" "INFO" "Removed shell config: $GITPROMPT_RC_PATH"
fi

SCRIPT_FINISHED_AT="$(current_timestamp)"
OVERALL_DURATION=$((SCRIPT_FINISHED_AT - SCRIPT_STARTED_AT))

printf '\n'
print_status "$COLOR_GREEN" "DONE" "Uninstalled $(format_duration_segment "$(format_duration "$OVERALL_DURATION")")"
