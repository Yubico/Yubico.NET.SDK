#!/usr/bin/env bash
# Validate that a built Yubico.NativeShims Linux shared library has all PT_LOAD
# segments aligned to at least 64 KB (0x10000).
#
# Usage:  check_alignment.sh <path-to-libYubico.NativeShims.so>
#
# Why: an ELF segment aligned to N bytes can only be memory-mapped on a system
# whose runtime page size divides N. x86_64 links at 4 KB by default, which fails
# on Android 15+ 16 KB-page devices (Google Play requirement) and on 64 KB-page
# aarch64 Linux distros (RHEL/Fedora). 64 KB alignment is a superset that loads
# correctly on 4 KB, 16 KB and 64 KB page systems. See CMakeLists.txt and
# https://developer.android.com/guide/practices/page-sizes
#
# Works on cross-compiled binaries because it reads ELF file metadata, not the
# runtime loader. Exits non-zero if any LOAD segment is aligned below 64 KB.

set -euo pipefail

# Minimum acceptable PT_LOAD alignment (64 KB). Covers Android 16 KB pages and
# aarch64 Linux 64 KB-page kernels in a single value.
MIN_ALIGN=$((0x10000))

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <path-to-shared-library>" >&2
    exit 2
fi

LIB="$1"

if [ ! -f "$LIB" ]; then
    echo "ERROR: shared library not found: $LIB" >&2
    exit 2
fi

# Pick an available ELF reader. readelf (GNU binutils) is present on Linux CI;
# llvm-readelf is the LLVM equivalent used on macOS dev machines.
READELF=""
for candidate in readelf llvm-readelf; do
    if command -v "$candidate" >/dev/null 2>&1; then
        READELF="$candidate"
        break
    fi
done
if [ -z "$READELF" ]; then
    echo "ERROR: neither 'readelf' nor 'llvm-readelf' found on PATH" >&2
    exit 2
fi

# Collect the alignment column (last field) of every LOAD program header.
# -W (wide) keeps each header on a single line so $NF is reliably the Align.
ALIGNS=$("$READELF" -lW "$LIB" | awk '$1=="LOAD"{print $NF}')

if [ -z "$ALIGNS" ]; then
    echo "ERROR: no PT_LOAD segments found in $LIB (not an ELF shared library?)" >&2
    exit 2
fi

echo "Library:  $LIB"
echo "Required: LOAD alignment >= 0x$(printf '%x' "$MIN_ALIGN") (64 KB)"

STATUS=0
COUNT=0
while IFS= read -r align; do
    [ -z "$align" ] && continue
    COUNT=$((COUNT + 1))
    # Alignments are reported as hex (e.g. 0x10000). Normalize to a number.
    dec=$((align))
    if [ "$dec" -lt "$MIN_ALIGN" ]; then
        echo "  FAIL: LOAD segment aligned to $align (< 64 KB)"
        STATUS=1
    else
        echo "  ok:   LOAD segment aligned to $align"
    fi
done <<< "$ALIGNS"

echo "Checked:  $COUNT LOAD segment(s)"

if [ $STATUS -eq 0 ]; then
    echo "PASS: all LOAD segments aligned to >= 64 KB"
else
    echo "FAIL: one or more LOAD segments aligned below 64 KB" >&2
    echo "      Ensure -Wl,-z,max-page-size=65536 is applied at link time." >&2
fi
exit $STATUS
