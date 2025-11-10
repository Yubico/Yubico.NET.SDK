#!/bin/bash
set -euo pipefail

# Get version parameter (empty string if not provided)
VERSION="${1:-}"

# Set environment variables
export VCPKG_INSTALLATION_ROOT=$GITHUB_WORKSPACE/vcpkg \
    PATH=/usr/local/bin:$PATH

# Install necessary packages
sudo apt-get update -qq && \
DEBIAN_FRONTEND=noninteractive sudo apt-get install -yq \
    git \
    tar \
    curl \
    zip \
    unzip \
    wget \
    build-essential \
    software-properties-common \
    ca-certificates \
    pkg-config \
    gnupg \
    libpcsclite-dev \
    zlib1g-dev \
    ninja-build \
    g++ \
    gcc

# Install latest version of CMake for Ubuntu
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ noble main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update -qq
sudo apt-get install cmake -yq

# Install VCPKG
git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh

## Build
if [ ! -f ./CMakeLists.txt ]; then
    cd ~/Yubico.NativeShims
fi

# Get absolute path for toolchain file
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

build_dir="linux-x64"
rm -rf "$build_dir"
mkdir -p "$build_dir"

# Display compiler configuration
echo "=== Compiler Configuration ==="
echo "CC: ${CC:-not set}"
echo "CXX: ${CXX:-not set}"
if [ -n "${CC:-}" ]; then
    echo "C Compiler version:"
    $CC --version || true
fi
if [ -n "${CXX:-}" ]; then
    echo "C++ Compiler version:"
    $CXX --version || true
fi
echo "============================="
echo ""

echo "Building for x64-linux with Zig targeting glibc 2.28..."
CMAKE_ARGS="-S . -B $build_dir -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE=$SCRIPT_DIR/cmake/x86_64-linux-gnu.toolchain.cmake"
if [ ! -z "$VERSION" ]; then
    CMAKE_ARGS="$CMAKE_ARGS -DPROJECT_VERSION=$VERSION"
fi
cmake $CMAKE_ARGS

cmake --build "$build_dir" -- -j $(nproc)

# Verify glibc compatibility
echo ""
echo "=== Verifying glibc Compatibility ==="
echo "Checking for glibc symbols (must be ≤ 2.28)..."

if ! readelf -V "$build_dir"/*.so | grep -q 'GLIBC_2'; then
    echo "ERROR: No GLIBC version symbols found"
    exit 1
fi

echo "Found GLIBC versions:"
readelf -V "$build_dir"/*.so | grep 'GLIBC_2' | sort -u

# Fail if any version > 2.28
if readelf -V "$build_dir"/*.so | grep -E 'GLIBC_2\.([3-9][0-9]|2[9-9])'; then
    echo "❌ ERROR: Binary contains glibc symbols newer than 2.28"
    exit 1
fi

echo "✅ All symbols compatible with glibc 2.28 or earlier"
echo "============================="
