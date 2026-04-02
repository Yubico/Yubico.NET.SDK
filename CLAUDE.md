# CLAUDE.md

This file provides guidance to AI agents when working with code in this repository.

**IMPORTANT:** If you are working in a subproject directory (e.g., `src/SecurityDomain/`, `src/Piv/`, etc.), you MUST also read that subproject's `CLAUDE.md` file if it exists. Subproject CLAUDE.md files contain specific patterns, test harness details, and context for that module.

## Project Overview

Yubico.NET.SDK (YubiKit) is a .NET SDK for interacting with YubiKey devices. The project targets .NET 10, uses C# 14 language features (LangVersion=14), and has nullable reference types enabled throughout.

**Reference Documentation:**
- `docs/net10/` contains Microsoft Learn PDFs documenting new .NET 10 features
- We actively use new .NET 10 library features, C# 14 language features, and SDK/tooling improvements
- When implementing features, consult these docs to leverage the latest platform capabilities

## Project Structure

The SDK is organized into the following modules:

All project folders live under `src/` with the `Yubico.YubiKit.` prefix stripped from directory names. Assembly names, namespaces, and DLL output names remain unchanged (e.g., `Yubico.YubiKit.Core`).

**Core Infrastructure:**
- `src/Core/` - Device management, connection abstractions, APDU protocol handling, platform interop
- `src/Management/` - Device information queries, capability detection, firmware version

**YubiKey Applications:**
- `src/Piv/` - PIV (Personal Identity Verification) smart card functionality
- `src/Fido2/` - FIDO2/WebAuthn authentication
- `src/Oath/` - TOTP/HOTP one-time password generation
- `src/YubiOtp/` - Yubico OTP configuration and generation
- `src/OpenPgp/` - OpenPGP card implementation
- `src/SecurityDomain/` - Secure Channel Protocol (SCP03), key management

**Hardware Security Modules:**
- `src/YubiHsm/` - YubiHSM 2 hardware security module integration

**Shared Infrastructure:**
- `src/Cli.Shared/` - Shared CLI infrastructure for example tools

**Testing Infrastructure:**
- `src/Tests.Shared/` - Shared test utilities, multi-transport test harness
- `src/Tests.TestProject/` - xUnit v3 test project structure

**Module-Specific Documentation:**
Each module directory may contain:
- `CLAUDE.md` - AI agent guidance for module-specific patterns and test infrastructure
- `README.md` - Human-readable module documentation with usage examples
- `tests/CLAUDE.md` - Test infrastructure and patterns for that module

## Quick Reference - Critical Rules

**Documentation & Research:**
- ✅ ALWAYS use Context7 MCP (`context7-query-docs` tool) to look up library/API documentation, code patterns, setup/configuration steps, and framework usage without requiring explicit request
- ✅ Use Perplexity AI (`.claude/skills/tool-perplexity-search/SKILL.md`) for current events, recent releases, or up-to-date web information

**Skills to Apply When Coding in This Repository:**
- .claude/skills/tool-codemapper/SKILL.md
- .claude/skills/domain-build/SKILL.md
- .claude/skills/domain-test/SKILL.md
- .claude/skills/domain-yubikit-compare/SKILL.md
- .claude/skills/workflow-interface-refactor/SKILL.md
- .claude/skills/workflow-tdd/SKILL.md
- .claude/skills/workflow-debug/SKILL.md
- .claude/skills/tool-perplexity-search/SKILL.md
   
**Memory Management:**
- ✅ Sync + ≤512 bytes → `Span<byte>` with `stackalloc`
- ✅ Sync + >512 bytes → `ArrayPool<byte>.Shared.Rent()`
- ✅ Async → `Memory<byte>` or `IMemoryOwner<byte>`
- ❌ NEVER use `.ToArray()` unless data must escape scope
- ❌ NEVER forget to return `ArrayPool` buffers (use try/finally)

**Security:**
- ✅ ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`
- ✅ ALWAYS dispose crypto objects: `using var aes = Aes.Create()`
- ❌ NEVER log PINs, keys, or sensitive payloads
- ❌ NEVER use timing-vulnerable comparisons (use `FixedTimeEquals`)

**Modern C#:**
- ✅ ALWAYS use `is null` / `is not null` (never `== null`)
- ✅ ALWAYS use switch expressions (never old switch statements)
- ✅ ALWAYS use file-scoped namespaces
- ✅ ALWAYS use collection expressions `[..]` (C# 12)
- ❌ NEVER suppress nullable warnings with `!` without justification

**Code Quality:**
- ✅ ALWAYS follow `.editorconfig` (run `dotnet format` before commit)
- ✅ ALWAYS handle `CancellationToken` in async methods
- ✅ ALWAYS use `readonly` on fields that don't change
- ❌ NEVER use `#region` (split large classes instead)
- ❌ NEVER use exceptions for control flow

**Legacy Code Reference (Java/C# Implementations):**
- ✅ ALWAYS do forensic byte-level analysis before implementing
- ✅ ALWAYS read actual source code line-by-line, not just documentation
- ✅ ALWAYS trace through with concrete examples (input → output bytes)
- ✅ ALWAYS document the exact wire format/data structure before coding
- ❌ NEVER implement based on conceptual understanding alone
- ❌ NEVER skip verifying exact encoding details (TLV structure, byte order, flags)

**Crypto APIs:**
- ✅ USE: `SHA256.HashData(data, outputSpan)` (Span-based)
- ❌ AVOID: `SHA256.Create().ComputeHash(data)` (allocates array)

**Testing:**
- ✅ ALWAYS use `dotnet build.cs test` (handles xUnit v2/v3 runner differences automatically)
- ❌ NEVER use `dotnet test` directly (fails on xUnit v3 projects with wrong syntax)
- See `docs/TESTING.md` for full testing guidance

**Codebase Orientation:**
- ✅ Run `codemapper .` to generate API surface maps (~1.5s for entire repo)
- ✅ Maps output to `./codebase_ast/` - one file per project (gitignored but readable by agents)
- ✅ Find symbols: `grep -rn "IYubiKey" ./codebase_ast/`
- ✅ Load context: `cat ./codebase_ast/Yubico.YubiKit.Core.txt`
- See `.claude/skills/tool-codemapper/SKILL.md` for full usage

## Build and Test Commands

**IMPORTANT: Use the build script (`build.cs`) for all build, test, and packaging operations.**

The project uses a Bullseye-based build script that provides consistent, well-tested build workflows.

### Quick Start

```bash
# Build the solution
dotnet build.cs build

# Run unit tests
dotnet build.cs test

# Run tests with code coverage
dotnet build.cs coverage

# Create NuGet packages
dotnet build.cs pack

# Publish packages to local feed
dotnet build.cs publish
```

### Available Build Targets

- **clean** - Remove artifacts (add `--clean` to also run `dotnet clean`)
- **restore** - Restore NuGet dependencies
- **build** - Build the solution
- **test** - Run unit tests with nice summary output
- **coverage** - Run tests with code coverage (saves to `artifacts/coverage/`)
- **pack** - Create NuGet packages
- **setup-feed** - Configure local NuGet feed
- **publish** - Publish packages to local feed
- **default** - Run tests and publish

### Build Script Options

```bash
# Override package version
dotnet build.cs pack --package-version 1.0.0-preview.2

# Include XML documentation in packages
dotnet build.cs pack --include-docs

# Dry run (show what would be published)
dotnet build.cs publish --dry-run

# Full clean build
dotnet build.cs build --clean

# Custom NuGet feed
dotnet build.cs publish --nuget-feed-name MyFeed --nuget-feed-path ~/my-feed
```

### Direct dotnet Commands (Fallback)

If you need to bypass the build script:

```bash
# Build directly
dotnet build Yubico.YubiKit.sln

# Run all tests directly
dotnet test Yubico.YubiKit.sln

# Run specific test project
dotnet test src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj

# Run with coverage directly
dotnet test --settings coverlet.runsettings.xml --collect:"XPlat Code Coverage"
```

**Note:** Prefer using `dotnet build.cs [target]` for better output formatting, error handling, and consistent workflows.

## Architecture

### Core Components

**Yubico.YubiKit.Core** - Foundational library:
- **Device Management**: `DeviceRepository`, `DeviceMonitorService`, `DeviceListenerService` for device discovery and lifecycle
- **Connection Layer**: Abstraction over SmartCard/PCSC and HID connection types
- **Protocol Layer**: ISO 7816-4 APDU handling with command chaining and extended APDU support
- **Platform Interop**: Cross-platform native library loading (Windows, macOS, Linux)
- **Dependency Injection**: `AddYubiKeyManagerCore()` extension method in `DependencyInjection.cs`

**Yubico.YubiKit.Management** - Management interface:
- `ManagementSession<TConnection>` for device info queries
- `DeviceInfo` represents capabilities, firmware version, form factor
- Generic over connection type for protocol flexibility

### Key Patterns

**Device Discovery and Monitoring**:
- `IDeviceRepository` maintains device cache and publishes `DeviceEvent` via `IObservable<DeviceEvent>`
- `DeviceMonitorService` runs as hosted service for device arrival/removal
- `DeviceListenerService` handles background scanning
- Uses System.Reactive for event streaming

**Connection Abstraction**:
- `IConnection` base interface (SmartCard, HID, etc.)
- `IProtocol` abstracts communication (e.g., `ISmartCardProtocol`)
- Factory pattern: `ISmartCardConnectionFactory`, `IProtocolFactory<T>`, `IYubiKeyFactory`

**APDU Processing Pipeline**:
- `IApduFormatter` (ShortApduFormatter, ExtendedApduFormatter)
- `IApduProcessor` decorators: `CommandChainingProcessor`, `ChainedResponseProcessor`, `ApduFormatProcessor`
- Transparent handling of APDU size limits and command chaining

**Application Sessions**:
- `ApplicationSession` base class for common functionality
- Protocol-specific sessions (e.g., `ManagementSession<TConnection>`)
- Generic over connection type

### Property Conventions

**Immutability Preference:**
- ✅ `{ get; init; }` - Immutable properties set only at construction
- ✅ `{ get; private set; }` - Properties modified only within the class
- ⚠️ `{ get; set; }` - Use sparingly, only for configuration/mutable DTOs

**Property Initialization:**
- Validate in constructor or via dedicated `Validate()` method
- Use `ArgumentNullException.ThrowIfNull()` for required parameters
- Use `ArgumentOutOfRangeException.ThrowIfNegative()` for numeric constraints

**Computed Properties:**
```csharp
// ✅ Expression-bodied for simple computations
public bool IsValid => _data.Length > 0 && _version >= MinVersion;

// ✅ Traditional getter for complex logic
public ReadOnlySpan<byte> Data
{
    get
    {
        ThrowIfDisposed();
        return _data.AsSpan();
    }
}
```

### Logging Conventions

**Use Static LoggingFactory - NEVER inject ILogger:**
```csharp
// ✅ CORRECT: Static logger from factory
public class FidoSession
{
    private static readonly ILogger Logger = LoggingFactory.CreateLogger<FidoSession>();
}

// ❌ WRONG: Injected logger (breaks consistency)
public class FidoSession(ILogger<FidoSession> logger) { }
```

**Log Levels:**
- `Trace` - Raw APDU/CBOR bytes, detailed protocol steps
- `Debug` - Protocol-level operations, state transitions
- `Info` - Session creation, major operations (enroll, authenticate)
- `Warning` - Recoverable errors, fallback behavior
- `Error` - Operation failures, exceptions

**Logging Sensitive Data:**
- ❌ NEVER log PINs, keys, or credentials
- ✅ Log credential IDs as hex (public identifier)
- ✅ Log lengths, not contents, of sensitive buffers

### Target Framework

Projects target`net10.0` with `LangVersion=14.0` for:
- Primary constructors
- Collection expressions `[..]`
- Extension types

### Platform-Specific Code

Platform interop in `Core/PlatformInterop/` with subdirectories:
- `Windows/`, `macOS/`, `Linux/` contain P/Invoke declarations
- `UnmanagedDynamicLibrary` and `SafeLibraryHandle` manage native library loading
- `SdkPlatformInfo` detects runtime platform

## Performance and Security Best Practices

### Memory Management Hierarchy

**Follow this order of precedence (most preferred to least):**

#### 1. Span<byte> and ReadOnlySpan<byte> (BEST)

Use for synchronous operations with stack-allocated or borrowed memory.

```csharp
// ✅ Zero allocation, stack-based
Span<byte> buffer = stackalloc byte[256];
ProcessApdu(buffer);

// ✅ Slicing without allocation
ReadOnlySpan<byte> header = apduData.AsSpan()[..5];
ReadOnlySpan<byte> body = apduData.AsSpan()[5..];

// ✅ Parameters for zero-copy
public void ProcessData(ReadOnlySpan<byte> data) { }
```

**Limitations:**
- Cannot be used in async methods (compiler error)
- Cannot be stored in fields (ref struct)
- Limit stackalloc to ≤512 bytes

#### 2. Memory<byte> and ReadOnlyMemory<byte>

Use when crossing async boundaries.

```csharp
// ✅ Async-safe
public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
{
    return await stream.ReadAsync(buffer, ct);
}

// ✅ Convert to Span when sync context resumes
public async Task ProcessAsync(Memory<byte> data, CancellationToken ct)
{
    await SomeAsyncOperation();
    Span<byte> span = data.Span;  // Now work with span
    ProcessData(span);
}

// ✅ Use IMemoryOwner for temporary buffers in async
using var owner = MemoryPool<byte>.Shared.Rent(4096);
Memory<byte> memory = owner.Memory[..actualSize];
await ProcessAsync(memory, ct);
```

#### 3. ArrayPool<T> Rented Arrays

Use for temporary buffers >512 bytes.

```csharp
// ✅ Rent, use, return
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    Span<byte> span = buffer.AsSpan(0, actualLength);
    ProcessData(span);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ✅ Zero sensitive data before returning
byte[] pinBuffer = ArrayPool<byte>.Shared.Rent(8);
try
{
    Span<byte> pin = pinBuffer.AsSpan(0, pinLength);
    // Use for PIN operations
}
finally
{
    CryptographicOperations.ZeroMemory(pinBuffer.AsSpan(0, pinLength));
    ArrayPool<byte>.Shared.Return(pinBuffer, clearArray: false); // Already zeroed
}
```

**Guidelines:**
- Always use try/finally
- Consider `clearArray: true` for defense-in-depth on sensitive data
- Don't rent excessively large buffers (wastes pool resources)

#### 4. Regular Arrays (LAST RESORT)

Only allocate when:
- Data must be returned and lifetime is unclear
- Storing in fields/properties
- Interop requires array type
- Collection initialization with known size

```csharp
// ❌ BAD - Allocates every call
public byte[] ProcessData(byte[] input)
{
    return new byte[input.Length];
}

// ✅ BETTER - Use Span
public void ProcessData(ReadOnlySpan<byte> input, Span<byte> output) { }

// ✅ OR - Use ArrayPool
public byte[] ProcessData(byte[] input)
{
    byte[] temp = ArrayPool<byte>.Shared.Rent(input.Length);
    try
    {
        // Process...
        byte[] result = new byte[actualLength];
        Array.Copy(temp, result, actualLength);
        return result;
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(temp);
    }
}
```

### Decision Tree

```
Need byte buffer?
├─ Synchronous?
│  ├─ ≤512 bytes? → Span<byte> with stackalloc ✅ BEST
│  └─ >512 bytes? → ArrayPool<byte>.Shared.Rent() ✅
├─ Async boundaries?
│  ├─ Temporary? → IMemoryOwner<byte> with MemoryPool ✅
│  └─ Parameter? → Memory<byte> ✅
└─ Must return/store?
   ├─ Caller provides? → Accept Span<byte> or Memory<byte> ✅
   └─ Must allocate? → new byte[] ⚠️ LAST RESORT
```

### Type Selection: readonly struct vs struct vs class

Choose the appropriate type based on size, mutability, and usage patterns.

#### Use `readonly struct` (PREFERRED for value types)

**✅ When:**
- Type is ≤16 bytes (2 machine words)
- Immutable data (no fields change after construction)
- Passed by value frequently
- Used in hot paths (APDU processing, protocol handling)

**✅ Benefits:**
- Prevents defensive copies
- Clear immutability contract
- Potential stack allocation
- No GC pressure
```csharp
// ✅ BEST - Small, immutable, value semantics
public readonly struct StatusWord
{
    public StatusWord(byte sw1, byte sw2) => (SW1, SW2) = (sw1, sw2);
    
    public byte SW1 { get; }
    public byte SW2 { get; }
    public ushort Value => (ushort)((SW1 << 8) | SW2);
    
    public bool IsSuccess => Value == 0x9000;
}

// ✅ GOOD - Wraps small data
public readonly struct SlotNumber
{
    public SlotNumber(byte value) => Value = value;
    public byte Value { get; }
}

// ✅ GOOD - Protocol metadata
public readonly struct ApduHeader
{
    public ApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla;
        INS = ins;
        P1 = p1;
        P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}
```

**❌ Don't use for:**
- Types >16 bytes (expensive copying)
- Mutable data
- Types stored in collections (boxing overhead)

#### Use `struct` (mutable value types)

**⚠️ Use sparingly - only when mutation is truly needed:**
- Temporary computation buffers
- Performance-critical mutable state (rare)
```csharp
// ⚠️ Acceptable - Mutable builder pattern
public struct ApduBuilder
{
    private byte _cla;
    private byte _ins;
    private byte[] _data;
    
    public ApduBuilder WithCommand(byte cla, byte ins)
    {
        _cla = cla;
        _ins = ins;
        return this;
    }
    
    public CommandApdu Build() => new(_cla, _ins, 0, 0, _data);
}
```

**❌ Problems with mutable structs:**
- Defensive copies hurt performance
- Confusing semantics (copy on assign)
- Hard to reason about

**Better alternatives:**
```csharp
// ✅ BETTER - Immutable with builder
public readonly struct CommandApdu
{
    // ... immutable properties
    
    public static Builder CreateBuilder() => new();
    
    public class Builder  // Class builder for mutable state
    {
        private byte _cla;
        private byte _ins;
        
        public Builder WithCommand(byte cla, byte ins)
        {
            _cla = cla;
            _ins = ins;
            return this;
        }
        
        public CommandApdu Build() => new(_cla, _ins, 0, 0);
    }
}
```

#### Use `class` (reference types)

**✅ When:**
- Type is >16 bytes
- Contains reference types (arrays, strings, objects)
- Needs identity semantics (not value semantics)
- Represents entities or services
- Implements interfaces with mutable operations
```csharp
// ✅ GOOD - Large data structure
public class DeviceInfo
{
    public string SerialNumber { get; init; }
    public Version FirmwareVersion { get; init; }
    public FormFactor FormFactor { get; init; }
    public IReadOnlyList<Transport> AvailableTransports { get; init; }
    // ... more properties (>16 bytes total)
}

// ✅ GOOD - Contains managed resources
public sealed class SmartCardConnection : IConnection
{
    private readonly SafeHandle _handle;
    private readonly ILogger _logger;
    
    // ... implementation
}

// ✅ GOOD - Service/manager
public class YubiKeyManager : IYubiKeyManager
{
    private readonly IDeviceRepository _repository;
    private readonly IYubiKeyFactory _factory;
    
    // ... implementation
}
```

#### Special Case: APDU Types

For APDU-related types in this codebase:
```csharp
// ✅ CommandApdu - Use readonly struct if small OR class if contains byte[]
// Option A: Small header-only commands
public readonly struct CommandApduHeader
{
    public CommandApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla; INS = ins; P1 = p1; P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}

// Option B: Full APDU with data - use class (contains byte array)
public sealed class CommandApdu
{
    private readonly ReadOnlyMemory<byte> _data;
    
    public CommandApdu(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }
    
    public ReadOnlySpan<byte> AsSpan() => _data.Span;
}

// ✅ ResponseApdu - Class (contains byte array)
public sealed class ResponseApdu
{
    private readonly ReadOnlyMemory<byte> _data;
    
    public ResponseApdu(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }
    
    public ReadOnlySpan<byte> Data => _data.Span[..^2];
    public StatusWord SW => new(_data.Span[^2], _data.Span[^1]);
}
```

#### Size Guidelines

**16-byte threshold explained:**
```csharp
// ✅ 16 bytes - OK for readonly struct
public readonly struct DeviceId
{
    public Guid Value { get; }  // 16 bytes (128 bits)
}

// ✅ 8 bytes - Excellent for readonly struct
public readonly struct Timestamp
{
    public long Ticks { get; }  // 8 bytes
}

// ❌ 24+ bytes - Use class instead
public struct DeviceInfo  // BAD - too large, expensive to copy
{
    public Guid Id { get; }              // 16 bytes
    public long Timestamp { get; }        // 8 bytes
    public int FirmwareVersion { get; }   // 4 bytes
    // Total: 28 bytes - too large!
}

// ✅ Convert to class
public sealed class DeviceInfo
{
    public Guid Id { get; init; }
    public long Timestamp { get; init; }
    public int FirmwareVersion { get; init; }
}
```

#### Common Mistakes

**❌ BAD: Large struct**
```csharp
public struct ApduCommand  // 32+ bytes!
{
    public byte[] Data { get; set; }  // Reference type in struct!
    public DateTime Timestamp { get; set; }
    public Guid CorrelationId { get; set; }
}
```

**❌ BAD: Mutable struct in collection**
```csharp
var list = new List<MutableStruct>();
list[0].Value = 10;  // Modifies COPY, not original!
```

**✅ GOOD: readonly struct or class**
```csharp
public readonly struct ApduHeader { /* 4 bytes */ }
public sealed class ApduCommand { /* Contains byte[] */ }
```

#### Decision Matrix

| Criterion | readonly struct | struct | class |
|-----------|----------------|--------|-------|
| Size | ≤16 bytes | ≤16 bytes | Any size |
| Mutability | Immutable | Mutable | Either |
| Contains references | No | No | Yes |
| Hot path | ✅ Ideal | ⚠️ Careful | ✅ OK |
| Stack allocation | ✅ Yes | ✅ Yes | ❌ Heap only |
| Defensive copies | ✅ None | ❌ Many | N/A |
| GC pressure | ✅ None | ✅ None | ❌ Yes |

**When in doubt:** Use `class` for correctness, profile if performance critical, then consider `readonly struct` if ≤16 bytes and immutable.

### Anti-Patterns

**❌ NEVER: Unnecessary ToArray()**
```csharp
// ❌ BAD
byte[] data = someSpan.ToArray();
ProcessData(data);

// ✅ GOOD
ProcessData(someSpan);
```

**❌ NEVER: LINQ on byte spans**
```csharp
// ❌ BAD
byte[] result = data.Select(b => (byte)(b ^ 0xFF)).ToArray();

// ✅ GOOD
Span<byte> result = stackalloc byte[data.Length];
for (int i = 0; i < data.Length; i++)
{
    result[i] = (byte)(data[i] ^ 0xFF);
}
```

**❌ NEVER: Forget to return rented arrays**
```csharp
// ❌ BAD - Memory leak
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
ProcessData(buffer);
// Forgot to return!

// ✅ GOOD
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
try
{
    ProcessData(buffer);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### Cryptography APIs

Use modern .NET 8/9/10 Span-based APIs.

```csharp
// ✅ Hashing
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(inputData, hash);

// ✅ HMAC
Span<byte> hmac = stackalloc byte[32];
HMACSHA256.HashData(key, data, hmac);

// ✅ Random
Span<byte> random = stackalloc byte[16];
RandomNumberGenerator.Fill(random);

// ✅ AES
using var aes = Aes.Create();
aes.EncryptCbc(plaintext, iv, ciphertext, PaddingMode.PKCS7);
aes.DecryptCbc(ciphertext, iv, plaintext, PaddingMode.PKCS7);
```

**❌ AVOID legacy APIs:**
```csharp
// ❌ OLD - Allocates
using var sha = SHA256.Create();
byte[] hash = sha.ComputeHash(data);

// ✅ NEW - Zero allocation
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(data, hash);
```

### Sensitive Data Handling

**CRITICAL: YubiKey operations involve PINs, passwords, private keys. All sensitive material must be cleared from memory.**

**✅ Dispose cryptographic objects:**
```csharp
using var aes = Aes.Create();
using var rsa = RSA.Create();
using var hmac = new HMACSHA256(key);
// Keys automatically zeroed on dispose
```

**✅ Zero sensitive buffers:**
```csharp
// ✅ BEST - Span on stack
Span<byte> pin = stackalloc byte[8];
GetPin(pin);
CryptographicOperations.ZeroMemory(pin);

// ✅ GOOD - Rented array
byte[]? keyBuffer = null;
try
{
    keyBuffer = ArrayPool<byte>.Shared.Rent(256);
    Span<byte> key = keyBuffer.AsSpan(0, keyLength);
    // Use key...
}
finally
{
    if (keyBuffer is not null)
    {
        CryptographicOperations.ZeroMemory(keyBuffer);
        ArrayPool<byte>.Shared.Return(keyBuffer, clearArray: false);
    }
}
```

**⚠️ Sensitive data includes:**
- PIN codes
- Password bytes (UTF-8 encoded)
- Private keys
- Session keys
- Challenge-response data
- Decrypted payloads

**❌ NEVER log sensitive data:**
```csharp
// ❌ NEVER
_logger.LogDebug("PIN: {Pin}", pin);
_logger.LogDebug("Key: {Key}", Convert.ToBase64String(privateKey));

// ✅ YES - Log metadata only
_logger.LogDebug("PIN verification for slot {Slot}", slotNumber);
_logger.LogDebug("Key operation completed, length: {Length}", privateKey.Length);
```

### Security Audit Checklist

When implementing or reviewing authentication/cryptographic code, run these verification commands:

```bash
# 1. Sensitive data cleanup - verify ZeroMemory usage
grep -rn "ZeroMemory\|Clear()" src/ | wc -l
# Expected: At least one per sensitive operation (PIN, key, PUK)

# 2. Secret logging audit - ensure no values logged
grep -rn "Log.*\(pin\|key\|puk\|secret\)" -i src/
# Expected: No matches (or only variable names, never values)

# 3. ArrayPool cleanup audit - verify finally blocks
grep -A10 "ArrayPool.*Rent" src/ | grep -c "finally"
# Expected: Every Rent should have corresponding finally block

# 4. Input validation - ensure parameter checks
grep -c "ArgumentNullException\|ArgumentException" src/
# Expected: At least one per public method with parameters
```

Document any violations and fix before claiming security phase complete.

### APDU and Protocol Buffers

**✅ Prefer Span for APDU data:**
```csharp
public readonly struct CommandApdu
{
    private readonly ReadOnlyMemory<byte> _data;
    
    public ReadOnlySpan<byte> AsSpan() => _data.Span;
    
    public CommandApdu(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray(); // Only allocate for storage
    }
}
```

**✅ Use Span slicing:**
```csharp
// ❌ BAD
byte[] header = apduData.Take(5).ToArray();
byte[] body = apduData.Skip(5).ToArray();

// ✅ GOOD
ReadOnlySpan<byte> apdu = apduData.AsSpan();
ReadOnlySpan<byte> header = apdu[..5];
ReadOnlySpan<byte> body = apdu[5..];
```

## Code Style and Language Features

### EditorConfig Compliance

**CRITICAL: All code must follow `.editorconfig` rules.**

Before committing:
1. Ensure IDE respects `.editorconfig`
2. Run `dotnet format` to auto-fix violations
3. Never override rules in individual files

### Modern C# Language Features (C# 8-13)

Use modern patterns - this is a preview language project.

**Null Checking:**
```csharp
// ✅ Pattern matching
if (obj is null) { }
if (obj is not null) { }

// ❌ Avoid
if (obj == null) { }
if (obj != null) { }
```

**Switch Expressions:**
```csharp
// ✅ Modern switch
string status = code switch
{
    0x9000 => "Success",
    0x6300 => "Warning",
    >= 0x6400 and < 0x6500 => "Execution Error",
    0x6982 => "Security Status Not Satisfied",
    _ => "Unknown"
};

// ✅ Property patterns
bool isValid = device switch
{
    { IsConnected: true, FirmwareVersion: >= 5 } => true,
    { IsConnected: false } => false,
    _ => throw new InvalidOperationException()
};

// ✅ Type pattern with declaration
if (connection is SmartCardConnection { IsConnected: true } sc)
{
    await sc.TransmitAsync(apdu);
}
```

**Collection Expressions (C# 12):**
```csharp
// ✅ Modern
int[] numbers = [1, 2, 3, 4, 5];
List<string> combined = [..list1, ..list2, "extra"];
ReadOnlySpan<byte> bytes = [0x00, 0xA4, 0x04, 0x00];

// ❌ Verbose
int[] numbers = new int[] { 1, 2, 3, 4, 5 };
```

**Target-Typed New (C# 9):**
```csharp
// ✅ When type is obvious
CommandApdu apdu = new(cla, ins, p1, p2, data);
Dictionary<string, int> map = new();
```

**Init-Only Properties and Records:**
```csharp
// ✅ Records for immutable DTOs
public record DeviceInfo(
    string SerialNumber,
    Version FirmwareVersion,
    FormFactor FormFactor)
{
    public bool IsLocked { get; init; }
}

// ✅ Init for immutable properties
public class YubiKeyOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public required string ApplicationId { get; init; }
}
```

**File-Scoped Namespaces (C# 10):**
```csharp
// ✅ REQUIRED
namespace Yubico.YubiKit.Core;

public class MyClass { }

// ❌ NEVER use block-scoped
```

**Primary Constructors (C# 12):**
```csharp
// ✅ For simple DI
public class DeviceService(ILogger<DeviceService> logger, IOptions<DeviceOptions> options)
{
    public void Log(string msg) => logger.LogInformation(msg);
}
```

**Range and Index:**
```csharp
// ✅ Modern
ReadOnlySpan<byte> header = apdu[..5];
ReadOnlySpan<byte> body = apdu[5..^2];
byte last = apdu[^1];
```

### What NOT to Do

**❌ NO string concatenation in loops:**
```csharp
// ❌ BAD
string result = "";
foreach (var item in items) result += item;

// ✅ GOOD
var sb = new StringBuilder();
foreach (var item in items) sb.Append(item);
```

**❌ NO nullable warnings suppression without justification:**
```csharp
// ❌ BAD
string value = nullableString!;

// ✅ GOOD
string value = nullableString ?? throw new ArgumentNullException(nameof(nullableString));
```

**❌ NO exceptions for control flow:**
```csharp
// ❌ BAD
try
{
    var device = devices.First(d => d.IsConnected);
}
catch (InvalidOperationException)
{
    device = null;
}

// ✅ GOOD
var device = devices.FirstOrDefault(d => d.IsConnected);
```

**❌ NO public mutable state:**
```csharp
// ❌ BAD
public byte[] Data;

// ✅ GOOD
public ReadOnlyMemory<byte> Data { get; }
public byte[] Data { get; init; }
public byte[] Data { get; private set; }
```

**❌ NO #region:**
```csharp
// ❌ If you need regions, the class is too big - split it
```

**❌ NO var when type isn't obvious:**
```csharp
// ❌ BAD
var result = GetData(); // What type?

// ✅ GOOD - Type obvious
var list = new List<Device>();
var client = new HttpClient();

// ✅ GOOD - Explicit when unclear
DeviceInfo result = GetData();
```

### Additional Guidelines

**✅ Prefer immutable types:**
```csharp
public record ConnectionOptions(TimeSpan Timeout, int RetryCount);

public readonly struct StatusWord
{
    public StatusWord(byte sw1, byte sw2) => (SW1, SW2) = (sw1, sw2);
    public byte SW1 { get; }
    public byte SW2 { get; }
    public ushort Value => (ushort)((SW1 << 8) | SW2);
}
```

**✅ Use readonly:**
```csharp
public class ApduProcessor
{
    private readonly ILogger _logger;
    private readonly IApduFormatter _formatter;
    
    public ApduProcessor(ILogger logger, IApduFormatter formatter)
    {
        _logger = logger;
        _formatter = formatter;
    }
}
```

**✅ Use ValueTask for hot paths:**
```csharp
public ValueTask<IConnection> GetConnectionAsync(CancellationToken ct)
{
    return _cachedConnection is not null
        ? ValueTask.FromResult(_cachedConnection)
        : ConnectSlowPathAsync(ct);
}
```

**✅ Validate external input:**
```csharp
public void SetPin(ReadOnlySpan<byte> pin)
{
    if (pin.Length is < 6 or > 8)
        throw new ArgumentException("PIN must be 6-8 bytes", nameof(pin));
    
    foreach (byte b in pin)
    {
        if (b is < 0x30 or > 0x39)
            throw new ArgumentException("PIN must contain only digits", nameof(pin));
    }
}
```

**✅ Use constant-time comparisons:**
```csharp
// ✅ Prevents timing attacks
bool isValid = CryptographicOperations.FixedTimeEquals(expected, actual);

// ❌ Timing attack vulnerable
bool isValid = expected.SequenceEqual(actual);
```

## Testing

### Integration Test Strategy

**Run only what's affected.** Don't run the full integration suite unless you're finishing a module or touching shared infrastructure.

| Phase | What to run | Command |
|-------|------------|---------|
| **During development** | Smoke test on affected module only | `dotnet build.cs -- test --integration --project Piv --smoke` |
| **Targeted check** | Specific test you touched | `dotnet build.cs -- test --integration --project Oath --filter "FullyQualifiedName~Calculate"` |
| **Finishing a module** | Full integration for that module | `dotnet build.cs -- test --integration --project Piv` |
| **Before PR** | Full integration for all affected modules | Run per-module, not all modules |

**`--smoke` skips:** `Slow` tests (RSA 3072/4096 keygen, 30+ sec each) and `RequiresUserPresence` tests (need physical touch).

**Mark slow tests:** Any integration test that generates RSA 3072+ keys or has delays >5s must have `[Trait(TestCategories.Category, TestCategories.Slow)]`.

### Test Philosophy: Value Over Coverage

**CRITICAL: Only write tests that provide real value. Don't create tests just to increase coverage metrics.**

#### ❌ DON'T Write These Tests

**Validation-Only Tests** - Tests that only check input validation provide minimal value:
```csharp
// ❌ BAD - Only tests ArgumentNullException.ThrowIfNull()
[Fact]
public void Method_WithNullInput_ThrowsArgumentNullException()
{
    var sut = new MyClass();
    Assert.Throws<ArgumentNullException>(() => sut.Process(null));
}

// ❌ BAD - Only tests basic type checking
[Fact]
public void Method_WithWrongType_ThrowsArgumentException()
{
    var sut = new MyClass();
    Assert.Throws<ArgumentException>(() => sut.Process(wrongType));
}
```

**Why these are bad:**
- They test framework behavior, not your code
- They give false sense of security ("95% coverage!")
- They don't catch real bugs
- They add maintenance burden without value

**Skipped Tests** - Don't create tests you know will never run:
```csharp
// ❌ BAD - Creating a test that will never be implemented
[Fact(Skip = "Requires mocking static methods which can't be done")]
public void Method_ActualBehavior_WorksCorrectly()
{
    // This will never run
}
```

**Why this is bad:**
- False advertising - looks like you have tests but they don't run
- Maintenance burden - someone has to read and understand why it's skipped
- If you can't test it, that's valuable information - document it, don't fake it

#### ✅ DO Write These Tests

**Behavior Tests** - Tests that verify actual functionality:
```csharp
// ✅ GOOD - Tests real protocol configuration logic
[Fact]
public void Configure_FirmwareBelow400_UsesNeoMaxSize()
{
    var protocol = new PcscProtocol(logger, connection);

    protocol.Configure(new FirmwareVersion(3, 5, 0));

    Assert.Equal(254, protocol.MaxApduSize);
}
```

**Integration Tests** - When unit testing is impossible/impractical:
```csharp
// ✅ GOOD - Tests actual SCP handshake with real hardware
[Fact]
public async Task CreateSession_WithSCP03_AuthenticatesAndCommunicates()
{
    var device = await YubiKey.FindFirstAsync();
    using var session = await ManagementSession.CreateAsync(
        device, scpKeyParams: scp03Keys);

    var deviceInfo = await session.GetDeviceInfoAsync();
    Assert.NotEqual(0, deviceInfo.SerialNumber);
}
```

#### When You Can't Write Meaningful Tests

If you can't test actual behavior due to:
- Static methods that can't be mocked
- Complex external dependencies
- Architectural limitations

**Then:**
1. **Document the limitation** clearly in code comments
2. **Point to integration tests** that exercise the code path
3. **Don't create fake tests** just to have coverage

**Example:**
```csharp
/// <summary>
///     ScpInitializer routes SCP initialization requests.
///
///     LIMITATION: Cannot unit test actual SCP initialization because
///     ScpState.Scp03InitAsync is a static method. This is tested via
///     integration tests in ManagementTests.CreateManagementSession_with_SCP03_DefaultKeys.
/// </summary>
public class ScpInitializer
{
    // Only test the routing logic, not the static method calls
}
```

#### Red Flags in Test Reviews

🚩 **"This test only validates inputs"** - Remove it or test real behavior
🚩 **"Skip = 'requires mocking X'"** - Either use integration tests or document why it's not testable
🚩 **"Test passes but doesn't verify functionality"** - You're testing the wrong thing
🚩 **"Need to mock 5+ dependencies to test this"** - Architectural problem, not a testing problem
🚩 **"This increases coverage from 85% to 90%"** - Coverage metrics are not the goal

#### Test Value Checklist

Before writing a test, ask:
- ✅ Does this test actual behavior or just input validation?
- ✅ Would this catch a real bug if behavior changed?
- ✅ Can I explain what value this test provides?
- ✅ Would I trust this test to catch regressions?

If you answered "no" to any of these, don't write the test.

### Test Structure

- **UnitTests**: xUnit, no hardware required
- **IntegrationTests**: xUnit, requires physical YubiKey
- **TestProject**: ASP.NET Core with NSubstitute, targets .NET 9 with AOT

### Guidelines

**✅ Test all public APIs:**
```csharp
[Fact]
public async Task ConnectAsync_WhenDeviceAvailable_ReturnsConnection()
{
    // Arrange
    var device = new MockYubiKey { IsConnected = true };

    // Act
    var connection = await device.ConnectAsync<ISmartCardConnection>();

    // Assert
    Assert.NotNull(connection);
    Assert.True(connection.IsConnected);
}
```

**✅ Use descriptive test names:**
```csharp
// ✅ GOOD
[Fact]
public void CommandApdu_WithNullData_ThrowsArgumentNullException()

// ❌ BAD
[Fact]
public void Test1()
```

**✅ Clean up in integration tests:**
```csharp
[Fact]
public async Task IntegrationTest_WithRealDevice()
{
    await using var connection = await _device.ConnectAsync<ISmartCardConnection>();

    try
    {
        var result = await connection.TransmitAsync(apdu);
        Assert.NotNull(result);
    }
    finally
    {
        await ResetDeviceAsync(connection);
    }
}
```

## Git Workflow

- Main development branch: `develop` (not `main`)
- Current working branch: `yubikit`
- Use `develop` as base for pull requests

### Commit Discipline (CRITICAL for Agents)

**Only commit files YOU created or modified in the current session.**

```bash
# Check what's staged first
git status

# Add only YOUR files explicitly - NEVER use git add . or git add -A
git add path/to/your/file.cs

# Commit
git commit -m "feat(scope): description"
```

See `docs/COMMIT_GUIDELINES.md` for detailed rules.

## Pre-Commit Checklist

Before committing:
1. ✅ Ran `git status` to verify only your files are being committed
2. ✅ Code builds without warnings: `dotnet build.cs build`
3. ✅ All tests pass: `dotnet build.cs test`
4. ✅ Code formatted: `dotnet format`
5. ✅ No nullable reference warnings
6. ✅ Sensitive data properly zeroed
7. ✅ No unnecessary allocations in hot paths
8. ✅ Modern C# patterns (is null, switch expressions, etc.)
9. ✅ EditorConfig rules followed
