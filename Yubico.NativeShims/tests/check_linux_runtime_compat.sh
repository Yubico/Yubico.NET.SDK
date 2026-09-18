#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <shared-library>" >&2
    exit 2
fi

LIBRARY="$1"
if [ ! -f "$LIBRARY" ]; then
    echo "ERROR: shared library not found: $LIBRARY" >&2
    exit 1
fi

ldd --version | sed -n '1p'

set +e
DEPENDENCIES="$(ldd "$LIBRARY" 2>&1)"
LDD_STATUS=$?
set -e
printf '%s\n' "$DEPENDENCIES"

if [ "$LDD_STATUS" -ne 0 ]; then
    echo "ERROR: the container's dynamic loader rejected $LIBRARY" >&2
    exit 1
fi

# The base compatibility images intentionally do not install PC/SC. Every
# other dependency must resolve, and a glibc version error is therefore fatal.
UNEXPECTED_MISSING="$(printf '%s\n' "$DEPENDENCIES" \
    | grep 'not found' \
    | grep -vE '^[[:space:]]*libpcsclite\.so\.1 => not found$' \
    || true)"
if [ -n "$UNEXPECTED_MISSING" ]; then
    echo "ERROR: unexpected unresolved dependencies:" >&2
    printf '%s\n' "$UNEXPECTED_MISSING" >&2
    exit 1
fi

echo "Dynamic loader compatibility passed."
