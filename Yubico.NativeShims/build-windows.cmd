:: 32-bit builds
cmake -S . -B build32 -A Win32 -DCMAKE_TOOLCHAIN_FILE=%VCPKG_INSTALLATION_ROOT%/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x86-windows-static
cmake --build build32 --config Release
mkdir win-x86
copy build32\Release\Yubico.NativeShims.dll win-x86

:: 64-bit builds
cmake -S . -B build64 -A x64 -DCMAKE_TOOLCHAIN_FILE=%VCPKG_INSTALLATION_ROOT%/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-windows-static
cmake --build build64 --config Release
mkdir win-x64
copy build64\Release\Yubico.NativeShims.dll win-x64

:: ARM64 builds
cmake -S . -B buildarm -A arm64 -DCMAKE_TOOLCHAIN_FILE=%VCPKG_INSTALLATION_ROOT%/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=arm64-windows-static
cmake --build buildarm --config Release
mkdir win-arm64
copy buildarm\Release\Yubico.NativeShims.dll win-arm64