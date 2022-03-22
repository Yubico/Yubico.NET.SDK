sudo dpkg --add-architecture i386
sudo dpkg --add-architecture arm64
sudo cp ./apt-arm-sources.list /etc/apt/sources.list.d/apt-arm-sources.list
sudo cp ./apt-sources.list /etc/apt/sources.list -f
sudo apt update

sudo apt install libpcsclite-dev -y
cmake -S . -B ./build64 -DCMAKE_BUILD_TYPE=Release
cmake --build ./build64
mkdir ubuntu-x64
cp ./build64/libYubico.NativeShims.so ./ubuntu-x64/

sudo apt install gcc-i686-linux-gnu g++-i686-linux-gnu libpcsclite-dev:i386 -y
export PKG_CONFIG_PATH=/usr/lib/i386-linux-gnu/pkgconfig
cmake -S . -B ./build32 -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=./cmake/ubuntu-x86.toolchain.cmake
cmake --build ./build32
mkdir ubuntu-x86
cp ./build32/libYubico.NativeShims.so ./ubuntu-x86/

sudo apt install gcc-aarch64-linux-gnu g++-aarch64-linux-gnu libpcsclite-dev:arm64 -y
export PKG_CONFIG_PATH=/usr/lib/aarch64-linux-gnu/pkgconfig
cmake -S . -B ./buildarm -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=./cmake/ubuntu-arm64.toolchain.cmake
cmake --build ./buildarm
mkdir ubuntu-arm64
cp ./buildarm/libYubico.NativeShims.so ./ubuntu-arm64/
