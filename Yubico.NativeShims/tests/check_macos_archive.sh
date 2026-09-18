#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 2 ]; then
    echo "usage: $0 <archive> <maximum-macos-version>" >&2
    exit 2
fi

ARCHIVE="$1"
MAXIMUM_VERSION="$2"
TEMP_FILE="$(mktemp)"
trap 'rm -f "$TEMP_FILE"' EXIT

if [ ! -f "$ARCHIVE" ]; then
    echo "ERROR: archive not found: $ARCHIVE" >&2
    exit 2
fi

otool -l "$ARCHIVE" > "$TEMP_FILE"
MEMBER_COUNT=$(grep -Ec '\.a\(.+\):$' "$TEMP_FILE" || true)
VERSIONS=$(awk '
    $1 == "cmd" { command = $2 }
    command == "LC_BUILD_VERSION" && $1 == "minos" { print $2 }
    command == "LC_VERSION_MIN_MACOSX" && $1 == "version" { print $2 }
' "$TEMP_FILE")
VERSION_COUNT=$(printf '%s\n' "$VERSIONS" | grep -c . || true)

if [ "$MEMBER_COUNT" -eq 0 ] || [ "$VERSION_COUNT" -ne "$MEMBER_COUNT" ]; then
    echo "ERROR: expected one macOS deployment version for each of $MEMBER_COUNT archive members, found $VERSION_COUNT" >&2
    exit 1
fi

while IFS= read -r version; do
    if ! awk -v actual="$version" -v maximum="$MAXIMUM_VERSION" 'BEGIN {
        split(actual, a, ".")
        split(maximum, m, ".")
        exit !((a[1] + 0 < m[1] + 0) || (a[1] + 0 == m[1] + 0 && a[2] + 0 <= m[2] + 0))
    }'; then
        echo "ERROR: archive member targets macOS $version, newer than $MAXIMUM_VERSION" >&2
        exit 1
    fi
done <<< "$VERSIONS"

echo "PASS: all $MEMBER_COUNT archive members target macOS $MAXIMUM_VERSION or earlier"
