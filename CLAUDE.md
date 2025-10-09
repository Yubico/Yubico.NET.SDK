# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Yubico.NET.SDK (YubiKit) is a .NET SDK for interacting with YubiKey devices. The project targets both .NET 8 and .NET 10, uses C# preview language features, and has nullable reference types enabled throughout.

## Build and Test Commands

### Build
```bash
dotnet build Yubico.YubiKit.sln
```

### Run All Tests
```bash
dotnet test Yubico.YubiKit.sln
```

### Run Specific Test Projects
```bash
# Unit tests only
dotnet test Yubico.YubiKit.Tests.UnitTests/Yubico.YubiKit.Tests.UnitTests.csproj

# Integration tests only (requires YubiKey hardware)
dotnet test Yubico.YubiKit.Tests.IntegrationTests/Yubico.YubiKit.Tests.IntegrationTests.csproj
```

### Run Tests with Coverage
```bash
dotnet test --settings coverlet.runsettings.xml --collect:"XPlat Code Coverage"
```

## Architecture

### Core Components

**Yubico.YubiKit.Core** - The foundational library containing:
- **Device Management**: `DeviceRepository`, `DeviceMonitorService`, `DeviceListenerService` handle device discovery and lifecycle
- **Connection Layer**: Abstraction over different connection types (SmartCard/PCSC, HID)
- **Protocol Layer**: ISO 7816-4 APDU handling with support for command chaining and extended APDUs
- **Platform Interop**: Cross-platform native library loading for Windows, macOS, and Linux
- **Dependency Injection**: Extension methods in `DependencyInjection.cs` for registering services via `AddYubiKeyManagerCore()`

**Yubico.YubiKit.Management** - Management interface for YubiKeys:
- `ManagementSession<TConnection>` provides device info queries
- `DeviceInfo` represents device capabilities, firmware version, form factor, etc.
- Generic over connection type to support different protocols

### Key Patterns

**Device Discovery and Monitoring**:
- `IDeviceRepository` maintains a cache of discovered devices and publishes `DeviceEvent` notifications via `IObservable<DeviceEvent>`
- `DeviceMonitorService` runs as a hosted service monitoring for device arrival/removal
- `DeviceListenerService` handles background device scanning
- Uses System.Reactive for event streaming

**Connection Abstraction**:
- `IConnection` is the base interface (SmartCard, HID, etc.)
- `IProtocol` abstracts communication (e.g., `ISmartCardProtocol`)
- Factory pattern used throughout: `ISmartCardConnectionFactory`, `IProtocolFactory<T>`, `IYubiKeyFactory`

**APDU Processing Pipeline**:
- `IApduFormatter` (ShortApduFormatter, ExtendedApduFormatter)
- `IApduProcessor` decorators: `CommandChainingProcessor`, `ChainedResponseProcessor`, `ApduFormatProcessor`
- Enables transparent handling of APDU size limits and command chaining

**Application Sessions**:
- Base class `ApplicationSession` provides common session functionality
- Specific sessions (e.g., `ManagementSession<TConnection>`) implement protocol-specific operations
- Sessions are generic over connection type for flexibility

### Multi-targeting

Projects target both `net8` and `net10.0`. The codebase uses `LangVersion=preview` to access the latest C# features including:
- Primary constructors
- Collection expressions (`[..]`)
- Extension types (see `DependencyInjection.cs`)

### Platform-Specific Code

Platform interop is isolated in `Core/PlatformInterop/` with platform-specific subdirectories:
- Windows/, macOS/, Linux/ contain P/Invoke declarations
- `UnmanagedDynamicLibrary` and `SafeLibraryHandle` manage native library loading
- `SdkPlatformInfo` detects runtime platform

## Code Style

- 4-space indentation (spaces, not tabs)
- CRLF line endings
- File headers with Apache 2.0 license (see `.editorconfig`)
- No `this.` qualification
- Nullable reference types enabled - all nullability must be explicit
- Async/await throughout with proper `ConfigureAwait(false)` usage
- Prefer dependency injection over static methods/singletons

## Test Structure

- **UnitTests**: Uses xUnit, no hardware required
- **IntegrationTests**: Uses xUnit, requires physical YubiKey device
- **TestProject**: ASP.NET Core test project with NSubstitute for mocking, targets .NET 9 with AOT

All test projects use:
- xUnit as the test framework
- `coverlet.collector` for code coverage
- Implicit usings enabled
- Root namespaces: `Yubico.YubiKit.UnitTests` and `Yubico.YubiKit.IntegrationTests`

## Git Workflow

- Main development branch: `develop` (not `main`)
- Current working branch: `yubikit`
- Use `develop` as the base branch for pull requests
