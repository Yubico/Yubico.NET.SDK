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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.CoreYubiKey;

/// <summary>
///     Phase 38.5: <see cref="YubiKeyConnectionExtensions.ConnectSessionTransportAsync" /> held-transport
///     fallback. Covers ISA cases (a)-(m): fall back past a held SmartCard transport to the next candidate,
///     never fall back on a non-held error / cancellation / non-SmartCard held error, validate input, and
///     never fall back for an override (single-element list).
/// </summary>
public class ConnectSessionTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // SCardException stores HResult = (int)errorCode; a held SmartCard surfaces one of these PC/SC codes.
    private static SCardException Held(uint code) => new("held by another process", (long)code);

    // (a) Held SmartCard (sharing violation) falls back to the next candidate.
    [Fact]
    public async Task ConnectSessionTransportAsync_SmartCardSharingViolation_FallsBackToNext()
    {
        var hid = new RecordingConnection(ConnectionType.HidFido);
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, Held(ErrorCode.SCARD_E_SHARING_VIOLATION))
            .Returns(ConnectionType.HidFido, hid);

        var connection = await device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct);

        Assert.Same(hid, connection);
        Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], device.Attempts);
    }

    // (b) Held SmartCard (server too busy) also falls back.
    [Fact]
    public async Task ConnectSessionTransportAsync_SmartCardServerTooBusy_FallsBackToNext()
    {
        var hid = new RecordingConnection(ConnectionType.HidOtp);
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, Held(ErrorCode.SCARD_E_SERVER_TOO_BUSY))
            .Returns(ConnectionType.HidOtp, hid);

        var connection = await device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidOtp], "Test", Ct);

        Assert.Same(hid, connection);
        Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidOtp], device.Attempts);
    }

    // (c) A non-held SCardException does not fall back; it propagates and no further candidate is attempted.
    [Fact]
    public async Task ConnectSessionTransportAsync_NonHeldScardError_PropagatesNoFallback()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, Held(ErrorCode.SCARD_E_NO_SMARTCARD))
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        var ex = await Assert.ThrowsAsync<SCardException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_NO_SMARTCARD), ex.HResult);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (d) A non-SCard exception does not fall back.
    [Fact]
    public async Task ConnectSessionTransportAsync_NonScardError_PropagatesNoFallback()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, new InvalidOperationException("boom"))
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAsync<InvalidOperationException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (e) Cancellation thrown by a connect attempt propagates and is never treated as held.
    [Fact]
    public async Task ConnectSessionTransportAsync_ConnectThrowsCanceled_PropagatesNoFallback()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, new OperationCanceledException())
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (f) Success on the first candidate short-circuits: exactly one attempt, no catch.
    [Fact]
    public async Task ConnectSessionTransportAsync_FirstCandidateConnects_ReturnsImmediately()
    {
        var sc = new RecordingConnection(ConnectionType.SmartCard);
        var device = new FallbackProbeYubiKey()
            .Returns(ConnectionType.SmartCard, sc)
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        var connection = await device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct);

        Assert.Same(sc, connection);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (g) An already-canceled token throws before any connect attempt.
    [Fact]
    public async Task ConnectSessionTransportAsync_PreCanceledToken_NoAttempts()
    {
        var device = new FallbackProbeYubiKey()
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", cts.Token));

        Assert.Empty(device.Attempts);
    }

    // (h) An override (single-element list) whose only transport is held rethrows; it never falls back.
    [Fact]
    public async Task ConnectSessionTransportAsync_SingleElementHeld_RethrowsNoFallback()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, Held(ErrorCode.SCARD_E_SHARING_VIOLATION));

        var ex = await Assert.ThrowsAsync<SCardException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard], "Test", Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SHARING_VIOLATION), ex.HResult);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (i) Input validation: null/empty/non-concrete/duplicate candidates throw with no connect attempt.
    [Fact]
    public async Task ConnectSessionTransportAsync_NullCandidates_ThrowsArgumentNull()
    {
        var device = new FallbackProbeYubiKey();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            device.ConnectSessionTransportAsync(null!, "Test", Ct));
        Assert.Empty(device.Attempts);
    }

    [Fact]
    public async Task ConnectSessionTransportAsync_EmptyCandidates_ThrowsArgument()
    {
        var device = new FallbackProbeYubiKey();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            device.ConnectSessionTransportAsync([], "Test", Ct));
        Assert.Empty(device.Attempts);
    }

    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    public async Task ConnectSessionTransportAsync_NonConcreteCandidate_ThrowsArgument(ConnectionType invalid)
    {
        var device = new FallbackProbeYubiKey();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            device.ConnectSessionTransportAsync([invalid], "Test", Ct));
        Assert.Empty(device.Attempts);
    }

    [Fact]
    public async Task ConnectSessionTransportAsync_DuplicateCandidate_ThrowsArgumentNoAttempt()
    {
        var device = new FallbackProbeYubiKey()
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));

        await Assert.ThrowsAsync<ArgumentException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.SmartCard], "Test", Ct));

        Assert.Empty(device.Attempts);
    }

    // (j) A token canceled between attempts stops the loop before opening a fallback transport.
    [Fact]
    public async Task ConnectSessionTransportAsync_CanceledBetweenAttempts_DoesNotOpenFallback()
    {
        using var cts = new CancellationTokenSource();
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, () =>
            {
                cts.Cancel();
                return Held(ErrorCode.SCARD_E_SHARING_VIOLATION);
            })
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", cts.Token));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // (k) Held first, then a real (non-held) failure on the next candidate surfaces the second error.
    [Fact]
    public async Task ConnectSessionTransportAsync_HeldThenRealFailure_SurfacesSecond()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, Held(ErrorCode.SCARD_E_SHARING_VIOLATION))
            .Throws(ConnectionType.HidFido, new InvalidOperationException("real failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct));

        Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], device.Attempts);
    }

    // (l) A held-coded error on a NON-SmartCard candidate does not fall back (SmartCard-only scope).
    [Fact]
    public async Task ConnectSessionTransportAsync_HeldOnNonSmartCard_DoesNotFallBack()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.HidFido, Held(ErrorCode.SCARD_E_SHARING_VIOLATION))
            .Returns(ConnectionType.SmartCard, new RecordingConnection(ConnectionType.SmartCard));

        var ex = await Assert.ThrowsAsync<SCardException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.HidFido, ConnectionType.SmartCard], "Test", Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SHARING_VIOLATION), ex.HResult);
        Assert.Equal([ConnectionType.HidFido], device.Attempts);
    }

    // (m) Public-helper contract: a device-unsupported candidate surfaces its own connect error unchanged
    // (the helper does not re-validate capability), and a non-held error does not fall back.
    [Fact]
    public async Task ConnectSessionTransportAsync_UnsupportedCandidate_PropagatesNoFallback()
    {
        var device = new FallbackProbeYubiKey()
            .Throws(ConnectionType.SmartCard, new NotSupportedException("device does not expose SmartCard"))
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAsync<NotSupportedException>(() => device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido], "Test", Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    [Fact]
    public async Task ConnectSessionTransportAsync_SessionInitSharingViolation_FallsBackAndDisposesFailedConnection()
    {
        var smartCard = new RecordingConnection(ConnectionType.SmartCard);
        var hid = new RecordingConnection(ConnectionType.HidFido);
        var device = new FallbackProbeYubiKey()
            .Returns(ConnectionType.SmartCard, smartCard)
            .Returns(ConnectionType.HidFido, hid);

        var result = await device.ConnectSessionTransportAsync(
            [ConnectionType.SmartCard, ConnectionType.HidFido],
            "Test",
            async (connection, transport, _) =>
            {
                await Task.Yield();
                if (transport == ConnectionType.SmartCard)
                    throw Held(ErrorCode.SCARD_E_SHARING_VIOLATION);
                return connection;
            },
            Ct);

        Assert.Same(hid, result);
        Assert.True(smartCard.Disposed);
        Assert.False(hid.Disposed);
        Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], device.Attempts);
    }

    /// <summary>
    ///     A fake physical device whose per-transport <see cref="ConnectAsync{TConnection}" /> either returns a
    ///     recording connection or throws a caller-supplied exception, recording the ordered attempts.
    /// </summary>
    private sealed class FallbackProbeYubiKey : IYubiKey
    {
        private readonly Dictionary<ConnectionType, Func<IConnection>> _behaviors = new();

        public string DeviceId => "fallback-probe";
        public List<ConnectionType> Attempts { get; } = [];

        public ConnectionType AvailableConnections
        {
            get
            {
                var combined = ConnectionType.Unknown;
                foreach (var key in _behaviors.Keys)
                    combined |= key;
                return combined;
            }
        }

        public FallbackProbeYubiKey Returns(ConnectionType transport, IConnection connection)
        {
            _behaviors[transport] = () => connection;
            return this;
        }

        public FallbackProbeYubiKey Throws(ConnectionType transport, Exception exception)
        {
            _behaviors[transport] = () => throw exception;
            return this;
        }

        public FallbackProbeYubiKey Throws(ConnectionType transport, Func<Exception> exceptionFactory)
        {
            _behaviors[transport] = () => throw exceptionFactory();
            return this;
        }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            var transport = TransportOf(typeof(TConnection));
            Attempts.Add(transport);

            if (!_behaviors.TryGetValue(transport, out var behavior))
                throw new InvalidOperationException($"No behavior configured for {transport}.");

            return Task.FromResult((TConnection)behavior());
        }

        private static ConnectionType TransportOf(Type connectionType)
        {
            if (connectionType == typeof(ISmartCardConnection))
                return ConnectionType.SmartCard;
            if (connectionType == typeof(IFidoHidConnection))
                return ConnectionType.HidFido;
            if (connectionType == typeof(IOtpHidConnection))
                return ConnectionType.HidOtp;
            throw new InvalidOperationException($"Unexpected connection type {connectionType.Name}.");
        }
    }

    /// <summary>
    ///     A fake connection implementing all three concrete connection interfaces, recording disposal.
    /// </summary>
    private sealed class RecordingConnection(ConnectionType type)
        : ISmartCardConnection, IFidoHidConnection, IOtpHidConnection
    {
        public ConnectionType Type { get; } = type;
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        // ISmartCardConnection
        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => false;

        // IFidoHidConnection / IOtpHidConnection (identical Send/Receive signatures satisfy both)
        public int PacketSize => 64;
        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}