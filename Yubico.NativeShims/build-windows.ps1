# Update to latest vcpkg baseline
Push-Location $env:VCPKG_INSTALLATION_ROOT
git checkout master
git restore .
git pull
vcpkg x-update-baseline
Pop-Location

# 32-bit builds
cmake -S . -B build32 -A Win32 -DCMAKE_TOOLCHAIN_FILE="$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x86-windows-static
cmake --build build32 --config Release
mkdir win-x86
Copy-Item build32\Release\Yubico.NativeShims.dll win-x86

# 64-bit builds
cmake -S . -B build64 -A x64 -DCMAKE_TOOLCHAIN_FILE="$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x64-windows-static
cmake --build build64 --config Release
mkdir win-x64
Copy-Item build64\Release\Yubico.NativeShims.dll win-x64

# ARM64 builds
cmake -S . -B buildarm -A arm64 -DCMAKE_TOOLCHAIN_FILE="$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=arm64-windows-static
cmake --build buildarm --config Release
mkdir win-arm64
Copy-Item buildarm\Release\Yubico.NativeShims.dll win-arm64