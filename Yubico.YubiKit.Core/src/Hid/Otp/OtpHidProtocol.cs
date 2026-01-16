// Copyright 2025 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Hid.Otp;

/// <summary>
/// Implements OTP HID protocol for communication with YubiKey OTP/Keyboard interface.
/// Uses 8-byte feature reports with CRC validation.
/// Based on the Java yubikit-android OtpProtocol implementation.
/// </summary>
internal sealed class OtpHidProtocol : IOtpHidProtocol
{
    private readonly IOtpHidConnection _connection;
    private readonly ILogger<OtpHidProtocol> _logger;
    private FirmwareVersion? _firmwareVersion;
    private bool _initialized;
    private bool _disposed;

    public OtpHidProtocol(IOtpHidConnection connection, ILogger<OtpHidProtocol>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? NullLogger<OtpHidProtocol>.Instance;
    }

    public FirmwareVersion? FirmwareVersion => _firmwareVersion;

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
    {
        EnsureInitialized();
        _logger.LogDebug("OTP protocol configured, firmware version: {Version}", _firmwareVersion);
    }

    /// <summary>
    /// Ensures the protocol is initialized by reading the initial status.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        // Read initial feature report to get version
        var featureReport = ReadFeatureReport();

        // Extract version from feature report bytes 1-3
        _firmwareVersion = new FirmwareVersion(featureReport[1], featureReport[2], featureReport[3]);

        // Handle NEO quirk: if major version is 3, may have cached pgmSeq in arbitrator
        if (_firmwareVersion.Major == 3)
        {
            _logger.LogDebug("NEO detected (firmware 3.x), refreshing programming sequence");
            // Force communication with applet to refresh pgmSeq by writing an invalid scan map
            var scanMap = new byte[51];
            Array.Fill(scanMap, (byte)'c');
            try
            {
                SendAndReceiveAsync(0x12, scanMap, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                // Expected to fail - the scan map command should be rejected
            }
        }

        _initialized = true;
        _logger.LogDebug("OTP protocol initialized, firmware version: {Version}", _firmwareVersion);
    }

    public async Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Auto-initialize if not already done
        if (!_initialized)
        {
            _logger.LogDebug("Auto-initializing OTP protocol for SendAndReceiveAsync");
            EnsureInitialized();
        }

        // Pad data to slot data size (64 bytes)
        byte[] payload;
        if (data.Length > OtpConstants.SlotDataSize)
        {
            throw new ArgumentException($"Payload too large for HID frame! Max {OtpConstants.SlotDataSize} bytes.", nameof(data));
        }

        payload = new byte[OtpConstants.SlotDataSize];
        data.Span.CopyTo(payload);

        _logger.LogTrace("Sending OTP slot command 0x{Slot:X2} with {Length} bytes payload", slot, data.Length);

        var programmingSequence = await SendFrameAsync(slot, payload, cancellationToken).ConfigureAwait(false);
        
        // Read response using Java-style single polling loop
        return await ReadFrameJavaStyleAsync(programmingSequence, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads one frame response using the legacy C# approach:
    /// 1. First wait for ReadPending flag (like WaitForReadPending)
    /// 2. Then read data packets using the frame reader pattern
    /// </summary>
    /// <param name="programmingSequence">The programming sequence before the command was sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<ReadOnlyMemory<byte>> ReadFrameJavaStyleAsync(
        int programmingSequence,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("ReadFrameJavaStyleAsync starting, programmingSequence={ProgSeq}", programmingSequence);

        // Phase 1: Wait for ReadPending flag (legacy C# WaitForReadPending approach)
        var (firstReport, hasData) = await WaitForReadyToReadAsync(programmingSequence, cancellationToken)
            .ConfigureAwait(false);

        if (!hasData)
        {
            // Status-only response (config command, sequence updated)
            var status = firstReport.Slice(1, 6).ToArray();
            _logger.LogDebug("Status-only response: {Status}", Convert.ToHexString(status));
            return status;
        }

        // Phase 2: Read data packets (legacy C# frame reader pattern)
        return await ReadDataPacketsAsync(firstReport, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for the device to be ready to read (ReadPending flag set) or command complete.
    /// Uses tight polling - the write-side delays provide sufficient device processing time.
    /// </summary>
    private async Task<(ReadOnlyMemory<byte> Report, bool HasData)> WaitForReadyToReadAsync(
        int programmingSequence,
        CancellationToken cancellationToken)
    {
        const int timeLimitMs = 1023; // Same as legacy SDK short timeout
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeLimitMs)
        {
            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            _logger.LogTrace("WaitForReadyToRead: statusByte=0x{Status:X2}, report={Report}",
                statusByte, Convert.ToHexString(report.Span));

            // Check for touch pending
            if ((statusByte & OtpConstants.ResponseTimeoutWaitFlag) != 0)
            {
                _logger.LogDebug("Touch pending, waiting for user interaction...");
                await WaitForTouchCompleteAsync(cancellationToken).ConfigureAwait(false);
                // After touch completes, continue polling
                continue;
            }

            // Check for ReadPending flag - data is ready
            if ((statusByte & OtpConstants.ResponsePendingFlag) != 0)
            {
                _logger.LogDebug("ReadPending flag detected, data ready");
                return (report, true); // Has data
            }

            // Status response (statusByte == 0) - check if command completed
            if (statusByte == 0)
            {
                var nextSeq = report.Span[OtpConstants.StatusOffsetProgSeq];
                var touchLow = report.Span[OtpConstants.StatusOffsetTouchLow];

                _logger.LogTrace("Status response: nextSeq={NextSeq}, progSeq={ProgSeq}",
                    nextSeq, programmingSequence);

                // Check if sequence incremented (command completed without data)
                if (nextSeq == programmingSequence + 1 ||
                    (nextSeq == 0 && programmingSequence > 0 &&
                     (touchLow & OtpConstants.ConfigSlotsProgrammedMask) == 0))
                {
                    _logger.LogDebug("Sequence updated from {OldSeq} to {NewSeq}, no data response",
                        programmingSequence, nextSeq);
                    return (report, false); // No data, just status
                }

                // Sequence hasn't changed - device still processing, keep polling
                _logger.LogTrace("Device still processing (seq unchanged), continuing poll");
                continue;
            }

            // WritePending or other flags - device is busy, keep polling
            _logger.LogTrace("Device busy (statusByte=0x{Status:X2}), continuing poll", statusByte);
        }

        await ResetStateAsync(cancellationToken).ConfigureAwait(false);
        throw new TimeoutException($"Timeout waiting for device response after {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Reads data packets after ReadPending was detected.
    /// Uses the legacy C# frame reader pattern.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> ReadDataPacketsAsync(
        ReadOnlyMemory<byte> firstReport,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var previousSeq = -1;

        // Process the first report (already has ReadPending set)
        var report = firstReport;

        while (true)
        {
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            // Check if ReadPending is still set
            if ((statusByte & OtpConstants.ResponsePendingFlag) == 0)
            {
                // End of data chain
                _logger.LogDebug("End of data chain (ReadPending cleared)");
                break;
            }

            var packetSeq = statusByte & OtpConstants.SequenceMask;

            // Check for sequence reset (second time seeing seq=0 means end of transmission)
            if (packetSeq == 0 && previousSeq != -1)
            {
                await ResetStateAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Transmission complete (seq reset to 0)");
                break;
            }

            // Add payload to buffer (7 bytes)
            stream.Write(report.Span[..OtpConstants.FeatureReportDataSize]);
            previousSeq = packetSeq;

            _logger.LogTrace("Added packet seq={Seq}, total={Total} bytes", packetSeq, stream.Length);

            // Read next report
            report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
        }

        var rawResponse = stream.ToArray();
        _logger.LogDebug("{Length} bytes read over HID: {Response}",
            rawResponse.Length, Convert.ToHexString(rawResponse));

        return rawResponse;
    }

    /// <summary>
    /// Waits for touch to complete (TouchPending flag to clear).
    /// </summary>
    private async Task WaitForTouchCompleteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int timeLimitMs = 14000; // 14 seconds for touch (YubiKey times out at 15)

        while (stopwatch.ElapsedMilliseconds < timeLimitMs)
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);

            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            if ((statusByte & OtpConstants.ResponseTimeoutWaitFlag) == 0)
            {
                _logger.LogDebug("Touch completed after {Ms}ms", stopwatch.ElapsedMilliseconds);
                return;
            }
        }

        throw new TimeoutException("Timeout waiting for user touch");
    }

    public async Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Auto-initialize if not already done to get version
        if (!_initialized)
        {
            EnsureInitialized();
        }

        var featureReport = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
        // Return bytes 1-6 (skip first and last byte)
        return featureReport.Slice(1, 6);
    }

    /// <summary>
    /// Reads a single 8-byte feature report synchronously.
    /// </summary>
    private byte[] ReadFeatureReport()
    {
        var report = _connection.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
        _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report.Span));
        return report.ToArray();
    }

    /// <summary>
    /// Reads a single 8-byte feature report asynchronously.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> ReadFeatureReportAsync(CancellationToken cancellationToken)
    {
        var report = await _connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogTrace("Read feature report: {Report}", Convert.ToHexString(report.Span));
        return report;
    }

    /// <summary>
    /// Writes a single 8-byte feature report.
    /// </summary>
    private async Task WriteFeatureReportAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Write feature report: {Report}", Convert.ToHexString(buffer.Span));
        await _connection.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for the WRITE flag to be cleared (device ready to receive).
    /// Uses "sleep-first" pattern: delay BEFORE checking the flag.
    /// This gives the device time to process each frame before we poll.
    /// </summary>
    private async Task AwaitReadyToWriteAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            // Sleep first - give device time to clear write flag
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            if ((report.Span[OtpConstants.FeatureReportDataSize] & OtpConstants.SlotWriteFlag) == 0)
            {
                return;
            }
        }

        throw new TimeoutException("Timeout waiting for YubiKey to become ready to receive");
    }

    /// <summary>
    /// Packs and sends one 70-byte frame as multiple 8-byte feature reports.
    /// </summary>
    private async Task<int> SendFrameAsync(byte slot, byte[] payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("SendFrameAsync: slot=0x{Slot:X2}, payloadLen={Len}", slot, payload.Length);
        _logger.LogTrace("Sending payload to slot 0x{Slot:X2}: {Payload}", slot, Convert.ToHexString(payload));

        // Format 70-byte frame: [64-byte payload][1-byte slot][2-byte CRC][3-byte filler]
        var frame = new byte[OtpConstants.FrameSize];
        payload.CopyTo(frame.AsSpan());
        frame[OtpConstants.SlotDataSize] = slot;

        // Calculate and write CRC (little-endian)
        var crc = ChecksumUtils.CalculateCrc(payload, payload.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(OtpConstants.SlotDataSize + 1), crc);
        // Last 3 bytes are filler (already zero)

        _logger.LogTrace("Frame (70 bytes): {Frame}", Convert.ToHexString(frame));

        // Get current programming sequence
        var statusReport = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
        var programmingSequence = statusReport.Span[OtpConstants.StatusOffsetProgSeq];

        _logger.LogDebug("Initial programming sequence: {ProgSeq}, statusReport: {Report}",
            programmingSequence, Convert.ToHexString(statusReport.Span));

        // Send frame as 8-byte feature reports
        var report = new byte[OtpConstants.FeatureReportSize];
        byte seq = 0;
        var frameOffset = 0;
        var sentCount = 0;

        while (frameOffset < OtpConstants.FrameSize)
        {
            // Copy 7 bytes of frame data to report
            var bytesToCopy = Math.Min(OtpConstants.FeatureReportDataSize, OtpConstants.FrameSize - frameOffset);
            Array.Clear(report, 0, OtpConstants.FeatureReportDataSize);
            Array.Copy(frame, frameOffset, report, 0, bytesToCopy);

            // Set sequence with write flag
            report[OtpConstants.FeatureReportDataSize] = (byte)(OtpConstants.SlotWriteFlag | seq);
            await AwaitReadyToWriteAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("Sending report #{Count} (seq={Seq}): {Report}", 
                sentCount, seq, Convert.ToHexString(report));
            await WriteFeatureReportAsync(report, cancellationToken).ConfigureAwait(false);
            sentCount++;

            frameOffset += OtpConstants.FeatureReportDataSize;
            seq++;
        }

        _logger.LogDebug("Sent {Count} reports, returning programmingSequence={ProgSeq}", sentCount, programmingSequence);
        return programmingSequence;
    }

    /// <summary>
    /// Resets the state of YubiKey from reading.
    /// </summary>
    private async Task ResetStateAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[OtpConstants.FeatureReportSize];
        buffer[OtpConstants.FeatureReportSize - 1] = OtpConstants.DummyReportWrite;
        await WriteFeatureReportAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connection.Dispose();
        _disposed = true;
    }
}
