# HID Layer Refactor Implementation Plan

**Goal:** Simplify the HID connection/protocol architecture by removing unnecessary abstraction layers, fixing async patterns, and improving type safety.

**Architecture:** The current HID stack has three layers: platform-specific sync connections (`IHidConnectionSync`), async wrapper connections (`IOtpHidConnection`, `IFidoHidConnection`), and protocols (`IOtpHidProtocol`, `IFidoHidProtocol`). The middle async wrapper layer adds no value since the underlying operations are inherently synchronous (ioctl calls). This refactor collapses the wrapper layer, has protocols work directly with sync connections, and cleans up obsolete interfaces.

**Tech Stack:** C# 14, .NET 10, xUnit for tests

---

## Current State Analysis

### Layer Diagram (Before)
```
┌─────────────────────────────────────────────────────────────────┐
│ Protocol Layer (async)                                          │
│   OtpHidProtocol : IOtpHidProtocol                              │
│   FidoHidProtocol : IFidoHidProtocol                            │
├─────────────────────────────────────────────────────────────────┤
│ Async Wrapper Layer (unnecessary)                               │
│   OtpHidConnection : IOtpHidConnection                          │
│   FidoHidConnection : IFidoHidConnection                        │
│   HidConnection : IHidConnection (legacy)                       │
├─────────────────────────────────────────────────────────────────┤
│ Platform Layer (sync)                                           │
│   LinuxHidFeatureReportConnection : IHidConnectionSync          │
│   LinuxHidIOReportConnection : IHidConnectionSync               │
│   MacOS equivalents...                                          │
└─────────────────────────────────────────────────────────────────┘
```

### Issues Identified

1. **Async-over-sync anti-pattern**: `OtpHidConnection.ReceiveAsync()` just calls `Task.FromResult(syncConnection.GetReport())` - fake async
2. **Redundant wrapper classes**: `OtpHidConnection`, `FidoHidConnection`, `HidConnection` do identical wrapping
3. **Generic factory defeats type safety**: `OtpProtocolFactory<TConnection>` does runtime `is not IOtpHidConnection` check
4. **Obsolete properties still present**: `IHidDevice.VendorId`, `.ProductId`, `.Usage`, `.UsagePage` marked obsolete but still implemented
5. **Legacy interface not needed**: `IHidConnection` only used by `HidYubiKey.CreateLegacyHidConnection()`
6. **Fake async methods**: `HidYubiKey.CreateOtpConnection()` has `await Task.CompletedTask` that does nothing
7. **Sync-over-async in NEO quirk**: `EnsureInitialized()` calls `.GetAwaiter().GetResult()`

---

## Refactor Strategy

### What We're NOT Changing
- `OtpHidProtocol` internal logic (timing, polling, frame handling) - this works correctly
- Platform-specific connections (`LinuxHidFeatureReportConnection`, etc.)
- `IHidDevice` core methods (`ConnectToFeatureReports()`, `ConnectToIOReports()`)
- Public API surface used by `ManagementSession`

### What We're Changing
- Remove async wrapper layer entirely
- Protocols work directly with `IHidConnectionSync`
- Simplify factories to non-generic
- Clean up obsolete properties
- Fix async patterns in `HidYubiKey`

### Target Architecture (After)
```
┌─────────────────────────────────────────────────────────────────┐
│ Protocol Layer                                                  │
│   OtpHidProtocol(IHidConnectionSync, reportSize: 8)             │
│   FidoHidProtocol(IHidConnectionSync, packetSize: 64)           │
├─────────────────────────────────────────────────────────────────┤
│ Platform Layer (sync - inherently blocking I/O)                 │
│   LinuxHidFeatureReportConnection : IHidConnectionSync          │
│   LinuxHidIOReportConnection : IHidConnectionSync               │
│   MacOS equivalents...                                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Task 1: Add Unit Tests for Current Behavior (Safety Net)

**Files:**
- Create: `Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/Otp/OtpHidProtocolTests.cs`

**Why:** Before refactoring, we need tests that verify current behavior. These will catch regressions.

**Step 1: Create test file with mock connection**

```csharp
// Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/Otp/OtpHidProtocolTests.cs
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.UnitTests.Hid.Otp;

public class OtpHidProtocolTests
{
    /// <summary>
    /// Mock sync connection for testing protocol logic without hardware.
    /// </summary>
    private class MockHidConnectionSync : IHidConnectionSync
    {
        private readonly Queue<byte[]> _reportsToReturn = new();
        private readonly List<byte[]> _reportsSent = new();
        
        public int InputReportSize => 8;
        public int OutputReportSize => 8;
        public ConnectionType Type => ConnectionType.Hid;
        
        public void QueueReport(byte[] report) => _reportsToReturn.Enqueue(report);
        public IReadOnlyList<byte[]> SentReports => _reportsSent;
        
        public byte[] GetReport()
        {
            if (_reportsToReturn.Count == 0)
                throw new InvalidOperationException("No reports queued");
            return _reportsToReturn.Dequeue();
        }
        
        public void SetReport(byte[] report)
        {
            _reportsSent.Add(report.ToArray());
        }
        
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OtpHidProtocol(null!));
    }

    [Fact]
    public async Task SendAndReceiveAsync_PayloadTooLarge_ThrowsArgumentException()
    {
        var mock = new MockHidConnectionSync();
        // Queue initial status report for initialization
        mock.QueueReport(new byte[] { 0x00, 0x05, 0x04, 0x03, 0x00, 0x00, 0x00, 0x00 });
        
        var protocol = new OtpHidProtocol(new OtpHidConnection(mock));

        var oversizedPayload = new byte[65]; // Max is 64
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => protocol.SendAndReceiveAsync(0x13, oversizedPayload));
    }
}
```

**Step 2: Run test to verify setup**

```bash
dotnet test Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj --filter "FullyQualifiedName~OtpHidProtocolTests"
```

Expected: Tests pass (or we discover current behavior to document)

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/Otp/OtpHidProtocolTests.cs
git commit -m "test: add OtpHidProtocol unit tests as refactor safety net"
```

---

## Task 2: Extract Protocol Report Sizes to Constants

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpConstants.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpHidConnection.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/Fido/FidoHidConnection.cs`

**Why:** Report sizes (8 for OTP, 64 for FIDO) are magic numbers in multiple places.

**Step 1: Add constant to OtpConstants**

In `OtpConstants.cs`, verify `FeatureReportSize = 8` exists (it does). Add comment if missing.

**Step 2: Create FidoConstants file**

```csharp
// Yubico.YubiKit.Core/src/Hid/Fido/FidoConstants.cs
namespace Yubico.YubiKit.Core.Hid.Fido;

/// <summary>
/// Constants for FIDO/CTAP HID protocol.
/// </summary>
internal static class FidoConstants
{
    /// <summary>
    /// FIDO HID packet size (64 bytes per spec).
    /// </summary>
    public const int PacketSize = 64;
}
```

**Step 3: Update FidoHidConnection to use constant**

```csharp
// In FidoHidConnection.cs, change:
public int PacketSize => 64;
// To:
public int PacketSize => FidoConstants.PacketSize;
```

**Step 4: Run tests**

```bash
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "Hid"
```

Expected: PASS

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/Fido/FidoConstants.cs Yubico.YubiKit.Core/src/Hid/Fido/FidoHidConnection.cs
git commit -m "refactor: extract FIDO packet size to FidoConstants"
```

---

## Task 3: Refactor OtpHidProtocol to Accept IHidConnectionSync Directly

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpHidProtocol.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/HidYubiKey.cs`
- Delete: `Yubico.YubiKit.Core/src/Hid/Otp/OtpHidConnection.cs`
- Delete: `Yubico.YubiKit.Core/src/Hid/Interfaces/IOtpHidConnection.cs`

**Why:** `OtpHidConnection` is just a pass-through wrapper. The protocol can work directly with the sync connection.

**Step 1: Modify OtpHidProtocol constructor**

```csharp
// In OtpHidProtocol.cs
// Change:
internal sealed class OtpHidProtocol : IOtpHidProtocol
{
    private readonly IOtpHidConnection _connection;
    
    public OtpHidProtocol(IOtpHidConnection connection, ILogger<OtpHidProtocol>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        // ...
    }
// To:
internal sealed class OtpHidProtocol : IOtpHidProtocol
{
    private readonly IHidConnectionSync _connection;
    
    public OtpHidProtocol(IHidConnectionSync connection, ILogger<OtpHidProtocol>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        // ...
    }
```

**Step 2: Update internal methods**

Replace async wrapper calls with direct sync calls wrapped in Task:

```csharp
// Change:
private async Task<ReadOnlyMemory<byte>> ReadFeatureReportAsync(CancellationToken cancellationToken)
{
    var report = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
    _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report.Span));
    return report;
}

// To:
private Task<ReadOnlyMemory<byte>> ReadFeatureReportAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    var report = _connection.GetReport();
    _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report));
    return Task.FromResult<ReadOnlyMemory<byte>>(report);
}

// Change:
private async Task WriteFeatureReportAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
{
    _logger.LogTrace("Write feature report: {Report}", Convert.ToHexString(buffer.Span));
    await _connection.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
}

// To:
private Task WriteFeatureReportAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    _logger.LogTrace("Write feature report: {Report}", Convert.ToHexString(buffer.Span));
    _connection.SetReport(buffer.ToArray());
    return Task.CompletedTask;
}
```

**Step 3: Update ReadFeatureReport (sync version)**

```csharp
// Change:
private byte[] ReadFeatureReport()
{
    var report = _connection.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
    _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report.Span));
    return report.ToArray();
}

// To:
private byte[] ReadFeatureReport()
{
    var report = _connection.GetReport();
    _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report));
    return report;
}
```

**Step 4: Update HidYubiKey.CreateOtpConnection**

```csharp
// In HidYubiKey.cs, change CreateOtpConnection to return the protocol directly,
// or update ConnectAsync to create protocol directly:

// Change in ConnectAsync:
if (typeof(TConnection) == typeof(IOtpHidConnection))
{
    var connection = await CreateOtpConnection(cancellationToken).ConfigureAwait(false);
    return connection as TConnection ??
           throw new InvalidOperationException("Connection is not of the expected type.");
}

// We need to think about this - the issue is TConnection is the connection type,
// but we're removing the connection wrapper...
// 
// Option A: Keep IOtpHidConnection as an interface but have OtpHidProtocol implement it
// Option B: Change callers to request IOtpHidProtocol instead
// Option C: Create a minimal adapter that satisfies IOtpHidConnection
```

**PAUSE - Design Decision Required**

The current public API exposes `IOtpHidConnection` as a connection type that callers can request via `ConnectAsync<IOtpHidConnection>()`. We need to decide:

**Option A: Protocol IS the Connection**
- `OtpHidProtocol` implements `IOtpHidConnection` 
- Callers get a protocol when they request a connection
- Pro: Minimal API change
- Con: Conflates protocol and connection concepts

**Option B: Keep Thin Wrapper**
- Keep `OtpHidConnection` but make it just hold the sync connection
- Protocol instantiated separately
- Pro: Clean separation
- Con: Keeps the wrapper we wanted to remove

**Option C: Change Public API**
- Callers request `IOtpHidProtocol` instead of `IOtpHidConnection`
- Remove `IOtpHidConnection` from public API
- Pro: Most honest design
- Con: Breaking change

**Recommendation: Option A** - Have `OtpHidProtocol` implement `IOtpHidConnection`. The "connection" is really the protocol-level abstraction anyway.

**Step 5: Make OtpHidProtocol implement IOtpHidConnection**

```csharp
// Modify IOtpHidConnection.cs to be minimal:
public interface IOtpHidConnection : IConnection
{
    Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(byte slot, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken = default);
    FirmwareVersion? FirmwareVersion { get; }
}

// Then OtpHidProtocol already has these methods!
internal sealed class OtpHidProtocol : IOtpHidProtocol, IOtpHidConnection
{
    // ... existing implementation
    
    // Add IConnection members:
    public ConnectionType Type => ConnectionType.HidOtp;
}
```

Wait, this is getting complicated. Let me reconsider...

**Revised Approach: Keep Layering, Fix Only the Anti-patterns**

Actually, the layering isn't wrong - it's the fake async that's wrong. Let's take a more conservative approach:

1. Keep `IOtpHidConnection` as the protocol-level connection interface
2. Keep `OtpHidConnection` but acknowledge it's a legitimate adapter
3. Fix the fake async patterns where they cause real problems

---

## Task 3 (Revised): Clean Up Async Patterns

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/HidYubiKey.cs`

**Why:** Remove fake `await Task.CompletedTask` statements that add no value.

**Step 1: Remove fake async from HidYubiKey**

```csharp
// In HidYubiKey.cs, change:
private async Task<IFidoHidConnection> CreateFidoConnection(CancellationToken cancellationToken = default)
{
    await Task.CompletedTask; // Make async
    // ...
}

// To:
private IFidoHidConnection CreateFidoConnection()
{
    // ...
}

// And update ConnectAsync to not await:
if (typeof(TConnection) == typeof(IFidoHidConnection))
{
    var connection = CreateFidoConnection();
    return connection as TConnection ??
           throw new InvalidOperationException("Connection is not of the expected type.");
}
```

**Step 2: Apply same change to CreateOtpConnection and CreateLegacyHidConnection**

```csharp
private IOtpHidConnection CreateOtpConnection()
{
    if (hidDevice.InterfaceType != YubiKeyHidInterfaceType.Otp)
    {
        throw new NotSupportedException(
            $"OTP connection requires OTP/Keyboard HID interface (UsagePage=0x0001, Usage=0x06), " +
            $"found {hidDevice.InterfaceType} (UsagePage=0x{hidDevice.DescriptorInfo.UsagePage:X4}, Usage=0x{hidDevice.DescriptorInfo.Usage:X4})");
    }

    logger.LogInformation(
        "Connecting to OTP/Keyboard HID interface VID={VendorId:X4} PID={ProductId:X4}",
        hidDevice.DescriptorInfo.VendorId,
        hidDevice.DescriptorInfo.ProductId);

    var syncConnection = hidDevice.ConnectToFeatureReports();
    return new OtpHidConnection(syncConnection);
}

private IHidConnection CreateLegacyHidConnection()
{
    logger.LogInformation(
        "Connecting to HID YubiKey VID={VendorId:X4} PID={ProductId:X4} Usage={Usage:X4} InterfaceType={InterfaceType}",
        hidDevice.DescriptorInfo.VendorId,
        hidDevice.DescriptorInfo.ProductId,
        hidDevice.DescriptorInfo.Usage,
        hidDevice.InterfaceType);

    var reportType = HidInterfaceClassifier.GetReportType(hidDevice.InterfaceType);
    
    var syncConnection = reportType switch
    {
        HidReportType.InputOutput => hidDevice.ConnectToIOReports(),
        HidReportType.Feature => hidDevice.ConnectToFeatureReports(),
        _ => throw new NotSupportedException($"HID interface type {hidDevice.InterfaceType} is not supported.")
    };

    return new HidConnection(syncConnection);
}
```

**Step 3: Update ConnectAsync to use synchronous helpers**

```csharp
public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
    where TConnection : class, IConnection
{
    if (typeof(TConnection) == typeof(IFidoHidConnection))
    {
        var connection = CreateFidoConnection();
        return Task.FromResult(connection as TConnection ??
               throw new InvalidOperationException("Connection is not of the expected type."));
    }

    if (typeof(TConnection) == typeof(IOtpHidConnection))
    {
        var connection = CreateOtpConnection();
        return Task.FromResult(connection as TConnection ??
               throw new InvalidOperationException("Connection is not of the expected type."));
    }

    if (typeof(TConnection) == typeof(IHidConnection))
    {
        var connection = CreateLegacyHidConnection();
        return Task.FromResult(connection as TConnection ??
               throw new InvalidOperationException("Connection is not of the expected type."));
    }

    throw new NotSupportedException(
        $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");
}
```

**Step 4: Run integration tests**

```bash
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "CreateManagementSession_with_HidOtp_CreateAsync"
```

Expected: PASS

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidYubiKey.cs
git commit -m "refactor: remove fake async from HidYubiKey connection methods"
```

---

## Task 4: Remove Obsolete Properties from IHidDevice

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/Interfaces/IHidDevice.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/Linux/LinuxHidDevice.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/MacOS/MacOSHidDevice.cs` (if exists)

**Why:** Obsolete properties (`VendorId`, `ProductId`, `Usage`, `UsagePage`) should be removed since `DescriptorInfo` provides all this data.

**Step 1: Search for usages of obsolete properties**

```bash
grep -rn "\.VendorId\|\.ProductId\|\.Usage\|\.UsagePage" Yubico.YubiKit.Core/src/Hid/
```

**Step 2: Update any callers to use DescriptorInfo**

Any code like `device.VendorId` should become `device.DescriptorInfo.VendorId`.

**Step 3: Remove obsolete properties from interface**

```csharp
// In IHidDevice.cs, remove:
[Obsolete("Use DescriptorInfo.VendorId instead")]
short VendorId { get; }

[Obsolete("Use DescriptorInfo.ProductId instead")]
short ProductId { get; }

[Obsolete("Use DescriptorInfo.Usage instead")]
short Usage { get; }

[Obsolete("Use InterfaceType instead...")]
HidUsagePage UsagePage { get; }
```

**Step 4: Remove implementations from platform classes**

Remove the corresponding properties from `LinuxHidDevice.cs` and any other platform implementations.

**Step 5: Build and verify**

```bash
dotnet build Yubico.YubiKit.sln
```

Expected: Build succeeds (or shows us what else needs updating)

**Step 6: Run integration tests**

```bash
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "Hid"
```

Expected: PASS

**Step 7: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/
git commit -m "refactor: remove obsolete VendorId/ProductId/Usage/UsagePage from IHidDevice"
```

---

## Task 5: Simplify OtpProtocolFactory

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpProtocolFactory.cs`

**Why:** The generic `<TConnection>` parameter is meaningless when the factory does a runtime type check anyway.

**Step 1: Change factory to non-generic**

```csharp
// Change from:
public class OtpProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    where TConnection : IConnection
{
    public IOtpHidProtocol Create(TConnection connection)
    {
        if (connection is not IOtpHidConnection otpConnection)
            throw new NotSupportedException(...);
        return new OtpHidProtocol(otpConnection, ...);
    }
}

// To:
public class OtpProtocolFactory(ILoggerFactory loggerFactory)
{
    public IOtpHidProtocol Create(IOtpHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new OtpHidProtocol(connection, loggerFactory.CreateLogger<OtpHidProtocol>());
    }

    public static OtpProtocolFactory Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
```

**Step 2: Update any callers**

Search for `OtpProtocolFactory<` and update to `OtpProtocolFactory`.

**Step 3: Apply same change to FidoProtocolFactory**

```csharp
public class FidoProtocolFactory(ILoggerFactory loggerFactory)
{
    public IFidoHidProtocol Create(IFidoHidConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new FidoHidProtocol(connection, loggerFactory.CreateLogger<FidoHidProtocol>());
    }

    public static FidoProtocolFactory Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}
```

**Step 4: Build and run tests**

```bash
dotnet build Yubico.YubiKit.sln
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "Hid"
```

Expected: PASS

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/Otp/OtpProtocolFactory.cs Yubico.YubiKit.Core/src/Hid/Fido/FidoProtocolFactory.cs
git commit -m "refactor: remove unnecessary generic parameter from protocol factories"
```

---

## Task 6: Remove Legacy IHidConnection Interface

**Files:**
- Delete: `Yubico.YubiKit.Core/src/Hid/Interfaces/IHidConnection.cs`
- Delete: `Yubico.YubiKit.Core/src/Hid/HidConnection.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/HidYubiKey.cs`

**Why:** `IHidConnection` is only used for "legacy support" and adds no value.

**Step 1: Search for usages**

```bash
grep -rn "IHidConnection" Yubico.YubiKit.Core/
```

**Step 2: Remove legacy connection support from HidYubiKey**

```csharp
// In HidYubiKey.ConnectAsync, remove:
if (typeof(TConnection) == typeof(IHidConnection))
{
    var connection = CreateLegacyHidConnection();
    return Task.FromResult(connection as TConnection ??
           throw new InvalidOperationException("Connection is not of the expected type."));
}

// And remove the method:
private IHidConnection CreateLegacyHidConnection() { ... }
```

**Step 3: Delete the files**

```bash
rm Yubico.YubiKit.Core/src/Hid/Interfaces/IHidConnection.cs
rm Yubico.YubiKit.Core/src/Hid/HidConnection.cs
```

**Step 4: Build and run tests**

```bash
dotnet build Yubico.YubiKit.sln
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "Hid"
```

Expected: Build succeeds, tests pass (or we find callers that need updating)

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor: remove unused legacy IHidConnection interface"
```

---

## Task 7: Add XML Documentation

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpHidProtocol.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/Otp/OtpHidConnection.cs`
- Modify: `Yubico.YubiKit.Core/src/Hid/HidYubiKey.cs`

**Why:** After refactoring, ensure public/internal APIs have clear documentation explaining the architecture.

**Step 1: Document OtpHidProtocol class**

```csharp
/// <summary>
/// Implements OTP HID protocol for communication with YubiKey OTP/Keyboard interface.
/// </summary>
/// <remarks>
/// <para>
/// The OTP HID protocol uses 8-byte feature reports to communicate with the YubiKey.
/// Data is sent as 70-byte frames split across multiple reports, with CRC validation.
/// </para>
/// <para>
/// Key timing behavior:
/// <list type="bullet">
/// <item>Write operations use "sleep-first" pattern (50ms delay before checking write flag)</item>
/// <item>Read operations use tight polling (write-side delays provide sufficient processing time)</item>
/// </list>
/// </para>
/// <para>
/// Based on the Java yubikit-android OtpProtocol implementation.
/// </para>
/// </remarks>
internal sealed class OtpHidProtocol : IOtpHidProtocol
```

**Step 2: Document OtpHidConnection class**

```csharp
/// <summary>
/// Adapts a synchronous HID feature report connection for async OTP protocol use.
/// </summary>
/// <remarks>
/// <para>
/// This adapter wraps <see cref="IHidConnectionSync"/> to provide the async interface
/// expected by <see cref="OtpHidProtocol"/>. The underlying operations are synchronous
/// (OS-level ioctl calls), so the async methods complete synchronously via Task wrappers.
/// </para>
/// <para>
/// OTP communication uses 8-byte feature reports (HID Report ID 0).
/// </para>
/// </remarks>
internal class OtpHidConnection(IHidConnectionSync syncConnection) : IOtpHidConnection
```

**Step 3: Document HidYubiKey connection methods**

Add XML docs explaining the connection type selection logic.

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/
git commit -m "docs: add XML documentation to HID layer classes"
```

---

## Task 8: Final Verification

**Step 1: Run all HID integration tests**

```bash
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "Hid"
```

Expected: All tests pass

**Step 2: Run the specific test that was failing before**

```bash
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj --filter "CreateManagementSession_with_HidOtp_CreateAsync"
```

Expected: PASS

**Step 3: Build entire solution**

```bash
dotnet build Yubico.YubiKit.sln
```

Expected: No errors, minimal warnings

**Step 4: Final commit**

```bash
git add -A
git commit -m "refactor: complete HID layer cleanup"
```

---

## Summary of Changes

| Change | Impact | Risk |
|--------|--------|------|
| Remove fake async in HidYubiKey | Low | Low - cosmetic only |
| Extract FidoConstants | Low | None - additive |
| Simplify protocol factories | Medium | Low - internal API |
| Remove obsolete IHidDevice properties | Medium | Medium - check callers |
| Remove IHidConnection | Medium | Medium - check callers |
| Add documentation | Low | None |

## What We Decided NOT to Change

1. **Keep the async wrapper pattern** - While `OtpHidConnection` does wrap sync in async, this is a legitimate adapter pattern for the protocol layer
2. **Keep IOtpHidConnection interface** - It's a valid abstraction at the protocol level
3. **Keep protocol logic unchanged** - The timing fixes work, don't touch them

## Testing Checklist

- [ ] `CreateManagementSession_with_HidOtp_CreateAsync` passes
- [ ] All `--filter "Hid"` tests pass
- [ ] Solution builds without errors
- [ ] No new warnings introduced
