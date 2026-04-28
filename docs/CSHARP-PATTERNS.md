---
name: CSharpPatterns
description: Modern C# language patterns and property/init conventions for the YubiKit SDK (C# 14, .NET 10, nullable enabled). READ WHEN designing new types, choosing property accessors (init/private set), writing switch expressions, using collection expressions, primary constructors, file-scoped namespaces, records, ValueTask, parameter validation, FixedTimeEquals comparisons. Cross-references docs/MEMORY-MANAGEMENT.md (Type Selection lives in CLAUDE.md verbatim).
---

# C# Patterns

This is the JIT reference for C# style and idiom decisions. The Quick Reference in `CLAUDE.md` lists the mandates; this doc has the patterns and examples.

> Note: **Type Selection (readonly struct vs struct vs class)** lives in `CLAUDE.md` verbatim, not here. It's load-on-startup because the 16-byte / sensitive-payload rules are project-critical.

## Property Conventions

### Immutability Preference

- ✅ `{ get; init; }` — immutable, set only at construction
- ✅ `{ get; private set; }` — modified only within the class
- ⚠️ `{ get; set; }` — sparingly, only for configuration/mutable DTOs

### Initialization

- Validate in constructor or via dedicated `Validate()` method
- `ArgumentNullException.ThrowIfNull()` for required parameters
- `ArgumentOutOfRangeException.ThrowIfNegative()` for numeric constraints

### Computed Properties

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

## Modern C# Language Features

This is a preview-language project (C# 14, `LangVersion=14.0`). Use modern patterns.

### Null Checking

```csharp
// ✅ Pattern matching
if (obj is null) { }
if (obj is not null) { }

// ❌ Avoid
if (obj == null) { }
if (obj != null) { }
```

### Switch Expressions

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
    await sc.TransmitAsync(apdu);
```

### Collection Expressions (C# 12)

```csharp
// ✅ Modern
int[] numbers = [1, 2, 3, 4, 5];
List<string> combined = [..list1, ..list2, "extra"];
ReadOnlySpan<byte> bytes = [0x00, 0xA4, 0x04, 0x00];

// ❌ Verbose
int[] numbers = new int[] { 1, 2, 3, 4, 5 };
```

### Target-Typed New (C# 9)

```csharp
// ✅ When type is obvious
CommandApdu apdu = new(cla, ins, p1, p2, data);
Dictionary<string, int> map = new();
```

### Init-Only Properties and Records

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

### File-Scoped Namespaces (C# 10)

```csharp
// ✅ REQUIRED
namespace Yubico.YubiKit.Core;

public class MyClass { }
```

Block-scoped namespaces are NEVER used in this codebase.

### Primary Constructors (C# 12)

```csharp
// ✅ For simple DI
public class DeviceService(ILogger<DeviceService> logger, IOptions<DeviceOptions> options)
{
    public void Log(string msg) => logger.LogInformation(msg);
}
```

### Range and Index

```csharp
ReadOnlySpan<byte> header = apdu[..5];
ReadOnlySpan<byte> body = apdu[5..^2];
byte last = apdu[^1];
```

## What NOT to Do (Examples)

The mandates live in `CLAUDE.md`. The reasoning behind each:

### ❌ String concatenation in loops

```csharp
// ❌ BAD
string result = "";
foreach (var item in items) result += item;

// ✅ GOOD
var sb = new StringBuilder();
foreach (var item in items) sb.Append(item);
```

### ❌ Suppressing nullable warnings without justification

```csharp
// ❌ BAD
string value = nullableString!;

// ✅ GOOD
string value = nullableString ?? throw new ArgumentNullException(nameof(nullableString));
```

### ❌ Exceptions for control flow

```csharp
// ❌ BAD
try { var device = devices.First(d => d.IsConnected); }
catch (InvalidOperationException) { device = null; }

// ✅ GOOD
var device = devices.FirstOrDefault(d => d.IsConnected);
```

### ❌ Public mutable state

```csharp
// ❌ BAD
public byte[] Data;

// ✅ GOOD
public ReadOnlyMemory<byte> Data { get; }
public byte[] Data { get; init; }
public byte[] Data { get; private set; }
```

### ❌ `var` when type isn't obvious

```csharp
// ❌ BAD - what type?
var result = GetData();

// ✅ GOOD - type obvious
var list = new List<Device>();
var client = new HttpClient();

// ✅ GOOD - explicit when unclear
DeviceInfo result = GetData();
```

## Additional Patterns

### Prefer Immutable Types

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

### Use `readonly` Fields

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

### `ValueTask` for Hot Paths

```csharp
public ValueTask<IConnection> GetConnectionAsync(CancellationToken ct)
    => _cachedConnection is not null
        ? ValueTask.FromResult(_cachedConnection)
        : ConnectSlowPathAsync(ct);
```

### Validate External Input

```csharp
public void SetPin(ReadOnlySpan<byte> pin)
{
    if (pin.Length is < 6 or > 8)
        throw new ArgumentException("PIN must be 6-8 bytes", nameof(pin));

    foreach (byte b in pin)
        if (b is < 0x30 or > 0x39)
            throw new ArgumentException("PIN must contain only digits", nameof(pin));
}
```

### Constant-Time Comparisons

See `docs/CRYPTO-APIS.md` for the `FixedTimeEquals` pattern. Required for any secret-derived byte comparison.
