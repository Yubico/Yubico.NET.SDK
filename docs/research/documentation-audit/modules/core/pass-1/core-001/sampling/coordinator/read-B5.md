PROPOSED SAMPLE — no verdict: core-001:unit:971739df4dbce461 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Fido/Hid/CtapConstants.cs:1–8
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Protocols.Fido.Hid;

/// <summary>
/// CTAP HID protocol constants as defined in the FIDO CTAP specification.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:1430eb9373891ea4 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Fido/Hid/IFidoHidProtocol.cs:20–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Initializes the CTAP HID channel if it has not already been initialized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);


PROPOSED SAMPLE — no verdict: core-001:unit:31fae25215752be4 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Otp/Hid/ChecksumUtils.cs:31–58
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Calculate the CRC13239 checksum for a byte buffer.
    /// </summary>
    /// <param name="data">The data to checksum.</param>
    /// <param name="length">How much of the buffer should be checksummed.</param>
    /// <returns>The calculated checksum.</returns>
    public static ushort CalculateCrc(ReadOnlySpan<byte> data, int length)
    {
        var crc = 0xFFFF;

        for (var index = 0; index < length; index++)
        {
            crc ^= data[index];
            for (var i = 0; i < 8; i++)
            {
                var j = crc & 1;
                crc >>= 1;
                if (j == 1)
                {
                    crc ^= CrcPolynomial;
                }
            }
        }

        return (ushort)(crc & 0xFFFF);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:95d7c83254104597 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Otp/Hid/OtpConstants.cs:56–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Dummy report write - forces device update or aborts current operation.
    /// </summary>
    public const byte DummyReportWrite = 0x8F;


PROPOSED SAMPLE — no verdict: core-001:unit:be9a9bc376b46500 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/ProtocolFactory.cs:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Protocols;

/// <summary>
///     Creates Core protocol implementations from opened YubiKey connections.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:e0f280ed806713e7 / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationSession.cs:425–432
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Enforces a firmware prerequisite within a derived session operation.</summary>
    protected void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e0af29983be3ad4c / B5 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/RawSmartCardSession.cs:147–167
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Configures APDU formatting for a known device firmware version.</summary>
    /// <remarks>
    ///     This does not apply applet capability or feature gates. SCP sessions must provide configuration during
    ///     creation and cannot be reconfigured after the secure channel is established.
    /// </remarks>
    public void Configure(
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null)
    {
        ThrowIfDisposed();
        if (_usesScp)
        {
            throw new InvalidOperationException(
                "An SCP raw session must be configured during creation, before SCP is established.");
        }

        SmartCardProtocol.Configure(firmwareVersion, configuration);
        FirmwareVersion = firmwareVersion;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3dc28415274d2715 / B5 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Abstractions/IConnection.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    ConnectionType Type { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:e04bd130212e9058 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Fido/Hid/FidoHidConnection.cs:50–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var report = connection.GetReport();
        if (report.Length != PacketSize)
            throw new InvalidOperationException($"Expected {PacketSize}-byte packet, got {report.Length} bytes");

        return Task.FromResult<ReadOnlyMemory<byte>>(report);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ffe87a0dc122f066 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:34–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly IFidoHidConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));


PROPOSED SAMPLE — no verdict: core-001:unit:14fe4ebef38d1ed0 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs:483–488
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static bool IsKeepAlivePacket(ReadOnlySpan<byte> packet) =>
        packet.Length >= CtapConstants.InitHeaderSize &&
        (packet[4] & CtapConstants.InitPacketMask) != 0 &&
        GetPacketCommand(packet) == CtapConstants.CtapHidKeepAlive;


PROPOSED SAMPLE — no verdict: core-001:unit:accb5a8244e081d6 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs:42–43
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly IOtpHidConnection _connection;


PROPOSED SAMPLE — no verdict: core-001:unit:f54885613567a25d / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs:43–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly ExchangeGuard _exchangeGuard = new();


PROPOSED SAMPLE — no verdict: core-001:unit:7a5b8a3b94cca491 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs:453–461
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void ThrowIfResetFailed()
    {
        if (_resetFailure is { } failure)
            throw new InvalidOperationException(
                "OTP HID state could not be recovered after a failed reset; this protocol is unusable. Dispose it and reopen the connection.",
                failure);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1ffab427759120b0 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationIds.cs:29–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly byte[] _otp = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01];


PROPOSED SAMPLE — no verdict: core-001:unit:80a2e5151f5d45e8 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationIds.cs:31–32
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly byte[] _fido = [0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01];


PROPOSED SAMPLE — no verdict: core-001:unit:e7371b49389c4918 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationIds.cs:32–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly byte[] _oath = [0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01];


PROPOSED SAMPLE — no verdict: core-001:unit:282f8f4de8569fc5 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationIds.cs:33–34
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly byte[] _openPgp = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01];


PROPOSED SAMPLE — no verdict: core-001:unit:5dc687764c3b8497 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/ApplicationSession.cs:245–270
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private protected static async Task<TSession> ConstructInitializedAsync<TSession>(
        IConnection connection,
        SessionCreationOptions? options,
        Func<TSession> create,
        Func<TSession, CancellationToken, Task> initialize,
        CancellationToken cancellationToken)
        where TSession : ApplicationSession
    {
        ArgumentNullException.ThrowIfNull(initialize);
        ValidatePreferredConnectionType(connection, options);

        var session = Construct(connection, create);
        try
        {
            await session.InitializeSessionAsync(token => initialize(session, token), cancellationToken)
                .ConfigureAwait(false);
            return session;
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:22756bb8361b62c6 / B5 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Sessions/RawSmartCardSession.cs:38–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private RawSmartCardSession(ISmartCardConnection connection, bool usesScp)
        : base(connection)
    {
        _usesScp = usesScp;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7e1d5b20f31edc04 / B5 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/architecture/raw-access-tiers.md:267–283
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Migration From ProtocolFactory

`ProtocolFactory` and the `IProtocol` family are internal session machinery in v2. Code that previously called
`ProtocolFactory.Create(connection)` should use the corresponding raw session:

```csharp
// Before
using ISmartCardProtocol protocol = ProtocolFactory.Create(connection);
await protocol.SelectAsync(applicationId, cancellationToken);

// After
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);
await raw.SelectAsync(applicationId, cancellationToken);
```

Use `RawCtapHidSession` or `RawOtpHidSession` for the corresponding HID logical exchanges. Third-party protocol
composition should wrap a raw session rather than injecting or implementing Core protocol internals.



PROPOSED SAMPLE — no verdict: core-001:unit:ddf5b4cb930d2fa5 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolConcurrencyTests.cs:208–223
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public async Task<bool> WaitForSendsAsync(int count, TimeSpan timeout, CancellationToken ct)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_wireTags.Count >= count)
                        return true;
                }

                if (!await _sendArrivals.WaitAsync(timeout, ct).ConfigureAwait(false))
                    return false;
            }
        }


PROPOSED SAMPLE — no verdict: core-001:unit:c4787e6e83d209e3 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolConcurrencyTests.cs:277–299
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private void ProcessCompleteRequest()
        {
            byte[] responsePayload;
            if (_requestCommand == CtapConstants.CtapHidInit)
            {
                InitExchangeCount++;
                responsePayload = new byte[17];
                _requestBuffer.AsSpan(0, CtapConstants.NonceSize).CopyTo(responsePayload);
                BinaryPrimitives.WriteUInt32BigEndian(responsePayload.AsSpan(8), AssignedChannelId);
                responsePayload[12] = 2;
                responsePayload[13] = 5;
                responsePayload[14] = 8;
                responsePayload[15] = 0;
            }
            else
            {
                responsePayload = Responder(_requestCommand, _requestBuffer);
            }

            EnqueueResponse(_requestCommand, responsePayload);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:7321fe1e15184a10 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:851–852
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private const byte CtapErrKeepAliveCancel = 0x2D;


PROPOSED SAMPLE — no verdict: core-001:unit:5369991596726ada / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:852–853
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 // CTAP2_ERR_KEEPALIVE_CANCEL
    private const byte CtapErrNoCredentials = 0x27;


PROPOSED SAMPLE — no verdict: core-001:unit:28d11837d607f4aa / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:893–895
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly TaskCompletionSource<ReadOnlyMemory<byte>> _heldRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:13551f225e5bb1f2 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:915–917
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public List<ReadOnlyMemory<byte>> RetainedSentPackets { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:8c552de5da184e67 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:926–948
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RetainedSentPackets.Add(packet);
            byte[] snapshot = packet.ToArray();
            SentPacketSnapshots.Add(snapshot);
            if ((snapshot[4] & ~CtapConstants.InitPacketMask) == CtapConstants.CtapHidInit)
            {
                InitRequestCount++;
                _lastInitRequest = snapshot;
                _initResponseSent = false;
            }

            if (ThrowOnNextSend)
            {
                ThrowOnNextSend = false;
                throw new IOException("Scripted send failure.");
            }

            return Task.CompletedTask;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:373adeddffa5ed68 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:987–1003
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private byte[] CreateInitResponse()
        {
            if (_lastInitRequest is null)
                throw new InvalidOperationException("INIT request was not sent.");

            var payload = new byte[17];
            _lastInitRequest.AsSpan(CtapConstants.InitHeaderSize, CtapConstants.NonceSize).CopyTo(payload);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), TestChannelId);
            payload[12] = 2;
            payload[13] = 5;
            payload[14] = 8;
            payload[15] = 0;
            payload[16] = 0;
            return CreateInitPacket(CtapConstants.BroadcastChannelId, CtapConstants.CtapHidInit, payload);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:a6409df9890388c9 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:1026–1037
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            return resolvedException is null
                ? default
                : ValueTask.FromException(resolvedException);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:a8c19d826d96991d / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Fido/Hid/FidoHidProtocolTests.cs:1041–1042
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public List<Exception?> Exceptions { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:a38082b8b32d418b / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolConcurrencyTests.cs:138–139
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private volatile TaskCompletionSource? _hold;


PROPOSED SAMPLE — no verdict: core-001:unit:95c29f78c32cee7d / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolConcurrencyTests.cs:154–157
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void HoldSends() =>
            _hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:84a633fd9be55a84 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly List<byte[]> _reportsSent = new();


PROPOSED SAMPLE — no verdict: core-001:unit:1c9c6d64e4945a01 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:274–295
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task SendAndReceiveAsync_WhenObservedTouchWaitExpires_ThrowsTouchSpecificTimeoutAndResets()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock, touchTimeout: TimeSpan.Zero);
        QueueUntilTouchWait(mock);
        var prompt = new RecordingUserPresencePrompt();

        OtpHidTouchTimeoutException exception = await Assert.ThrowsAsync<OtpHidTouchTimeoutException>(() =>
            protocol.SendAndReceiveAsync(
                0x30,
                ReadOnlyMemory<byte>.Empty,
                CreatePresenceNotification(prompt),
                TestContext.Current.CancellationToken));

        Assert.IsAssignableFrom<TimeoutException>(exception);
        Assert.Single(prompt.Requested);
        Assert.Empty(prompt.Resolved);
        Assert.Equal(OtpConstants.DummyReportWrite, mock.SentReports[^1][OtpConstants.FeatureReportDataSize]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:57678adc5582df43 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:480–501
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task SendAndReceiveAsync_RequestCallbackFails_ResetsStateBeforePropagating()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);
        QueueUntilTouchWait(mock);
        var expected = new InvalidOperationException("Prompt failed.");
        var prompt = new RecordingUserPresencePrompt(() => throw expected);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendAndReceiveAsync(
                0x30,
                ReadOnlyMemory<byte>.Empty,
                CreatePresenceNotification(prompt),
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Empty(prompt.Resolved);
        Assert.Equal(OtpConstants.DummyReportWrite, mock.SentReports[^1][OtpConstants.FeatureReportDataSize]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:dc0f21427d29a65c / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:574–592
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ReadStatusAsync_ReturnsStatusBytes()
    {
        var mock = new MockHidConnection();
        var protocol = CreateProtocolWithMock(mock);

        // Queue status report for ReadStatusAsync
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x01, 0x02, 0x03, 0x00]);

        var status = await protocol.ReadStatusAsync(TestContext.Current.CancellationToken);

        // Should return bytes 1-6 (skip first and last)
        Assert.Equal(6, status.Length);
        Assert.Equal(0x05, status.Span[0]); // Major version
        Assert.Equal(0x04, status.Span[1]); // Minor version
        Assert.Equal(0x03, status.Span[2]); // Patch version
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7b6fa526bc4a6aa0 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:905–909
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Dispose()
        {
        }


PROPOSED SAMPLE — no verdict: core-001:unit:3631aef26af6b769 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolConnectionOwnershipTests.cs:42–52
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Fact]
    public void FidoHidProtocol_Dispose_DoesNotDisposeTheConnection()
    {
        var connection = new RecordingFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        protocol.Dispose();

        Assert.Equal(0, connection.DisposeCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c99301c915f20513 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolConnectionOwnershipTests.cs:65–66
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class RecordingFidoHidConnection : IFidoHidConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:ddd57563894b9645 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolConnectionOwnershipTests.cs:98–100
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Dispose() => DisposeCount++;


PROPOSED SAMPLE — no verdict: core-001:unit:738bb07041058b8c / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolFactoryTests.cs:53–54
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private class FakeConnection : IConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:a30850a5a85e6f4a / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolFactoryTests.cs:73–75
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool SupportsExtendedApdu() => false;


PROPOSED SAMPLE — no verdict: core-001:unit:c7319c1c1182a4be / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationIdsTests.cs:1–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;




PROPOSED SAMPLE — no verdict: core-001:unit:5fc487677b35ab28 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationIdsTests.cs:77–89
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [MemberData(nameof(IdentifierNames))]
    public void Identifier_DoesNotHandOutTheSameArrayTwice(string identifierName)
    {
        PropertyInfo property = GetIdentifierProperty(identifierName);

        Assert.True(MemoryMarshal.TryGetArray(Read(property), out ArraySegment<byte> first));
        Assert.True(MemoryMarshal.TryGetArray(Read(property), out ArraySegment<byte> second));

        Assert.NotSame(first.Array, second.Array);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2dd4265675872883 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationIdsTests.cs:114–117
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static ReadOnlyMemory<byte> Read(PropertyInfo property) =>
        Assert.IsType<ReadOnlyMemory<byte>>(property.GetValue(null));


PROPOSED SAMPLE — no verdict: core-001:unit:5bcf834655fef594 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:1–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;




PROPOSED SAMPLE — no verdict: core-001:unit:bef6ce6dc7566d2b / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:104–116
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task DisposeAsync_ThenDispose_EntersCleanupExactlyOnce()
    {
        var session = new ProbeSession(new TrackingConnection());

        await session.DisposeAsync();
        session.Dispose();

        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Equal(1, session.AsyncCleanupCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:37c38232c3e94180 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:292–312
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task DisposeAsync_OwnedConnectionFailure_RunsManagedCleanupOnceAndSharesException()
    {
        var expected = new InvalidOperationException("async connection teardown failed");
        var connection = new TrackingConnection(expected);
        var session = new ProbeSession(connection, ownsConnection: true);

        Exception? first = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? second = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? third = Record.Exception(session.Dispose);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Same(expected, third);
        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ab707f00fedfc8e6 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:404–405
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly ManualResetEventSlim _resumeSyncBeforeBase = new(initialState: false);


PROPOSED SAMPLE — no verdict: core-001:unit:c4c984606fade307 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:408–409
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _managedCleanupCount;


PROPOSED SAMPLE — no verdict: core-001:unit:d08a207938a4d4fd / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:431–444
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>
        ///     Binds like a production factory. Tests that assert detachment MUST use this: a directly
        ///     constructed session is never bound, so a later construction would succeed whether or not
        ///     detachment happened, and the assertion would prove nothing.
        /// </summary>
        public static ProbeSession Create(
            IConnection connection,
            bool ownsConnection = false,
            IProtocol? protocol = null)
            => Construct(
                connection,
                () => new ProbeSession(connection, ownsConnection: ownsConnection, protocol: protocol));


PROPOSED SAMPLE — no verdict: core-001:unit:40c60fb8b4d02780 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:458–464
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void SetInitializedAndAuthenticated()
        {
            IsInitialized = true;
            IsAuthenticated = true;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:fefa3dfcb51c8048 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:500–505
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class TrackingConnection(
        Exception? asyncDisposeException = null,
        Exception? disposeException = null,
        bool pauseAsyncDisposal = false,
        bool pauseDisposal = false) : IConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:ae29023d98d446cd / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:505–507
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly TaskCompletionSource _asyncDisposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:e3bb2586dc5120b7 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:513–514
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _disposeCount;


PROPOSED SAMPLE — no verdict: core-001:unit:3b564994b3457b22 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:520–522
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task AsyncDisposalStarted => _asyncDisposalStarted.Task;


PROPOSED SAMPLE — no verdict: core-001:unit:d4ffb9d87b207fdb / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:536–548
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public async ValueTask DisposeAsync()
        {
            _ = Interlocked.Increment(ref _disposeAsyncCount);
            _asyncDisposalStarted.TrySetResult();

            if (pauseAsyncDisposal)
                await _resumeAsyncDisposal.Task.ConfigureAwait(false);

            if (asyncDisposeException is not null)
                throw asyncDisposeException;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:a0767c592da2a882 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:548–550
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void ResumeAsyncDisposal() => _resumeAsyncDisposal.TrySetResult();


PROPOSED SAMPLE — no verdict: core-001:unit:a943d9ad06221eb1 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:563–569
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Dispose()
        {
            _ = Interlocked.Increment(ref _disposeCount);
            throw disposeException;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:12fed26282d091eb / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawOtpHidSessionTests.cs:108–116
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void QueueStatusOnlyExchange(ScriptedOtpConnection connection, byte programmingSequence)
    {
        connection.Enqueue(Status(programmingSequence: programmingSequence));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(Status(programmingSequence: programmingSequence));
        connection.Enqueue(Status(programmingSequence: (byte)(programmingSequence + 1)));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b72162c065486e80 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionConstructionFailureTests.cs:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

[CollectionDefinition(Name, DisableParallelization = true)]



PROPOSED SAMPLE — no verdict: core-001:unit:f58ce9eb55ed24f4 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionConstructionFailureTests.cs:124–132
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool SupportsExtendedApdu()
        {
            if (Interlocked.Increment(ref _supportsCallCount) == 1)
                throw failure;

            return true;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:cf7a795602b142eb / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionConstructionFailureTests.cs:146–147
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType Type => ConnectionType.HidFido;


PROPOSED SAMPLE — no verdict: core-001:unit:343220c27faae036 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionYubiKeyExtensionsTests.cs:210–218
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedConnections.Add(typeof(TConnection));
            return Task.FromResult((TConnection)connection);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:139af565330d0bd8 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSmartCardNativeBoundaryTests.cs:28–73
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task BorrowedRawSession_CancelDuringNativeTransmit_RefusesOverlapAndDrainsBeforeSequentialReuse()
    {
        var api = new ControlledSCardConnectionApi();
        await using ISmartCardConnection connection =
            await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var raw = await RawSmartCardSession.CreateAsync(connection, Ct);

        try
        {
            Task<ApduResponse> first = raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xA1, 0, 0), cancellationToken: cancellation.Token);
            await api.FirstTransmitEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            await cancellation.CancelAsync();

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => raw.TransmitAndReceiveAsync(
                new ApduCommand(0, 0xB2, 0, 0), cancellationToken: Ct));
            Assert.False(first.IsCompleted);
            Assert.Equal(1, api.TransmitCalls);

            Task disposal = raw.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            _ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
                RawSmartCardSession.CreateAsync(connection, Ct));
            Assert.Equal(0, api.DisconnectCalls);

            api.ReleaseTransmit.Set();
            Assert.True((await first.WaitAsync(TimeSpan.FromSeconds(5), Ct)).IsOK());
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), Ct);
            Assert.Equal(0, api.DisconnectCalls);

            await using (RawSmartCardSession next = await RawSmartCardSession.CreateAsync(connection, Ct))
                Assert.True((await next.TransmitAndReceiveAsync(
                    new ApduCommand(0, 0xC3, 0, 0), cancellationToken: Ct)).IsOK());

            Assert.Equal(2, api.TransmitCalls);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            await raw.DisposeAsync();
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a10092607196bea8 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSmartCardSessionTests.cs:254–255
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly Queue<ReadOnlyMemory<byte>> _responses = new();


PROPOSED SAMPLE — no verdict: core-001:unit:c28fdc0c1ced3cdd / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/SessionConstructionGuardTests.cs:147–148
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class TrackingSession : ApplicationSession
    {


PROPOSED SAMPLE — no verdict: core-001:unit:f72a718d07e9a370 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/UserPresenceNotificationTests.cs:83–100
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task RequestFailure_PropagatesWithoutResolutionOrRetry()
    {
        var expected = new IOException("request failed");
        var prompt = new RecordingUserPresencePrompt(requestException: expected);
        UserPresenceNotification notification = UserPresenceNotification.Create(prompt, CreateContext());

        IOException actual = await Assert.ThrowsAsync<IOException>(() =>
            notification.RequestAsync(TestContext.Current.CancellationToken).AsTask());
        await notification.ResolveAsync(UserPresenceOutcome.Failed, actual);
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(1, prompt.RequestCount);
        Assert.Equal(0, prompt.ResolutionCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a0d008f7add4778f / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/UserPresenceNotificationTests.cs:224–225
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public UserPresenceContext? RequestContext { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:b3b9e834ec15a544 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/UserPresenceNotificationTests.cs:262–263
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int RequestCount { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:7449f0b2fc818733 / B5 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AppletSessionShapeTests.cs:15–16
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed class AppletSessionShapeTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:4018a6fd33733c9c / B5 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Abstractions/IConnection.cs:26–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public interface IConnection : IDisposable, IAsyncDisposable
{
