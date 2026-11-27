// Copyright 2026 Yubico AB
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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.YubiOtp.Backend;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class YubiOtpSessionTests
{
    [Fact]
    public async Task CreateAsync_WithScpOnHid_RejectsBeforeStatusReadAndReleasesClaim()
    {
        var connection = Substitute.For<IOtpHidConnection>();
        connection.Type.Returns(ConnectionType.HidOtp);
        connection.FeatureReportSize.Returns(8);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<NotSupportedException>(() => YubiOtpSession.CreateAsync(
            connection, new SessionCreationOptions { ScpKeyParameters = scp }, TestContext.Current.CancellationToken));

        await connection.DidNotReceive().SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        await connection.DidNotReceive().ReceiveAsync(Arg.Any<CancellationToken>());
        connection.DidNotReceive().Dispose();
        await using var next = await RawOtpHidSession.CreateAsync(connection, TestContext.Current.CancellationToken);
        Assert.True(next.IsInitialized);
    }

    [Fact]
    public async Task CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached()
    {
        var connection = new UnsupportedConnection();

        _ = await Assert.ThrowsAsync<NotSupportedException>(
            () => YubiOtpSession.CreateAsync(
                connection,
                cancellationToken: TestContext.Current.CancellationToken));

        await using var probe = new ProbeSession(connection);
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = Substitute.For<IOtpHidConnection>();
        connection.Type.Returns(ConnectionType.HidOtp);
        connection.FeatureReportSize.Returns(8);
        connection.SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("session-init probe failure")));
        connection.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(
                new InvalidOperationException("session-init probe failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            YubiOtpSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: a failed CreateAsync must not dispose a connection it did not create.
        connection.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task CreateAsync_SmartCardBackendConstruction_PreservesProgrammingSequence()
    {
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Type.Returns(ConnectionType.SmartCard);
        connection.Transport.Returns(Transport.Usb);
        byte[] managementResponse = [.. "YubiKey 5.7.0"u8, 0x90, 0x00];
        byte[] otpResponse = [5, 7, 0, 9, 0, 0, 0x90, 0x00];
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult((ReadOnlyMemory<byte>)managementResponse),
                Task.FromResult((ReadOnlyMemory<byte>)otpResponse));

        using var session = await YubiOtpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var backend = Assert.IsType<SmartCardBackend>(
            typeof(YubiOtpSession)
                .GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(session));
        var programmingSequence = (byte)typeof(SmartCardBackend)
            .GetField("_lastProgSeq", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(backend)!;

        Assert.Equal(9, programmingSequence);
    }

    [Theory]
    [InlineData(2, VersionQualifierType.Final, false)]
    [InlineData(2, null, false)]
    [InlineData(2, VersionQualifierType.Beta, true)]
    [InlineData(0, VersionQualifierType.Final, true)]
    public async Task CreateUsingCachedInfoAsync_SeparatesAdmissionFromNumericFraming(
        int cachedMajor, VersionQualifierType? qualifier, bool admit)
    {
        var metadataVersion = new FirmwareVersion(cachedMajor, 1, 0);
        var info = DeviceInfo.CreateFromTlvs([], metadataVersion) with
        {
            // Custom IYubiKey providers may omit the qualifier despite its annotations.
            VersionQualifier = qualifier is { } type ? new VersionQualifier(metadataVersion, type, 0) : null!
        };
        var ct = TestContext.Current.CancellationToken;
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Type.Returns(ConnectionType.SmartCard);
        connection.Transport.Returns(Transport.Usb);
        byte[] rawStatus = [2, 1, 7, 9, 0, 0, 0x90, 0x00];
        byte[] writtenStatus = [2, 1, 7, 10, 0, 0, 0x90, 0x00];
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(rawStatus), Task.FromResult<ReadOnlyMemory<byte>>(writtenStatus));

        await using var session = await YubiOtpSession.CreateUsingCachedInfoAsync(connection, null, info, ct);
        Assert.Equal(cachedMajor == 0 ? new FirmwareVersion(2, 1, 7) : metadataVersion, session.FirmwareVersion);
        Assert.Equal(new FirmwareVersion(2, 1, 7), session.GetConfigState().FirmwareVersion);
        using var config = new HotpSlotConfiguration(new byte[20], 16);
        if (admit)
        {
            await session.PutConfigurationAsync(Slot.Two, config, cancellationToken: ct);
            await connection.Received(2).TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => session.PutConfigurationAsync(Slot.Two, config, cancellationToken: ct));
            await connection.Received(1).TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }
    }

    [Theory]
    [InlineData(2, false, true)]
    [InlineData(3, false, false)]
    [InlineData(2, true, true)]
    public async Task CreateAsync_ManagementSentinelUsesNumericOtpStatusForSequence(
        byte patch, bool cacheWithoutVersion, bool acceptsReset)
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Type.Returns(ConnectionType.SmartCard);
        connection.Transport.Returns(Transport.Usb);
        List<byte[]> sent = [];
        Queue<byte[]> replies = new([
            [.. "YubiKey 0.0.1"u8, 0x90, 0x00],
            [5, 4, patch, 9, 0, 0, 0x90, 0x00],
            [5, 4, patch, 0, 1, 0, 0x90, 0x00]
        ]);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent.Add(call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray());
                return Task.FromResult<ReadOnlyMemory<byte>>(replies.Dequeue());
            });
        // A custom cache may exist without usable firmware; fall back to applet initialization.
        var info = DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 4, (int)patch));
        await using var session = await YubiOtpSession.CreateUsingCachedInfoAsync(connection, null,
            cacheWithoutVersion ? info with { FirmwareVersion = null! } : null, ct);

        Assert.Equal(new FirmwareVersion(5, 4, (int)patch), session.FirmwareVersion);
        if (acceptsReset)
        {
            await session.DeleteSlotAsync(Slot.One, cancellationToken: ct);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.DeleteSlotAsync(Slot.One, cancellationToken: ct));
        }

        Assert.Equal(3, sent.Count);
        byte[] expectedWrite = [0x00, 0x01, 0x01, 0x00, 52, .. new byte[52], 0x00];
        Assert.Equal(expectedWrite, sent[2]);
    }

    [Fact]
    public async Task CalculateHmacSha1Async_TouchConfiguredSmartCard_DoesNotNotify()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var sessionLease = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(20)));

        _ = await session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task CalculateYubicoOtpAsync_TouchConfiguredSmartCard_DoesNotNotify()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var sessionLease = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(16)));

        _ = await session.CalculateYubicoOtpAsync(
            Slot.One, new byte[6], TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task CalculateYubicoOtpAsync_NonTouchSmartCard_DoesNotNotify()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x01, prompt);
        using var sessionLease = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(16)));

        _ = await session.CalculateYubicoOtpAsync(
            Slot.One,
            new byte[6],
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task PutConfigurationAsync_TouchConfiguredSmartCard_DoesNotNotify()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var _ = session;
        byte[] statusResponse = [5, 7, 0, 10, 0x05, 0, 0x90, 0x00];
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)statusResponse));
        using var configuration = new HmacSha1SlotConfiguration(new byte[20]);

        await session.PutConfigurationAsync(
            Slot.One,
            configuration,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task PutConfigurationAsync_ShortStatusFallback_DoesNotPublishAndAllowsNextValidWrite()
    {
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05);
        await using var lease = session;
        using var configuration = new HmacSha1SlotConfiguration(new byte[20]);
        var ct = TestContext.Current.CancellationToken;
        List<byte> instructions = [];
        Queue<byte[]> replies = new([
            [0x90, 0x00],
            [5, 7, 0, 0xFE, 0x00, 0x90, 0x00],
            [5, 7, 0, 10, 0x02, 0, 0x90, 0x00]
        ]);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                instructions.Add(call.ArgAt<ReadOnlyMemory<byte>>(0).Span[1]);
                return Task.FromResult<ReadOnlyMemory<byte>>(replies.Dequeue());
            });

        await Assert.ThrowsAsync<BadResponseException>(() => session.PutConfigurationAsync(
            Slot.Two, configuration, cancellationToken: ct));
        Assert.True(session.GetConfigState().IsConfigured(Slot.One));
        Assert.False(session.GetConfigState().IsConfigured(Slot.Two));

        await session.PutConfigurationAsync(Slot.Two, configuration, cancellationToken: ct);
        Assert.False(session.GetConfigState().IsConfigured(Slot.One));
        Assert.True(session.GetConfigState().IsConfigured(Slot.Two));
        Assert.Equal([(byte)0x01, (byte)0x03, (byte)0x01], instructions);
    }

    [Fact]
    public async Task CalculateHmacSha1Async_SmartCardTransportTimeout_PropagatesWithoutNotification()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var _ = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(new TimeoutException("Touch timed out.")));

        await Assert.ThrowsAsync<TimeoutException>(() => session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            TestContext.Current.CancellationToken));

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task CalculateHmacSha1Async_SmartCardCallerCancellation_PropagatesWithoutNotification()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var _ = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<ReadOnlyMemory<byte>>(cancellation.Token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            cancellation.Token));

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task CalculateHmacSha1Async_SmartCardResponseFailure_PropagatesWithoutNotification()
    {
        var prompt = new RecordingUserPresencePrompt();
        var (connection, session) = await CreateFakeSessionAsync(touchLow: 0x05, prompt);
        using var _ = session;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(2)));

        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateHmacSha1Async(
            Slot.One,
            new byte[] { 0x01 },
            TestContext.Current.CancellationToken));

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    /// <summary>
    /// Creates an initialized YubiOtpSession backed by a fake (substitute) SmartCard
    /// connection, with the SELECT/init sequence already consumed.
    /// </summary>
    private static async Task<(ISmartCardConnection Connection, YubiOtpSession Session)> CreateFakeSessionAsync(
        byte touchLow = 0,
        IUserPresencePrompt? userPresencePrompt = null,
        FirmwareVersion? firmware = null)
    {
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Type.Returns(ConnectionType.SmartCard);
        connection.Transport.Returns(Transport.Usb);
        byte[] managementResponse = [.. "YubiKey 5.7.0"u8, 0x90, 0x00];
        byte[] otpResponse = [5, 7, 0, 9, touchLow, 0, 0x90, 0x00];
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult((ReadOnlyMemory<byte>)managementResponse),
                Task.FromResult((ReadOnlyMemory<byte>)otpResponse));

        var session = await YubiOtpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = userPresencePrompt, FirmwareVersionOverride = firmware },
            cancellationToken: TestContext.Current.CancellationToken);

        connection.ClearReceivedCalls();

        return (connection, session);
    }

    private static byte[] CreateApduResponseData(int responseLength)
    {
        var response = new byte[responseLength + 2];
        response[^2] = 0x90;
        return response;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScanMap_WritesFullMapAndOptionalCode(bool reset)
    {
        var (connection, session) = await CreateFakeSessionAsync(firmware: new FirmwareVersion(3));
        await using var lease = session;
        byte[] code = [1, 2, 3, 4, 5, 6];
        byte[] map = reset ? new byte[45] : [.. Enumerable.Range(1, 45).Select(i => (byte)i)];
        byte[]? sent = null;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray();
                return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 3, 0, 0, 10, 0, 0, 0x90, 0x00 });
            });
        var ct = TestContext.Current.CancellationToken;
        if (reset)
        {
            await session.ResetScanMapAsync(code, ct);
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(() => session.SetScanMapAsync(map.AsMemory(0, 38), code, ct));
            Assert.Null(sent);
            await session.SetScanMapAsync(map, code, ct);
        }

        byte[] expected = [0x00, 0x01, 0x12, 0x00, 51, .. map, .. code, 0x00];
        Assert.Equal(expected, sent);
    }

    [Fact]
    public async Task ScanMap_OlderFirmwareRejectsBothOperationsWithoutIO()
    {
        var (connection, session) = await CreateFakeSessionAsync(firmware: new FirmwareVersion(2, 9, 9));
        await using var lease = session;
        var ct = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SetScanMapAsync(new byte[45], cancellationToken: ct));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.ResetScanMapAsync(cancellationToken: ct));
        await connection.DidNotReceive().TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public async Task UpdateConfigurationAsync_OnlyBlocksCodeChangesOnRestrictedFirmware(int patch, bool restricted)
    {
        var (connection, session) = await CreateFakeSessionAsync(firmware: new FirmwareVersion(4, 3, patch));
        await using var lease = session;
        using var configuration = new UpdateConfiguration();
        var ct = TestContext.Current.CancellationToken;
        byte[] currentCode = [1, 2, 3, 4, 5, 6];
        byte[] newCode = [6, 5, 4, 3, 2, 1];
        byte[] reply = [4, 3, (byte)patch, 10, 0, 0, 0x90, 0x00];
        byte[]? sent = null;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray();
                return Task.FromResult<ReadOnlyMemory<byte>>(reply);
            });

        if (restricted)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                session.UpdateConfigurationAsync(Slot.Two, configuration, newCode, currentCode, ct));
            Assert.Null(sent);
            newCode = (byte[])currentCode.Clone();
        }

        await session.UpdateConfigurationAsync(Slot.Two, configuration, newCode, currentCode, ct);
        Assert.NotNull(sent);
        Assert.Equal(newCode, sent[43..49]);
        Assert.Equal(currentCode, sent[57..63]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateConfigurationAsync_ZeroAndOmittedCodesDoNotChangeProtection(bool omitCurrentCode)
    {
        var (connection, session) = await CreateFakeSessionAsync(firmware: new FirmwareVersion(4, 3, 2));
        await using var lease = session;
        using var configuration = new UpdateConfiguration();
        var ct = TestContext.Current.CancellationToken;
        byte[] zeros = new byte[6];
        byte[]? sent = null;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray();
                return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 4, 3, 2, 10, 0, 0, 0x90, 0x00 });
            });

        await session.UpdateConfigurationAsync(Slot.Two, configuration,
            accessCode: omitCurrentCode ? zeros : default,
            currentAccessCode: omitCurrentCode ? default : zeros,
            cancellationToken: ct);

        Assert.NotNull(sent);
        Assert.Equal(0x05, sent[2]); // Update slot 2
        Assert.Equal(omitCurrentCode ? 52 : 58, sent[4]);
        Assert.Equal(zeros, sent[43..49]);
        if (!omitCurrentCode)
        {
            Assert.Equal(zeros, sent[57..63]);
        }
    }

    public class KeyLengthPreflightValidation
    {
        [Fact]
        public async Task CalculateYubicoOtpAsync_ChallengeTooShort_ThrowsBeforeAnyDeviceIO()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            using var _ = session;

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                session.CalculateYubicoOtpAsync(Slot.One, new byte[5], TestContext.Current.CancellationToken));
            Assert.Equal("challenge", ex.ParamName);

            await connection.DidNotReceive().TransmitAndReceiveAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CalculateYubicoOtpAsync_ChallengeTooLong_ThrowsBeforeAnyDeviceIO()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            using var _ = session;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                session.CalculateYubicoOtpAsync(Slot.One, new byte[7], TestContext.Current.CancellationToken));

            await connection.DidNotReceive().TransmitAndReceiveAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CalculateYubicoOtpAsync_ExactLengthChallenge_ProceedsToDeviceIO()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            using var _ = session;

            byte[] challengeResponse = new byte[18]; // 16 bytes + SW
            challengeResponse[^2] = 0x90;
            connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((ReadOnlyMemory<byte>)challengeResponse));

            var result = await session.CalculateYubicoOtpAsync(Slot.One, new byte[6], TestContext.Current.CancellationToken);

            Assert.Equal(16, result.Length);
            await connection.Received(1).TransmitAndReceiveAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }

        // Mode-specific constructors reject wrong key lengths before a configuration can reach the
        // session; SlotConfigurationTests covers those constructors independently. The challenge-length
        // tests above exercise the session/fake-connection path and establish no I/O for bad challenges,
        // not key validation or real-device SmartCard challenge support.
    }

    public class NdefUriEncoding
    {
        [Fact]
        public async Task SetNdefConfigurationAsync_UndefinedType_DoesNotSendWrite()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            await using var lease = session;
            var ct = TestContext.Current.CancellationToken;

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("ndefType", () =>
                session.SetNdefConfigurationAsync(Slot.Two, "payload", ndefType: (NdefType)0x56, cancellationToken: ct));

            await connection.DidNotReceive().TransmitAndReceiveAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void BuildNdefPayload_HttpsWwwUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("https://www.yubico.com/products", NdefType.Uri);

            // Prefix index 2 = "https://www."
            Assert.Equal(56, payload.Length);
            Assert.Equal((byte)NdefType.Uri, payload[1]);
            Assert.Equal(2, payload[2]); // prefix index

            var remaining = Encoding.UTF8.GetBytes("yubico.com/products");
            Assert.Equal(remaining.Length + 1, payload[0]); // length = prefix byte + remaining
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_HttpUri_CompressesCorrectPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("http://example.com", NdefType.Uri);

            // Prefix index 3 = "http://"
            Assert.Equal(3, payload[2]);

            var remaining = Encoding.UTF8.GetBytes("example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_HttpWwwUri_CompressesLongestPrefix()
        {
            // "http://www." (index 1) is longer than "http://" (index 3)
            var payload = YubiOtpSession.BuildNdefPayload("HTTP://WWW.Example.com", NdefType.Uri);

            Assert.Equal(1, payload[2]); // index 1 = "http://www."

            var remaining = Encoding.UTF8.GetBytes("Example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_TelUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("tel:+1234567890", NdefType.Uri);

            Assert.Equal(5, payload[2]); // index 5 = "tel:"

            var remaining = Encoding.UTF8.GetBytes("+1234567890");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_NoMatchingPrefix_UsesIndex0()
        {
            var payload = YubiOtpSession.BuildNdefPayload("custom://my-protocol", NdefType.Uri);

            Assert.Equal(0, payload[2]); // no prefix compression

            var remaining = Encoding.UTF8.GetBytes("custom://my-protocol");
            Assert.Equal(remaining.Length + 1, payload[0]); // length includes prefix byte
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_MailtoUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("mailto:user@example.com", NdefType.Uri);

            Assert.Equal(6, payload[2]); // index 6 = "mailto:"

            var remaining = Encoding.UTF8.GetBytes("user@example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_UrnNfcPrefix_CompressesCorrectly()
        {
            // "urn:nfc:" (index 35) should be preferred over "urn:" (index 19)
            var payload = YubiOtpSession.BuildNdefPayload("urn:nfc:ext:example", NdefType.Uri);

            Assert.Equal(35, payload[2]); // index 35 = "urn:nfc:"

            var remaining = Encoding.UTF8.GetBytes("ext:example");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public async Task SetNdefConfigurationAsync_NullContent_DoesNotSendWrite()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            await using var lease = session;
            var ct = TestContext.Current.CancellationToken;

            await Assert.ThrowsAsync<ArgumentNullException>("uri", () =>
                session.SetNdefConfigurationAsync(Slot.Two, null!, cancellationToken: ct)); // Deliberate runtime null.

            await connection.DidNotReceive().TransmitAndReceiveAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DisableNdefAsync_WritesZeroBlockFollowedByCurrentCode()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            await using var lease = session;
            var ct = TestContext.Current.CancellationToken;
            byte[] currentCode = [1, 2, 3, 4, 5, 6];
            byte[]? sent = null;
            connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    sent = call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray();
                    return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 5, 7, 0, 10, 0, 0, 0x90, 0x00 });
                });

            await session.DisableNdefAsync(Slot.Two, currentCode, ct);

            byte[] expected = [0x00, 0x01, 0x09, 0x00, 62, .. new byte[56], .. currentCode, 0x00];
            Assert.Equal(expected, sent);
        }

        [Fact]
        public void BuildNdefPayload_EmptyUri_UsesNoPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("", NdefType.Uri);

            Assert.Equal(1, payload[0]); // length = 1 (just the prefix byte)
            Assert.Equal(0, payload[2]); // no prefix
        }
    }

    public class NdefTextEncoding
    {
        [Fact]
        public void BuildNdefPayload_TextRecord_EncodesWithLanguageHeader()
        {
            var payload = YubiOtpSession.BuildNdefPayload("Hello World", NdefType.Text);

            Assert.Equal(56, payload.Length);
            Assert.Equal((byte)NdefType.Text, payload[1]);

            // Language header: [0x02]["en"]
            Assert.Equal(0x02, payload[2]); // language length
            Assert.Equal((byte)'e', payload[3]);
            Assert.Equal((byte)'n', payload[4]);

            var textBytes = Encoding.UTF8.GetBytes("Hello World");
            Assert.Equal(textBytes, payload[5..(5 + textBytes.Length)]);

            // Length = 1 (lang length byte) + 2 (lang code) + text length
            Assert.Equal(1 + 2 + textBytes.Length, payload[0]);
        }
    }

    public class HmacChallengeSemantics
    {
        [Fact]
        public async Task CalculateHmacSha1Async_ReturnsIndependentWritableResults()
        {
            var (connection, session) = await CreateFakeSessionAsync();
            await using var lease = session;
            var ct = TestContext.Current.CancellationToken;
            byte[] transportReply = CreateApduResponseData(20);
            transportReply.AsSpan(0, 20).Fill(0x5A);
            connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ReadOnlyMemory<byte>>(transportReply));

            Memory<byte> first = await session.CalculateHmacSha1Async(Slot.Two, ReadOnlyMemory<byte>.Empty, ct);
            Memory<byte> second = await session.CalculateHmacSha1Async(Slot.Two, ReadOnlyMemory<byte>.Empty, ct);

            Assert.Equal(20, first.Length);
            Assert.True(CryptographicOperations.FixedTimeEquals(first.Span, transportReply.AsSpan(0, 20)));
            CryptographicOperations.ZeroMemory(first.Span);
            Assert.True(first.Span.IndexOfAnyExcept((byte)0) < 0);
            Assert.True(CryptographicOperations.FixedTimeEquals(second.Span, transportReply.AsSpan(0, 20)));
            Assert.Equal(0x5A, transportReply[0]);
        }

        [Theory]
        [InlineData(new byte[0], (byte)0x00)]
        [InlineData(new byte[] { 0x41 }, (byte)0x00)]
        [InlineData(new byte[] { 0x41, 0x00 }, (byte)0x01)]
        [InlineData(new byte[] { 0x00 }, (byte)0x01)]
        [Trait("Audit", "YESDK-1659")]
        public async Task CalculateHmacSha1Async_ShortAndExplicitlyPaddedChallenges_SendIdenticalRequests(
            byte[] challenge, byte padByte)
        {
            var (connection, session) = await CreateFakeSessionAsync();
            using var sessionLease = session;
            List<byte[]> commands = [];
            connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    // Capture during transmission: the session clears its encoded challenge on return.
                    commands.Add(call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray());
                    return Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(20));
                });
            byte[] explicitChallenge = [.. challenge, .. Enumerable.Repeat(padByte, 64 - challenge.Length)];

            _ = await session.CalculateHmacSha1Async(Slot.Two, challenge, TestContext.Current.CancellationToken);
            _ = await session.CalculateHmacSha1Async(Slot.Two, explicitChallenge, TestContext.Current.CancellationToken);

            Assert.Equal(2, commands.Count);
            Assert.Equal(commands[0], commands[1]);
            byte[] expectedCommand = [0x00, 0x01, 0x38, 0x00, 0x40, .. explicitChallenge, 0x00];
            Assert.Equal(expectedCommand, commands[0]);
            Assert.Equal(challenge, explicitChallenge[..challenge.Length]);
        }

        [Theory]
        [InlineData(63, (byte)0x00)]
        [InlineData(63, (byte)0x01)]
        [InlineData(64, (byte)0x00)]
        [InlineData(64, (byte)0x01)]
        [Trait("Audit", "YESDK-1659")]
        public async Task CalculateHmacSha1Async_AtShortAndFixedLengthBoundary_PreservesInputBytes(
            int length, byte lastByte)
        {
            var (connection, session) = await CreateFakeSessionAsync();
            using var sessionLease = session;
            byte[]? command = null;
            connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    command = call.ArgAt<ReadOnlyMemory<byte>>(0).ToArray();
                    return Task.FromResult((ReadOnlyMemory<byte>)CreateApduResponseData(20));
                });
            byte[] challenge = [.. Enumerable.Range(1, length - 1).Select(i => (byte)i), lastByte];
            byte[] originalChallenge = [.. challenge];

            _ = await session.CalculateHmacSha1Async(Slot.One, challenge, TestContext.Current.CancellationToken);

            Assert.NotNull(command);
            Assert.Equal(70, command.Length);
            Assert.Equal(new byte[] { 0x00, 0x01, 0x30, 0x00, 0x40 }, command[..5]);
            Assert.Equal(originalChallenge, command[5..(5 + length)]);
            if (length == 63)
            {
                Assert.Equal(lastByte == 0x00 ? 0x01 : 0x00, command[68]);
            }
            Assert.Equal(0x00, command[69]);
            Assert.Equal(originalChallenge, challenge);
        }
    }

    public class HmacChallengePadding
    {
        [Fact]
        public void PadHmacChallenge_ShortChallenge_PadsTo64Bytes()
        {
            byte[] challenge = [0x01, 0x02, 0x03];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(64, padded.Length);
            Assert.Equal(0x01, padded[0]);
            Assert.Equal(0x02, padded[1]);
            Assert.Equal(0x03, padded[2]);
        }

        [Fact]
        public void PadHmacChallenge_PadByteDiffersFromLastByte_NonZeroLastByte()
        {
            byte[] challenge = [0x01, 0x02, 0x05]; // last byte = 0x05 (non-zero)

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            // Pad byte should be 0x00 (differs from 0x05)
            Assert.Equal(0x00, padded[3]);
            Assert.All(padded[3..], b => Assert.Equal(0x00, b));
        }

        [Fact]
        public void PadHmacChallenge_PadByteDiffersFromLastByte_ZeroLastByte()
        {
            byte[] challenge = [0x01, 0x02, 0x00]; // last byte = 0x00

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            // Pad byte should be 0x01 (differs from 0x00)
            Assert.Equal(0x01, padded[3]);
            Assert.All(padded[3..], b => Assert.Equal(0x01, b));
        }

        /// <summary>Same as ykman: an empty challenge does not end in 0x00, so it pads with 0x00.</summary>
        [Fact]
        public void PadHmacChallenge_EmptyChallenge_PadsWithZero()
        {
            byte[] challenge = [];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(64, padded.Length);
            Assert.All(padded, b => Assert.Equal(0x00, b));
        }

        [Fact]
        public void PadHmacChallenge_FullChallenge_NoPadding()
        {
            var challenge = new byte[64];
            challenge[0] = 0xAA;
            challenge[63] = 0xBB;

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(64, padded.Length);
            Assert.Equal(0xAA, padded[0]);
            Assert.Equal(0xBB, padded[63]);
        }

        [Fact]
        public void PadHmacChallenge_SingleByte_PadsRemaining()
        {
            byte[] challenge = [0xFF];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(0xFF, padded[0]);
            // Last byte is 0xFF (non-zero), pad with 0x00
            Assert.All(padded[1..], b => Assert.Equal(0x00, b));
        }
    }

    public class SlotMapping
    {
        [Theory]
        [InlineData(Slot.One, SlotOperation.Configure, ConfigSlot.Config1)]
        [InlineData(Slot.Two, SlotOperation.Configure, ConfigSlot.Config2)]
        [InlineData(Slot.One, SlotOperation.Update, ConfigSlot.Update1)]
        [InlineData(Slot.Two, SlotOperation.Update, ConfigSlot.Update2)]
        [InlineData(Slot.One, SlotOperation.Ndef, ConfigSlot.Ndef1)]
        [InlineData(Slot.Two, SlotOperation.Ndef, ConfigSlot.Ndef2)]
        [InlineData(Slot.One, SlotOperation.ChallengeHmac, ConfigSlot.ChalHmac1)]
        [InlineData(Slot.Two, SlotOperation.ChallengeHmac, ConfigSlot.ChalHmac2)]
        [InlineData(Slot.One, SlotOperation.ChallengeYubicoOtp, ConfigSlot.ChalYubico1)]
        [InlineData(Slot.Two, SlotOperation.ChallengeYubicoOtp, ConfigSlot.ChalYubico2)]
        public void Map_ValidSlotAndOperation_ReturnsCorrectConfigSlot(
            Slot slot, SlotOperation operation, ConfigSlot expected)
        {
            var result = slot.Map(operation);
            Assert.Equal(expected, result);
        }
    }

    private sealed class UnsupportedConnection : IConnection
    {
        public ConnectionType Type => ConnectionType.Unknown;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingUserPresencePrompt(Action? onRequested = null) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)> Resolved { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            onRequested?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ProbeSession(IConnection connection) : ApplicationSession(connection);
}