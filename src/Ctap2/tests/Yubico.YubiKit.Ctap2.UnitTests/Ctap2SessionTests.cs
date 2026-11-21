using FluentAssertions;
using NSubstitute;
using System.Buffers.Binary;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Ctap2.Backend;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.UnitTests;

public class Ctap2SessionTests
{
    [Fact]
    public async Task CreateAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Ctap2Session.CreateAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_UnsupportedConnectionType_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedConnection = Substitute.For<IConnection>();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => Ctap2Session.CreateAsync(unsupportedConnection, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Transport.Returns(Transport.Usb);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(
                new InvalidOperationException("session-init probe failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Ctap2Session.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        // The assertion covers this failed probe's connection lifetime, not device interoperability.
        connection.DidNotReceive().Dispose();
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_UsbBefore58_ThrowsNotSupportedException()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
            Ctap2Session.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 7, 2)));

        exception.Message.Should().Contain("firmware 5.8.0");
        exception.Message.Should().Contain("IFidoHidConnection");
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_Usb58_Succeeds()
    {
        Ctap2Session.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(5, 8, 0));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 255, 255)]
    public void EnsureSmartCardTransportSupported_UsbSentinelFirmware_Succeeds(int major, int minor, int patch)
    {
        Ctap2Session.EnsureSmartCardTransportSupported(Transport.Usb, new FirmwareVersion(major, minor, patch));
    }

    [Fact]
    public void EnsureSmartCardTransportSupported_ReportedNfcBefore58_Succeeds()
    {
        Ctap2Session.EnsureSmartCardTransportSupported(Transport.Nfc, new FirmwareVersion(5, 0, 0));
    }

    [Fact]
    public async Task DisposeAsync_OwnedConnection_UsesAsyncDisposalAndIsIdempotent()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00]);
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

        await session.DisposeAsync();
        await session.DisposeAsync();

        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    [Fact]
    public async Task DisposeAsync_OwnedConnectionFailure_IsSharedAndLeavesSessionDisposed()
    {
        var expected = new InvalidOperationException("async connection teardown failed");
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00])
        {
            DisposeAsyncException = expected
        };
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

        Exception? first = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? second = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeAsync_WhileOwnedConnectionTeardownIsPaused_GetInfoAsyncThrowsObjectDisposedException()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00])
        {
            PauseAsyncDisposal = true
        };
        var device = new SingleConnectionYubiKey(connection);
        var session = await device.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            TestContext.Current.CancellationToken);

        Task disposal = session.DisposeAsync().AsTask();
        await connection.AsyncDisposalStarted.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => session.GetInfoAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            connection.ResumeAsyncDisposal();
            await disposal;
        }
    }

    [Fact]
    public async Task MakeCredentialAsync_AfterDisposal_InvalidArgumentsThrowObjectDisposedBeforeValidation()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00]);
        var session = await Ctap2Session.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.MakeCredentialAsync(
                default,
                null!,
                null!,
                null!,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(Ctap2Session).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    /// <summary>up=false refuses the makeCredential request and prompt; session initialization I/O occurs first.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1618")]
    public async Task MakeCredentialAsync_FalseUserPresence_ThrowsBeforeSendingOrPrompting()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.MakeCredentialAsync(
                new byte[32],
                new PublicKeyCredentialRpEntity("example.com"),
                new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                [PublicKeyCredentialParameters.CreateES256()],
                new MakeCredentialOptions { UserPresence = false },
                TestContext.Current.CancellationToken));

        Assert.Equal("options", ex.ParamName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
        Assert.Empty(prompt.Requested);
    }

    [Fact]
    public async Task MakeCredentialAsync_WithSmartCardPrompt_NotifiesRequiredRpScopeAndTimeout()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        CtapException exception = await Assert.ThrowsAsync<CtapException>(() =>
            session.MakeCredentialAsync(
                new byte[32],
                new PublicKeyCredentialRpEntity("example.com"),
                new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                [PublicKeyCredentialParameters.CreateES256()],
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.UserActionTimeout, exception.Status);
        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Basis);
        Assert.Equal("FIDO2", requested.Application);
        Assert.Equal(UserPresenceOperations.Ctap2.MakeCredential, requested.Operation);
        Assert.Equal("example.com", requested.Scope);
        var resolved = Assert.Single(prompt.Resolved);
        Assert.Same(requested, resolved.Context);
        Assert.Equal(UserPresenceOutcome.TimedOut, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);
    }

    [Fact]
    public async Task MakeCredentialAsync_InsideIntentScope_ReportsIntentWithOperation()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        using (UserPresenceIntent.BeginScope("register your work passkey"))
        {
            _ = await Assert.ThrowsAsync<CtapException>(() =>
                session.MakeCredentialAsync(
                    new byte[32],
                    new PublicKeyCredentialRpEntity("example.com"),
                    new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                    [PublicKeyCredentialParameters.CreateES256()],
                    cancellationToken: TestContext.Current.CancellationToken));
        }

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal("register your work passkey", requested.Intent);
        Assert.Equal(UserPresenceOperations.Ctap2.MakeCredential, requested.Operation);
        Assert.Same(requested, Assert.Single(prompt.Resolved).Context);
    }

    [Fact]
    public async Task HidWait_InsideIntentScope_ReportsIntentWhenTransportRequestsPresence()
    {
        var connection = new TouchWaitingHidConnection(MinimalGetInfoResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        using (UserPresenceIntent.BeginScope("pick this key"))
        {
            _ = await Assert.ThrowsAsync<CtapException>(() =>
                session.SelectionAsync(TestContext.Current.CancellationToken));
        }

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        Assert.Equal(UserPresenceOperations.Ctap2.Selection, requested.Operation);
        Assert.Equal("pick this key", requested.Intent);
        Assert.Same(requested, Assert.Single(prompt.Resolved).Context);
    }

    [Fact]
    public async Task MakeCredentialAsync_AfterIntentScopeEnds_ReportsNoIntent()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        using (UserPresenceIntent.BeginScope("earlier operation"))
        {
        }

        _ = await Assert.ThrowsAsync<CtapException>(() =>
            session.MakeCredentialAsync(
                new byte[32],
                new PublicKeyCredentialRpEntity("example.com"),
                new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
                [PublicKeyCredentialParameters.CreateES256()],
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(Assert.Single(prompt.Requested).Intent);
    }

    [Fact]
    public async Task SmartCardBackend_WhenResolutionThrowsAfterSuccess_PropagatesResolutionException()
    {
        var connection = new DisposeTrackingSmartCardConnection([0x00, 0xAA, 0x90, 0x00]);
        var expected = new InvalidOperationException("resolution failed");
        var prompt = new RecordingUserPresencePrompt(resolutionException: expected);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(
                new byte[] { (byte)CtapCommand.MakeCredential },
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SmartCardBackend_WhenOperationAndResolutionThrow_PreservesOperationException()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]);
        var resolutionException = new InvalidOperationException("resolution failed");
        var prompt = new RecordingUserPresencePrompt(resolutionException: resolutionException);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        CtapException actual = await Assert.ThrowsAsync<CtapException>(() => backend.SendCborAsync(
            new byte[] { (byte)CtapCommand.MakeCredential },
            notification,
            TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.UserActionTimeout, actual.Status);
        Assert.Single(prompt.Resolved);
    }

    [Fact]
    public async Task SmartCardBackend_WhenRequestThrows_DoesNotTransmitOrResolve()
    {
        var connection = new DisposeTrackingSmartCardConnection([0x00, 0xAA, 0x90, 0x00]);
        var expected = new InvalidOperationException("request failed");
        var prompt = new RecordingUserPresencePrompt(requestException: expected);
        var backend = new SmartCardBackend(new PcscProtocol(connection));
        UserPresenceNotification notification =
            UserPresenceNotification.Create(prompt, CreatePolicyContext());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            backend.SendCborAsync(
                new byte[] { (byte)CtapCommand.MakeCredential },
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Empty(connection.TransmittedCommands);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task GetAssertionAsync_WithSmartCardPrompt_ReportsOperation()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.NoCredentials, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<CtapException>(() => session.GetAssertionAsync(
            "example.com", new byte[32], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(UserPresenceOperations.Ctap2.GetAssertion, Assert.Single(prompt.Requested).Operation);
        Assert.Same(prompt.Requested[0], Assert.Single(prompt.Resolved).Context);
    }

    [Fact]
    public async Task GetAssertionAsync_WithUserPresenceFalse_DoesNotNotifySmartCardPrompt()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [(byte)CtapStatus.NoCredentials, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        CtapException exception = await Assert.ThrowsAsync<CtapException>(() =>
            session.GetAssertionAsync(
                "example.com",
                new byte[32],
                new GetAssertionOptions { UserPresence = false },
                TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.NoCredentials, exception.Status);
        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task SendCborRequestAsync_WithPrompt_DoesNotInferUserPresence()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [0x00, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await session.SendCborRequestAsync(
            new byte[] { CtapCommand.Selection },
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Fact]
    public async Task ReadOnlyInfoAndSelection_SendNoClientPinCommands()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] info = [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00];
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], info, info, [0x00, 0x90, 0x00]);
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);

        Assert.Contains("FIDO_2_0", (await session.GetInfoAsync(ct)).Versions);
        await session.SelectionAsync(ct);

        Assert.Collection(connection.TransmittedCommands,
            select => Assert.Equal(0xA4, select[1]),
            probe => AssertCtapCommand(probe, CtapCommand.GetInfo),
            read => AssertCtapCommand(read, CtapCommand.GetInfo),
            selection => AssertCtapCommand(selection, CtapCommand.Selection));

        static void AssertCtapCommand(byte[] apdu, byte command)
        {
            Assert.Equal(0x10, apdu[1]);
            Assert.Equal(1, apdu[4]);
            Assert.Equal(command, apdu[5]);
        }
    }

    [Fact]
    public async Task SelectionAsync_WithSmartCardPrompt_DoesNotPredictUserPresence()
    {
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00],
            [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00],
            [0x00, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        await session.SelectionAsync(TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }

    [Theory]
    [InlineData(false, UserPresenceOperations.Ctap2.Selection)]
    [InlineData(true, UserPresenceOperations.Ctap2.Reset)]
    public async Task HidWait_ReportsSessionOperation(bool reset, string expectedOperation)
    {
        var connection = new TouchWaitingHidConnection(MinimalGetInfoResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await Ctap2Session.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<CtapException>(() => reset
            ? session.ResetAsync(TestContext.Current.CancellationToken)
            : session.SelectionAsync(TestContext.Current.CancellationToken));

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceApplications.Fido2, requested.Application);
        Assert.Equal(expectedOperation, requested.Operation);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        Assert.Same(requested, Assert.Single(prompt.Resolved).Context);
    }

    [Fact]
    public async Task ResetAsync_ConfirmedResetReselectsAndUsesRefreshedLimitsOnSameSession()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] before = [0x00, .. MinimalGetInfoResponse(maxMsgSize: 2048, firmwareVersion: 0x050800), 0x90, 0x00];
        byte[] after = [0x00, .. MinimalGetInfoResponse(firmwareVersion: 0x050801), 0x90, 0x00];
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], before, [0x00, 0x90, 0x00], [0x90, 0x00], after, [0x00, 0x90, 0x00]);
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);

        await session.ResetAsync(ct);

        Assert.True(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(new FirmwareVersion(5, 8, 1), session.FirmwareVersion);
        await Assert.ThrowsAsync<ArgumentException>(() => session.SendCborRequestAsync(new byte[1025], ct));
        await session.SelectionAsync(ct);
        Assert.Equal(new byte[] { 0xA4, 0x10, 0x10, 0xA4, 0x10, 0x10 },
            connection.TransmittedCommands.Select(command => command[1]).ToArray());
        Assert.Equal(CtapCommand.Reset, connection.TransmittedCommands[2][5]);
        Assert.Equal(CtapCommand.GetInfo, connection.TransmittedCommands[4][5]);
        Assert.Equal(CtapCommand.Selection, connection.TransmittedCommands[5][5]);
        Assert.Equal(0, connection.DisposeAsyncCount);
    }

    [Fact]
    public async Task ResetAsync_RefusedResetDoesNotReselectOrEndSession()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] info = [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00];
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], info, [(byte)CtapStatus.NotAllowed, 0x90, 0x00], info);
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);

        Assert.Equal(CtapStatus.NotAllowed, (await Assert.ThrowsAsync<CtapException>(() => session.ResetAsync(ct))).Status);

        Assert.True(session.IsInitialized);
        Assert.Contains("FIDO_2_0", (await session.GetInfoAsync(ct)).Versions);
        Assert.Equal(new byte[] { 0xA4, 0x10, 0x10, 0x10 },
            connection.TransmittedCommands.Select(command => command[1]).ToArray());
    }

    [Fact]
    public async Task ResetAsync_HidConfirmationRefreshesEvenWhenPresenceResolutionThrows()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new TouchWaitingHidConnection(MinimalGetInfoResponse(), resetSucceeds: true);
        var expected = new InvalidOperationException("presence resolution failed after success");
        var prompt = new RecordingUserPresencePrompt(resolutionException: expected);
        await using var session = await Ctap2Session.CreateAsync(connection,
            new SessionCreationOptions { UserPresencePrompt = prompt }, ct);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(ct)));

        Assert.True(session.IsInitialized);
        Assert.Equal(2, connection.InitCount);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(prompt.Resolved).Outcome);
        Assert.Contains("FIDO_2_0", (await session.GetInfoAsync(ct)).Versions);
    }

    [Fact]
    public async Task ResetAsync_FailedRefreshIsTerminalAndDisposalDoesNotReplaceIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var secondary = new InvalidOperationException("owned connection teardown failed");
        var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [0x00, .. MinimalGetInfoResponse(), 0x90, 0x00], [0x00, 0x90, 0x00],
            [0x90, 0x00], [0x00, 0xA0, 0x90, 0x00]) // Malformed refresh GetInfo.
        {
            DisposeAsyncException = secondary
        };
        var device = new SingleConnectionYubiKey(connection);
        await using var session = await device.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }, ct);

        var primary = await Assert.ThrowsAsync<CborContentException>(() => session.ResetAsync(ct));

        Assert.Same(secondary, primary.Data["ResetRefreshCleanupException"]);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GetInfoAsync(ct));
        await session.DisposeAsync();
        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal(5, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(null, 1024)]
    [InlineData(1200, 1200)]
    public async Task SendsAtAdvertisedSizeButRejectsOversizedRawAndTypedRequests(int? advertised, int limit)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [0x00, .. MinimalGetInfoResponse(advertised), 0x90, 0x00], [0x00, 0x90, 0x00])
        {
            ExtendedApdu = true
        };
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);
        int initialTransmissions = connection.TransmittedCommands.Count;
        var oversized = new byte[limit + 1];
        oversized[0] = CtapCommand.GetInfo;

        await Assert.ThrowsAsync<ArgumentException>(() => session.SendCborRequestAsync(oversized, ct));
        await Assert.ThrowsAsync<ArgumentException>(() => session.GetAssertionAsync(new string('a', limit), new byte[32], cancellationToken: ct));
        Assert.Equal(initialTransmissions, connection.TransmittedCommands.Count);
        await session.SendCborRequestAsync(oversized.AsMemory(0, limit), ct);
        Assert.Equal(initialTransmissions + 1, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(0x000001u, 5, 7, false)]
    [InlineData(0x050700u, 5, 8, true)]
    [InlineData(0x050700u, 0, 0, true)]
    [InlineData(0x050700u, null, null, false)]
    [InlineData(null, null, null, true)]
    public async Task UsbSmartCard_UsesCachedFirmwareBeforeAppletFirmware(uint? applet, int? cachedMajor, int? cachedMinor, bool supported)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [0x00, .. MinimalGetInfoResponse(firmwareVersion: applet), 0x90, 0x00]);
        var device = new SingleConnectionYubiKey(connection)
        {
            CachedDeviceInfo = cachedMajor is int major && cachedMinor is int minor
                ? DeviceInfo.CreateFromTlvs([], new FirmwareVersion(major, minor, 0))
                : null
        };

        if (supported)
        {
            await using var session = await device.CreateCtap2SessionAsync(cancellationToken: ct);
            var framing = applet == 0x050700u && cachedMajor == 0
                ? new FirmwareVersion(5, 7, 0)
                : device.CachedDeviceInfo?.FirmwareVersion ?? new FirmwareVersion();
            Assert.Equal(framing, session.FirmwareVersion);
        }
        else
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => device.CreateCtap2SessionAsync(cancellationToken: ct));
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task UsbSmartCard_OptimisticGateRetainsNumericApduConfiguration(bool beta, bool useOverride)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [0x00, .. MinimalGetInfoResponse(firmwareVersion: 0x050600), 0x90, 0x00],
            [0x00, 0x90, 0x00])
        { ExtendedApdu = true };
        var cachedVersion = beta ? new FirmwareVersion(5, 7, 0) : new FirmwareVersion();
        var device = new SingleConnectionYubiKey(connection)
        {
            CachedDeviceInfo = DeviceInfo.CreateFromTlvs([], cachedVersion) with
            {
                VersionQualifier = beta ? new VersionQualifier(cachedVersion, VersionQualifierType.Beta, 1) : null!
            }
        };
        var options = new SessionCreationOptions
        {
            FirmwareVersionOverride = useOverride ? new FirmwareVersion(5, 5, 0) : null,
            ProtocolConfiguration = new ProtocolConfiguration { ForceShortApdus = true }
        };

        await using var session = await device.CreateCtap2SessionAsync(options, ct);
        await session.SelectionAsync(ct);

        Assert.Equal(options.FirmwareVersionOverride ?? (beta ? cachedVersion : new FirmwareVersion(5, 6, 0)), session.FirmwareVersion);
        Assert.Equal(3, connection.TransmittedCommands.Count);
        Assert.Equal([0x80, 0x10, 0x00, 0x00, 0x01, 0x0B, 0x00], connection.TransmittedCommands[2]);
    }

    [Fact]
    public async Task UsbSmartCard_OverrideAndPermissiveCacheDoNotSuppressRequiredGetInfoFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [(byte)CtapStatus.OperationDenied, 0x90, 0x00]);
        var device = new SingleConnectionYubiKey(connection)
        {
            CachedDeviceInfo = DeviceInfo.CreateFromTlvs([], new FirmwareVersion())
        };

        var error = await Assert.ThrowsAsync<CtapException>(() => device.CreateCtap2SessionAsync(
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) }, ct));

        Assert.Equal(CtapStatus.OperationDenied, error.Status);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task UsbSmartCard_OverrideDoesNotSuppressCancellationDuringRequiredGetInfo()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var ct = cancellation.Token;
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Transport.Returns(Transport.Usb);
        connection.Type.Returns(ConnectionType.SmartCard);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), CancellationToken.None)
            .Returns(_ => Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x90, 0x00 }), call =>
            {
                Assert.Equal(new byte[] { 0x80, 0x10, 0x00, 0x00, 1, 4, 0 }, call.Arg<ReadOnlyMemory<byte>>().ToArray());
                cancellation.Cancel();
                return Task.FromCanceled<ReadOnlyMemory<byte>>(ct);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Ctap2Session.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) }, ct));
        await connection.Received(2).TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), CancellationToken.None);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public async Task MakeCredentialChecksCredBlobAgainstAdvertisedCapacity(int length, bool sends)
    {
        var ct = TestContext.Current.CancellationToken;
        using var connection = new DisposeTrackingSmartCardConnection(
            [0x90, 0x00], [0x00, .. MinimalGetInfoResponse(maxCredBlobLength: 64), 0x90, 0x00],
            [(byte)CtapStatus.OperationDenied, 0x90, 0x00]);
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);
        int before = connection.TransmittedCommands.Count;
        var options = new MakeCredentialOptions
        {
            Extensions = new Yubico.YubiKit.Ctap2.Extensions.ExtensionBuilder().WithCredBlob(new byte[length]).Build()
        };

        Task<MakeCredentialResponse> Send() => session.MakeCredentialAsync(new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 1 }, "demo", "Demo"),
            [PublicKeyCredentialParameters.CreateES256()], options, ct);

        if (sends)
        {
            Assert.Equal(CtapStatus.OperationDenied, (await Assert.ThrowsAsync<CtapException>(Send)).Status);
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>("options", Send);
        }
        Assert.Equal(before + (sends ? 1 : 0), connection.TransmittedCommands.Count);
    }

    private static byte[] MinimalGetInfoResponse(int? maxMsgSize = null, uint? firmwareVersion = null, int? maxCredBlobLength = null)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2 + (maxMsgSize.HasValue ? 1 : 0) + (firmwareVersion.HasValue ? 1 : 0) +
            (maxCredBlobLength.HasValue ? 1 : 0));
        writer.WriteInt32(0x01);
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteEndArray();
        writer.WriteInt32(0x03);
        writer.WriteByteString(new byte[16]);
        if (maxMsgSize is int maxSize)
        {
            writer.WriteInt32(0x05);
            writer.WriteInt32(maxSize);
        }
        if (firmwareVersion is uint wireVersion)
        {
            writer.WriteInt32(0x0E);
            writer.WriteUInt32(wireVersion);
        }
        if (maxCredBlobLength is int blobLimit)
        {
            writer.WriteInt32(0x0F);
            writer.WriteInt32(blobLimit);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static UserPresenceContext CreatePolicyContext() => new()
    {
        Basis = UserPresenceBasis.PolicyRequires,
        Application = "FIDO2",
        Scope = "example.com"
    };

    private sealed class TouchWaitingHidConnection(byte[] getInfo, bool resetSucceeds = false) : IFidoHidConnection
    {
        private byte[]? _init;
        private int _responseCount;
        public int PacketSize => CtapConstants.PacketSize;
        public ConnectionType Type => ConnectionType.HidFido;
        public int InitCount { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            if ((packet.Span[4] & 0x7F) == CtapConstants.CtapHidInit)
            {
                InitCount++;
                _init = packet.ToArray();
            }
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] response = _responseCount++ switch
            {
                0 => InitResponse(),
                1 => Packet(0x01020304, CtapConstants.CtapHidCbor, [0x00, .. getInfo]),
                2 => Packet(0x01020304, CtapConstants.CtapHidKeepAlive, [0x02]),
                3 when resetSucceeds => Packet(0x01020304, CtapConstants.CtapHidCbor, [0x00]),
                4 when resetSucceeds => InitResponse(),
                5 or 6 when resetSucceeds => Packet(0x01020304, CtapConstants.CtapHidCbor, [0x00, .. getInfo]),
                _ => Packet(0x01020304, CtapConstants.CtapHidCbor, [(byte)CtapStatus.UserActionTimeout])
            };
            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        private byte[] InitResponse()
        {
            var payload = new byte[17];
            (_init ?? throw new InvalidOperationException("INIT not sent"))
                .AsSpan(CtapConstants.InitHeaderSize, 8).CopyTo(payload);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), 0x01020304);
            payload[12] = 2;
            return Packet(CtapConstants.BroadcastChannelId, CtapConstants.CtapHidInit, payload);
        }

        private static byte[] Packet(uint channel, byte command, ReadOnlySpan<byte> data)
        {
            var packet = new byte[CtapConstants.PacketSize];
            BinaryPrimitives.WriteUInt32BigEndian(packet, channel);
            packet[4] = (byte)(command | 0x80);
            packet[5] = (byte)(data.Length >> 8);
            packet[6] = (byte)data.Length;
            data.CopyTo(packet.AsSpan(CtapConstants.InitHeaderSize));
            return packet;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class DisposeTrackingSmartCardConnection(params byte[][] responses) : ISmartCardConnection
    {
        private readonly Queue<byte[]> _responses = new(responses);
        private readonly TaskCompletionSource _asyncDisposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeAsyncDisposal = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }
        public int DisposeAsyncCount { get; private set; }
        public Exception? DisposeAsyncException { get; init; }
        public bool PauseAsyncDisposal { get; init; }
        public Task AsyncDisposalStarted => _asyncDisposalStarted.Task;
        public List<byte[]> TransmittedCommands { get; } = [];
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            TransmittedCommands.Add(command.ToArray());
            return Task.FromResult((ReadOnlyMemory<byte>)_responses.Dequeue());
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullDisposable.Instance;

        public bool ExtendedApdu { get; init; }
        public bool SupportsExtendedApdu() => ExtendedApdu;

        public void Dispose() => DisposeCount++;

        public async ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            _asyncDisposalStarted.TrySetResult();

            if (PauseAsyncDisposal)
            {
                await _resumeAsyncDisposal.Task.ConfigureAwait(false);
            }

            if (DisposeAsyncException is not null)
            {
                throw DisposeAsyncException;
            }
        }

        public void ResumeAsyncDisposal() => _resumeAsyncDisposal.TrySetResult();
    }

    private sealed class SingleConnectionYubiKey(ISmartCardConnection connection) : IYubiKey
    {
        public string DeviceId => "fido-disposal-probe";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public DeviceInfo? CachedDeviceInfo { get; init; }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromResult((connection as TConnection)!);
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class RecordingUserPresencePrompt(
        Exception? requestException = null,
        Exception? resolutionException = null) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)>
            Resolved
        { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            return requestException is null
                ? default
                : ValueTask.FromException(requestException);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            return resolutionException is null
                ? default
                : ValueTask.FromException(resolutionException);
        }
    }
}