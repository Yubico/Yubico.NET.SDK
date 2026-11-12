PROPOSED SAMPLE — no verdict: core-001:unit:a7c4a6b478cc9c7a / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/ConnectionInUseException.cs:1–56
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Thrown when a requested YubiKey connection or session conflicts with an existing holder, including
///     another PC/SC client when opening with strict exclusive access.
/// </summary>
/// <remarks>
///     <para>
///         Connection and session acquisition raise this for the following conflicts:
///     </para>
///     <list type="bullet">
///         <item>
///             Opening a second connection to any known interface of a grouped physical YubiKey. The first
///             connection claims every stable member interface ID, so the refusal occurs before native open.
///         </item>
///         <item>
///             Creating a second session on a connection that already hosts a live one. Same mechanism,
///             one level down: the new session's SELECT runs on the same card channel.
///         </item>
///         <item>
///             Opening a built-in PC/SC connection with
///             <see cref="Transports.SmartCard.SmartCardSharing.Exclusive" /> when another smart-card client
///             holds the card. The native <c>SCARD_E_SHARING_VIOLATION</c> is wrapped in this exception, with a
///             <see cref="Native.Desktop.SCard.SCardException" /> as its <see cref="Exception.InnerException" />.
///             The competing client may be in this process or another process; no shared retry occurs.
///         </item>
///     </list>
///     <para>
///         Managed connection and session conflicts are refused at acquisition, before any command reaches
///         the device, so the exception lands on
///         the call that would have caused the damage rather than on the victim's next operation. Dispose the
///         current holder first; a connection may host any number of sessions in sequence, and an exclusive
///         physical key any number of connections in sequence.
///     </para>
///     <para>
///         For native PC/SC contention, close the competing client before retrying. Applications that accept
///         shared card-wide state may explicitly select <see cref="Transports.SmartCard.SmartCardSharing.Shared" />
///         or <see cref="Transports.SmartCard.SmartCardSharing.ExclusiveWithSharedFallback" /> before factory
///         construction. Those policies do not bypass managed ownership guards; a failed shared open surfaces
///         as <see cref="Native.Desktop.SCard.SCardException" /> rather than this strict-exclusive mapping.
///     </para>
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:6fafc3d16be878bf / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceConnectionRegistry.cs:59–63
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Whether this process currently holds at least one live connection to the interface.</summary>
    public static bool IsInUse(string deviceId) =>
        Interfaces.TryGetValue(deviceId, out var ownership) && ownership.HasConnections;


PROPOSED SAMPLE — no verdict: core-001:unit:1f43bb31b9a99e4c / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceConnectionRegistry.cs:270–277
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>Completes when a discovery-lease release clears an unrecovered state; already complete if none.</summary>
        public Task WaitForRecovery()
        {
            lock (_sync)
                return _recoveryCompletion?.Task ?? Task.CompletedTask;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:63aa8710ae155517 / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISmartCardConnection.cs:21–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     True when this connection holds the card exclusively; see <see cref="SmartCardSharing" /> for what a
    ///     shared handle means. Custom implementations that do not model sharing report false.
    /// </summary>
    bool IsExclusive => false;


PROPOSED SAMPLE — no verdict: core-001:unit:5f8bc8be883eb29d / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs:38–53
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


/// <summary>The built-in smart-card connection factory.</summary>
/// <remarks>
///     Custom factories implement <see cref="ISmartCardConnectionFactory" /> rather than deriving from this type.
///     Configure connection logging through <see cref="YubiKitLogging" />.
///     Without an explicit sharing policy, new factories snapshot <see cref="DefaultSharing" />, initially
///     <see cref="SmartCardSharing.Exclusive" />. Contention fails with <see cref="ConnectionInUseException" />
///     rather than retrying shared access. All built-in sharing modes request card power-off on release.
///     Direct <see cref="CreateAsync" /> calls do not acquire the physical-device ownership guard used by
///     <see cref="Abstractions.IYubiKey.ConnectAsync{TConnection}" />. Coordinate discovery, including background
///     monitoring, and other connections to the key for the full direct connection lifetime. With shared
///     access, a discovery metadata read or another handle can select a different applet and disturb a session.
///     Prefer <c>device.ConnectAsync&lt;ISmartCardConnection&gt;()</c> for SDK-managed ownership; the sharing policy
///     still determines whether other processes can open the card.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:a7472e3bb01063ea / B1 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/RecordingSmartCardConnection.cs:30–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the commands transmitted through this connection, in order.
    /// </summary>
    public List<byte[]> TransmittedCommands { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:23c94a53a4cf4727 / B1 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:510–511
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        Beginning,


PROPOSED SAMPLE — no verdict: core-001:unit:73f56f81969d98ff / B1 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/SmartCardSharing.cs:42–45
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>The factory default: open exclusively or fail with <see cref="Devices.ConnectionInUseException" />; never retry shared.</summary>
    Exclusive,


PROPOSED SAMPLE — no verdict: core-001:unit:7c7ab9076a83cc1e / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Fido/FidoCommands.cs:246–248
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:329317501a183f73 / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs:207–208
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:914c925013cf6445 / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/OpenPgp/OpenPgpKeysCommands.cs:136–137
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:b894464f9dd75094 / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Otp/OtpCommands.cs:576–578
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];


PROPOSED SAMPLE — no verdict: core-001:unit:b7cd32c941a627dc / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Piv/PivCommands.cs:383–384
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:4b49917685ba7b55 / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Piv/PivCommands.cs:570–571
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:33ef7a843647f935 / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Commands/src/Piv/PivCommands.cs:653–654
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];


PROPOSED SAMPLE — no verdict: core-001:unit:25c478c7304f56dc / B1 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/RecordingSmartCardConnection.cs:68–74
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return default;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:751938c8fd251302 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DeviceConnectionRegistry.cs:161–162
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _connectionCount;


PROPOSED SAMPLE — no verdict: core-001:unit:7391db03d44cc0a6 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DisposalGate.cs:53–82
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private async Task TearDownAsync(TaskCompletionSource claim, Func<ValueTask> teardown)
    {
        Exception? failure = null;
        try
        {
            try
            {
                await teardown().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failure = ex;
                onFailure?.Invoke(ex);
                throw;
            }
            finally
            {
                if (failure is null || releaseLeaseOnFailure)
                    lease?.Dispose();
            }

            claim.SetResult();
        }
        catch (Exception ex)
        {
            claim.SetException(ex);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6c7f0156ee70e8b9 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/HidConnectionSlot.cs:126–131
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:806535e6212119aa / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/PcscConnectionSlot.cs:27–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal PcscConnectionSlot(
        IPcscDevice pcscDevice,
        ISmartCardConnectionFactory smartCardConnectionFactory)
    {
        _pcscDevice = pcscDevice;
        _smartCardConnectionFactory = smartCardConnectionFactory;
        InterfaceId = $"pcsc:{pcscDevice.ReaderName}";
    }


PROPOSED SAMPLE — no verdict: core-001:unit:fd20a8a6daf993ee / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/DesktopSmartCardDeviceListener.cs:34–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(1000);


PROPOSED SAMPLE — no verdict: core-001:unit:3f826327a54d8ea5 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/DesktopSmartCardDeviceListener.cs:42–43
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private SCardContext? _context;


PROPOSED SAMPLE — no verdict: core-001:unit:b0da9a6ee7f248bd / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/FindPcscDevices.cs:164–171
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static bool IsIntegratedSmartCardYubiKeyReader(string readerName)
    {
        var pid = ReaderNamePidParser.FromReaderName(readerName);
        return pid is { } value
            && ReaderNamePidParser.ExpectedConnectionsForPid(value).SupportsConnection(ConnectionType.SmartCard);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1cbc332bcc33a27a / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISCardApi.cs:35–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static NativeSCardApi Instance { get; } = new();


PROPOSED SAMPLE — no verdict: core-001:unit:e537dcedcf111d4c / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISCardConnectionApi.cs:21–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    uint Connect(
        SCardContext context,
        string readerName,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        out SCardCardHandle cardHandle,
        out SCARD_PROTOCOL activeProtocol);


PROPOSED SAMPLE — no verdict: core-001:unit:a2543b389fd3b603 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISCardConnectionApi.cs:29–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    uint BeginTransaction(SCardCardHandle cardHandle);


PROPOSED SAMPLE — no verdict: core-001:unit:6f9709c1f760afd3 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISCardConnectionApi.cs:40–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    uint Disconnect(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition);


PROPOSED SAMPLE — no verdict: core-001:unit:e7b21994e1483471 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/ISmartCardConnection.cs:70–71
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    // byte[] getAtr();



PROPOSED SAMPLE — no verdict: core-001:unit:9715fd23161cbcd7 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:1–19
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

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;




PROPOSED SAMPLE — no verdict: core-001:unit:5e7f801af3e0b797 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly Action<Thread> _startWorker;


PROPOSED SAMPLE — no verdict: core-001:unit:2426f3a0239dcbe2 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly AutoResetEvent _signal = new(false);


PROPOSED SAMPLE — no verdict: core-001:unit:0ffbd3041f68ccc4 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:33–34
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private NativeWorkItem? _executingWork;


PROPOSED SAMPLE — no verdict: core-001:unit:d261ddd7d88711cf / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:509–510
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private enum TransactionPhase
    {


PROPOSED SAMPLE — no verdict: core-001:unit:5cc1208a32fa99dc / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:524–525
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public SCARD_DISPOSITION ScopeDisposition { get; } = scopeDisposition;


PROPOSED SAMPLE — no verdict: core-001:unit:49276f849fb765c9 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:558–559
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private T _result;


PROPOSED SAMPLE — no verdict: core-001:unit:a1e401ec2b99f21c / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs:592–594
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public override void Fail(Exception failure) => _completion.TrySetException(failure);


PROPOSED SAMPLE — no verdict: core-001:unit:9ea9c554224581f3 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscNativeResources.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly ISCardConnectionApi _api;


PROPOSED SAMPLE — no verdict: core-001:unit:41b892356800d072 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/PcscNativeResources.cs:103–108
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public uint EndTransaction(SCARD_DISPOSITION disposition) =>
        _cardHandle is null || _cardHandle.IsInvalid
            ? ErrorCode.SCARD_S_SUCCESS
            : _api.EndTransaction(_cardHandle, disposition);


PROPOSED SAMPLE — no verdict: core-001:unit:134f7ca39a4d364a / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs:54–55
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed class SmartCardConnectionFactory : ISmartCardConnectionFactory, IRegisteredSmartCardConnectionFactory
{


PROPOSED SAMPLE — no verdict: core-001:unit:b1a1e66c96c6de98 / B1 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:132–153
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        var outputBuffer = ArrayPool<byte>.Shared.Rent(MaxExtendedApduResponseSize);
        try
        {
            var responseLength = await _nativeState.RunAsync(
                () => _nativeState.Transmit(command.Span, outputBuffer),
                cancellationToken).ConfigureAwait(false);

            var response = new byte[responseLength];
            outputBuffer.AsSpan(0, responseLength).CopyTo(response);
            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outputBuffer, clearArray: true);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f1d4372202bdaa9b / B1 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/architecture/connection-ownership-and-contention.md:12–32
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Why connection ownership is enforced

A YubiKey's CCID interface has one selected applet on the basic channel. The following sequence was
measured on hardware:

```csharp
await using var piv = await yubiKey.CreatePivSessionAsync();
await piv.VerifyPinAsync(pin);
var info = await yubiKey.GetDeviceInfoAsync();
```

Before ownership enforcement, the Management `SELECT` deselected PIV. The next PIV operation failed
with `SW=0x6D00` (*instruction not supported*). The intervening Management operation itself succeeded,
so wire-time detection was too late. The SDK therefore refuses the conflicting acquisition before
opening another native handle.

The hardware investigation also established that Management can communicate over HID while CCID is
held. That remains useful protocol evidence, but it is no longer a supported parallel ownership mode.
The supported usage contract remains one connection for the physical key, regardless of interface;
enforcement is bounded by the known-member scope described below.




PROPOSED SAMPLE — no verdict: core-001:unit:76bce9eaa7b001ed / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/FidoHidOwnershipIntegrationTests.cs:48–62
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_SecondConcurrentFidoHidConnection_IsRefused(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var first = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d6201231a3c57f13 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/FidoHidOwnershipIntegrationTests.cs:87–110
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<ReadOnlyMemory<byte>> ReceiveInitEchoAsync(
        IFidoHidConnection connection,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var response = await connection.ReceiveAsync(cancellationToken);
            var span = response.Span;

            if (span.Length < 7 + NonceLength)
                continue;
            if (BinaryPrimitives.ReadUInt32BigEndian(span[..4]) != BroadcastChannel)
                continue;
            if (span[4] != CtapHidInit)
                continue;

            return response[7..(7 + NonceLength)];
        }

        throw new InvalidOperationException(
            "No CTAPHID_INIT response was received on the broadcast channel within the frame budget.");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5e913abec1e8ec34 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Transports/SmartCard/PcscLifetimeIntegrationTests.cs:22–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public Task InitializeAsync() => Task.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:2d70f4ce6efb7f43 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Transports/SmartCard/PcscLifetimeIntegrationTests.cs:53–92
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task PcscLifetime_SequentialTransactionsAndConnectionReopens_ReadStableDeviceInfo()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = timeout.Token;
        var candidates = AuthorizedDevices
            .GetByConnectionType(ConnectionType.SmartCard)
            .Where(device => device.IsUsbTransport && device.SerialNumber is not null)
            .OrderByDescending(device => device.SerialNumber)
            .ToList();

        Assert.NotEmpty(candidates);
        var selected = candidates[0];
        var device = selected.Device;
        var expectedSerialNumber = selected.SerialNumber!.Value;
        var expectedFirmwareVersion = selected.FirmwareVersion;
        output.WriteLine(
            $"Selected authorized USB smart-card device: serial={expectedSerialNumber}, " +
            $"firmware={expectedFirmwareVersion}, formFactor={selected.FormFactor}");

        for (var connectionCycle = 0; connectionCycle < 3; connectionCycle++)
        {
            await using (var connection = await device
                             .ConnectAsync<ISmartCardConnection>(cancellationToken))
            {
                for (var transactionCycle = 0; transactionCycle < 2; transactionCycle++)
                {
                    using (connection.BeginTransaction(cancellationToken))
                    {
                        var deviceInfo = await ProtocolDeviceInfo.ReadAsync(connection, cancellationToken);
                        Assert.Equal(expectedSerialNumber, deviceInfo.SerialNumber);
                        Assert.Equal(expectedFirmwareVersion, deviceInfo.FirmwareVersion);
                    }
                }
            }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:42875de817e3bdfe / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Transports/SmartCard/SmartCardDeviceListenerIntegrationTests.cs:32–69
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Start_TransitionsToStartedStatus()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = DesktopSmartCardDeviceListener.Create();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        try
        {
            // Act
            listener.Start();

            if (listener.Status != DeviceListenerStatus.Started)
            {
                _output.WriteLine($"Test skipped: listener did not start (status: {listener.Status})");
                return;
            }

            // Assert
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
            _output.WriteLine("SmartCard listener started successfully");
        }
        finally
        {
            listener?.Dispose();
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4e9e2576c70c8c9b / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Transports/SmartCard/SmartCardDeviceListenerIntegrationTests.cs:111–143
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Dispose_AfterStart_SetsStatusToStopped()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = DesktopSmartCardDeviceListener.Create();
            listener.Start();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        if (listener.Status != DeviceListenerStatus.Started)
        {
            _output.WriteLine($"Test skipped: listener did not start (status: {listener.Status})");
            listener.Dispose();
            return;
        }

        // Act
        listener.Dispose();

        // Assert
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        _output.WriteLine("Status correctly set to Stopped after disposal");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:decf8ef6a2d8b21b / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ApplicationSessionScpTests.cs:233–250
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ConfigureProtocol_WithoutScpOnNonSmartCardProtocol_Succeeds()
    {
        var session = TestSession.Create(new InertConnection());
        using var protocol = new NonSmartCardProtocol();

        await session.RunInitializeAsync(
            protocol,
            new FirmwareVersion(5, 7, 0),
            scpKeyParams: null,
            TestContext.Current.CancellationToken);

        Assert.Same(protocol, session.Protocol);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c36dc6665f2f26a9 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ApplicationSessionScpTests.cs:475–481
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
            if (configureException is not null)
                throw configureException;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:21b621ff07b21b15 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ApplicationSessionScpTests.cs:503–504
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FailingFidoConnection : IFidoHidConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:76de1c869bb5ef35 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ApplicationSessionScpTests.cs:504–505
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType Type => ConnectionType.HidFido;


PROPOSED SAMPLE — no verdict: core-001:unit:b874814041bef7a7 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ApplicationSessionScpTests.cs:506–507
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int SendCount { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:77e334d16d6b14fa / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionOwnershipContractTests.cs:326–338
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ------------------------------------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------------------------------------

    private static void AssertExclusiveInterfaceRefusal(ConnectionInUseException refusal, string deviceId)
    {
        Assert.Contains($"held interface: '{deviceId}'", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("one live connection at a time across all interfaces", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SmartCard", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("CCID", refusal.Message, StringComparison.Ordinal);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c462c54dfc3b511c / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionOwnershipContractTests.cs:367–368
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class CountingFactory : ISmartCardConnectionFactory
    {


PROPOSED SAMPLE — no verdict: core-001:unit:1db77308e3dc0e0d / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionOwnershipContractTests.cs:431–433
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public ValueTask DisposeAsync() => ValueTask.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:0b910444e6b2dc1e / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionOwnershipTests.cs:164–169
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static YubiKeyDevice CreateDevice(PcscConnectionSlot slot)
    {
        return new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4533d20585009bbe / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionOwnershipTests.cs:218–219
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FailThenSucceedFactory : ISmartCardConnectionFactory
    {


PROPOSED SAMPLE — no verdict: core-001:unit:131eb4b992393eff / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionOwnershipTests.cs:255–256
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class BlockingTransmitConnection : ThrowingSmartCardConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:e8b5cb0b222fb273 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs:379–395
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>I1, I2 — repeated sequential disposal is a no-op after the first.</summary>
    [Fact]
    public async Task RegisteredSmartCardConnection_RepeatedDispose_DisposesInnerOnce()
    {
        var inner = new FakeSmartCardConnection();
        var lease = new CountingLease();
        var wrapped = new RegisteredSmartCardConnection(inner, lease);

        wrapped.Dispose();
        wrapped.Dispose();
        await wrapped.DisposeAsync();

        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b39ebdf0080ad057 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs:436–437
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int ConnectCalls { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:253ea51a06b003ae / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs:462–463
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _disposeCount;


PROPOSED SAMPLE — no verdict: core-001:unit:f0d6a034c70c13ef / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs:491–501
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public async ValueTask DisposeAsync()
        {
            var ordinal = Interlocked.Increment(ref _disposeCount);
            DisposeEntered.Set();
            if (ordinal == 1 && DisposeGate is not null)
                await DisposeGate.Task.ConfigureAwait(false);

            Finish();
        }


PROPOSED SAMPLE — no verdict: core-001:unit:cf2d3ad0f7214673 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HeldExceptionPropagationTests.cs:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     A held <see cref="SCardException" /> from a SmartCard connect must propagate unchanged, without
///     cross-transport fallback. These tests pin the published-device and concrete PC/SC slot connect chains
///     against a future wrapping regression.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:57c9e23533f4dbbb / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HeldExceptionPropagationTests.cs:71–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly string _deviceId = $"member:{available}:{Guid.NewGuid():N}";


PROPOSED SAMPLE — no verdict: core-001:unit:61a56a695ce12781 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HeldExceptionPropagationTests.cs:88–91
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice, CancellationToken cancellationToken = default) =>
            Task.FromException<ISmartCardConnection>(exception);


PROPOSED SAMPLE — no verdict: core-001:unit:4a349d2a9cab87d4 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HidConnectionSlotTests.cs:24–45
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Theory]
    [InlineData(HidInterfaceType.Fido, 0x0001, "hid:test-hid:0001", typeof(IFidoHidConnection))]
    [InlineData(HidInterfaceType.Otp, 0x00AF, "hid:test-hid:00AF", typeof(IOtpHidConnection))]
    public async Task HidSlot_UsesExactUppercasePaddedInterfaceId_AndOpensTypedConnection(
        HidInterfaceType interfaceType,
        ushort usage,
        string expectedInterfaceId,
        Type expectedConnectionType)
    {
        var device = new FakeHidInterface("test-hid", interfaceType, usage);
        var slot = new HidConnectionSlot(device);
        var connectionType = ConnectionTypeMapper.ToConnectionType(interfaceType);

        await using var connection = await slot.OpenRawConnectionAsync(
            connectionType,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedInterfaceId, slot.InterfaceId);
        Assert.Equal(connectionType, slot.ConnectionType);
        Assert.IsAssignableFrom(expectedConnectionType, connection);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8a5dcadb26779e91 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HidConnectionSlotTests.cs:45–53
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void HidSlot_UnsupportedInterfaceType_ThrowsAtConstruction()
    {
        var device = new FakeHidInterface("test-hid", HidInterfaceType.Unknown, 0x0001);

        Assert.Throws<NotSupportedException>(() => new HidConnectionSlot(device));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5ee9c6e686ace85f / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HidConnectionSlotTests.cs:92–93
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public byte[] GetReport() => new byte[64];


PROPOSED SAMPLE — no verdict: core-001:unit:23d950d8aed651a1 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PcscConnectionSlotTests.cs:39–51
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task PcscSlot_WrongConnectionType_ThrowsWithoutTouchingFactory()
    {
        var factory = new RecordingSmartCardConnectionFactory(new FakeSmartCardConnection());
        var slot = new PcscConnectionSlot(new PcscDevice { ReaderName = "reader", Atr = null }, factory);

        await Assert.ThrowsAsync<NotSupportedException>(() => slot.OpenRawConnectionAsync(
            ConnectionType.HidFido,
            TestContext.Current.CancellationToken));
        Assert.Null(factory.LastDevice);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f5217fdf9ac66dfb / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PcscConnectionSlotTests.cs:56–64
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            LastDevice = smartCardDevice;
            return Task.FromResult(connection);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:99e7372a3a618d9f / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PcscConnectionSlotTests.cs:67–68
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeSmartCardConnection : ISmartCardConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:09ba4c6c878a24ab / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PcscDiscoveryLifetimeTests.cs:90–94
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public Task BuiltInPcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot() =>
        RunProbe("context", nameof(BuiltInPcscDiscovery_UnprovenContextReleaseRetainsOriginalClaimAndNativeRoot));


PROPOSED SAMPLE — no verdict: core-001:unit:089e31d2fb088253 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ResolvePreferredConnectionTests.cs:21–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private const ConnectionType Full =
        ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;


PROPOSED SAMPLE — no verdict: core-001:unit:bc020d9522237ab3 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ResolvePreferredConnectionTests.cs:23–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // Management preference order: SmartCard -> HidFido -> HidOtp (ISC-18/ISC-22).
    private static readonly ConnectionType[] ManagementOrder =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];


PROPOSED SAMPLE — no verdict: core-001:unit:792520a43ae7f3ca / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SessionTransportTests.cs:89–107
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CreateSessionOverTransportAsync_SessionCreationFails_DisposesConnection()
    {
        var connection = new RecordingConnection(ConnectionType.SmartCard);
        var device = new ProbeYubiKey()
            .Returns(ConnectionType.SmartCard, connection)
            .Returns(ConnectionType.HidFido, new RecordingConnection(ConnectionType.HidFido));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            device.CreateSessionOverTransportAsync<object>(
                ConnectionType.SmartCard,
                static (_, _) => throw new InvalidOperationException("initialization failed"),
                Ct));

        Assert.True(connection.Disposed);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5c944f58856816a1 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SessionTransportTests.cs:274–275
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Transport Transport => Transport.Usb;


PROPOSED SAMPLE — no verdict: core-001:unit:e029926a1e9c6337 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SessionTransportTests.cs:300–303
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();


PROPOSED SAMPLE — no verdict: core-001:unit:4429ee9c5a22cc88 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/DesktopSmartCardDeviceListenerSCardErrorTests.cs:26–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed class DesktopSmartCardDeviceListenerSCardErrorTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:e57765e071b17e08 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/DesktopSmartCardDeviceListenerSCardErrorTests.cs:205–206
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int GetStatusChangeCalls { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:abbf813ff2da914c / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/DesktopSmartCardDeviceListenerSCardErrorTests.cs:235–237
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private void RecordReleasedContext() => ReleasedContextCalls++;


PROPOSED SAMPLE — no verdict: core-001:unit:00195f036e18e87c / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/DesktopSmartCardDeviceListenerSCardErrorTests.cs:239–240
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
        private sealed class TestSCardContext(IntPtr handle, FakeSCardApi owner) : SCardContext(handle)
        {


PROPOSED SAMPLE — no verdict: core-001:unit:b564a05176790f32 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/ControlledSCardConnectionApi.cs:47–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public TaskCompletionSource DisconnectEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:fc00ebeb2dd79f1a / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/ControlledSCardConnectionApi.cs:72–74
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    // Opt in only for non-sensitive test commands; never retain arbitrary caller payloads by default.
    public bool CaptureTransmitBuffers { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:71132d68612d1386 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/ControlledSCardConnectionApi.cs:125–138
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public uint BeginTransaction(SCardCardHandle cardHandle)
    {
        _ = Interlocked.Increment(ref _beginTransactionCalls);
        Record(HoldBegin ? "begin-enter" : "begin");
        if (HoldBegin)
        {
            BeginEntered.TrySetResult();
            ReleaseBegin.Wait(TestContext.Current.CancellationToken);
            Record("begin-exit");
        }
        return ErrorCode.SCARD_S_SUCCESS;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ef3112014b3d86ba / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/ControlledSCardConnectionApi.cs:145–170
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public uint Transmit(
        SCardCardHandle cardHandle,
        SCARD_IO_REQUEST sendPci,
        ReadOnlySpan<byte> sendBuffer,
        Span<byte> receiveBuffer,
        out int bytesReceived)
    {
        var call = Interlocked.Increment(ref _transmitCalls);
        if (CaptureTransmitBuffers)
            _transmitBuffers.Enqueue(sendBuffer.ToArray());
        if (call == 1)
        {
            Record("transmit-enter");
            FirstTransmitEntered.TrySetResult();
            ReleaseTransmit.Wait(TestContext.Current.CancellationToken);
            Record("transmit-exit");
        }

        var (result, response) = TransmitResult?.Invoke(call) ??
            (ErrorCode.SCARD_S_SUCCESS, new byte[] { 0x90, 0x00 });
        response.CopyTo(receiveBuffer);
        bytesReceived = response.Length;
        return result;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4452d5e281d19d93 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/ControlledSCardConnectionApi.cs:216–217
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static void Reset() => Volatile.Write(ref _releaseHandleCalls, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:03322f8f66599a98 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/PcscCleanupFailureProbe.cs:22–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal static class PcscCleanupFailureProbe
{


PROPOSED SAMPLE — no verdict: core-001:unit:41b66b77535ba368 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/PcscCleanupFailureProbe.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static CancellationToken Ct => TestContext.Current.CancellationToken;


PROPOSED SAMPLE — no verdict: core-001:unit:31218f4596c4a81f / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/PcscIsolatedProbe.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private const string ProbeEnvironment = "YUBIKIT_PCSC_ISOLATED_PROBE";


PROPOSED SAMPLE — no verdict: core-001:unit:d736f3451d2568f0 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private int _createCalls;


PROPOSED SAMPLE — no verdict: core-001:unit:485fb49e647199dd / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:25–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task<ISmartCardConnection> CreateAsync(
        IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        _ = Interlocked.Increment(ref _createCalls);
        return Task.FromResult<ISmartCardConnection>(new InertSmartCardConnection());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6e08c22f74dfe1a3 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:49–50
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class AlwaysFailingSmartCardConnectionFactory(Exception failure) : ISmartCardConnectionFactory
{


PROPOSED SAMPLE — no verdict: core-001:unit:e0467b8767274957 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:67–68
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public ConnectionType Type => ConnectionType.SmartCard;


PROPOSED SAMPLE — no verdict: core-001:unit:d1c8c7f987c29804 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:76–79
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public void Dispose()
    {
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ef4f5814f550c0a6 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/Fakes/SmartCardConnectionFactoryFakes.cs:117–119
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;


PROPOSED SAMPLE — no verdict: core-001:unit:dba344cadebd82b6 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/FindPcscDevicesTests.cs:48–99
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ListenerScan_SaturatedPcscAdmission_PreservesSnapshotWithoutRemovalEvent()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var api = new StatusBlockingSCardApi("Yubico YubiKey OTP+FIDO+CCID");
        var finder = new FindYubiKeys(
            new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api),
            new EmptyHidFinder(),
            device => new MetadataSlot(rig, device.ReaderName), topologyResolver: rig);
        var hid = new OwnedMonitorFixture.FakeDeviceChangeListener();
        var smartCard = new OwnedMonitorFixture.FakeDeviceChangeListener();
        var clock = new ObservedTimeProvider();
        var interval = TimeSpan.FromHours(1);
        await using var monitor = await YubiKeyMonitor.CreateAsync(finder,
            [new(ConnectionType.Hid, () => hid), new(ConnectionType.SmartCard, () => smartCard)],
            interval, clock, TestContext.Current.CancellationToken);
        var existingDevice = Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestContext.Current.CancellationToken));
        Assert.Equal(111, existingDevice.SerialNumber);
        await using var watcher = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestContext.Current.CancellationToken);
        await watcher.WaitForCountAsync(1, "Initial replay missing", TestContext.Current.CancellationToken);
        var nativeCalls = api.EstablishContextCalls;
        Assert.True(nativeCalls > 0);

        using (await DiscoveryWorkerAdmissionCollection.SaturateAsync(TestContext.Current.CancellationToken))
        {
            smartCard.Raise();
            await clock.WaitForTimerAsync(interval);
            await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
            clock.Advance(RescanScheduler.QuietPeriod);
            // Failed scans do not publish a revision. The next interval timer proves the attempt ended.
            await clock.WaitForTimerAsync(interval);
            await OwnedMonitorFixture.WaitForRevisionAsync(monitor, 1, TestContext.Current.CancellationToken);
            Assert.Same(existingDevice, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestContext.Current.CancellationToken)));
            Assert.Equal(nativeCalls, api.EstablishContextCalls);
        }

        // The next real hint retries successfully once capacity returns, retaining the published object.
        smartCard.Raise();
        await clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        clock.Advance(RescanScheduler.QuietPeriod);
        await OwnedMonitorFixture.WaitForRevisionAsync(monitor, 2, TestContext.Current.CancellationToken);
        Assert.True(api.EstablishContextCalls > nativeCalls);
        Assert.Same(existingDevice, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestContext.Current.CancellationToken)));
        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(watcher.EndedNormally);
        Assert.All(watcher.Events.Skip(1), change =>
            Assert.Same(existingDevice, Assert.IsType<DeviceUpdated>(change).Device));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5ecd56767b03d939 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/FindPcscDevicesTests.cs:182–184
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public IReadOnlyList<string> StatusReaderNames { get; private set; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:40e3599ab290e2f8 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/FindPcscDevicesTests.cs:217–219
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool IsEnabled(LogLevel logLevel) => true;


PROPOSED SAMPLE — no verdict: core-001:unit:35eed0b90edf0eaa / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscConnectionLifetimeTests.cs:154–162
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task AsyncTransaction_ExternalSyncOnlyImplementation_UsesDefaultFallback()
    {
        ISmartCardConnection connection = new SyncOnlyTransactionConnection();
        using var scope = await connection.BeginTransactionAsync(Ct);
        Assert.Equal(1, ((SyncOnlyTransactionConnection)connection).BeginCalls);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:204fc3ecd9a8035a / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscContextIsolationTests.cs:83–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private const int MonitorAddress = 0x39A;


PROPOSED SAMPLE — no verdict: core-001:unit:78cd1ec16aa90462 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscContextIsolationTests.cs:134–135
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
        private sealed class MonitoringContext(nint handle, MonitoringApi owner) : SCardContext(handle)
        {


PROPOSED SAMPLE — no verdict: core-001:unit:ff45ccfa322ed808 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscReaderNameOwnershipTests.cs:80–99
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task FindAllAsync_PartialAllocationFailure_ReleasesEarlierNamesWithoutNativeStatusCall()
    {
        using var names = new NativeReaderNames { FailOnAllocation = 3 };
        var api = new ReaderStatusApi(names);
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api, names.Allocate, names.Free);

        var exception = await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Same(names.AllocationException, exception);
        Assert.Equal(2, names.AllocatedCount);
        Assert.Equal(2, names.FreedCount);
        Assert.Empty(names.Outstanding);
        Assert.Equal(0, names.DuplicateFrees);
        Assert.Equal(0, api.StatusCalls);
        Assert.True(api.Context.IsClosed);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:97212de7ff1e3488 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscReaderNameOwnershipTests.cs:103–104
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public HashSet<IntPtr> Outstanding { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:05c98e846d3a45d0 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscReaderNameOwnershipTests.cs:105–106
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int FreedCount { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:f5493372352f7285 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscReaderNameOwnershipTests.cs:148–149
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public SCARD_READER_STATE[] States { get; private set; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:1244570db6d5abc2 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardConnectionFactoryTests.cs:39–56
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ExternalFactory_SuccessfulConnectionDispose_ReleasesClaimAndAllowsReconnect()
    {
        var factory = new SuccessfulSmartCardConnectionFactory();
        var device = PcscTestDevices.Create(factory, out var slot);

        var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));

        await connection.DisposeAsync();

        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        await using var reopened = await device.ConnectAsync<ISmartCardConnection>(Ct);

        Assert.Equal(2, factory.CreateCalls);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:92b0180ca2af1ca2 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardConnectionFactoryTests.cs:108–128
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task BuiltInPcscConnection_PreOwnerLoggerFailure_ReleasesPhysicalClaim()
    {
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-logger-{Guid.NewGuid():N}",
            Atr = null
        };
        var factory = new SmartCardConnectionFactory();
        var slot = new PcscConnectionSlot(pcscDevice, factory);
        var device = new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
        using var logging = YubiKitLogging.UseTemporary(new ThrowingLoggerFactory());

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        using var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId], Ct);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:22c1af2afd58644c / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs:102–122
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectConnection_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var reader = new PcscDevice { ReaderName = "default-connection", Atr = null };
        await using var connection = new UsbSmartCardConnection(reader, api);

        if (contention)
            await Assert.ThrowsAsync<ConnectionInUseException>(() => connection.InitializeAsync(Ct).AsTask());
        else
        {
            await connection.InitializeAsync(Ct);
            Assert.True(connection.IsExclusive);
        }

        Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:76e45a85145ee160 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs:147–173
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeResources_DefaultRequiresExclusiveAccess(bool contention)
    {
        var api = new ControlledSCardConnectionApi { RefuseExclusive = contention };
        var resources = new PcscNativeResources("default-resources", api);
        try
        {
            if (contention)
                Assert.Throws<ConnectionInUseException>(resources.Open);
            else
            {
                resources.Open();
                Assert.True(resources.IsExclusive);
            }

            Assert.Equal([SCARD_SHARE.EXCLUSIVE], api.ConnectShareModes);
        }
        finally
        {
            Assert.True(resources.TryRelease(out var cleanupError));
            Assert.Null(cleanupError);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:04e891ab6ca26522 / B1 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/UsbSmartCardConnectionTests.cs:41–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public string ReaderName => "fake";


PROPOSED SAMPLE — no verdict: core-001:unit:66cc866c1db102c5 / B1 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/RecordingSmartCardConnection.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed class RecordingSmartCardConnection(params byte[][] responses) : ISmartCardConnection
{
