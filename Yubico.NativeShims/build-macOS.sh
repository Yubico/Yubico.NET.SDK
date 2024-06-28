rm -rf build64 buildarm osx-arm64 osx-x64

pushd $VCPKG_INSTALLATION_ROOT
git checkout master
git restore .
git pull
vcpkg x-update-baseline
popd

cmake \
    -S . \
    -B ./build64 \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=x64-osx \
    -DCMAKE_OSX_ARCHITECTURES=x86_64 \
    -DCMAKE_BUILD_TYPE=Release \
    -DOPENSSL_NO_DEBUG=ON \
    -DOPENSSL_NO_TESTS=ON \
    -DOPENSSL_NO_DOCS=ON

cmake --build ./build64
mkdir osx-x64
cp ./build64/libYubico.NativeShims.dylib ./osx-x64

cmake \
    -S . \
    -B ./buildarm \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_TOOLCHAIN_FILE=$VCPKG_INSTALLATION_ROOT/scripts/buildsystems/vcpkg.cmake \
    -DVCPKG_TARGET_TRIPLET=arm64-osx \
    -DCMAKE_OSX_ARCHITECTURES=arm64 \
    -DCMAKE_BUILD_TYPE=Release \
    -DOPENSSL_NO_DEBUG=ON \
    -DOPENSSL_NO_TESTS=ON \
    -DOPENSSL_NO_DOCS=ON

cmake --build ./buildarm
mkdir osx-arm64
cp ./buildarm/libYubico.NativeShims.dylib ./osx-arm64
