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

using NSubstitute;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class SmartCardBackendTests
{
    private readonly ISmartCardProtocol _protocol = Substitute.For<ISmartCardProtocol>();

    private SmartCardBackend CreateBackend(
        FirmwareVersion? version = null,
        byte initialProgSeq = 0) =>
        new(_protocol, version ?? new FirmwareVersion(5, 7, 0), initialProgSeq);

    private static ApduResponse MakeStatusResponse(byte progSeq, byte touchLo = 0, byte touchHi = 0)
    {
        // Response data: [fw_major, fw_minor, fw_patch, prog_seq, touch_lo, touch_hi] + SW 9000
        byte[] raw = [5, 7, 0, progSeq, touchLo, touchHi, 0x90, 0x00];
        return new ApduResponse(raw);
    }

    [Fact]
    public async Task WriteUpdateAsync_SendsCorrectApdu()
    {
        byte[] configData = [0x01, 0x02, 0x03];
        var response = MakeStatusResponse(progSeq: 1);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend(initialProgSeq: 0);
        await backend.WriteUpdateAsync(ConfigSlot.Config1, configData, CancellationToken.None);

        await _protocol.Received(1).TransmitAndReceiveAsync(
            Arg.Is<ApduCommand>(a =>
                a.Ins == YubiOtpConstants.InsConfig &&
                a.P1 == (byte)ConfigSlot.Config1 &&
                a.P2 == 0),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WriteUpdateAsync_ReturnsStatusBytes()
    {
        var response = MakeStatusResponse(progSeq: 1, touchLo: 0x03);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend(initialProgSeq: 0);
        var result = await backend.WriteUpdateAsync(ConfigSlot.Config1, ReadOnlyMemory<byte>.Empty, CancellationToken.None);

        // Status bytes are the data portion (without SW)
        Assert.Equal(6, result.Length);
        Assert.Equal(0x03, result.Span[4]); // touch_lo
    }

    [Fact]
    public async Task WriteUpdateAsync_ProgSeqNotIncremented_ThrowsInvalidOperation()
    {
        // prog_seq stays at 0 instead of incrementing to 1 — but 0 is rollover, so test with 5->5
        var response = MakeStatusResponse(progSeq: 5);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend(initialProgSeq: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => backend.WriteUpdateAsync(ConfigSlot.Config1, ReadOnlyMemory<byte>.Empty, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task WriteUpdateAsync_ProgSeqRolloverToZero_Succeeds()
    {
        // prog_seq rolling over to 0 is always accepted
        var response = MakeStatusResponse(progSeq: 0);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend(initialProgSeq: 255);
        var result = await backend.WriteUpdateAsync(ConfigSlot.Config1, ReadOnlyMemory<byte>.Empty, CancellationToken.None);

        Assert.True(result.Length >= 6);
    }

    [Fact]
    public async Task WriteUpdateAsync_Firmware5x_SkipsProgSeqValidation()
    {
        // Firmware 5.0.0-5.4.3 doesn't reliably update prog_seq
        var response = MakeStatusResponse(progSeq: 99);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend(version: new FirmwareVersion(5, 2, 0), initialProgSeq: 0);

        // Should not throw despite prog_seq mismatch
        var result = await backend.WriteUpdateAsync(ConfigSlot.Config1, ReadOnlyMemory<byte>.Empty, CancellationToken.None);
        Assert.True(result.Length >= 6);
    }

    [Fact]
    public async Task WriteUpdateAsync_EmptyResponse_FallsBackToReadStatus()
    {
        // First call returns empty data (just SW), second call returns full status
        var emptyResponse = new ApduResponse([], 0x9000 - 0x10000);
        var statusResponse = MakeStatusResponse(progSeq: 1);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(emptyResponse),
                Task.FromResult(statusResponse));

        var backend = CreateBackend(initialProgSeq: 0);
        var result = await backend.WriteUpdateAsync(ConfigSlot.Config1, ReadOnlyMemory<byte>.Empty, CancellationToken.None);

        // Should have called TransmitAndReceiveAsync twice (config + status)
        await _protocol.Received(2).TransmitAndReceiveAsync(
            Arg.Any<ApduCommand>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAndReceiveAsync_ReturnsExpectedLengthData()
    {
        byte[] responseData = [0x01, 0x02, 0x03, 0x04, 0x90, 0x00];
        var response = new ApduResponse(responseData);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend();
        var result = await backend.SendAndReceiveAsync(
            ConfigSlot.DeviceSerial,
            ReadOnlyMemory<byte>.Empty,
            4,
            CancellationToken.None);

        Assert.Equal(4, result.Length);
        Assert.Equal(0x01, result.Span[0]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_ResponseTooShort_ThrowsBadResponse()
    {
        // Only 2 data bytes, but we expect 4
        byte[] responseData = [0x01, 0x02, 0x90, 0x00];
        var response = new ApduResponse(responseData);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend();

        await Assert.ThrowsAsync<BadResponseException>(
            () => backend.SendAndReceiveAsync(
                ConfigSlot.DeviceSerial,
                ReadOnlyMemory<byte>.Empty,
                4,
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAndReceiveAsync_SendsCorrectSlotInP1()
    {
        byte[] responseData = new byte[22]; // 20 bytes + SW
        responseData[^2] = 0x90;
        responseData[^1] = 0x00;
        var response = new ApduResponse(responseData);

        _protocol.TransmitAndReceiveAsync(
                Arg.Any<ApduCommand>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var backend = CreateBackend();
        await backend.SendAndReceiveAsync(
            ConfigSlot.ChalHmac2,
            new byte[64],
            20,
            CancellationToken.None);

        await _protocol.Received(1).TransmitAndReceiveAsync(
            Arg.Is<ApduCommand>(a => a.P1 == (byte)ConfigSlot.ChalHmac2),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }
}
