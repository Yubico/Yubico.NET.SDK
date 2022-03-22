cmake -S . -B ./build64 -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=./cmake/osx-x64.toolchain.cmake
cmake --build ./build64
mkdir osx-x64
cp ./build64/libYubico.NativeShims.dylib ./osx-x64

cmake -S . -B ./buildarm -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=./cmake/osx-arm64.toolchain.cmake
cmake --build ./buildarm
mkdir osx-arm64
cp ./buildarm/libYubico.NativeShims.dylib ./osx-arm64
