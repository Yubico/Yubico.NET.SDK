# Yubico.NativeShims

Yubico.NativeShims is a cross-platform C library designed to bridge the gap in native interoperation (interop) within the .NET environment. It serves as a foundational tool to handle nuances in API signatures and build configurations across different operating systems, facilitating a more unified and streamlined P/Invoke integration for Yubico's development stack.

## Why Yubico.NativeShims?

1. **Unified P/Invoke Contracts**: Addresses the limitations of P/Invoke by providing a single, universal contract that adapts to platform-specific differences in native library APIs, avoiding the need for multiple, complex P/Invoke signatures.

2. **Optimized Native Dependencies**: Incorporates essential functionalities from native libraries directly, reducing the SDK's footprint by allowing static linking and selective inclusion of dependencies, ensuring a leaner, more efficient library.

## Building Yubico.NativeShims

### Prerequisites

- **VCPKG**: Utilizes VCPKG to manage native dependencies. Ensure `VCPKG_INSTALLATION_ROOT` environment variable is set to your VCPKG installation path.
- **Platforms**: Supports Windows (x86, x64, arm64), macOS (x64, arm64), and Linux (Ubuntu x64, arm64) through GitHub Actions CI workflows for comprehensive build coverage.

### Windows Build

- Install Visual Studio with C++ workload and ARM64 build tools.
- Use "x64 Native tools command prompt" to navigate and run `./build-windows.ps1`.

> **Note:** The Windows build statically links the MSVC C runtime (`/MT`) so that the resulting DLL does not require the Visual C++ Redistributable to be installed on end-user systems.

### macOS Build

- Requires XCode
- pkg-config (brew install pkg-config) 
- Navigate to Yubico.NativeShims folder and run `bash ./build-macOS.sh`.

### Linux Build

- Compiled with Zig compiler targeting glibc 2.28 for broad Linux distribution compatibility.
- Should ideally be run in a container (targeting Ubuntu 20.04) to avoid making changes to your environment.
- Run `sh ./build-linux-amd64` or `sh ./build-linux-arm64` depending on the target architecture.

Refer to the provided scripts and GitHub Actions CI workflows for detailed building instructions across different platforms and architectures.

## Native AOT

The NuGet package includes a merged static archive for each supported runtime identifier. When a downstream application publishes with `PublishAot` set to `true`, package targets direct-link Yubico.NativeShims and its OpenSSL cryptography dependency, link the platform smart-card library (`winscard`, `pcsclite`, or the macOS PCSC framework), and omit the redundant shared NativeShims library from the publish output. Unsupported runtime identifiers and missing archives fail the publish with a clear error instead of falling back to dynamic loading.

The package does not enable Native AOT. That deployment decision belongs to the downstream application. This support is additive: ordinary builds, non-AOT publishes, dynamic P/Invoke behavior, and the .NET Framework 4.7.2 package targets continue to use the existing shared libraries.

Source builds produce both shared and merged static libraries by default. Pass `-DYUBICO_BUILD_STATIC=OFF` only for a shared-only development build.

---
