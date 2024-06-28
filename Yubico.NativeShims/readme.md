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

### macOS Build

- Requires XCode
- pkg-config (brew install pkg-config) 
- Navigate to Yubico.NativeShims folder and run `sh ./build-macos.sh`.

### Linux Build

- Should ideally be run in a container (targeting Ubuntu 20.04) to avoid making changes to your environment.
- Run `sh ./build-linux-amd64` or `sh ./build-linux-arm64` depending on the target architecture.

Refer to the provided scripts and GitHub Actions CI workflows for detailed building instructions across different platforms and architectures.

---