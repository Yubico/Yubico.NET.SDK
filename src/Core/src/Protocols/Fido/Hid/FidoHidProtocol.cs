// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Protocols.Fido.Hid;

/// <summary>
/// Implements FIDO CTAP HID protocol for communication with YubiKey FIDO interface.
/// Supports CTAP HID framing, channel management, and YubiKey Management vendor commands.
/// Based on FIDO CTAP HID Protocol Specification.
/// </summary>
/// <remarks>
///     Concurrency: CTAP HID permits one transaction at a time per channel — a request is an init
///     packet plus continuation packets, and a foreign init packet mid-transaction aborts it on the
///     device. This class serializes full logical exchanges (including lazy channel initialization)
///     through an internal guard: overlapping calls are refused immediately. An exchange in flight runs to
///     completion.
/// </remarks>
internal class FidoHidProtocol(
    IFidoHidConnection connection,
    ILogger<FidoHidProtocol>? logger = null,
    Func<int, byte[]>? responseBufferFactory = null)
    : IFidoHidProtocol, IAsyncDisposable
{
    private readonly IFidoHidConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly ExchangeGuard _exchangeGuard = new();
    private readonly DisposalGate _disposalGate = new();
    private readonly ILogger<FidoHidProtocol> _logger = logger ?? NullLogger<FidoHidProtocol>.Instance;
    private readonly Func<int, byte[]> _responseBufferFactory = responseBufferFactory ?? (static length => new byte[length]);
    private uint? _channelId;
    private FirmwareVersion? _firmwareVersion;
    private bool _disposed;

    public bool IsChannelInitialized => _channelId.HasValue;
    public FirmwareVersion? FirmwareVersion => _firmwareVersion;

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
    {
        InitializeAsync().GetAwaiter().GetResult();

        _logger.LogDebug("HID protocol configured for firmware version {Version}", version);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _exchangeGuard.RunAsync(
                async exchangeToken =>
                {
                    await EnsureChannelInitializedAsync(exchangeToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> SendVendorCommandAsync(
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogTrace("Sending CTAP vendor command 0x{Command:X2} with {Length} bytes",
            command, data.Length);

        var response = await _exchangeGuard.RunAsync(
                async exchangeToken =>
                {
                    await EnsureChannelInitializedAsync(exchangeToken).ConfigureAwait(false);
                    return await TransmitCommand(
                            _channelId!.Value,
                            command,
                            data,
                            exchangeToken)
                        .ConfigureAwait(false);
                },
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogTrace("Received vendor command response: {Length} bytes", response.Length);
        return response;
    }

    /// <summary>
    /// Initializes the CTAP HID channel if not already done. Must be called from within the
    /// exchange guard (or single-threaded initialization) — the INIT handshake is itself an
    /// exchange that must not interleave with other traffic.
    /// </summary>
    private async Task EnsureChannelInitializedAsync(CancellationToken cancellationToken)
    {
        if (IsChannelInitialized)
            return;

        _logger.LogDebug("Auto-initializing HID channel");
        await AcquireCtapHidChannelAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires a CTAP HID channel by sending CTAPHID_INIT to the broadcast channel.
    /// </summary>
    private async Task AcquireCtapHidChannelAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Acquiring CTAP HID channel");

        // Generate 8-byte random nonce
        var nonce = new byte[CtapConstants.NonceSize];
        RandomNumberGenerator.Fill(nonce);
        try
        {
            // Send CTAPHID_INIT to broadcast channel
            var response = await TransmitCommand(
                    CtapConstants.BroadcastChannelId,
                    CtapConstants.CtapHidInit,
                    nonce.AsMemory(),
                    cancellationToken)
                .ConfigureAwait(false);

            // Verify nonce echo
            if (response.Length < 17)  // nonce(8) + channelId(4) + version(1) + firmware(3) + capabilities(1)
            {
                _logger.LogError("CTAPHID_INIT response too short: {Length} bytes. Expected at least 17.", response.Length);
                throw new InvalidOperationException($"CTAPHID_INIT response too short: {response.Length} bytes");
            }

            var receivedNonce = response.Span[..CtapConstants.NonceSize];
            if (!CryptographicOperations.FixedTimeEquals(nonce, receivedNonce))
            {
                _logger.LogError("CTAPHID_INIT nonce mismatch");
                throw new InvalidOperationException("CTAPHID_INIT nonce mismatch");
            }

            // Extract channel ID (bytes 8-11, big-endian)
            _channelId = BinaryPrimitives.ReadUInt32BigEndian(response.Span[8..12]);

            // Extract firmware version (bytes 13-15) - skip protocol version byte at 12
            if (response.Length >= 16)
            {
                var major = response.Span[13];
                var minor = response.Span[14];
                var patch = response.Span[15];
                _firmwareVersion = new FirmwareVersion(major, minor, patch);
                _logger.LogDebug("Extracted firmware version from CTAPHID_INIT: {Version}", _firmwareVersion);
            }

            _logger.LogDebug("Acquired CTAP HID channel: 0x{ChannelId:X8}", _channelId.Value);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    /// <summary>
    /// Transmits a CTAP HID command and receives the response.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> TransmitCommand(
        uint channelId,
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        await SendRequest(channelId, command, data, cancellationToken).ConfigureAwait(false);
        return await ReceiveResponse(channelId, command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a CTAP HID request with proper packet framing.
    /// </summary>
    private async Task SendRequest(
        uint channelId,
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        if (data.Length > CtapConstants.MaxPayloadSize)
            throw new ArgumentException(
                $"Data length {data.Length} exceeds max payload size {CtapConstants.MaxPayloadSize}",
                nameof(data));

        _logger.LogTrace("Sending CTAP HID command 0x{Command:X2} with {Length} bytes", command, data.Length);

        // Send initialization packet
        var initPacket = ConstructInitPacket(channelId, command, data.Span, data.Length);
        try
        {
            await _connection.SendAsync(initPacket, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(initPacket);
        }

        // Send continuation packets if needed
        if (data.Length > CtapConstants.InitDataSize)
        {
            var remaining = data[CtapConstants.InitDataSize..];
            byte sequence = 0;

            while (remaining.Length > 0)
            {
                var span = remaining.Span;
                var chunkSize = Math.Min(span.Length, CtapConstants.ContinuationDataSize);
                var continuationPacket = ConstructContinuationPacket(channelId, sequence, span[..chunkSize]);
                try
                {
                    await _connection.SendAsync(continuationPacket, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(continuationPacket);
                }

                remaining = remaining[chunkSize..];
                sequence++;
            }

            _logger.LogTrace("Sent {Count} continuation packets", sequence);
        }
    }

    /// <summary>
    /// Receives a CTAP HID response, handling keep-alive and multi-packet responses.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> ReceiveResponse(
        uint channelId,
        byte expectedCommand,
        CancellationToken cancellationToken)
    {
        _logger.LogTrace("Receiving CTAP HID response");

        // Get initialization packet, handling keep-alive
        var initPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        while (IsKeepAlivePacket(initPacket.Span))
        {
            ValidateInitPacket(initPacket.Span, channelId);
            _logger.LogTrace("Received keep-alive, waiting for response");
            initPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        ValidateInitPacket(initPacket.Span, channelId);

        var responseLength = GetPacketLength(initPacket.Span);
        if (responseLength > CtapConstants.MaxPayloadSize)
            throw new InvalidOperationException($"Response length {responseLength} exceeds max payload size");

        byte responseCommand = GetPacketCommand(initPacket.Span);
        if (responseCommand == CtapConstants.CtapHidError)
        {
            byte errorCode = responseLength > 0 ? initPacket.Span[CtapConstants.InitHeaderSize] : (byte)0x7F;
            throw new InvalidOperationException($"CTAP HID error response: 0x{errorCode:X2}");
        }

        byte normalizedExpectedCommand = (byte)(expectedCommand & ~CtapConstants.InitPacketMask);
        if (responseCommand != normalizedExpectedCommand)
        {
            throw new InvalidOperationException(
                $"CTAP HID response command 0x{responseCommand:X2} does not match request command 0x{expectedCommand:X2}");
        }

        byte[] responseData = _responseBufferFactory(responseLength);
        var ownershipTransferred = false;
        try
        {
            var initDataLength = Math.Min(responseLength, CtapConstants.InitDataSize);

            initPacket.Span.Slice(CtapConstants.InitHeaderSize, initDataLength)
                .CopyTo(responseData);

            // Receive continuation packets if needed
            var bytesReceived = initDataLength;
            byte expectedSequence = 0;
            while (bytesReceived < responseLength)
            {
                var contPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                ValidateContinuationPacket(contPacket.Span, channelId, expectedSequence);
                var contDataLength = Math.Min(
                    responseLength - bytesReceived,
                    CtapConstants.ContinuationDataSize);

                contPacket.Span.Slice(CtapConstants.ContinuationHeaderSize, contDataLength)
                    .CopyTo(responseData.AsSpan(bytesReceived));

                bytesReceived += contDataLength;
                expectedSequence++;
            }

            _logger.LogTrace("Received {Length} bytes in response", responseLength);
            ownershipTransferred = true;
            return responseData;
        }
        finally
        {
            if (!ownershipTransferred)
                CryptographicOperations.ZeroMemory(responseData);
        }
    }

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

    /// <summary>
    /// Extracts the command byte from a packet, removing the init bit.
    /// </summary>
    private static byte GetPacketCommand(ReadOnlySpan<byte> packet) =>
        (byte)(packet[4] & ~CtapConstants.InitPacketMask);

    private static bool IsKeepAlivePacket(ReadOnlySpan<byte> packet) =>
        packet.Length >= CtapConstants.InitHeaderSize &&
        (packet[4] & CtapConstants.InitPacketMask) != 0 &&
        GetPacketCommand(packet) == CtapConstants.CtapHidKeepAlive;

    private static void ValidateInitPacket(ReadOnlySpan<byte> packet, uint channelId)
    {
        if (packet.Length != CtapConstants.PacketSize)
            throw new InvalidOperationException("CTAP HID init packet must be exactly 64 bytes");

        var packetChannelId = BinaryPrimitives.ReadUInt32BigEndian(packet);
        if (packetChannelId != channelId)
            throw new InvalidOperationException("CTAP HID init packet channel mismatch");

        if ((packet[4] & CtapConstants.InitPacketMask) == 0)
            throw new InvalidOperationException("CTAP HID response packet is not an init packet");
    }

    private static void ValidateContinuationPacket(ReadOnlySpan<byte> packet, uint channelId, byte expectedSequence)
    {
        if (packet.Length != CtapConstants.PacketSize)
            throw new InvalidOperationException("CTAP HID continuation packet must be exactly 64 bytes");

        var packetChannelId = BinaryPrimitives.ReadUInt32BigEndian(packet);
        if (packetChannelId != channelId)
            throw new InvalidOperationException("CTAP HID continuation packet channel mismatch");

        if ((packet[4] & CtapConstants.InitPacketMask) != 0)
            throw new InvalidOperationException("CTAP HID continuation packet has init bit set");

        var sequence = packet[4];
        if (!IsExpectedContinuationSequence(sequence, expectedSequence))
            throw new InvalidOperationException("CTAP HID continuation packet sequence mismatch");
    }

    internal static bool IsExpectedContinuationSequence(byte sequence, byte expectedSequence) =>
        (sequence & ~CtapConstants.InitPacketMask) == (expectedSequence & ~CtapConstants.InitPacketMask);

    /// <summary>
    /// Extracts the payload length from an init packet.
    /// </summary>
    private static int GetPacketLength(ReadOnlySpan<byte> packet) =>
        (packet[5] << 8) | packet[6];

    /// <summary>
    ///     Releases this protocol. The connection is NOT disposed: a protocol is a user of the connection it
    ///     was handed, never its owner. Whoever created the connection disposes it.
    /// </summary>
    public void Dispose()
    {
        _disposalGate.Dispose(() =>
        {
            _exchangeGuard.CloseAndDrain();
            _channelId = null;
            _disposed = true;
        });
    }

    public ValueTask DisposeAsync() => _disposalGate.DisposeAsync(async () =>
    {
        await _exchangeGuard.CloseAndDrainAsync().ConfigureAwait(false);
        _channelId = null;
        _disposed = true;
    });
}