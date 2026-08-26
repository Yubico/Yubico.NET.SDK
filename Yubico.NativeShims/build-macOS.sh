#!/bin/bash

# Get version parameter
VERSION=$1

rm -rf build64 buildarm osx-arm64 osx-x64

pushd $VCPKG_INSTALLATION_ROOT
git checkout master
./bootstrap-vcpkg.sh
popd

CMAKE_ARGS="-S . -B ./build64 -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-osx -DVCPKG_OSX_DEPLOYMENT_TARGET=12.0 -DCMAKE_OSX_ARCHITECTURES=x86_64 -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0"
if [ ! -z "$VERSION" ]; then
    CMAKE_ARGS="$CMAKE_ARGS -DPROJECT_VERSION=$VERSION"
fi
cmake $CMAKE_ARGS

cmake --build ./build64
mkdir -p osx-x64
cp ./build64/libYubico.NativeShims.dylib ./osx-x64
mkdir -p osx-x64/static
cp ./build64/static/libYubico.NativeShims.a ./osx-x64/static

CMAKE_ARGS="-S . -B ./buildarm -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=arm64-osx -DVCPKG_OSX_DEPLOYMENT_TARGET=12.0 -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0"
if [ ! -z "$VERSION" ]; then
    CMAKE_ARGS="$CMAKE_ARGS -DPROJECT_VERSION=$VERSION"
fi
cmake $CMAKE_ARGS

cmake --build ./buildarm
mkdir -p osx-arm64
cp ./buildarm/libYubico.NativeShims.dylib ./osx-arm64
mkdir -p osx-arm64/static
cp ./buildarm/static/libYubico.NativeShims.a ./osx-arm64/static
