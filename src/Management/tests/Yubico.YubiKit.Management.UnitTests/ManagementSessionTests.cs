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

namespace Yubico.YubiKit.Management.UnitTests;

using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management.Backend;

[CollectionDefinition(nameof(ManagementSessionLoggingCollection), DisableParallelization = true)]
public sealed class ManagementSessionLoggingCollection;

[Collection(nameof(ManagementSessionLoggingCollection))]
public class ManagementSessionTests
{
    [Fact]
    public async Task CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached()
    {
        var connection = new UnsupportedConnection();

        _ = await Assert.ThrowsAsync<NotSupportedException>(
            () => ManagementSession.CreateAsync(
                connection,
                cancellationToken: TestContext.Current.CancellationToken));

        // This direct constructor does not attach a guard holder. The assertion above checks rejection,
        // but this probe cannot detect a stale attachment; that requires a separately bound factory probe.
        await using var probe = new ProbeSession(connection);
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_ReleasesGuardWithoutDisposingBorrowedConnection()
    {
        var connection = new FailingSmartCardConnection();
        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken));
        // The exact type excludes ConnectionInUseException from a stale factory attachment.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken));

        // Borrowed: a failed CreateAsync must not dispose a connection it did not create.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_CancellationDuringInitialization_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new FailingSmartCardConnection();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: cancellationSource.Token));

        // This pre-cancelled creation must not dispose the borrowed connection; it does not test mid-exchange cancellation.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public void IManagementSession_InheritsIAsyncDisposable()
    {
        // Verify that IManagementSession inherits from IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IManagementSession)));
    }

    [Fact]
    public void ManagementSession_ImplementsIAsyncDisposable()
    {
        // Verify that ManagementSession implements IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(ManagementSession)));
    }

    /// <summary>
    ///     Checks a retained encoded-buffer view after a successful fake backend write. Backend failure,
    ///     cancellation, and caller-owned lock-code cleanup are not covered by this test.
    /// </summary>
    [Fact]
    public async Task SetDeviceConfigAsync_ZeroesEncodedConfigAfterBackendWrite()
    {
        var backend = new CapturingBackend();
        var session = CreateSessionForBackend(backend);
        var lockCode = Enumerable.Repeat((byte)0xA5, 16).ToArray();
        var config = new DeviceConfig { UsbEnabled = DeviceCapabilities.Otp };

        await session.SetDeviceConfigAsync(
            config,
            new SetDeviceConfigOptions { CurrentLockCode = lockCode },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(backend.SawNonZeroConfig);
        Assert.True(backend.CapturedConfig.Span.ToArray().All(static b => b == 0));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResetDeviceAsync_Hid_RejectsWithoutProtocolIo(bool useFido)
    {
        using var protocol = new InertHidProtocol();
        IManagementBackend backend = useFido ? new FidoHidBackend(protocol) : new OtpBackend(protocol);
        var session = CreateSessionForBackend(backend, new FirmwareVersion(5, 6, 0));

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            session.ResetDeviceAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Device reset is only available over SmartCard (CCID) connections.", exception.Message);
    }

    /// <summary>
    ///     A read after disposal must report ObjectDisposedException rather than an incidental protocol error.
    /// </summary>
    /// <remarks>
    ///     The helper skips initialization and pins firmware at 5.6.0. These disposal tests assert the
    ///     exception type; they do not establish that an undisposed helper session could perform the operation.
    /// </remarks>
    [Fact]
    public async Task GetDeviceInfoAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetDeviceInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetDeviceConfigAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.SetDeviceConfigAsync(
                new DeviceConfig { UsbEnabled = DeviceCapabilities.Otp },
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetDeviceAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.ResetDeviceAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     A real, fully constructed session over an inert connection: the disposal gate, connection, and
    ///     logger are genuine, so <c>DisposeAsync</c> runs the production teardown rather than a simulation of
    ///     it. Only initialization is skipped.
    /// </summary>
    private static ManagementSession CreateDisposableSession()
    {
        var session = (ManagementSession)Activator.CreateInstance(
            typeof(ManagementSession),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [new InertSmartCardConnection(), null],
            culture: null)!;

        typeof(ApplicationSession)
            .GetProperty(nameof(ApplicationSession.FirmwareVersion))!
            .SetValue(session, new FirmwareVersion(5, 6, 0));

        return session;
    }

    private static ManagementSession CreateSessionForBackend(
        IManagementBackend backend,
        FirmwareVersion? firmwareVersion = null)
    {
        var session = (ManagementSession)RuntimeHelpers.GetUninitializedObject(typeof(ManagementSession));

        typeof(ManagementSession)
            .GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, backend);

        typeof(ApplicationSession)
            .GetProperty(nameof(ApplicationSession.FirmwareVersion))!
            .SetValue(session, firmwareVersion ?? new FirmwareVersion(5, 0, 0));

        return session;
    }

    private sealed class CapturingBackend : IManagementBackend
    {
        public ReadOnlyMemory<byte> CapturedConfig { get; private set; }
        public bool SawNonZeroConfig { get; private set; }

        public ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken = default)
        {
            CapturedConfig = config;
            SawNonZeroConfig = config.Span.ContainsAnyExcept((byte)0);
            return ValueTask.CompletedTask;
        }

    }

    [Fact]
    public async Task LegacySmartCardMode_SendsSetModeWithDeviceSelector()
    {
        using var protocol = new RecordingSmartCardProtocol();
        var backend = new SmartCardBackend(protocol);
        await backend.SetModeAsync(new FirmwareVersion(4, 3, 0), 0x02, 0x0F, 0x1234, TestContext.Current.CancellationToken);

        var command = Assert.Single(protocol.Commands);
        Assert.Equal(0x16, command.Ins);
        Assert.Equal(0x11, command.P1);
        Assert.Equal(0, command.P2);
        Assert.Equal(new byte[] { 0x02, 0x0F, 0x34, 0x12 }, protocol.CommandData);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task LegacySmartCardMode_DoesNotReachModernOrDevelopmentFirmware(int major)
    {
        using var protocol = new RecordingSmartCardProtocol();
        var backend = new SmartCardBackend(protocol);
        await Assert.ThrowsAsync<NotSupportedException>(() => backend.SetModeAsync(
            new FirmwareVersion(major, 0, 0), 0x02, 0x0F, 0x1234, TestContext.Current.CancellationToken).AsTask());
        Assert.Empty(protocol.Commands);
    }

    [Theory]
    [InlineData(5, 6, true, false)]
    [InlineData(5, 6, false, true)]
    [InlineData(5, 6, null, true)]
    [InlineData(5, 7, true, true)]
    [InlineData(0, 0, true, true)]
    public async Task SetDeviceConfigAsync_NfcRestriction_PreflightsOnlyTrue(
        int major, int minor, bool? request, bool reachesBackend)
    {
        var protocol = new RecordingSmartCardProtocol();
        var session = CreateSessionForBackend(new SmartCardBackend(protocol), new FirmwareVersion(major, minor, 0));
        var config = new DeviceConfig { NfcRestricted = request };
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!reachesBackend)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => session.SetDeviceConfigAsync(config, cancellationToken: cancellationToken));
            Assert.Empty(protocol.Commands);
            return;
        }

        await session.SetDeviceConfigAsync(config, cancellationToken: cancellationToken);
        Assert.Equal(0x1C, Assert.Single(protocol.Commands).Ins);
        byte[] expected = request is { } restriction ? [3, 0x17, 1, restriction ? (byte)1 : (byte)0] : [0];
        Assert.Equal(expected, protocol.CommandData);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SetDeviceConfigAsync_HeldWrite_ClearsOnlyItsEncodingAfterTermination(bool cancel, bool replacement)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var backend = new HoldingBackend();
        var session = CreateSessionForBackend(backend);
        byte[] code = [0xA5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
        try
        {
            await VerifyHeldWriteAsync(session, backend, code, replacement, cancel, cancellation);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(code);
        }
    }

    private static async Task VerifyHeldWriteAsync(
        ManagementSession session, HoldingBackend backend, byte[] code, bool replacement, bool cancel,
        CancellationTokenSource cancellation)
    {
        var options = replacement
            ? new SetDeviceConfigOptions { NewLockCode = code }
            : new SetDeviceConfigOptions { CurrentLockCode = code };

        var write = session.SetDeviceConfigAsync(new DeviceConfig(), options, cancellation.Token);
        try
        {
            Assert.False(write.IsCompleted);
            Assert.Equal(code, backend.Encoding.Span[3..]);
        }
        finally
        {
            if (cancel)
                cancellation.Cancel();
            else
                backend.Fail();

            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => write);
            else
                Assert.Same(backend.Failure, await Assert.ThrowsAsync<InvalidOperationException>(() => write));
        }

        Assert.False(backend.Encoding.Span.ContainsAnyExcept((byte)0));
        Assert.Equal(new byte[] { 0xA5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, code);
    }

    private sealed class HoldingBackend : IManagementBackend
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public InvalidOperationException Failure { get; } = new("Scripted backend failure.");
        public ReadOnlyMemory<byte> Encoding { get; private set; }

        public ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken)
        {
            Encoding = config;
            return new ValueTask(_completion.Task.WaitAsync(cancellationToken));
        }

        public void Fail() => _completion.SetException(Failure);

    }

    private sealed class InertHidProtocol : IFidoHidProtocol, IOtpHidProtocol
    {
        public bool IsChannelInitialized => true;
        public FirmwareVersion? FirmwareVersion => new FirmwareVersion(5, 6, 0);

        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected protocol initialization.");

        public Task<ReadOnlyMemory<byte>> SendCommandAsync(
            byte command,
            ReadOnlyMemory<byte> data,
            UserPresenceNotification userPresenceNotification,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected FIDO exchange.");

        public Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
            byte slot,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected OTP exchange.");

        public Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected status read.");

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }
        public void Dispose() { }
    }

    private sealed class UnsupportedConnection : IConnection
    {
        public ConnectionType Type => ConnectionType.Unknown;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(4, 0, false, false)]
    [InlineData(4, 1, false, true)]
    [InlineData(4, 1, true, false)]
    [InlineData(0, 0, false, true)]
    public async Task GetDeviceInfoAsync_AppliesEffectiveFirmwareFloorOnlyToPublicReads(
        int major, int minor, bool useOverride, bool supported)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection((byte)major, (byte)minor);
        var options = new SessionCreationOptions { FirmwareVersionOverride = useOverride ? new FirmwareVersion(4, 0, 9) : null };
        await using var session = await ManagementSession.CreateAsync(connection, options, cancellationToken);

        Assert.Equal(1, connection.Reads);
        if (supported)
        {
            var info = await session.GetDeviceInfoAsync(cancellationToken);
            Assert.Equal(new FirmwareVersion(major, minor, 0), info.FirmwareVersion);
            Assert.Equal(2, connection.Reads);
        }
        else
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => session.GetDeviceInfoAsync(cancellationToken));
            Assert.Equal(1, connection.Reads);
        }
    }

    [Fact]
    public async Task CreateAsync_UnknownFirmwareAfterSuccessfulSelect_AllowsGatesButDoesNotFabricateMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection(4, 0) { UnknownSelectVersion = true };
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);

        Assert.True(session.IsSupported(new Feature("configuration", 5, 0, 0)));
        _ = await Assert.ThrowsAsync<ApduException>(() => session.GetDeviceInfoAsync(ct));
        Assert.Equal(2, connection.Reads);
    }

    internal sealed class VersionedSmartCardConnection(byte major, byte minor) : ISmartCardConnection
    {
        public int Reads { get; private set; }
        public int Selects { get; private set; }
        public byte[] ResetCommand { get; private set; } = [];
        public bool RejectReset { get; init; }
        public bool FailReselect { get; init; }
        public bool UnknownSelectVersion { get; init; }
        public bool ReportsBeta { get; init; }
        public bool ExtendedCapable { get; init; }
        public byte[] ConfigurationCommand { get; private set; } = [];
        public Exception? DisposalFailure { get; init; }
        public int DisposeAttempts { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] response = command.Span[1] switch
            {
                0xA4 => Select(),
                0x1D => ReadInfo(),
                0x1C => WriteConfig(command),
                0x1F => Reset(command),
                _ => throw new InvalidOperationException("Unexpected Management command."),
            };
            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        private byte[] Select()
        {
            Selects++;
            if (UnknownSelectVersion)
                return [0x90, 0];
            return FailReselect && Selects > 1
                ? [0x6A, 0x82]
                : [.. System.Text.Encoding.UTF8.GetBytes($"{major}.{minor}.0"), 0x90, 0];
        }

        private byte[] Reset(ReadOnlyMemory<byte> command)
        {
            ResetCommand = command.ToArray();
            return RejectReset ? [0x69, 0x85] : [0x90, 0];
        }

        private byte[] ReadInfo()
        {
            Reads++;
            if (ReportsBeta)
                return [21, 0x05, 3, major, minor, 0, 0x19, 14,
                    0x01, 3, major, minor, 0, 0x02, 1, 1, 0x03, 4, 0, 0, 0, 1, 0x90, 0];
            return (major, minor) == (4, 0) ? [0x6D, 0] : [5, 0x05, 3, major, minor, (byte)(Selects - 1), 0x90, 0];
        }

        private byte[] WriteConfig(ReadOnlyMemory<byte> command)
        {
            ConfigurationCommand = command.ToArray();
            return [0x90, 0];
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken) => throw new NotSupportedException();
        public bool SupportsExtendedApdu() => ExtendedCapable;
        public void Dispose() { }
        public ValueTask DisposeAsync()
        {
            DisposeAttempts++;
            return DisposalFailure is { } failure ? ValueTask.FromException(failure) : default;
        }
    }

    [Theory]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, true)]
    public async Task SetDeviceConfigAsync_UsesNumericApduFramingIndependentlyOfFeatureAdmission(
        bool beta, bool unknown, bool forceShort, bool extended)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection(unknown ? (byte)4 : (byte)5, unknown ? (byte)0 : (byte)7)
        {
            ReportsBeta = beta,
            UnknownSelectVersion = unknown,
            ExtendedCapable = true,
        };
        var options = new SessionCreationOptions { ProtocolConfiguration = new() { ForceShortApdus = forceShort } };
        await using var session = await ManagementSession.CreateAsync(connection, options, ct);

        Assert.Equal(beta || unknown, session.IsSupported(new Feature("future feature", 99, 0, 0)));
        await session.SetDeviceConfigAsync(new DeviceConfig { NfcRestricted = true }, cancellationToken: ct);

        byte[] expected = extended
            ? [0, 0x1C, 0, 0, 0, 0, 4, 3, 0x17, 1, 1, 0, 0]
            : [0, 0x1C, 0, 0, 4, 3, 0x17, 1, 1, 0];
        Assert.Equal(expected, connection.ConfigurationCommand);
    }

    [Fact]
    public async Task ResetDeviceAsync_RefreshAndCleanupLoggingFail_PreservesRefreshErrorThroughImplicitDisposal()
    {
        var ct = TestContext.Current.CancellationToken;
        var cleanupFailure = new InvalidOperationException("Scripted owned connection disposal failure.");
        var provider = new ThrowOnCleanupLogProvider(cleanupFailure);
        using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(provider));
        using var logging = YubiKitLogging.UseTemporary(factory);
        var connection = new VersionedSmartCardConnection(5, 6) { FailReselect = true, DisposalFailure = cleanupFailure };

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);
            session.OwnConnection();
            await session.ResetDeviceAsync(ct);
        });

        Assert.Contains("reset was confirmed", failure.Message, StringComparison.Ordinal);
        Assert.Equal(unchecked((short)0x6A82), Assert.IsType<ApduException>(failure.InnerException).SW);
        Assert.Equal(1, connection.DisposeAttempts);
        Assert.Equal(1, provider.FailedLogs);
    }

    [Fact]
    public async Task DisposeAsync_OutsideResetRefreshFailure_StillPropagatesOwnedConnectionFailure()
    {
        var cleanupFailure = new InvalidOperationException("Scripted owned connection disposal failure.");
        var connection = new VersionedSmartCardConnection(5, 6) { DisposalFailure = cleanupFailure };
        var session = await ManagementSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        session.OwnConnection();

        Assert.Same(cleanupFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask()));
        Assert.Same(cleanupFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask()));
        Assert.Equal(1, connection.DisposeAttempts);
    }

    private sealed class ThrowOnCleanupLogProvider(Exception cleanupFailure) : ILoggerProvider, ILogger
    {
        public int FailedLogs { get; private set; }
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Dispose() { }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!ReferenceEquals(exception, cleanupFailure))
                return;

            FailedLogs++;
            throw new InvalidOperationException("Scripted logging provider failure.");
        }
    }

    [Fact]
    public async Task ResetDeviceAsync_ConfirmedReset_ReselectsAndContinuesOnSameSession()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection(5, 6);
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);

        await session.ResetDeviceAsync(ct);

        Assert.Equal(new byte[] { 0, 0x1F, 0, 0, 0 }, connection.ResetCommand);
        Assert.Equal(2, connection.Selects);
        Assert.Equal(2, connection.Reads);
        var info = await session.GetDeviceInfoAsync(ct);
        Assert.Equal(new FirmwareVersion(5, 6, 1), info.FirmwareVersion);
    }

    [Fact]
    public async Task ResetDeviceAsync_RejectedReset_PreservesStatusAndDoesNotRefresh()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection(5, 6) { RejectReset = true };
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);
        _ = await Assert.ThrowsAsync<ApduException>(() => session.ResetDeviceAsync(ct));
        Assert.Single(connection.ResetCommand, b => b == 0x1F);
        Assert.Equal(1, connection.Selects);
        Assert.Equal(new FirmwareVersion(5, 6, 0), (await session.GetDeviceInfoAsync(ct)).FirmwareVersion);
    }

    [Fact]
    public async Task ResetDeviceAsync_ConfirmedResetWithFailedReselect_EndsSessionAndPreservesCause()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new VersionedSmartCardConnection(5, 6) { FailReselect = true };
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetDeviceAsync(ct));
        _ = Assert.IsType<ApduException>(failure.InnerException);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GetDeviceInfoAsync(ct));
    }

    private sealed class ProbeSession(IConnection connection) : ApplicationSession(connection);

    private sealed class RecordingSmartCardProtocol : ISmartCardProtocol
    {
        public List<ApduCommand> Commands { get; } = [];
        public byte[] CommandData { get; private set; } = [];

        public Task<ApduResponse> TransmitAndReceiveAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            CommandData = command.Data.ToArray();
            return Task.FromResult(new ApduResponse([], unchecked((short)0x9000)));
        }

        public Task<ReadOnlyMemory<byte>> SelectAsync(
            ReadOnlyMemory<byte> applicationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     A structurally valid SmartCard connection that never answers. Enough for a session to be
    ///     constructed and disposed for real; any actual exchange is a test bug.
    /// </summary>
    private sealed class InertSmartCardConnection : ISmartCardConnection
    {
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("the disposal guard should have rejected this call");

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullConnectionDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => default;

        private sealed class NullConnectionDisposable : IDisposable
        {
            public static NullConnectionDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class FailingSmartCardConnection : ISmartCardConnection
    {
        public int DisposeCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("session-init probe failure");
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}