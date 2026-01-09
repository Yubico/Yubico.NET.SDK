# HID Protocol Implementation Plan

**Goal:** Implement CTAP HID protocol to enable sending commands to YubiKey over HID connections, allowing ManagementSession to work with HID devices.

**Architecture:** Create `HidProtocol` and `HidProtocolFactory` mirroring the existing `PcscProtocol` pattern. The HID protocol implements CTAP HID framing (64-byte packets with init/continuation structure) to send APDUs over HID connections. Channel initialization via CTAPHID_INIT establishes a unique channel ID for multiplexed communication.

**Tech Stack:** C# 14, .NET 8+, HID native interop (IOKit/HID.dll), xUnit for testing

**Build & Test:** This project uses `build.cs` for all build and test operations:
- Build: `dotnet run --project build.cs build`
- Test: `dotnet run --project build.cs test`
- Test specific project: `dotnet run --project build.cs test --project Management.IntegrationTests`
- Test with filter: `dotnet run --project build.cs test --project UnitTests --filter "FullyQualifiedName~MyTest"`

See `BUILD.md` for full build.cs documentation.

---

## Current State

**What Exists:**
- ✅ HID device enumeration (`HidYubiKey`, `MacOSHidDevice`, etc.)
- ✅ Low-level HID connections (`IHidConnection`, `IHidConnectionSync`)
- ✅ Platform-specific HID implementations (macOS/Windows)
- ✅ ManagementSession with HID support placeholder (line 65-67)
- ✅ SmartCard protocol as reference pattern

**What's Missing:**
- ❌ `IHidProtocol` interface
- ❌ `HidProtocol` implementing CTAP HID framing
- ❌ `HidProtocolFactory` for creating protocol instances
- ❌ CTAP constants and utilities

**Current Build Error:**
```
ManagementSession.cs(65,35): error CS0103: The name 'HidProtocolFactory' does not exist
```

---

## Task 1: Create CTAP Constants

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/CtapConstants.cs`

**Step 1: Create constants file**

Create the CTAP HID protocol constants based on FIDO CTAP specification.

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// CTAP HID protocol constants as defined in the FIDO CTAP specification.
/// </summary>
internal static class CtapConstants
{
    // CTAP HID Commands
    public const byte CtapHidMsg = 0x03;        // CTAP1/U2F raw message
    public const byte CtapHidCbor = 0x10;       // CTAP2 CBOR encoded message
    public const byte CtapHidInit = 0x06;       // Initialize channel
    public const byte CtapHidPing = 0x01;       // Echo data through local processing
    public const byte CtapHidCancel = 0x11;     // Cancel outstanding request
    public const byte CtapHidError = 0x3F;      // Error response
    public const byte CtapHidKeepAlive = 0x3B;  // Processing status notification

    // Packet Structure
    public const int PacketSize = 64;
    public const int MaxPayloadSize = 7609;     // 64 - 7 + 128 * (64 - 5)
    
    public const int InitHeaderSize = 7;
    public const int InitDataSize = PacketSize - InitHeaderSize;  // 57 bytes
    
    public const int ContinuationHeaderSize = 5;
    public const int ContinuationDataSize = PacketSize - ContinuationHeaderSize;  // 59 bytes

    // Channel Management
    public const uint BroadcastChannelId = 0xFFFFFFFF;
    public const int NonceSize = 8;
    
    // Bit masks
    public const byte InitPacketMask = 0x80;    // Bit 7 set for init packets
}
```

**Step 2: Build to verify syntax**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/CtapConstants.cs
git commit -m "feat: add CTAP HID protocol constants"
```

---

## Task 2: Create IHidProtocol Interface

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/IHidProtocol.cs`
- Reference: `Yubico.YubiKit.Core/src/SmartCard/PcscProtocol.cs` (lines 21-37)

**Step 1: Create protocol interface**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Protocol interface for HID communication using CTAP HID framing.
/// </summary>
public interface IHidProtocol : IProtocol
{
    /// <summary>
    /// Transmits an APDU command over HID and receives the response.
    /// </summary>
    /// <param name="command">The APDU command to transmit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets whether the HID channel has been initialized.
    /// </summary>
    bool IsChannelInitialized { get; }
}
```

**Step 2: Build to verify**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/IHidProtocol.cs
git commit -m "feat: add IHidProtocol interface"
```

---

## Task 3: Create HidProtocol Implementation (Part 1 - Structure and Init)

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/HidProtocol.cs`
- Reference: `legacy-develop/Yubico.YubiKey/src/Yubico/YubiKey/Pipelines/FidoTransform.cs`

**Step 1: Create class structure and channel initialization**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Implements CTAP HID protocol for communication with YubiKey over HID.
/// Based on FIDO CTAP HID Protocol Specification.
/// </summary>
internal class HidProtocol : IHidProtocol
{
    private readonly IHidConnection _connection;
    private readonly ILogger<HidProtocol> _logger;
    private uint? _channelId;
    private bool _disposed;

    public bool IsChannelInitialized => _channelId.HasValue;

    public HidProtocol(IHidConnection connection, ILogger<HidProtocol>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? NullLogger<HidProtocol>.Instance;
    }

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
    {
        // Initialize CTAP HID channel
        AcquireCtapHidChannel();
        _logger.LogDebug("HID protocol configured for firmware version {Version}", version);
    }

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!IsChannelInitialized)
            throw new InvalidOperationException("HID channel not initialized. Call Configure() first.");

        // TODO: Implement in next step
        throw new NotImplementedException();
    }

    /// <summary>
    /// Acquires a CTAP HID channel by sending CTAPHID_INIT to the broadcast channel.
    /// </summary>
    private void AcquireCtapHidChannel()
    {
        _logger.LogDebug("Acquiring CTAP HID channel");

        // Generate 8-byte random nonce
        Span<byte> nonce = stackalloc byte[CtapConstants.NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Send CTAPHID_INIT to broadcast channel
        var response = TransmitCommand(
            CtapConstants.BroadcastChannelId,
            CtapConstants.CtapHidInit,
            nonce);

        // Verify nonce echo
        if (response.Length < 12)
            throw new InvalidOperationException("CTAPHID_INIT response too short");

        var receivedNonce = response.Span[..CtapConstants.NonceSize];
        if (!nonce.SequenceEqual(receivedNonce))
            throw new InvalidOperationException("CTAPHID_INIT nonce mismatch");

        // Extract channel ID (bytes 8-11, big-endian)
        _channelId = BinaryPrimitives.ReadUInt32BigEndian(response.Span[8..12]);
        
        _logger.LogDebug("Acquired CTAP HID channel: 0x{ChannelId:X8}", _channelId.Value);
    }

    /// <summary>
    /// Transmits a CTAP HID command and receives the response.
    /// </summary>
    private ReadOnlyMemory<byte> TransmitCommand(uint channelId, byte command, ReadOnlySpan<byte> data)
    {
        SendRequest(channelId, command, data);
        return ReceiveResponse(channelId);
    }

    /// <summary>
    /// Sends a CTAP HID request with proper packet framing.
    /// </summary>
    private void SendRequest(uint channelId, byte command, ReadOnlySpan<byte> data)
    {
        // TODO: Implement in next step
        throw new NotImplementedException();
    }

    /// <summary>
    /// Receives a CTAP HID response, handling keep-alive and multi-packet responses.
    /// </summary>
    private ReadOnlyMemory<byte> ReceiveResponse(uint channelId)
    {
        // TODO: Implement in next step
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _channelId = null;
        _connection.Dispose();
        _disposed = true;
    }
}
```

**Step 2: Build to verify structure**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidProtocol.cs
git commit -m "feat: add HidProtocol class structure and channel init"
```

---

## Task 4: Implement HID Packet Construction

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/HidProtocol.cs`

**Step 1: Add packet construction methods**

Add these methods to `HidProtocol` class (after `ReceiveResponse` method):

```csharp
    /// <summary>
    /// Constructs a CTAP HID initialization packet.
    /// </summary>
    private static byte[] ConstructInitPacket(uint channelId, byte command, ReadOnlySpan<byte> data, int totalLength)
    {
        var packet = new byte[CtapConstants.PacketSize];

        // Channel ID (4 bytes, big-endian)
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);

        // Command byte with init bit set (bit 7)
        packet[4] = (byte)(command | CtapConstants.InitPacketMask);

        // Payload length (2 bytes, big-endian)
        packet[5] = (byte)(totalLength >> 8);
        packet[6] = (byte)(totalLength & 0xFF);

        // Data payload (up to 57 bytes)
        var bytesToCopy = Math.Min(data.Length, CtapConstants.InitDataSize);
        data[..bytesToCopy].CopyTo(packet.AsSpan(CtapConstants.InitHeaderSize));

        return packet;
    }

    /// <summary>
    /// Constructs a CTAP HID continuation packet.
    /// </summary>
    private static byte[] ConstructContinuationPacket(uint channelId, byte sequence, ReadOnlySpan<byte> data)
    {
        var packet = new byte[CtapConstants.PacketSize];

        // Channel ID (4 bytes, big-endian)
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);

        // Sequence number with init bit clear (bit 7 = 0)
        packet[4] = (byte)(sequence & ~CtapConstants.InitPacketMask);

        // Data payload (up to 59 bytes)
        var bytesToCopy = Math.Min(data.Length, CtapConstants.ContinuationDataSize);
        data[..bytesToCopy].CopyTo(packet.AsSpan(CtapConstants.ContinuationHeaderSize));

        return packet;
    }
```

**Step 2: Implement SendRequest method**

Replace the `SendRequest` method stub:

```csharp
    /// <summary>
    /// Sends a CTAP HID request with proper packet framing.
    /// </summary>
    private void SendRequest(uint channelId, byte command, ReadOnlySpan<byte> data)
    {
        if (data.Length > CtapConstants.MaxPayloadSize)
            throw new ArgumentException(
                $"Data length {data.Length} exceeds max payload size {CtapConstants.MaxPayloadSize}",
                nameof(data));

        _logger.LogTrace("Sending CTAP HID command 0x{Command:X2} with {Length} bytes", command, data.Length);

        // Send initialization packet
        var initPacket = ConstructInitPacket(channelId, command, data, data.Length);
        _connection.SetReportAsync(initPacket).AsTask().Wait();

        // Send continuation packets if needed
        if (data.Length > CtapConstants.InitDataSize)
        {
            var remaining = data[CtapConstants.InitDataSize..];
            byte sequence = 0;

            while (remaining.Length > 0)
            {
                var chunkSize = Math.Min(remaining.Length, CtapConstants.ContinuationDataSize);
                var continuationPacket = ConstructContinuationPacket(channelId, sequence, remaining[..chunkSize]);
                _connection.SetReportAsync(continuationPacket).AsTask().Wait();

                remaining = remaining[chunkSize..];
                sequence++;
            }

            _logger.LogTrace("Sent {Count} continuation packets", sequence);
        }
    }
```

**Step 3: Build to verify**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidProtocol.cs
git commit -m "feat: implement HID packet construction and sending"
```

---

## Task 5: Implement HID Response Reception

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/HidProtocol.cs`

**Step 1: Add packet parsing helpers**

Add these helper methods after `ConstructContinuationPacket`:

```csharp
    /// <summary>
    /// Extracts the command byte from a packet, removing the init bit.
    /// </summary>
    private static byte GetPacketCommand(ReadOnlySpan<byte> packet) =>
        (byte)(packet[4] & ~CtapConstants.InitPacketMask);

    /// <summary>
    /// Extracts the payload length from an init packet.
    /// </summary>
    private static int GetPacketLength(ReadOnlySpan<byte> packet) =>
        (packet[5] << 8) | packet[6];
```

**Step 2: Implement ReceiveResponse method**

Replace the `ReceiveResponse` method stub:

```csharp
    /// <summary>
    /// Receives a CTAP HID response, handling keep-alive and multi-packet responses.
    /// </summary>
    private ReadOnlyMemory<byte> ReceiveResponse(uint channelId)
    {
        _logger.LogTrace("Receiving CTAP HID response");

        // Get initialization packet, handling keep-alive
        var initPacket = _connection.GetReportAsync().AsTask().Result;
        while (GetPacketCommand(initPacket.Span) == CtapConstants.CtapHidKeepAlive)
        {
            _logger.LogTrace("Received keep-alive, waiting for response");
            initPacket = _connection.GetReportAsync().AsTask().Result;
        }

        var responseLength = GetPacketLength(initPacket.Span);
        if (responseLength > CtapConstants.MaxPayloadSize)
            throw new InvalidOperationException($"Response length {responseLength} exceeds max payload size");

        // Allocate buffer for complete response
        var responseData = new byte[responseLength];
        var initDataLength = Math.Min(responseLength, CtapConstants.InitDataSize);
        initPacket.Span.Slice(CtapConstants.InitHeaderSize, initDataLength)
            .CopyTo(responseData);

        // Receive continuation packets if needed
        var bytesReceived = initDataLength;
        while (bytesReceived < responseLength)
        {
            var contPacket = _connection.GetReportAsync().AsTask().Result;
            var contDataLength = Math.Min(
                responseLength - bytesReceived,
                CtapConstants.ContinuationDataSize);

            contPacket.Span.Slice(CtapConstants.ContinuationHeaderSize, contDataLength)
                .CopyTo(responseData.AsSpan(bytesReceived));

            bytesReceived += contDataLength;
        }

        _logger.LogTrace("Received {Length} bytes in response", responseLength);
        return responseData;
    }
```

**Step 3: Build to verify**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidProtocol.cs
git commit -m "feat: implement HID response reception with keep-alive handling"
```

---

## Task 6: Implement TransmitAndReceiveAsync

**Files:**
- Modify: `Yubico.YubiKit.Core/src/Hid/HidProtocol.cs`

**Step 1: Implement APDU transmission**

Replace the `TransmitAndReceiveAsync` method:

```csharp
    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!IsChannelInitialized)
            throw new InvalidOperationException("HID channel not initialized. Call Configure() first.");

        _logger.LogTrace("Transmitting APDU over HID: {Command}", command);

        // For Management application, use CTAPHID_MSG (0x03) to send raw APDUs
        // Serialize the APDU command
        var apduBytes = SerializeApdu(command);
        
        // Send via CTAP HID MSG command
        var response = await Task.Run(() => 
            TransmitCommand(_channelId!.Value, CtapConstants.CtapHidMsg, apduBytes),
            cancellationToken).ConfigureAwait(false);

        // Parse response APDU
        var apduResponse = ParseApduResponse(response);
        
        if (!apduResponse.IsOK())
            throw ApduException.FromResponse(apduResponse, command, "HID APDU command failed");

        _logger.LogTrace("Received APDU response: {Length} bytes, SW=0x{SW:X4}",
            apduResponse.Data.Length, apduResponse.SW);

        return apduResponse.Data;
    }

    /// <summary>
    /// Serializes an APDU command to bytes.
    /// </summary>
    private static byte[] SerializeApdu(ApduCommand command)
    {
        // Calculate total length: 4 (header) + data + Lc/Le bytes
        var hasData = command.Data.Length > 0;
        var length = 4 + (hasData ? 1 + command.Data.Length : 0) + 1; // +1 for Le=0

        var buffer = new byte[length];
        var offset = 0;

        // CLA, INS, P1, P2
        buffer[offset++] = command.Cla;
        buffer[offset++] = command.Ins;
        buffer[offset++] = command.P1;
        buffer[offset++] = command.P2;

        // Lc and Data
        if (hasData)
        {
            buffer[offset++] = (byte)command.Data.Length;
            command.Data.Span.CopyTo(buffer.AsSpan(offset));
            offset += command.Data.Length;
        }

        // Le = 0 (expect up to 256 bytes response)
        buffer[offset] = 0x00;

        return buffer;
    }

    /// <summary>
    /// Parses an APDU response from bytes.
    /// </summary>
    private static ApduResponse ParseApduResponse(ReadOnlyMemory<byte> response)
    {
        if (response.Length < 2)
            throw new InvalidOperationException("APDU response too short");

        // Last 2 bytes are status word
        var sw = BinaryPrimitives.ReadUInt16BigEndian(
            response.Span[(response.Length - 2)..]);
        var data = response[..(response.Length - 2)];

        return new ApduResponse(data, sw);
    }
```

**Step 2: Build to verify**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidProtocol.cs
git commit -m "feat: implement APDU transmission over HID"
```

---

## Task 7: Create HidProtocolFactory

**Files:**
- Create: `Yubico.YubiKit.Core/src/Hid/HidProtocolFactory.cs`
- Reference: `Yubico.YubiKit.Core/src/SmartCard/PcscProtocolFactory.cs`

**Step 1: Create factory class**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Factory for creating HID protocol instances.
/// </summary>
public class HidProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    where TConnection : IConnection
{
    /// <summary>
    /// Creates a HID protocol instance for the given connection.
    /// </summary>
    /// <param name="connection">The HID connection to wrap with protocol handling.</param>
    /// <returns>A configured HID protocol instance.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the connection type is not an IHidConnection.
    /// </exception>
    public IHidProtocol Create(TConnection connection)
    {
        if (connection is not IHidConnection hidConnection)
            throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by HidProtocolFactory.");

        return new HidProtocol(hidConnection, loggerFactory.CreateLogger<HidProtocol>());
    }

    /// <summary>
    /// Creates a factory instance with optional logger factory.
    /// </summary>
    public static HidProtocolFactory<TConnection> Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? NullLoggerFactory.Instance);
}
```

**Step 2: Build to verify**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Core/src/Hid/HidProtocolFactory.cs
git commit -m "feat: add HidProtocolFactory for protocol instantiation"
```

---

## Task 8: Update ManagementSession to Use HidProtocol

**Files:**
- Modify: `Yubico.YubiKit.Management/src/ManagementSession.cs:60-70`

**Step 1: Verify the integration code**

Check that lines 60-70 in `ManagementSession.cs` already reference `HidProtocolFactory`:

```csharp
_protocol = connection switch
{
    ISmartCardConnection sc => PcscProtocolFactory<ISmartCardConnection>
        .Create(loggerFactory)
        .Create(sc),
    IHidConnection hid => HidProtocolFactory<IHidConnection>
        .Create(loggerFactory)
        .Create(hid),
    _ => throw new NotSupportedException(
        $"The connection type {connection.GetType().Name} is not supported by ManagementSession.")
};
```

This should already exist. No changes needed.

**Step 2: Build Management project**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS (the previous error should now be resolved)

**Step 3: Verify no changes needed**

Since the code is already correct, no commit needed for this task.

---

## Task 9: Fix HidProtocol Return Type

**Files:**
- Modify: `Yubico.YubiKit.Management/src/ManagementSession.cs:49`

**Context:** 
The `ManagementSession._protocol` field is typed as `IProtocol`, but `HidProtocolFactory.Create()` returns `IHidProtocol`. We need to cast or change the type.

**Step 1: Check if IProtocol can be used**

Since `IHidProtocol : IProtocol`, the assignment should work. However, we need `ISmartCardProtocol` for SmartCard connections.

Looking at the pattern, we need a common interface. Let's check the actual usage in `ManagementSession`.

**Step 2: Review TransmitAsync usage**

Check how `_protocol` is used in `ManagementSession.cs`:

```bash
grep -n "TransmitAsync\|_protocol\." Yubico.YubiKit.Management/src/ManagementSession.cs | head -20
```

**Step 3: Update protocol field type**

The `_protocol` field needs to support both `ISmartCardProtocol` and `IHidProtocol`. We need a common interface that both implement.

Actually, looking at the code more carefully, both `ISmartCardProtocol` and `IHidProtocol` extend `IProtocol`, but they have different `TransmitAndReceiveAsync` signatures. We need to add a common base method.

Let's check the actual usage pattern first before making changes.

**Step 4: Check ApplicationSession base class**

```bash
grep -n "class ApplicationSession\|TransmitAsync" Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs | head -20
```

This task needs more investigation. Let's defer until we see the actual compilation error.

---

## Task 10: Run First Integration Test

**Files:**
- Test: `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementTests.cs:27-38`

**Prerequisites:**
- YubiKey with serial 125 plugged in
- HID device enumeration working

**Step 1: Build the entire solution**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS or specific errors to fix

**Step 2: Run the HID integration test**

```bash
dotnet run --project build.cs test --project Management.IntegrationTests \
  --filter "FullyQualifiedName~CreateManagementSession_with_Hid_CreateAsync"
```

Expected: Either PASS or specific failure to debug

**Step 3: Verify test output**

The test should:
1. Find HID devices
2. Connect to first HID device
3. Create ManagementSession
4. Get device info
5. Assert serial number != 0

**Step 4: Debug if needed**

If test fails, use the `systematic-debugging` skill to diagnose:
- Check if HID device found
- Check if connection established
- Check if channel initialized
- Check APDU transmission

---

## Task 11: Handle Protocol Interface Mismatch (If Needed)

**Context:** This task is conditional based on Task 10 results.

**If compilation error occurs about incompatible protocol types:**

**Files:**
- Modify: `Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs`

**Step 1: Create common protocol interface**

Add a common method to `IProtocol` that both SmartCard and HID protocols can implement:

```csharp
public interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null);
    
    Task<ReadOnlyMemory<byte>> TransmitAsync(
        ApduCommand command, 
        CancellationToken cancellationToken = default);
}
```

**Step 2: Update ISmartCardProtocol**

Rename `TransmitAndReceiveAsync` to `TransmitAsync` or add a wrapper.

**Step 3: Update IHidProtocol**

Same as above.

**NOTE:** Only complete this task if the compilation actually fails. The current design might work as-is.

---

## Task 12: Run All HID Management Tests

**Files:**
- Test: `Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementTests.cs`

**Step 1: Run all HID-related tests**

```bash
dotnet run --project build.cs test --project Management.IntegrationTests \
  --filter "FullyQualifiedName~Hid"
```

Expected: All HID tests PASS

**Step 2: Verify specific tests**

Ensure these tests pass:
- `CreateManagementSession_with_Hid_CreateAsync` (line 27)
- `CreateManagementSession_Hid_with_CreateAsync` (line 41)

**Step 3: Check serial number**

Verify test output shows serial number 125 for your physical YubiKey.

**Step 4: Run full Management test suite**

```bash
dotnet run --project build.cs test --project Management.IntegrationTests
```

Expected: All tests PASS (SmartCard tests should still work)

---

## Task 13: Add Unit Tests for HID Protocol

**Files:**
- Create: `Yubico.YubiKit.UnitTests/Hid/HidProtocolTests.cs`

**Step 1: Create test file structure**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Moq;
using Xunit;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.UnitTests.Hid;

public class HidProtocolTests
{
    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HidProtocol(null!));
    }

    [Fact]
    public void IsChannelInitialized_BeforeConfigure_ReturnsFalse()
    {
        var mockConnection = new Mock<IHidConnection>();
        var protocol = new HidProtocol(mockConnection.Object);

        Assert.False(protocol.IsChannelInitialized);
    }

    // Add more unit tests for:
    // - Packet construction
    // - Channel initialization
    // - Error handling
}
```

**Step 2: Run unit tests**

```bash
dotnet run --project build.cs test --project UnitTests \
  --filter "FullyQualifiedName~HidProtocolTests"
```

Expected: PASS

**Step 3: Commit**

```bash
git add Yubico.YubiKit.UnitTests/Hid/HidProtocolTests.cs
git commit -m "test: add unit tests for HidProtocol"
```

---

## Task 14: Update Documentation

**Files:**
- Modify: `Yubico.YubiKit.Management/README.md`
- Modify: `Yubico.YubiKit.Management/CLAUDE.md`

**Step 1: Update README with HID example**

Add HID connection example to README:

```markdown
### Using HID Connection

```csharp
// Find HID devices
var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
var yubiKey = devices.First();

// Create management session over HID
using var connection = await yubiKey.ConnectAsync<IHidConnection>();
using var session = await ManagementSession.CreateAsync(connection);

// Get device information
var deviceInfo = await session.GetDeviceInfoAsync();
Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
```
```

**Step 2: Update CLAUDE.md**

Document the HID protocol implementation in the architecture section.

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Management/README.md Yubico.YubiKit.Management/CLAUDE.md
git commit -m "docs: add HID protocol documentation"
```

---

## Task 15: Final Verification

**Files:**
- All modified files

**Step 1: Run full solution build**

```bash
dotnet run --project build.cs build
```

Expected: SUCCESS with no warnings

**Step 2: Run all tests**

```bash
dotnet run --project build.cs test
```

Expected: All tests PASS

**Step 3: Test with physical YubiKey**

```bash
dotnet run --project build.cs test --project Management.IntegrationTests \
  --filter "FullyQualifiedName~CreateManagementSession_with_Hid_CreateAsync"
```

Expected: PASS with serial number 125 displayed

**Step 4: Final commit**

```bash
git status
git add --all
git commit -m "feat: complete HID protocol implementation"
```

---

## Troubleshooting Guide

### Issue: Channel initialization fails

**Symptoms:** Exception during `AcquireCtapHidChannel`

**Check:**
1. HID device enumeration working: `YubiKeyManager.FindAllAsync(ConnectionType.Hid)`
2. Low-level HID connection working: `SetReport`/`GetReport` succeeds
3. Response packet format correct: 17+ bytes with nonce echo

**Fix:** Add detailed logging to see exact bytes sent/received

### Issue: APDU transmission fails

**Symptoms:** Exception in `TransmitAndReceiveAsync`

**Check:**
1. Channel ID acquired successfully
2. APDU serialization correct (4-byte header + Lc + data + Le)
3. CTAPHID_MSG (0x03) command used for Management application
4. Response parsing handles status word correctly

**Fix:** Compare with legacy `FidoTransform.cs` packet structure

### Issue: Keep-alive not handled

**Symptoms:** Timeout or stuck waiting for response

**Check:**
1. Keep-alive command byte = 0x3B | 0x80 = 0xBB
2. Loop continues until non-keep-alive packet received
3. Timeout mechanism in place

**Fix:** Add timeout with cancellation token

### Issue: Multi-packet responses incorrect

**Symptoms:** Truncated or corrupted data

**Check:**
1. Response length from init packet header
2. Continuation packet sequence verified
3. Correct number of continuation packets received
4. Buffer size calculations correct

**Fix:** Add assertions for packet counts and buffer sizes

---

## Definition of Done Checklist

- [ ] All code compiles without errors or warnings
- [ ] `HidProtocol`, `IHidProtocol`, `HidProtocolFactory` implemented
- [ ] CTAP HID channel initialization works
- [ ] APDU transmission over HID works
- [ ] Multi-packet fragmentation/reassembly works
- [ ] Keep-alive handling works
- [ ] `ManagementTests.CreateManagementSession_with_Hid_CreateAsync()` passes
- [ ] Device info retrieved with correct serial number (125)
- [ ] All HID integration tests pass
- [ ] Unit tests added and passing
- [ ] Documentation updated
- [ ] No regressions in SmartCard tests

---

## Next Steps After Completion

Once HID protocol is working:

1. **Add more HID applications:**
   - FIDO2 over HID
   - OTP over HID (feature reports)
   - PIV over HID (if supported)

2. **Performance optimization:**
   - Reduce async/sync transitions
   - Pool packet buffers
   - Optimize packet parsing

3. **Enhanced error handling:**
   - CTAPHID_ERROR response codes
   - Timeout handling
   - Channel busy retry logic

4. **SCP over HID:**
   - Investigate if SCP03 works over HID
   - Implement `HidProtocolScp` if needed

---

## References

**Specifications:**
- FIDO CTAP HID Protocol: https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#usb

**Legacy Implementation:**
- `legacy-develop/Yubico.YubiKey/src/Yubico/YubiKey/Pipelines/FidoTransform.cs`
- `legacy-develop/Yubico.YubiKey/src/Yubico/YubiKey/FidoConnection.cs`

**Current Codebase:**
- `Yubico.YubiKit.Core/src/SmartCard/PcscProtocol.cs` (pattern reference)
- `Yubico.YubiKit.Core/src/Hid/IHidConnection.cs` (connection interface)
- `Yubico.YubiKit.Management/src/ManagementSession.cs` (integration point)

**Skills:**
- `test-driven-development` - For implementing each component
- `systematic-debugging` - For diagnosing test failures
- `verification-before-completion` - Before marking tasks done
