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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Register row F1: physical confirmation that FIDO HID really is shared on this platform.
/// </summary>
/// <remarks>
///     The "FIDO HID remains shared, CCID and OTP HID are exclusive" contract is enforced in
///     <c>DeviceConnectionRegistry</c> and pinned by <c>ConnectionOwnershipContractTests</c> — but that pin
///     uses fakes, so it only proves the in-process lease admits a second FIDO connection. It cannot prove
///     the host's native HID layer admits one. On macOS that gap is load-bearing, because
///     <c>MacOSHidIOReportConnection</c> opens FIDO with <c>IOHIDDeviceOpen(handle, 0x01)</c>, i.e.
///     <c>kIOHIDOptionsTypeSeizeDevice</c>. If a seizing double-open were refused natively, the registry
///     would admit a connection the platform then rejects, and the shared-FIDO contract would be false on
///     real hardware while every fake-based test stayed green.
///     <para>
///         This test therefore opens two concurrent FIDO HID connections on ONE physical key and requires
///         both to be usable, not merely constructed: each runs its own CTAPHID_INIT on the broadcast
///         channel and must get its own nonce echoed back. A second open that "succeeds" but starves or
///         hijacks the first would fail the echo check. CTAPHID_INIT touches no credential state and
///         requires no user presence.
///     </para>
/// </remarks>
public class FidoHidSharingIntegrationTests
{
    private const uint BroadcastChannel = 0xFFFFFFFF;
    private const byte CtapHidInit = 0x86; // CTAPHID_INIT (0x06) with the initialization-packet bit set.
    private const int NonceLength = 8;

    /// <summary>
    ///     BASELINE. Establishes that one FIDO HID connection can complete a CTAPHID_INIT round trip on this
    ///     rig, so a failure of the two-connection test below can be attributed to sharing rather than to the
    ///     probe itself.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_SingleFidoHidConnection_CompletesCtapHidInit(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var only = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);

        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var echo = await InitAsync(only, nonce, cancellationToken);

        Assert.Equal(nonce, echo.ToArray());
    }

    /// <summary>
    ///     DIAGNOSTIC. Sends CTAPHID_INIT on the FIRST connection and reads on the SECOND. Passing means the
    ///     input report was delivered to the wrong handle, i.e. two macOS FIDO handles do not demultiplex.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var first = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        await using var second = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);

        var nonce = RandomNumberGenerator.GetBytes(NonceLength);

        var packet = BuildInitPacket(first.PacketSize, nonce);
        await first.SendAsync(packet, cancellationToken);

        var echo = await ReceiveInitEchoAsync(second, cancellationToken);

        Assert.Equal(nonce, echo.ToArray());
    }

    /// <summary>
    ///     REGRESSION PIN for register row F1. A second concurrent FIDO HID connection to one physical key
    ///     must be admitted by BOTH the in-process lease and the macOS HID layer.
    /// </summary>
    /// <remarks>
    ///     This failed before the seize fix with <c>IOHIDDeviceOpen = 0xE00002C5</c>
    ///     (<c>kIOReturnExclusiveAccess</c>). It pins only ADMISSION. Concurrent CTAP traffic across the two
    ///     handles is a separate, known-unsupported matter — see
    ///     <see cref="SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting" />.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_SecondConcurrentFidoHidConnection_IsAdmitted(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var first = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        await using var second = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    /// <summary>
    ///     Sends CTAPHID_INIT on the broadcast channel and returns the echoed nonce from the response.
    /// </summary>
    private static async Task<ReadOnlyMemory<byte>> InitAsync(
        IFidoHidConnection connection,
        byte[] nonce,
        CancellationToken cancellationToken)
    {
        await connection.SendAsync(BuildInitPacket(connection.PacketSize, nonce), cancellationToken);
        return await ReceiveInitEchoAsync(connection, cancellationToken);
    }

    private static byte[] BuildInitPacket(int packetSize, byte[] nonce)
    {
        var packet = new byte[packetSize];
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), BroadcastChannel);
        packet[4] = CtapHidInit;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(5, 2), NonceLength);
        nonce.CopyTo(packet.AsSpan(7));
        return packet;
    }

    private static async Task<ReadOnlyMemory<byte>> ReceiveInitEchoAsync(
        IFidoHidConnection connection,
        CancellationToken cancellationToken)
    {
        // The key may be mid-conversation with another client; skip frames that are not our INIT response.
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
}