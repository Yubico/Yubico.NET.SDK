#!/bin/bash

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

# Install latest version of CMake for Ubuntu 20.04
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update -qq
sudo apt-get install cmake -yq

# Install VCPKG
git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh

## Build
if [ ! -f ./CMakeLists.txt ]; then
    cd ~/Yubico.NativeShims
fi

build_dir="linux-x64"
rm -rf "$build_dir"
mkdir -p "$build_dir"

echo "Building for x64-linux ..."
cmake -S . -B "$build_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE="$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake" \
    -DVCPKG_TARGET_TRIPLET=x64-linux \
    -DCMAKE_BUILD_TYPE=Release \
    -DOPENSSL_NO_DEBUG=ON \
    -DOPENSSL_NO_TESTS=ON \
    -DOPENSSL_NO_DOCS=ON

cmake --build "$build_dir" -- -j $(nproc)
