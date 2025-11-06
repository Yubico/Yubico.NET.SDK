#!/bin/bash

# End-to-end test: Load NativeShims in a .NET app on glibc 2.28
# This test uses Docker to simulate older Linux distributions

set -e

echo "================================================"
echo "NativeShims Runtime Compatibility Test"
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

# Create a temporary directory for our test
TEST_DIR=$(mktemp -d)
trap "rm -rf $TEST_DIR" EXIT

echo "Creating test environment in: $TEST_DIR"
echo ""

# Copy the library
cp "$LIBRARY_PATH" "$TEST_DIR/libYubico.NativeShims.so"

# Create a simple .NET test program
cat > "$TEST_DIR/TestApp.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
EOF

# Create a test program that loads the library
cat > "$TEST_DIR/Program.cs" << 'EOF'
using System;
using System.Runtime.InteropServices;

class Program
{
    // Try to load a simple function from NativeShims
    [DllImport("libYubico.NativeShims.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Native_BN_new();

    static int Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("NativeShims Load Test");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Print system information
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($".NET Version: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine();

        // Check glibc version
        try
        {
            var glibcVersion = Marshal.PtrToStringAnsi(gnu_get_libc_version());
            Console.WriteLine($"glibc version: {glibcVersion}");
        }
        catch
        {
            Console.WriteLine("Could not determine glibc version");
        }
        Console.WriteLine();

        // Try to load the library
        Console.WriteLine("Attempting to call Native_BN_new()...");
        try
        {
            var result = Native_BN_new();
            if (result != IntPtr.Zero)
            {
                Console.WriteLine("SUCCESS: Library loaded and function called!");
                Console.WriteLine($"Result: 0x{result.ToString("X")}");

                // Clean up
                Native_BN_clear_free(result);
                return 0;
            }
            else
            {
                Console.WriteLine("ERROR: Function returned null");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to load library: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return 1;
        }
    }

    [DllImport("libc.so.6")]
    private static extern IntPtr gnu_get_libc_version();

    [DllImport("libYubico.NativeShims.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Native_BN_clear_free(IntPtr bn);
}
EOF

# Create Dockerfile for testing on different glibc versions
cat > "$TEST_DIR/Dockerfile" << 'EOF'
ARG BASE_IMAGE
FROM ${BASE_IMAGE}

# Install .NET 6
RUN apt-get update && apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-6.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /test
COPY . .

# Build the test app
RUN dotnet build TestApp.csproj

# Make sure library is in the right place
RUN mkdir -p bin/Debug/net6.0/ && \
    cp libYubico.NativeShims.so bin/Debug/net6.0/

CMD ["dotnet", "run"]
EOF

# Test on different distributions
echo "================================================"
echo "Testing on different distributions"
echo "================================================"
echo ""

declare -a TEST_IMAGES=(
    "ubuntu:18.04"  # glibc 2.27
    "ubuntu:20.04"  # glibc 2.31
    "debian:10"     # glibc 2.28
    "centos:8"      # glibc 2.28
)

declare -a TEST_NAMES=(
    "Ubuntu 18.04 (glibc 2.27)"
    "Ubuntu 20.04 (glibc 2.31)"
    "Debian 10 (glibc 2.28)"
    "CentOS 8 (glibc 2.28)"
)

TOTAL_TESTS=${#TEST_IMAGES[@]}
PASSED=0
FAILED=0

for i in "${!TEST_IMAGES[@]}"; do
    IMAGE="${TEST_IMAGES[$i]}"
    NAME="${TEST_NAMES[$i]}"

    echo ""
    echo "------------------------------------------------"
    echo "Test $((i+1))/$TOTAL_TESTS: $NAME"
    echo "------------------------------------------------"

    # Build and run the test
    if docker build --build-arg BASE_IMAGE="$IMAGE" -t nativeshims-test:latest "$TEST_DIR" > /dev/null 2>&1 && \
       docker run --rm nativeshims-test:latest; then
        echo -e "${GREEN}✓ PASSED: $NAME${NC}"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED: $NAME${NC}"
        ((FAILED++))
    fi
done

echo ""
echo "================================================"
echo "SUMMARY"
echo "================================================"
echo "Total tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED${NC}"
    echo "The library is compatible with glibc 2.28+"
    exit 0
else
    echo -e "${RED}✗ SOME TESTS FAILED${NC}"
    echo "The library may not be compatible with all target systems"
    exit 1
fi
