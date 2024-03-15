#!/bin/bash

# cd ~/ && cp -r /mnt/c/Users/Dennis.Dyall/Documents/Work/Yubico.NET.SDK-private/Yubico.NativeShims/ . 
# echo "$USER ALL=(ALL:ALL) NOPASSWD: ALL" | sudo tee /etc/sudoers.d/$USER
# echo 'set completion-ignore-case On' | sudo tee -a /etc/inputrc

#set -e

export VCPKG_INSTALLATION_ROOT=$GITHUB_WORKSPACE/vcpkg \
    VCPKG_FORCE_SYSTEM_BINARIES=1 \
    PATH=/usr/local/bin:$PATH

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

echo "Installing latest Cmake"
wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | sudo tee /usr/share/keyrings/kitware-archive-keyring.gpg >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/kitware-archive-keyring.gpg] https://apt.kitware.com/ubuntu/ focal main' | sudo tee /etc/apt/sources.list.d/kitware.list >/dev/null
sudo apt-get update
sudo apt-get install cmake
echo "Cmake installed"

echo "Installing VCPKG"
git clone https://github.com/Microsoft/vcpkg.git ${VCPKG_INSTALLATION_ROOT} && ${VCPKG_INSTALLATION_ROOT}/bootstrap-vcpkg.sh
echo "VCPKG Installed"

# Is this needed? Yes to install it. Unless we can find it from another source
echo "Adding libpcsclite-dev:arm64 sources"
echo "deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ focal main restricted universe multiverse" | sudo tee -a /etc/apt/sources.list > /dev/null

sudo dpkg --add-architecture arm64
echo "dpkg --print-architecture"
dpkg --print-architecture

sudo apt-get update
DEBIAN_FRONTEND=noninteractive sudo apt-get install libpcsclite-dev:arm64 

build_target() {
    local target_triplet=$1
    build_dir="build-$target_triplet"

    rm -rf "$build_dir"
    mkdir -p "$build_dir"

    echo "Building for $target_triplet ..."
    PKG_CONFIG_PATH="/usr/lib/aarch64-linux-gnu/pkgconfig:$(pwd)/${target_triplet}/vcpkg_installed/arm64-linux/lib/pkgconfig" cmake -S . -B "$build_dir" \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_TOOLCHAIN_FILE="$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake" \
        -DVCPKG_TARGET_TRIPLET="$target_triplet" \
        -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="$(pwd)/cmake/aarch64-linux-gnu.toolchain.cmake" \
        -DOPENSSL_ROOT_DIR=$(pwd)/linux-arm64/vcpkg_installed/arm64-linux

    cmake --build "$build_dir" -- -j $(nproc)
}

if [ ! -f ./CMakeLists.txt ]; then
    cd ~/Yubico.NativeShims
fi

build_target arm64-linux
cp build-arm64-linux/*.so ./linux-arm64

