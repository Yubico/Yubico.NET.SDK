param(
    [string]$Version = "1.0.0"
)

$vcpkgBaseline = (Get-Content (Join-Path $PSScriptRoot 'vcpkg.json') -Raw | ConvertFrom-Json).'builtin-baseline'
if ($vcpkgBaseline -notmatch '^[0-9a-f]{40}$') {
    throw "Invalid vcpkg builtin-baseline: $vcpkgBaseline"
}

# Build with the exact vcpkg revision declared by the manifest.
Push-Location $env:VCPKG_INSTALLATION_ROOT
try {
    git fetch origin $vcpkgBaseline
    if ($LASTEXITCODE -ne 0) { throw "Failed to fetch vcpkg baseline $vcpkgBaseline." }
    git checkout --detach $vcpkgBaseline
    if ($LASTEXITCODE -ne 0) { throw "Failed to check out vcpkg baseline $vcpkgBaseline." }
    .\bootstrap-vcpkg.bat
    if ($LASTEXITCODE -ne 0) { throw "Failed to bootstrap vcpkg baseline $vcpkgBaseline." }
}
finally {
    Pop-Location
}

# 32-bit builds
$cmakeArgs = @("-S", ".", "-B", "build32", "-A", "Win32", "-DCMAKE_TOOLCHAIN_FILE=$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake", "-DVCPKG_TARGET_TRIPLET=x86-windows-static")
if ($Version) { $cmakeArgs += "-DPROJECT_VERSION=$Version" }
cmake @cmakeArgs
cmake --build build32 --config Release
New-Item -ItemType Directory -Path win-x86 -Force
Copy-Item build32\Release\Yubico.NativeShims.dll win-x86
New-Item -ItemType Directory -Path win-x86\static -Force
Copy-Item build32\static\Yubico.NativeShims.lib win-x86\static

# 64-bit builds
$cmakeArgs = @("-S", ".", "-B", "build64", "-A", "x64", "-DCMAKE_TOOLCHAIN_FILE=$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake", "-DVCPKG_TARGET_TRIPLET=x64-windows-static")
if ($Version) { $cmakeArgs += "-DPROJECT_VERSION=$Version" }
cmake @cmakeArgs
cmake --build build64 --config Release
New-Item -ItemType Directory -Path win-x64 -Force
Copy-Item build64\Release\Yubico.NativeShims.dll win-x64
New-Item -ItemType Directory -Path win-x64\static -Force
Copy-Item build64\static\Yubico.NativeShims.lib win-x64\static

# ARM64 builds
$cmakeArgs = @("-S", ".", "-B", "buildarm", "-A", "arm64", "-DCMAKE_TOOLCHAIN_FILE=$env:VCPKG_INSTALLATION_ROOT\scripts\buildsystems\vcpkg.cmake", "-DVCPKG_TARGET_TRIPLET=arm64-windows-static")
if ($Version) { $cmakeArgs += "-DPROJECT_VERSION=$Version" }
cmake @cmakeArgs
cmake --build buildarm --config Release
New-Item -ItemType Directory -Path win-arm64 -Force
Copy-Item buildarm\Release\Yubico.NativeShims.dll win-arm64
New-Item -ItemType Directory -Path win-arm64\static -Force
Copy-Item buildarm\static\Yubico.NativeShims.lib win-arm64\static
