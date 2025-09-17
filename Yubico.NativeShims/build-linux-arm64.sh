#!/bin/bash

# Get version parameter
VERSION=$1

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
echo "deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports noble main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports noble-updates main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports noble-security main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports noble-backports main restricted universe multiverse" | sudo tee /etc/apt/sources.list.d/arm64.list
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

echo "Building for arm64-linux ..."
CMAKE_ARGS="-S . -B $build_dir -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=arm64-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE=$(pwd)/cmake/aarch64-linux-gnu.toolchain.cmake -DOPENSSL_ROOT_DIR=$(pwd)/linux-arm64/vcpkg_installed/arm64-linux"
if [ ! -z "$VERSION" ]; then
    CMAKE_ARGS="$CMAKE_ARGS -DPROJECT_VERSION=$VERSION"
fi
cmake $CMAKE_ARGS

cmake --build "$build_dir" -- -j $(nproc)
