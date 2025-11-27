#!/bin/bash
set -euo pipefail

# Get version parameter (empty string if not provided)
VERSION="${1:-}"

# Set environment variables
export VCPKG_INSTALLATION_ROOT=$GITHUB_WORKSPACE/vcpkg \
    VCPKG_FORCE_SYSTEM_BINARIES=1 \
    VCPKG_DISABLE_METRICS=1 \
    PATH=/usr/local/bin:$VCPKG_INSTALLATION_ROOT:$PATH

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
    zlib1g-dev \
    ninja-build \
    g++-aarch64-linux-gnu \
    gcc-aarch64-linux-gnu \
    linux-libc-dev

# Install latest version of CMake for Ubuntu
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ noble main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update -qq
sudo apt-get install cmake -yq

# Install VCPKG
git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh

# Install arm64 version of libpcsclite
# security.ubuntu.com only hosts amd64/i386, not arm64
# We need to restrict default sources to amd64 and add arm64 sources from ports.ubuntu.com

# Ubuntu 24.04 uses DEB822 format in /etc/apt/sources.list.d/ubuntu.sources
# We need to add Architectures: amd64 to existing sources and create arm64 sources

# Backup the original ubuntu.sources if it exists
if [ -f /etc/apt/sources.list.d/ubuntu.sources ]; then
    sudo cp /etc/apt/sources.list.d/ubuntu.sources /etc/apt/sources.list.d/ubuntu.sources.bak

    # Add "Architectures: amd64" to each stanza in ubuntu.sources
    # This awk script adds the Architectures line after the Types line in each stanza
    sudo awk '
        /^Types:/ { print; print "Architectures: amd64"; next }
        { print }
    ' /etc/apt/sources.list.d/ubuntu.sources.bak | sudo tee /etc/apt/sources.list.d/ubuntu.sources > /dev/null
fi

# Also handle traditional sources.list if it exists and has content
if [ -f /etc/apt/sources.list ] && [ -s /etc/apt/sources.list ]; then
    sudo cp /etc/apt/sources.list /etc/apt/sources.list.bak
    sudo sed -i 's/^deb \(https\?\)/deb [arch=amd64] \1/' /etc/apt/sources.list
    sudo sed -i 's/^deb-src \(https\?\)/deb-src [arch=amd64] \1/' /etc/apt/sources.list
fi

# Create arm64 sources in DEB822 format
sudo tee /etc/apt/sources.list.d/ubuntu-ports-arm64.sources > /dev/null <<EOF
Types: deb
URIs: http://ports.ubuntu.com/ubuntu-ports/
Suites: noble noble-updates noble-backports
Components: main restricted universe multiverse
Architectures: arm64
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg

Types: deb
URIs: http://ports.ubuntu.com/ubuntu-ports/
Suites: noble-security
Components: main restricted universe multiverse
Architectures: arm64
Signed-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg
EOF

# Add arm64 architecture and install
sudo dpkg --add-architecture arm64
sudo apt-get update -qq
sudo apt-get install libpcsclite-dev:arm64 -yq

## Build
if [ ! -f ./CMakeLists.txt ]; then
    cd ~/Yubico.NativeShims
fi

# Add paths to our libraries so that CMake finds the correct arm64 ones
export PKG_CONFIG_PATH="/usr/lib/aarch64-linux-gnu/pkgconfig:$(pwd)/arm64-linux/vcpkg_installed/arm64-linux/lib/pkgconfig"

build_dir=linux-arm64
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

echo "Building for arm64-linux with Zig targeting glibc 2.23..."
CMAKE_ARGS="-S . -B $build_dir -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=arm64-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE=$(pwd)/cmake/aarch64-linux-gnu.toolchain.cmake -DOPENSSL_ROOT_DIR=$(pwd)/linux-arm64/vcpkg_installed/arm64-linux"
if [ ! -z "$VERSION" ]; then
    CMAKE_ARGS="$CMAKE_ARGS -DPROJECT_VERSION=$VERSION"
fi
cmake $CMAKE_ARGS

cmake --build "$build_dir" -- -j $(nproc)

# Verify glibc compatibility
echo ""
echo "=== Verifying glibc Compatibility ==="
echo "Checking for glibc symbols (must be ≤ 2.23)..."

if ! readelf -V "$build_dir"/*.so | grep -q 'GLIBC_2'; then
    echo "ERROR: No GLIBC version symbols found"
    exit 1
fi

echo "Found GLIBC versions:"
readelf -V "$build_dir"/*.so | grep 'GLIBC_2' | sort -u

# Fail if any version > 2.23
if readelf -V "$build_dir"/*.so | grep -E 'GLIBC_2\.([3-9][0-9]|2[4-9])'; then
    echo "❌ ERROR: Binary contains glibc symbols newer than 2.23"
    exit 1
fi

echo "✅ All symbols compatible with glibc 2.23 or earlier"
echo "============================="
