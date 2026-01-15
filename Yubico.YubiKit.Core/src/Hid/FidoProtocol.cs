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
/// Implements FIDO CTAP HID protocol for communication with YubiKey FIDO interface.
/// Supports CTAP HID framing, channel management, and YubiKey Management vendor commands.
/// Based on FIDO CTAP HID Protocol Specification.
/// </summary>
internal class FidoProtocol(IFidoConnection connection, ILogger<FidoProtocol>? logger = null)
    : IFidoProtocol
{
    private readonly IFidoConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly ILogger<FidoProtocol> _logger = logger ?? NullLogger<FidoProtocol>.Instance;
    private uint? _channelId;
    private FirmwareVersion? _firmwareVersion;
    private bool _disposed;

    public bool IsChannelInitialized => _channelId.HasValue;
    public FirmwareVersion? FirmwareVersion => _firmwareVersion;

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
    {
        // Initialize CTAP HID channel if not already done
        if (!IsChannelInitialized)
        {
            AcquireCtapHidChannel();
        }
        _logger.LogDebug("HID protocol configured for firmware version {Version}", version);
    }

    public async Task<ReadOnlyMemory<byte>> SendVendorCommandAsync(
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Auto-initialize channel if not already done
        if (!IsChannelInitialized)
        {
            _logger.LogDebug("Auto-initializing HID channel for SendVendorCommandAsync");
            AcquireCtapHidChannel();
        }

        _logger.LogTrace("Sending CTAP vendor command 0x{Command:X2} with {Length} bytes",
            command, data.Length);

        // Send vendor command directly via CTAP HID
        var response = await TransmitCommand(
                _channelId!.Value,
                command,
                data,
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogTrace("Received vendor command response: {Length} bytes", response.Length);
        return response;
    }

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Auto-initialize channel if not already done
        if (!IsChannelInitialized)
        {
            _logger.LogDebug("Auto-initializing HID channel for TransmitAndReceiveAsync");
            AcquireCtapHidChannel();
        }

        _logger.LogTrace("Transmitting APDU over HID: {Command}", command);

        // For Management application, use CTAPHID_MSG (0x03) to send raw APDUs
        // Serialize the APDU command
        var apduBytes = SerializeApdu(command);
        
        // Send via CTAP HID MSG command
        var response = await TransmitCommand(
                _channelId!.Value,
                CtapConstants.CtapHidMsg,
                apduBytes,
                cancellationToken)
            .ConfigureAwait(false);

        // Parse response APDU
        var apduResponse = ParseApduResponse(response);
        
        if (!apduResponse.IsOK())
            throw ApduException.FromResponse(apduResponse, command, "HID APDU command failed");

        _logger.LogTrace("Received APDU response: {Length} bytes, SW=0x{SW:X4}",
            apduResponse.Data.Length, apduResponse.SW);

        return apduResponse.Data;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Auto-initialize channel if not already done
        if (!IsChannelInitialized)
        {
            _logger.LogDebug("Auto-initializing HID channel for SelectAsync");
            AcquireCtapHidChannel();
        }

        _logger.LogTrace("HID SelectAsync called for application ID, returning version string");

        // For HID, the Management application doesn't require SELECT - it's directly accessible.
        // Return version string based on firmware version obtained during CTAPHID_INIT
        var version = _firmwareVersion ?? new FirmwareVersion(5, 0, 0);
        var versionString = System.Text.Encoding.UTF8.GetBytes(
            $"YubiKey {version.Major}.{version.Minor}.{version.Patch}");
        return await Task.FromResult<ReadOnlyMemory<byte>>(versionString).ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires a CTAP HID channel by sending CTAPHID_INIT to the broadcast channel.
    /// </summary>
    private void AcquireCtapHidChannel()
    {
        _logger.LogDebug("Acquiring CTAP HID channel");

        // Generate 8-byte random nonce
        var nonce = new byte[CtapConstants.NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Send CTAPHID_INIT to broadcast channel
        var response = TransmitCommand(
                CtapConstants.BroadcastChannelId,
                CtapConstants.CtapHidInit,
                nonce.AsMemory(),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        // Verify nonce echo
        if (response.Length < 17)  // nonce(8) + channelId(4) + version(1) + firmware(3) + capabilities(1)
        {
            _logger.LogError("CTAPHID_INIT response too short: {Length} bytes. Expected at least 17.", response.Length);
            throw new InvalidOperationException($"CTAPHID_INIT response too short: {response.Length} bytes");
        }

        var receivedNonce = response.Span[..CtapConstants.NonceSize];
        if (!nonce.SequenceEqual(receivedNonce))
        {
            _logger.LogError("CTAPHID_INIT nonce mismatch. Sent: {SentNonce}, Received: {ReceivedNonce}",
                Convert.ToHexString(nonce),
                Convert.ToHexString(receivedNonce.ToArray()));
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
        return await ReceiveResponse(channelId, cancellationToken).ConfigureAwait(false);
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
        await _connection.SendAsync(initPacket, cancellationToken).ConfigureAwait(false);

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
                await _connection.SendAsync(continuationPacket, cancellationToken).ConfigureAwait(false);

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
        CancellationToken cancellationToken)
    {
        _logger.LogTrace("Receiving CTAP HID response");

        // Get initialization packet, handling keep-alive
        var initPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        while (GetPacketCommand(initPacket.Span) == CtapConstants.CtapHidKeepAlive)
        {
            _logger.LogTrace("Received keep-alive, waiting for response");
            initPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        var responseLength = GetPacketLength(initPacket.Span);
        if (responseLength > CtapConstants.MaxPayloadSize)
            throw new InvalidOperationException($"Response length {responseLength} exceeds max payload size");

        // Allocate buffer for complete response
        var responseData = new byte[responseLength];
        var initDataLength = Math.Min(responseLength, CtapConstants.InitDataSize);
        
        // Ensure we don't try to read more data than the packet contains
        var availableDataInPacket = Math.Min(initDataLength, initPacket.Length - CtapConstants.InitHeaderSize);
        if (availableDataInPacket < 0)
            availableDataInPacket = 0;
            
        initPacket.Span.Slice(CtapConstants.InitHeaderSize, availableDataInPacket)
            .CopyTo(responseData);

        // Receive continuation packets if needed
        var bytesReceived = availableDataInPacket;
        while (bytesReceived < responseLength)
        {
            var contPacket = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            var contDataLength = Math.Min(
                responseLength - bytesReceived,
                CtapConstants.ContinuationDataSize);

            // Ensure we don't try to read more data than the packet contains
            var availableContData = Math.Min(contDataLength, contPacket.Length - CtapConstants.ContinuationHeaderSize);
            if (availableContData < 0)
                availableContData = 0;

            contPacket.Span.Slice(CtapConstants.ContinuationHeaderSize, availableContData)
                .CopyTo(responseData.AsSpan(bytesReceived));

            bytesReceived += availableContData;
        }

        _logger.LogTrace("Received {Length} bytes in response", responseLength);
        return responseData;
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

    /// <summary>
    /// Extracts the payload length from an init packet.
    /// </summary>
    private static int GetPacketLength(ReadOnlySpan<byte> packet) =>
        (packet[5] << 8) | packet[6];

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
        return new ApduResponse(response);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _channelId = null;
        _connection.Dispose();
        _disposed = true;
    }
}
