#!/bin/zsh
# Open an interactive Terminal when the calling agent has no terminal input.
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

command=''
for argument in "$@"; do
  command+=" ${(q)argument}"
done
command+="; release_sign_status=\$?; print -r -- \$release_sign_status > ${(q)status_file}"

osascript -e 'on run argv' \
  -e 'tell application "Terminal"' \
  -e 'activate' \
  -e 'do script (item 1 of argv)' \
  -e 'end tell' \
  -e 'end run' \
  "$command"
