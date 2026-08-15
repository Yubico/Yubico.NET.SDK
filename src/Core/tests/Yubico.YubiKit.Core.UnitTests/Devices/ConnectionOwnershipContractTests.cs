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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     The connection-ownership contract, enforced at acquisition rather than on the wire.
/// </summary>
/// <remarks>
///     <para>
///         Two rules, one fact at two scopes. A CCID interface admits ONE live connection, and a connection
///         admits ONE live session. Both are refused before any command reaches the card, because a YubiKey's
///         CCID interface holds exactly one selected applet and a second applet SELECT deselects the first —
///         measured, SW=0x6D00, see docs/architecture/connection-ownership-and-contention.md.
///     </para>
///     <para>
///         The third rule is ownership: a protocol/session is a pure USER of the connection it is handed.
///         Whoever created the connection disposes it. That is what makes successive applet sessions over one
///         connection possible, which is the ergonomic price of the two refusal rules.
///     </para>
/// </remarks>
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class ConnectionOwnershipContractTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ------------------------------------------------------------------------------------------------
    // Rule 1 — one live connection per stateful or multi-report interface.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    ///     A second connection to a CCID interface that already has a live one is refused, and the message
    ///     names the interface so the caller can tell WHICH device is held.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SecondConnectionToHeldCcidInterface_IsRefused()
    {
        var factory = new CountingFactory();
        var device = CreateSmartCardDevice(factory);

        await using var first = await device.ConnectAsync<ISmartCardConnection>(Ct);

        var refusal = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        AssertExclusiveInterfaceRefusal(refusal, device.DeviceId);
        Assert.Equal(1, factory.CreateCalls); // refused before a second physical handle was opened
    }

    /// <summary>
    ///     Exclusive is not permanent: the interface is reusable the moment the holder is disposed. Sequential
    ///     use is the supported pattern, so the refusal above must not become a one-shot device.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_AfterFirstConnectionDisposed_SecondSucceeds()
    {
        var factory = new CountingFactory();
        var device = CreateSmartCardDevice(factory);

        var first = await device.ConnectAsync<ISmartCardConnection>(Ct);
        await first.DisposeAsync();

        await using var second = await device.ConnectAsync<ISmartCardConnection>(Ct);

        Assert.Equal(2, factory.CreateCalls);
    }

    [Fact]
    public async Task ConnectAsync_SecondConnectionToHeldOtpHidInterface_IsRefusedBeforePhysicalOpen()
    {
        var hidDevice = new FakeHidDevice(
            $"ownership-otp-{Guid.NewGuid():N}",
            HidInterfaceType.Otp);
        var device = CreateHidDevice(hidDevice);

        await using var first = await device.ConnectAsync<IOtpHidConnection>(Ct);

        var refusal = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => device.ConnectAsync<IOtpHidConnection>(Ct));

        AssertExclusiveInterfaceRefusal(refusal, device.DeviceId);
        Assert.Equal(1, hidDevice.FeatureReportConnectCalls);
    }

    [Fact]
    public async Task ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens()
    {
        var hidDevice = new FakeHidDevice(
            $"ownership-otp-{Guid.NewGuid():N}",
            HidInterfaceType.Otp);
        var device = CreateHidDevice(hidDevice);

        var first = await device.ConnectAsync<IOtpHidConnection>(Ct);
        await first.DisposeAsync();

        await using var second = await device.ConnectAsync<IOtpHidConnection>(Ct);

        Assert.Equal(2, hidDevice.FeatureReportConnectCalls);
    }

    /// <summary>
    ///     FIDO HID is now exclusive: one physical YubiKey FIDO HID interface admits exactly one SDK
    ///     connection/native HID handle at a time. A second connection is refused immediately before
    ///     opening the second native handle.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SecondConnectionToHeldFidoHidInterface_IsRefusedBeforePhysicalOpen()
    {
        var hidDevice = new FakeHidDevice(
            $"ownership-fido-{Guid.NewGuid():N}",
            HidInterfaceType.Fido);
        var device = CreateHidDevice(hidDevice);

        await using var first = await device.ConnectAsync<IFidoHidConnection>(Ct);

        var refusal = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => device.ConnectAsync<IFidoHidConnection>(Ct));

        AssertExclusiveInterfaceRefusal(refusal, device.DeviceId);
        Assert.Equal(1, hidDevice.IoReportConnectCalls); // refused before second native open
    }

    /// <summary>
    ///     Exclusive is not permanent: the FIDO HID interface is reusable the moment the holder is disposed.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_FidoHidConnectionDisposed_InterfaceReopens()
    {
        var hidDevice = new FakeHidDevice(
            $"ownership-fido-{Guid.NewGuid():N}",
            HidInterfaceType.Fido);
        var device = CreateHidDevice(hidDevice);

        var first = await device.ConnectAsync<IFidoHidConnection>(Ct);
        await first.DisposeAsync();

        await using var second = await device.ConnectAsync<IFidoHidConnection>(Ct);

        Assert.Equal(2, hidDevice.IoReportConnectCalls);
    }

    /// <summary>
    ///     INVARIANT PIN (must pass before and after). The independent interfaces of one physical key remain
    ///     independent: a held CCID interface must not block that key's HID interface. Management-over-HID
    ///     fallback is achieved through held-transport exception detection in YubiKeyConnectionExtensions,
    ///     not through concurrent connection sharing. A held FIDO interface triggers fallback to OTP if
    ///     present.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_CcidHeld_SameKeysHidInterfaceStillConnects()
    {
        var smartCard = CreateSmartCardDevice(new CountingFactory());
        var hid = CreateHidDevice(new FakeHidDevice(
            $"ownership-fido-{Guid.NewGuid():N}",
            HidInterfaceType.Fido));

        await using var ccid = await smartCard.ConnectAsync<ISmartCardConnection>(Ct);
        await using var fido = await hid.ConnectAsync<IFidoHidConnection>(Ct);

        Assert.NotNull(fido);
    }

    // ------------------------------------------------------------------------------------------------
    // Rule 2 — one live session per connection (Python's model, enforced at binding).
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    ///     Rule 1 cannot see this case: one connection, opened once, handed to two sessions. That is exactly
    ///     what Python's base Session guards, and it is the same applet-deselection hazard one level down.
    /// </summary>
    [Fact]
    public async Task Session_SecondLiveSessionOnOneConnection_IsRefused()
    {
        await using var connection = new RecordingSmartCardConnection();

        await using var first = await ProbeSession.CreateAsync(connection, Ct);

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => ProbeSession.CreateAsync(connection, Ct));
    }

    /// <summary>
    ///     Refusal is per LIVE session, not per connection lifetime — successive sessions are the supported
    ///     way to use two applets over one connection.
    /// </summary>
    [Fact]
    public async Task Session_AfterFirstDisposed_SecondSessionOnSameConnectionSucceeds()
    {
        await using var connection = new RecordingSmartCardConnection();

        var first = await ProbeSession.CreateAsync(connection, Ct);
        await first.DisposeAsync();

        await using var second = await ProbeSession.CreateAsync(connection, Ct);

        Assert.True(second.IsInitialized);
    }

    // ------------------------------------------------------------------------------------------------
    // Rule 3 — a session is a pure user of the connection it is handed.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    ///     Disposing a session must leave a caller-created connection alive. The session did not create it,
    ///     so it does not get to destroy it.
    /// </summary>
    [Fact]
    public async Task Session_Dispose_DoesNotDisposeACallerCreatedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        var session = await ProbeSession.CreateAsync(connection, Ct);
        await session.DisposeAsync();

        Assert.Equal(0, connection.DisposeCount);

        // ...and the connection is still usable, which is the point of not disposing it.
        _ = await connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);
    }

    /// <summary>
    ///     The end-to-end shape the refusal rules require: one connection, successive applet sessions, no
    ///     reconnect. This is Rust's <c>into_connection</c> handoff expressed in C# lifetimes.
    /// </summary>
    [Fact]
    public async Task SuccessiveSessions_OverOneConnection_BothReachTheWire()
    {
        await using var connection = new RecordingSmartCardConnection();

        await using (var first = await ProbeSession.CreateAsync(connection, Ct))
            await first.SelectAsync(Ct);

        await using (var second = await ProbeSession.CreateAsync(connection, Ct))
            await second.SelectAsync(Ct);

        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(4, connection.TransmitCalls); // 2 sessions x (init select + explicit select)
    }

    /// <summary>
    ///     A protocol handed a connection is a user, not an owner.
    /// </summary>
    [Fact]
    public void PcscProtocol_Dispose_DoesNotDisposeTheConnection()
    {
        var connection = new RecordingSmartCardConnection();
        var protocol = new PcscProtocol(connection);

        protocol.Dispose();

        Assert.Equal(0, connection.DisposeCount);
    }

    // ------------------------------------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------------------------------------

    private static void AssertExclusiveInterfaceRefusal(ConnectionInUseException refusal, string deviceId)
    {
        Assert.Contains($"exclusive interface '{deviceId}'", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SmartCard", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("CCID", refusal.Message, StringComparison.Ordinal);
    }

    private static PcscYubiKey CreateSmartCardDevice(ISmartCardConnectionFactory factory) =>
        new(
            new PcscDevice { ReaderName = $"ownership-reader-{Guid.NewGuid():N}", Atr = null },
            factory,
            NullLogger<PcscYubiKey>.Instance);

    private static HidYubiKey CreateHidDevice(IHidDevice hidDevice) =>
        new(hidDevice, NullLogger<HidYubiKey>.Instance);

    private sealed class CountingFactory : ISmartCardConnectionFactory
    {
        private int _createCalls;

        public int CreateCalls => Volatile.Read(ref _createCalls);

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            _ = Interlocked.Increment(ref _createCalls);
            return Task.FromResult<ISmartCardConnection>(new RecordingSmartCardConnection());
        }
    }

    private sealed class FakeHidDevice(string name, HidInterfaceType interfaceType) : IHidDevice
    {
        private int _featureReportConnectCalls;
        private int _ioReportConnectCalls;

        public string ReaderName { get; } = name;

        public HidDescriptorInfo DescriptorInfo { get; } = new()
        {
            VendorId = 0x1050,
            ProductId = 0x0407,
            UsagePage = interfaceType == HidInterfaceType.Fido ? (ushort)0xF1D0 : (ushort)0x0001,
            Usage = interfaceType == HidInterfaceType.Fido ? (ushort)0x0001 : (ushort)0x0006
        };

        public HidInterfaceType InterfaceType { get; } = interfaceType;

        public int FeatureReportConnectCalls => Volatile.Read(ref _featureReportConnectCalls);
        public int IoReportConnectCalls => Volatile.Read(ref _ioReportConnectCalls);

        public IHidConnection ConnectToFeatureReports()
        {
            _ = Interlocked.Increment(ref _featureReportConnectCalls);
            return new InertHidConnection(ConnectionType.HidOtp);
        }

        public IHidConnection ConnectToIOReports()
        {
            _ = Interlocked.Increment(ref _ioReportConnectCalls);
            return new InertHidConnection(ConnectionType.HidFido);
        }
    }

    private sealed class InertHidConnection(ConnectionType type) : IHidConnection
    {
        public ConnectionType Type { get; } = type;

        public int InputReportSize => 64;

        public int OutputReportSize => 64;

        public void SetReport(byte[] report)
        {
        }

        public byte[] GetReport() => new byte[64];

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    ///     A SmartCard connection that answers every APDU with 0x9000 and counts disposals, so a test can tell
    ///     "was it torn down" apart from "was it merely finished with".
    /// </summary>
    private sealed class RecordingSmartCardConnection : ISmartCardConnection
    {
        private int _disposeCount;
        private int _transmitCalls;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public int TransmitCalls => Volatile.Read(ref _transmitCalls);

        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(DisposeCount > 0, this);
            _ = Interlocked.Increment(ref _transmitCalls);
            return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x90, 0x00 });
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose() => Interlocked.Increment(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     The smallest real <see cref="ApplicationSession" />: it selects an applet and initializes, which is
    ///     all the ownership and one-session-per-connection rules act on. Core cannot reference PIV or OATH, and
    ///     it should not have to — the rules live in the base session, not in any applet.
    /// </summary>
    private sealed class ProbeSession : ApplicationSession
    {
        private readonly ISmartCardProtocol _protocol;

        private ProbeSession(ISmartCardConnection connection)
            : base(connection)
        {
            _protocol = ProtocolFactory.Create(connection);
            Protocol = _protocol;
        }

        public static async Task<ProbeSession> CreateAsync(
            ISmartCardConnection connection,
            CancellationToken cancellationToken)
        {
            var session = Construct(connection, () => new ProbeSession(connection));
            try
            {
                await session.SelectAsync(cancellationToken);
                await session.InitializeProtocolAsync(
                    session._protocol,
                    new FirmwareVersion(5, 7, 2),
                    cancellationToken: cancellationToken);
                return session;
            }
            catch
            {
                await session.DisposeAsync();
                throw;
            }
        }

        public Task SelectAsync(CancellationToken cancellationToken) =>
            _protocol.SelectAsync(ApplicationIds.Management, cancellationToken);
    }
}