# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Yubico.NET.SDK (YubiKit) is a .NET SDK for interacting with YubiKey devices. The project targets .NET 8 and .NET 10, uses C# preview language features (LangVersion=preview), and has nullable reference types enabled throughout.

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

### Multi-targeting

Projects target `net8` and `net10.0` with `LangVersion=preview` for:
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

## Pre-Commit Checklist

Before committing:
1. ✅ Code builds without warnings: `dotnet build`
2. ✅ All tests pass: `dotnet test`
3. ✅ Code formatted: `dotnet format`
4. ✅ No nullable reference warnings
5. ✅ Sensitive data properly zeroed
6. ✅ No unnecessary allocations in hot paths
7. ✅ Modern C# patterns (is null, switch expressions, etc.)
8. ✅ EditorConfig rules followed