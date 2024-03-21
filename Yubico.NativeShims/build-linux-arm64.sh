#!/bin/bash

export VCPKG_INSTALLATION_ROOT=$GITHUB_WORKSPACE/vcpkg \
    VCPKG_FORCE_SYSTEM_BINARIES=1 \
    PATH=/usr/local/bin:$VCPKG_INSTALLATION_ROOT:$PATH

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
    gcc-aarch64-linux-gnu

# Install latest version of CMake for Ubuntu 20.04
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update -qq
sudo apt-get install cmake -yq

# Install VCPKG
git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh

# Install arm64 version of libpcsclite
echo "deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ focal main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ focal-updates main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ focal-security main restricted universe multiverse
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ focal-backports main restricted universe multiverse" | sudo tee -a /etc/apt/sources.list > /dev/null
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
cmake -S . -B "$build_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE="$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake" \
    -DVCPKG_TARGET_TRIPLET="arm64-linux" \
    -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="$(pwd)/cmake/aarch64-linux-gnu.toolchain.cmake" \
    -DOPENSSL_ROOT_DIR=$(pwd)/linux-arm64/vcpkg_installed/arm64-linux

cmake --build "$build_dir" -- -j $(nproc)
