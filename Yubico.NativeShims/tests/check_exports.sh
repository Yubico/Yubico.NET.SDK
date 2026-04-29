#!/usr/bin/env bash
# Validate that a built Yubico.NativeShims shared library exports exactly the
# canonical set of symbols defined in expected_symbols.txt.
#
# Usage:  check_exports.sh <path-to-libYubico.NativeShims.{so,dylib}>
#
# Catches: symbols dropped from exports.gnu / exports.llvm, accidental static
# qualifier on a Native_* function, regressions where the export-file list
# drifts from the actual implementation. Works on cross-compiled binaries
# because nm operates on file metadata, not runtime loading.
#
# Exits non-zero on any mismatch (missing or extra symbol).

set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <path-to-shared-library>" >&2
    exit 2
fi

LIB="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXPECTED_FILE="$SCRIPT_DIR/expected_symbols.txt"

if [ ! -f "$LIB" ]; then
    echo "ERROR: shared library not found: $LIB" >&2
    exit 2
fi
if [ ! -f "$EXPECTED_FILE" ]; then
    echo "ERROR: expected_symbols.txt not found at $EXPECTED_FILE" >&2
    exit 2
fi

# Strip comments + blank lines from expected list
EXPECTED=$(grep -v '^[[:space:]]*#' "$EXPECTED_FILE" | grep -v '^[[:space:]]*$' | sort -u)

# Extract Native_* symbols from the binary.
#   macOS: nm -gU lists external defined; symbols carry leading underscore.
#   Linux: nm -D --defined-only lists dynamic-section defined symbols.
UNAME="$(uname -s)"
case "$UNAME" in
    Darwin)
        ACTUAL=$(nm -gU "$LIB" | awk '{print $NF}' | sed 's/^_//' | grep '^Native_' | sort -u)
        ;;
    Linux)
        ACTUAL=$(nm -D --defined-only "$LIB" | awk '{print $NF}' | grep '^Native_' | sort -u)
        ;;
    *)
        echo "ERROR: unsupported host OS '$UNAME' (expected Darwin or Linux)" >&2
        exit 2
        ;;
esac

MISSING=$(comm -23 <(echo "$EXPECTED") <(echo "$ACTUAL") || true)
EXTRA=$(comm -13 <(echo "$EXPECTED") <(echo "$ACTUAL") || true)

EXPECTED_COUNT=$(echo "$EXPECTED" | wc -l | tr -d ' ')
ACTUAL_COUNT=$(echo "$ACTUAL" | wc -l | tr -d ' ')

echo "Library:  $LIB"
echo "Expected: $EXPECTED_COUNT symbols"
echo "Actual:   $ACTUAL_COUNT Native_* symbols"

STATUS=0
if [ -n "$MISSING" ]; then
    echo ""
    echo "FAIL: symbols listed in expected_symbols.txt but NOT exported by the binary:"
    echo "$MISSING" | sed 's/^/  - /'
    STATUS=1
fi
if [ -n "$EXTRA" ]; then
    echo ""
    echo "FAIL: Native_* symbols exported by the binary but NOT in expected_symbols.txt:"
    echo "$EXTRA" | sed 's/^/  - /'
    STATUS=1
fi

if [ $STATUS -eq 0 ]; then
    echo "PASS: export table matches expected symbol list"
fi
exit $STATUS
