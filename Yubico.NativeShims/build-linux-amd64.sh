#!/bin/bash

# cd ~/ && cp -r /mnt/c/Users/Dennis.Dyall/Documents/Work/Yubico.NET.SDK-private/Yubico.NativeShims/ .
# echo "$USER ALL=(ALL:ALL) NOPASSWD: ALL" | sudo tee /etc/sudoers.d/$USER
# echo 'set completion-ignore-case On' | sudo tee -a /etc/inputrc

set -e

export VCPKG_INSTALLATION_ROOT=~/vcpkg \
    VCPKG_ROOT=$VCPKG_INSTALLATION_ROOT \
    VCPKG_FORCE_SYSTEM_BINARIES=1 \
    CMAKE_TOOLCHAIN_FILE=$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake \
    PATH=/usr/local/bin:$VCPKG_ROOT:$PATH

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

wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update
sudo apt-get install cmake -yq

git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh

build_target() {
    local target_triplet=$1
    local build_dir=$2
    local cross_toolchain_file=$3

    rm -rf "$build_dir"
    mkdir -p "$build_dir"
       
    echo "Building for $target_triplet ..."

    cmake -S . -B "$build_dir" \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_TOOLCHAIN_FILE="$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake" \
        -DVCPKG_TARGET_TRIPLET="$target_triplet"
    
    cmake --build "$build_dir" -- -j $(nproc)
    cp "$build_dir"/*.so ./"$build_dir"
}

if [ ! -f ./CMakeLists.txt ]; then
    cd ~/Yubico.NativeShims
fi

build_target x64-linux linux-x64