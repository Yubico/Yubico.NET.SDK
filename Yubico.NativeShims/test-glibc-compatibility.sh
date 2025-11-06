#!/bin/bash

# Test script to verify glibc 2.28 compatibility of NativeShims
# This script checks the actual glibc version requirements of the compiled library

set -e

echo "================================================"
echo "NativeShims glibc Compatibility Test"
echo "================================================"
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if library file is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <path-to-libYubico.NativeShims.so>"
    echo ""
    echo "Example:"
    echo "  $0 linux-x64/libYubico.NativeShims.so"
    exit 1
fi

LIBRARY_PATH="$1"

if [ ! -f "$LIBRARY_PATH" ]; then
    echo -e "${RED}Error: Library file not found: $LIBRARY_PATH${NC}"
    exit 1
fi

echo -e "${YELLOW}Testing library: $LIBRARY_PATH${NC}"
echo ""

# 1. Check if the file is a valid ELF binary
echo "1. Checking if file is a valid ELF binary..."
if file "$LIBRARY_PATH" | grep -q "ELF"; then
    echo -e "${GREEN}✓ Valid ELF binary${NC}"
else
    echo -e "${RED}✗ Not a valid ELF binary${NC}"
    exit 1
fi
echo ""

# 2. Check GLIBC version requirements
echo "2. Checking GLIBC version requirements..."
echo ""
echo "Required GLIBC versions:"
readelf -V "$LIBRARY_PATH" | grep "Version:" | grep "GLIBC" | sort -u

# Extract the highest GLIBC version required
MAX_GLIBC=$(readelf -V "$LIBRARY_PATH" | grep -oP 'GLIBC_\K[0-9.]+' | sort -V | tail -1)

if [ -z "$MAX_GLIBC" ]; then
    echo -e "${RED}✗ Could not determine GLIBC version${NC}"
    exit 1
fi

echo ""
echo -e "Highest GLIBC version required: ${YELLOW}$MAX_GLIBC${NC}"

# Compare with target version 2.28
if [ "$(printf '%s\n' "2.28" "$MAX_GLIBC" | sort -V | head -n1)" = "2.28" ]; then
    if [ "$MAX_GLIBC" = "2.28" ]; then
        echo -e "${GREEN}✓ Requires exactly GLIBC 2.28${NC}"
    elif [ "$(printf '%s\n' "2.28" "$MAX_GLIBC" | sort -V | tail -n1)" = "$MAX_GLIBC" ]; then
        echo -e "${GREEN}✓ Compatible with GLIBC 2.28+ (requires $MAX_GLIBC)${NC}"
    fi
else
    echo -e "${RED}✗ Requires GLIBC $MAX_GLIBC which is newer than 2.28${NC}"
    echo -e "${RED}  This will NOT work on systems with glibc 2.28${NC}"
    exit 1
fi
echo ""

# 3. Check all symbol versions
echo "3. Detailed symbol version analysis..."
echo ""
readelf -V "$LIBRARY_PATH" | grep "GLIBC" | awk '{print $5}' | sort | uniq -c | sort -rn
echo ""

# 4. List dynamic dependencies
echo "4. Dynamic library dependencies..."
echo ""
readelf -d "$LIBRARY_PATH" | grep NEEDED
echo ""

# 5. Check for any suspicious symbols
echo "5. Checking for symbols that might require newer glibc..."
echo ""
SUSPICIOUS_SYMBOLS=$(objdump -T "$LIBRARY_PATH" | grep -E "GLIBC_2\.(3[0-9]|[4-9][0-9])" || true)
if [ -z "$SUSPICIOUS_SYMBOLS" ]; then
    echo -e "${GREEN}✓ No symbols requiring glibc > 2.28 found${NC}"
else
    echo -e "${RED}✗ Found symbols requiring glibc > 2.28:${NC}"
    echo "$SUSPICIOUS_SYMBOLS"
    exit 1
fi
echo ""

# Summary
echo "================================================"
echo "SUMMARY"
echo "================================================"
echo -e "Library: ${YELLOW}$LIBRARY_PATH${NC}"
echo -e "Maximum GLIBC required: ${YELLOW}$MAX_GLIBC${NC}"

if [ "$(printf '%s\n' "2.28" "$MAX_GLIBC" | sort -V | head -n1)" = "2.28" ] && \
   [ "$(printf '%s\n' "2.33" "$MAX_GLIBC" | sort -V | tail -n1)" = "2.33" ]; then
    echo -e "Status: ${GREEN}✓ COMPATIBLE with glibc 2.28${NC}"
    echo ""
    echo "This library should work on:"
    echo "  - Ubuntu 18.04+ (glibc 2.27)"
    echo "  - Debian 10+ (glibc 2.28)"
    echo "  - RHEL 8+ (glibc 2.28)"
    echo "  - CentOS 8+ (glibc 2.28)"
    echo "  - MX Linux 21+ (glibc 2.31)"
    exit 0
else
    echo -e "Status: ${RED}✗ NOT COMPATIBLE with glibc 2.28${NC}"
    echo ""
    echo "This library requires glibc $MAX_GLIBC or higher"
    exit 1
fi
