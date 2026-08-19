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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class RawSmartCardSessionTests
{
    [Fact]
    public async Task CreateAsync_WhileRawSessionHoldsConnection_RefusesAppletBeforeWireIo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        await using var raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);

        _ = Assert.Throws<ConnectionInUseException>(() => StubAppletSession.Create(connection));

        Assert.Empty(connection.Commands);
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        ApduResponse response = await raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 1, 2, 3),
            cancellationToken: cancellationToken);
        Assert.True(response.IsOK());
    }

    [Fact]
    public async Task SelectAsync_SendsOnlyCallerAidAndCreationPerformsNoWireIo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        await using var raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);
        Assert.Empty(connection.Commands);
        connection.EnqueueResponse(new byte[] { 0x01, 0x02, 0x90, 0x00 });

        ReadOnlyMemory<byte> result = await raw.SelectAsync(
            new byte[] { 0xA0, 0x00, 0x01 },
            cancellationToken);

        Assert.Equal(new byte[] { 0x01, 0x02 }, result.ToArray());
        byte[] command = Assert.Single(connection.Commands).ToArray();
        Assert.Equal(new byte[] { 0x00, 0xA4, 0x04, 0x00 }, command[..4]);
        Assert.Equal(new byte[] { 0xA0, 0x00, 0x01 }, command.AsSpan(7, 3).ToArray());
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_WithThrowOnErrorFalse_ReturnsDataAndStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0xCA, 0xFE, 0x69, 0x82 });
        await using var raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);

        ApduResponse response = await raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 1, 2, 3),
            throwOnError: false,
            cancellationToken);

        Assert.Equal(new byte[] { 0xCA, 0xFE }, response.Data.ToArray());
        Assert.Equal(0x6982, (ushort)response.SW);
    }

    [Fact]
    public async Task Configure_ForceShortApdus_ChangesFormattingAndRecordsFirmwareVersion()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        await using var raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);

        raw.Configure(
            new FirmwareVersion(5, 7, 0),
            new ProtocolConfiguration { ForceShortApdus = true });
        _ = await raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 1, 2, 3, new byte[] { 0xAA, 0xBB, 0xCC }),
            cancellationToken: cancellationToken);

        Assert.Equal(new FirmwareVersion(5, 7, 0), raw.FirmwareVersion);
        Assert.Equal(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x03, 0xAA, 0xBB, 0xCC, 0x00 },
            Assert.Single(connection.Commands).ToArray());
    }

    [Fact]
    public async Task Dispose_BorrowedConnection_RemainsOpenForSequentialSession()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        var first = await RawSmartCardSession.CreateAsync(connection, cancellationToken);

        await first.DisposeAsync();

        Assert.Equal(0, connection.DisposeCount);
        await using var applet = StubAppletSession.Create(connection);
        Assert.NotNull(applet);
    }

    [Fact]
    public async Task AppletThenRawThenApplet_ReusesOneConnectionSequentially()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        using (StubAppletSession firstApplet = StubAppletSession.Create(connection))
        {
            await Assert.ThrowsAsync<ConnectionInUseException>(
                () => RawSmartCardSession.CreateAsync(connection, cancellationToken));
        }

        await using (RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken))
        {
            Assert.NotNull(raw);
        }

        using StubAppletSession secondApplet = StubAppletSession.Create(connection);
        Assert.NotNull(secondApplet);
    }

    [Fact]
    public async Task CreateAsync_ScpInitializationFails_ReleasesClaimAndLeavesBorrowedConnectionOpen()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => RawSmartCardSession.CreateAsync(
                connection,
                scp,
                new FirmwareVersion(5, 7, 2),
                cancellationToken: cancellationToken));

        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(0x50, Assert.Single(connection.Commands).Span[1]);
        using StubAppletSession next = StubAppletSession.Create(connection);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task CreateAsync_WithScp_ConfiguresBaseProcessorBeforeEstablishment()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        using var scp = Scp03KeyParameters.Default;
        Task<RawSmartCardSession> creation = RawSmartCardSession.CreateAsync(
            connection,
            scp,
            new FirmwareVersion(5, 7, 2),
            new ProtocolConfiguration { ForceShortApdus = true },
            cancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => creation);

        ReadOnlyMemory<byte> initializeUpdate = Assert.Single(connection.Commands);
        Assert.Equal(0x50, initializeUpdate.Span[1]);
        Assert.Equal(8, initializeUpdate.Span[4]);
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_OverlappingOperationThrowsThenSequentialCallSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new RecordingSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        await using var raw = await RawSmartCardSession.CreateAsync(connection, cancellationToken);
        connection.HoldNextTransmission();

        Task<ApduResponse> first = raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 1, 0, 0),
            cancellationToken: cancellationToken);
        await connection.TransmissionStarted.Task.WaitAsync(cancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 2, 0, 0),
            cancellationToken: cancellationToken));

        connection.ReleaseTransmission();
        Assert.True((await first).IsOK());
        Assert.True((await raw.TransmitAndReceiveAsync(
            new ApduCommand(0, 3, 0, 0),
            cancellationToken: cancellationToken)).IsOK());
    }

    private sealed class StubAppletSession : ApplicationSession
    {
        private StubAppletSession(ISmartCardConnection connection)
            : base(connection)
        {
        }

        public static StubAppletSession Create(ISmartCardConnection connection) =>
            Construct(connection, () => new StubAppletSession(connection));
    }

    private sealed class RecordingSmartCardConnection : ISmartCardConnection
    {
        private readonly Queue<ReadOnlyMemory<byte>> _responses = new();
        private TaskCompletionSource? _transmissionHold;

        public List<ReadOnlyMemory<byte>> Commands { get; } = [];
        public int DisposeCount { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public TaskCompletionSource TransmissionStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void EnqueueResponse(ReadOnlyMemory<byte> response) => _responses.Enqueue(response);

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.ToArray());
            TaskCompletionSource? hold = _transmissionHold;
            if (hold is not null)
            {
                TransmissionStarted.TrySetResult();
                await hold.Task.WaitAsync(cancellationToken);
                _transmissionHold = null;
            }

            return _responses.Dequeue();
        }

        public void HoldNextTransmission() =>
            _transmissionHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseTransmission() => _transmissionHold?.TrySetResult();

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;
        public void Dispose()
        {
            DisposeCount++;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}