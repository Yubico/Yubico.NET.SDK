rm -rf build64 buildarm osx-arm64 osx-x64

pushd $VCPKG_INSTALLATION_ROOT
git checkout master
./bootstrap-vcpkg.sh
popd

cmake \
    -S . \
    -B ./build64 \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=x64-osx \
    -DCMAKE_OSX_ARCHITECTURES=x86_64

cmake --build ./build64
mkdir -p osx-x64
cp ./build64/libYubico.NativeShims.dylib ./osx-x64

cmake \
    -S . \
    -B ./buildarm \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=arm64-osx \
    -DCMAKE_OSX_ARCHITECTURES=arm64

cmake --build ./buildarm
mkdir -p osx-arm64
cp ./buildarm/libYubico.NativeShims.dylib ./osx-arm64
