#!/bin/bash

# Ensure script fails if any command fails
set -e

# Update and install dependencies
sudo apt-get update -qq
DEBIAN_FRONTEND=noninteractive sudo apt-get install -yq \
    wget \
    ca-certificates \
    gnupg \
    software-properties-common \
    build-essential \
    pkg-config \
    ninja-build \
    zlib1g-dev \
    libpcsclite-dev \
    git \
    curl \
    zip \
    unzip \
    tar

# Install the latest version of CMake from Kitware's repository
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update
sudo apt-get install cmake -yq

# Setup vcpkg
if [ -z "$VCPKG_INSTALLATION_ROOT" ]; then
    echo "VCPKG_INSTALLATION_ROOT is not set. Please set it to your vcpkg installation path."
    exit 1
fi

pushd $VCPKG_INSTALLATION_ROOT
git checkout master
git restore .
git pull
./bootstrap-vcpkg.sh
vcpkg integrate install
vcpkg x-update-baseline
popd

# Set environment variables to match Docker setup
export PATH=/usr/local/bin:$PATH
export CMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake
export VCPKG_FORCE_SYSTEM_BINARIES=1

# Build for Linux amd64
rm -rf build64 linux-x64
mkdir build64 linux-x64
cmake \
    -S . \
    -B ./build64 \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=x64-linux
cmake --build ./build64 -- -j $(nproc)
cp ./build64/*.so ./linux-x64

# Build for Linux arm64
rm -rf buildarm linux-arm64
mkdir buildarm linux-arm64
cmake \
    -S . \
    -B ./buildarm \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=arm64-linux
cmake --build ./buildarm -- -j $(nproc)
cp ./buildarm/*.so ./linux-arm64
