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
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.YubiOtp.Backend;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class HidBackendTests
{
    private readonly IOtpHidProtocol _protocol = Substitute.For<IOtpHidProtocol>();

    private HidBackend CreateBackend() => new(_protocol);

    /// <summary>
    /// Creates response data with a valid CRC appended.
    /// </summary>
    private static byte[] MakeResponseWithCrc(byte[] data)
    {
        var result = new byte[data.Length + 2];
        data.CopyTo(result, 0);

        // Calculate CRC complement and append as little-endian
        var crc = ChecksumUtils.CalculateCrc(data, data.Length);

        // We need the CRC such that CheckCrc(result, result.Length) == ValidResidue
        // The standard approach: append ~crc (complement)
        result[data.Length] = (byte)(crc & 0xFF);
        result[data.Length + 1] = (byte)(crc >> 8);

        // Verify our construction is correct
        // Actually, let's compute it properly. The CRC of data+crc_bytes should equal ValidResidue.
        // We need to find crc_lo, crc_hi such that CalculateCrc(result, result.Length) == 0xF0B8
        // The standard CRC-CCITT approach: append the complement of the CRC
        var fullCrc = (ushort)(0xFFFF & ~ChecksumUtils.CalculateCrc(data, data.Length));
        result[data.Length] = (byte)(fullCrc & 0xFF);
        result[data.Length + 1] = (byte)(fullCrc >> 8);

        return result;
    }

    [Fact]
    public async Task WriteUpdateAsync_DelegatesToProtocol()
    {
        byte[] statusBytes = [5, 7, 0, 1, 0x03, 0x00];
        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(statusBytes));

        var backend = CreateBackend();
        var result = await backend.WriteUpdateAsync(ConfigSlot.Config1, new byte[52], CancellationToken.None);

        Assert.Equal(6, result.Length);
        await _protocol.Received(1).SendAndReceiveAsync(
            (byte)ConfigSlot.Config1,
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAndReceiveAsync_ValidCrc_ReturnsDataWithoutCrc()
    {
        byte[] hmacResult = [0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44,
                             0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xAA, 0xBB,
                             0xCC, 0xDD, 0xEE, 0xFF];
        var responseWithCrc = MakeResponseWithCrc(hmacResult);

        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(responseWithCrc));

        var backend = CreateBackend();
        var result = await backend.SendAndReceiveAsync(
            ConfigSlot.ChalHmac1,
            new byte[64],
            20,
            UserPresenceNotification.None,
            cancellationToken: CancellationToken.None);

        Assert.Equal(20, result.Length);
        Assert.Equal(0xAA, result.Span[0]);
        Assert.Equal(0xFF, result.Span[19]);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WithPresenceContext_DefersResolutionUntilValidationCompletes()
    {
        byte[] responseWithCrc = MakeResponseWithCrc(new byte[20]);
        var prompt = Substitute.For<IUserPresencePrompt>();
        var context = new UserPresenceContext
        {
            Basis = UserPresenceBasis.DeviceWaiting,
            Application = "YubiOTP",
            Scope = "One"
        };
        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                UserPresenceNotification notification = callInfo.ArgAt<UserPresenceNotification>(2);
                await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, CancellationToken.None);
                return (ReadOnlyMemory<byte>)responseWithCrc;
            });
        var backend = new HidBackend(_protocol);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        _ = await backend.SendAndReceiveAsync(
            ConfigSlot.ChalHmac1,
            new byte[64],
            20,
            notification,
            TestContext.Current.CancellationToken);

        await _protocol.Received(1).SendAndReceiveAsync(
            (byte)ConfigSlot.ChalHmac1,
            Arg.Any<ReadOnlyMemory<byte>>(),
            notification,
            Arg.Any<CancellationToken>());
        await prompt.Received(1).OnUserPresenceRequestedAsync(context, Arg.Any<CancellationToken>());
        await prompt.Received(1).OnUserPresenceResolvedAsync(
            context,
            UserPresenceOutcome.Completed,
            CancellationToken.None);
    }

    [Fact]
    public async Task SendAndReceiveAsync_InvalidCrc_ThrowsBadResponse()
    {
        // Create response with bad CRC
        byte[] data = new byte[22]; // 20 data + 2 bad CRC bytes
        data[20] = 0xFF;
        data[21] = 0xFF;

        var prompt = Substitute.For<IUserPresencePrompt>();
        var context = new UserPresenceContext
        {
            Basis = UserPresenceBasis.DeviceWaiting,
            Application = "YubiOTP",
            Scope = "One"
        };
        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                UserPresenceNotification notification = callInfo.ArgAt<UserPresenceNotification>(2);
                await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, CancellationToken.None);
                return (ReadOnlyMemory<byte>)data;
            });

        var backend = new HidBackend(_protocol);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        await Assert.ThrowsAsync<BadResponseException>(
            () => backend.SendAndReceiveAsync(
                ConfigSlot.ChalHmac1,
                new byte[64],
                20,
                notification,
                CancellationToken.None).AsTask());

        await prompt.Received(1).OnUserPresenceResolvedAsync(
            context,
            UserPresenceOutcome.Failed,
            CancellationToken.None);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenResolutionThrowsAfterValidation_PropagatesResolutionException()
    {
        byte[] responseWithCrc = MakeResponseWithCrc(new byte[20]);
        var expected = new InvalidOperationException("resolution failed");
        var prompt = new ThrowingPrompt(resolutionException: expected);
        UserPresenceContext context = CreateContext();
        ConfigurePresenceResponse(responseWithCrc);
        var backend = new HidBackend(_protocol);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendAndReceiveAsync(
                ConfigSlot.ChalHmac1,
                new byte[64],
                20,
                notification,
                TestContext.Current.CancellationToken).AsTask());

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenValidationAndResolutionThrow_PreservesValidationException()
    {
        byte[] invalidResponse = new byte[22];
        var prompt = new ThrowingPrompt(
            resolutionException: new InvalidOperationException("resolution failed"));
        UserPresenceContext context = CreateContext();
        ConfigurePresenceResponse(invalidResponse);
        var backend = new HidBackend(_protocol);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        BadResponseException actual = await Assert.ThrowsAsync<BadResponseException>(() =>
            backend.SendAndReceiveAsync(
                ConfigSlot.ChalHmac1,
                new byte[64],
                20,
                notification,
                TestContext.Current.CancellationToken).AsTask());

        Assert.Equal("Invalid CRC in OTP HID response.", actual.Message);
    }

    [Fact]
    public async Task SendAndReceiveAsync_WhenRequestThrows_DoesNotResolve()
    {
        byte[] responseWithCrc = MakeResponseWithCrc(new byte[20]);
        var expected = new InvalidOperationException("request failed");
        var prompt = new ThrowingPrompt(requestException: expected);
        UserPresenceContext context = CreateContext();
        ConfigurePresenceResponse(responseWithCrc);
        var backend = new HidBackend(_protocol);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, context);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendAndReceiveAsync(
                ConfigSlot.ChalHmac1,
                new byte[64],
                20,
                notification,
                TestContext.Current.CancellationToken).AsTask());

        Assert.Same(expected, actual);
        Assert.Equal(0, prompt.ResolutionCount);
    }

    [Fact]
    public async Task SendAndReceiveAsync_ResponseTooShort_ThrowsBadResponse()
    {
        // Response shorter than expectedLength + 2 (CRC)
        byte[] data = [0x01, 0x02, 0x03];

        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(data));

        var backend = CreateBackend();

        await Assert.ThrowsAsync<BadResponseException>(
            () => backend.SendAndReceiveAsync(
                ConfigSlot.DeviceSerial,
                ReadOnlyMemory<byte>.Empty,
                4,
                UserPresenceNotification.None,
                cancellationToken: CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAndReceiveAsync_SendsCorrectSlotByte()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var responseWithCrc = MakeResponseWithCrc(data);

        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(responseWithCrc));

        var backend = CreateBackend();
        await backend.SendAndReceiveAsync(
            ConfigSlot.ChalHmac2,
            new byte[64],
            4,
            UserPresenceNotification.None,
            cancellationToken: CancellationToken.None);

        await _protocol.Received(1).SendAndReceiveAsync(
            (byte)ConfigSlot.ChalHmac2,
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<UserPresenceNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAndReceiveAsync_SerialRead_ValidCrc_ReturnsFourBytes()
    {
        byte[] serialData = [0x00, 0x12, 0xD6, 0x87]; // Serial number bytes
        var responseWithCrc = MakeResponseWithCrc(serialData);

        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(responseWithCrc));

        var backend = CreateBackend();
        var result = await backend.SendAndReceiveAsync(
            ConfigSlot.DeviceSerial,
            ReadOnlyMemory<byte>.Empty,
            4,
            UserPresenceNotification.None,
            cancellationToken: CancellationToken.None);

        Assert.Equal(4, result.Length);
        Assert.Equal(0x00, result.Span[0]);
        Assert.Equal(0x87, result.Span[3]);
    }

    private void ConfigurePresenceResponse(byte[] response)
    {
        _protocol.SendAndReceiveAsync(
                Arg.Any<byte>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<UserPresenceNotification>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                UserPresenceNotification notification = callInfo.ArgAt<UserPresenceNotification>(2);
                await notification.RequestAsync(UserPresenceBasis.DeviceWaiting, CancellationToken.None);
                return (ReadOnlyMemory<byte>)response;
            });
    }

    private static UserPresenceContext CreateContext() => new()
    {
        Basis = UserPresenceBasis.DeviceWaiting,
        Application = "YubiOTP",
        Scope = "One"
    };

    private sealed class ThrowingPrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {
        public int ResolutionCount { get; private set; }

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken) => requestException is null
                ? default
                : ValueTask.FromException(requestException);

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            ResolutionCount++;
            return resolutionException is null
                ? default
                : ValueTask.FromException(resolutionException);
        }
    }
}