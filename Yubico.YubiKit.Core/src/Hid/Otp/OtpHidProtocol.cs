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
    /// Uses exponential backoff like legacy C# WaitFor.
    /// </summary>
    private async Task<(ReadOnlyMemory<byte> Report, bool HasData)> WaitForReadyToReadAsync(
        int programmingSequence,
        CancellationToken cancellationToken)
    {
        const int timeLimitMs = 1023; // Same as legacy SDK short timeout
        var sleepDurationMs = 1;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Read immediately first, then delay on subsequent iterations
        while (stopwatch.ElapsedMilliseconds < timeLimitMs)
        {
            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            Console.WriteLine($"[OTP DEBUG] WaitForRead: status=0x{statusByte:X2}, data={Convert.ToHexString(report.Span)}");
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
                await Task.Delay(sleepDurationMs, cancellationToken).ConfigureAwait(false);
                sleepDurationMs = Math.Min(sleepDurationMs * 2, 64); // Cap at 64ms
                continue;
            }

            // WritePending or other flags - device is busy, keep polling
            _logger.LogTrace("Device busy (statusByte=0x{Status:X2}), continuing poll", statusByte);
            await Task.Delay(sleepDurationMs, cancellationToken).ConfigureAwait(false);
            sleepDurationMs = Math.Min(sleepDurationMs * 2, 64);
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
    /// Waits for the ReadPending flag to be set, indicating device has response data.
    /// Returns the first report that indicates completion (either with data or status-only).
    /// </summary>
    private async Task<(ReadOnlyMemory<byte> Report, bool IsDataPacket)> WaitForReadPendingAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int timeLimitMs = 1023; // Same as legacy SDK short timeout
        var sleepDurationMs = 1;

        while (stopwatch.ElapsedMilliseconds < timeLimitMs)
        {
            await Task.Delay(sleepDurationMs, cancellationToken).ConfigureAwait(false);
            sleepDurationMs *= 2; // Exponential backoff like legacy SDK

            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            _logger.LogTrace("WaitForReadPending: statusByte=0x{Status:X2}, report={Report}", 
                statusByte, Convert.ToHexString(report.Span));

            // Check for touch pending
            if ((statusByte & OtpConstants.ResponseTimeoutWaitFlag) != 0)
            {
                _logger.LogDebug("Touch pending detected, waiting longer...");
                await WaitForTouchCompleteAsync(cancellationToken).ConfigureAwait(false);
                // After touch completes, read again to get actual response
                var touchReport = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
                var touchStatus = touchReport.Span[OtpConstants.FeatureReportDataSize];
                return (touchReport, (touchStatus & OtpConstants.ResponsePendingFlag) != 0);
            }

            // Check if ReadPending is set - this is the first data packet (seq=0)
            if ((statusByte & OtpConstants.ResponsePendingFlag) != 0)
            {
                _logger.LogDebug("ReadPending flag detected after {Ms}ms, seq={Seq}", 
                    stopwatch.ElapsedMilliseconds, statusByte & OtpConstants.SequenceMask);
                return (report, true);  // isDataPacket = true
            }
            
            // Status-only response (no data, but command was processed)
            if (statusByte == 0)
            {
                var progSeq = report.Span[OtpConstants.StatusOffsetProgSeq];
                _logger.LogDebug("Got status-only response: progSeq={ProgSeq}", progSeq);
                return (report, false);  // isDataPacket = false
            }
        }

        throw new TimeoutException($"Timeout waiting for ReadPending flag after {stopwatch.ElapsedMilliseconds}ms");
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
    /// </summary>
    private async Task AwaitReadyToWriteAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            if ((report.Span[OtpConstants.FeatureReportDataSize] & OtpConstants.SlotWriteFlag) == 0)
            {
                return;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Timeout waiting for YubiKey to become ready to receive");
    }

    /// <summary>
    /// Determines if a packet should be sent (all-zero packets are skipped except first and last).
    /// NOTE: Disabled on Linux - send all packets for compatibility.
    /// </summary>
    private static bool ShouldSend(ReadOnlySpan<byte> packet, byte seq)
    {
        // Always send all packets - skipping doesn't work reliably on all platforms
        return true;
        
        // Original optimization (disabled):
        // if (seq == 0 || seq == 9)
        // {
        //     return true;
        // }
        // for (var i = 0; i < 7; i++)
        // {
        //     if (packet[i] != 0)
        //     {
        //         return true;
        //     }
        // }
        // return false;
    }

    /// <summary>
    /// Packs and sends one 70-byte frame as multiple 8-byte feature reports.
    /// </summary>
    private async Task<int> SendFrameAsync(byte slot, byte[] payload, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[OTP DEBUG] SendFrameAsync: slot=0x{slot:X2}, payloadLen={payload.Length}");
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

        Console.WriteLine($"[OTP DEBUG] Frame (70 bytes): {Convert.ToHexString(frame.AsSpan(60, 10))}... (last 10 bytes)");
        _logger.LogTrace("Frame (70 bytes): {Frame}", Convert.ToHexString(frame));

        // Get current programming sequence
        var statusReport = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
        var programmingSequence = statusReport.Span[OtpConstants.StatusOffsetProgSeq];
        
        Console.WriteLine($"[OTP DEBUG] Initial status: {Convert.ToHexString(statusReport.Span)}, progSeq={programmingSequence}");
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

            if (ShouldSend(report, seq))
            {
                // Set sequence with write flag
                report[OtpConstants.FeatureReportDataSize] = (byte)(OtpConstants.SlotWriteFlag | seq);
                await AwaitReadyToWriteAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("Sending report #{Count} (seq={Seq}): {Report}", 
                    sentCount, seq, Convert.ToHexString(report));
                await WriteFeatureReportAsync(report, cancellationToken).ConfigureAwait(false);
                sentCount++;
            }

            frameOffset += OtpConstants.FeatureReportDataSize;
            seq++;
        }

        Console.WriteLine($"[OTP DEBUG] Sent {sentCount} reports, progSeq={programmingSequence}");
        _logger.LogDebug("Sent {Count} reports, returning programmingSequence={ProgSeq}", sentCount, programmingSequence);
        return programmingSequence;
    }

    /// <summary>
    /// Reads one frame response.
    /// </summary>
    /// <param name="programmingSequence">The programming sequence before the command was sent.</param>
    /// <param name="firstReport">The first report already read by WaitForReadPending.</param>
    /// <param name="isDataPacket">True if firstReport is a data packet, false if status-only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<ReadOnlyMemory<byte>> ReadFrameAsync(
        int programmingSequence, 
        ReadOnlyMemory<byte> firstReport, 
        bool isDataPacket,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        byte seq = 0;
        var needsTouch = false;
        var readCount = 0;

        _logger.LogDebug("ReadFrameAsync starting, programmingSequence={ProgSeq}, isDataPacket={IsData}", 
            programmingSequence, isDataPacket);

        // Process the first report that was already read by WaitForReadPending
        ReadOnlyMemory<byte> report = firstReport;
        var processFirstReport = true;

        while (true)
        {
            if (!processFirstReport)
            {
                report = await ReadFeatureReportAsync(cancellationToken).ConfigureAwait(false);
            }
            processFirstReport = false;
            
            readCount++;
            var statusByte = report.Span[OtpConstants.FeatureReportDataSize];

            _logger.LogTrace("ReadFrameAsync: report #{Count}, statusByte=0x{Status:X2}, data={Data}",
                readCount, statusByte, Convert.ToHexString(report.Span));

            if ((statusByte & OtpConstants.ResponsePendingFlag) != 0)
            {
                // Response packet
                var packetSeq = statusByte & OtpConstants.SequenceMask;
                _logger.LogTrace("Response packet, packetSeq={PacketSeq}, expected={Expected}", packetSeq, seq);
                
                if (seq == packetSeq)
                {
                    // Correct sequence
                    stream.Write(report.Span[..OtpConstants.FeatureReportDataSize]);
                    seq++;
                }
                else if (packetSeq == 0 && seq > 0)
                {
                    // Transmission complete (got seq=0 again after receiving data)
                    await ResetStateAsync(cancellationToken).ConfigureAwait(false);
                    var rawResponse = stream.ToArray();
                    _logger.LogDebug("{Length} bytes read over HID in {Count} reports: {Response}", 
                        rawResponse.Length, readCount, Convert.ToHexString(rawResponse));
                    return ExtractPayloadFromFrame(rawResponse);
                }
            }
            else if ((statusByte & ~OtpConstants.SequenceMask) == 0)
            {
                // Status response (no ResponsePending, no WritePending, no TouchPending)
                var nextSeq = report.Span[OtpConstants.StatusOffsetProgSeq];
                var touchLow = report.Span[OtpConstants.StatusOffsetTouchLow];
                
                _logger.LogDebug("Status response: nextSeq={NextSeq}, programmingSequence={ProgSeq}, streamLen={StreamLen}, touchLow=0x{TouchLow:X2}",
                    nextSeq, programmingSequence, stream.Length, touchLow);
                
                // If we have data in the buffer, we successfully received data and this is end-of-chain
                if (stream.Length > 0)
                {
                    var rawResponse = stream.ToArray();
                    _logger.LogDebug("{Length} bytes read (end of chain): {Response}", 
                        rawResponse.Length, Convert.ToHexString(rawResponse));
                    return ExtractPayloadFromFrame(rawResponse);
                }

                if (nextSeq == programmingSequence + 1 ||
                    (nextSeq == 0 && programmingSequence > 0 &&
                     (touchLow & OtpConstants.ConfigSlotsProgrammedMask) == 0))
                {
                    // Sequence updated, return status
                    var status = report.Slice(1, 6).ToArray();
                    _logger.LogDebug("HID programming sequence updated. New status: {Status}", Convert.ToHexString(status));
                    return status;
                }

                if (needsTouch)
                {
                    throw new TimeoutException("Timed out waiting for touch");
                }

                throw new InvalidOperationException(
                    $"No data returned from YubiKey (progSeq={programmingSequence}, nextSeq={nextSeq}, statusByte=0x{statusByte:X2})");
            }
            else
            {
                // Need to wait
                _logger.LogTrace("Waiting: statusByte=0x{Status:X2}", statusByte);
                if ((statusByte & OtpConstants.ResponseTimeoutWaitFlag) != 0)
                {
                    needsTouch = true;
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(20, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Extracts and validates the response payload from a raw frame.
    /// </summary>
    /// <param name="frame">The raw 70-byte frame data.</param>
    /// <returns>The extracted payload data.</returns>
    private ReadOnlyMemory<byte> ExtractPayloadFromFrame(byte[] frame)
    {
        if (frame.Length < OtpConstants.FrameSize)
        {
            _logger.LogWarning("Frame too short: {Length} bytes, expected {Expected}", 
                frame.Length, OtpConstants.FrameSize);
            // Return what we have in the data portion
            return frame.AsMemory(0, Math.Min(frame.Length, OtpConstants.SlotDataSize));
        }

        // Get response length from byte 64
        var responseLength = frame[OtpConstants.ResponseLengthOffset];
        
        _logger.LogDebug("Frame response length: {Length}, frameLen={FrameLen}", responseLength, frame.Length);

        if (responseLength > OtpConstants.SlotDataSize)
        {
            _logger.LogWarning("Invalid response length {Length}, capping at {Max}", 
                responseLength, OtpConstants.SlotDataSize);
            responseLength = OtpConstants.SlotDataSize;
        }

        // Verify CRC over payload + length byte
        var crcSpan = frame.AsSpan(0, OtpConstants.ResponseLengthOffset + 1);
        var storedCrc = BinaryPrimitives.ReadUInt16LittleEndian(
            frame.AsSpan(OtpConstants.ResponseCrcOffset, 2));
        var calculatedCrc = ChecksumUtils.CalculateCrc(crcSpan.ToArray(), crcSpan.Length);
        
        // Include stored CRC in calculation for residue check
        var fullCrc = ChecksumUtils.CalculateCrc(
            frame.AsSpan(0, OtpConstants.ResponseCrcOffset + 2).ToArray(),
            OtpConstants.ResponseCrcOffset + 2);

        _logger.LogDebug("CRC check: stored=0x{Stored:X4}, calculated=0x{Calculated:X4}, residue=0x{Residue:X4}",
            storedCrc, calculatedCrc, fullCrc);

        // Valid CRC residue is 0xF0B8
        if (fullCrc != ChecksumUtils.ValidResidue)
        {
            _logger.LogWarning("CRC mismatch! Residue=0x{Residue:X4}, expected=0x{Expected:X4}", 
                fullCrc, ChecksumUtils.ValidResidue);
            // Return data anyway - some implementations don't check CRC
        }

        return frame.AsMemory(0, responseLength);
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
