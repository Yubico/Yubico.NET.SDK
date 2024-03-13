#!/bin/bash

# Ensure vcpkg is up to date
pushd $VCPKG_INSTALLATION_ROOT
git checkout master
git pull
./bootstrap-vcpkg.sh
vcpkg integrate install
popd

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
