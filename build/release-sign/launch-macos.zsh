#!/bin/zsh
# Open an interactive Terminal when the calling agent has no terminal input.
#
# Terminal types its command into a shell that may not have started yet, and a
# terminal line that long is cut at 1024 bytes, spilling the rest into the next
# read (the PIN prompt). So the command goes into a script and Terminal is only
# told to run that script.
set -eu

if [[ $OSTYPE != darwin* || $# -lt 3 ]]; then
  print -u2 -- 'Usage (macOS): zsh launch-macos.zsh STATUS_FILE RELEASE_SIGN run [flags...]'
  exit 2
fi

status_file=$1
shift
if [[ $status_file != /* || -e $status_file || ! -d ${status_file:h} ]]; then
  print -u2 -- "Status file must be an unused absolute path in an existing directory: $status_file"
  exit 1
fi

script=$(mktemp "${status_file:h}/release-sign-launch.XXXXXX")
{
  print -r -- "${(j: :)${(q)@}}"
  print -r -- "print -r -- \$? > ${(q)status_file}"
  print -r -- "rm -f -- ${(q)script}"
} > $script

osascript -e 'on run argv' \
  -e 'tell application "Terminal"' \
  -e 'activate' \
  -e 'do script (item 1 of argv)' \
  -e 'end tell' \
  -e 'end run' \
  "zsh ${(q)script}"
