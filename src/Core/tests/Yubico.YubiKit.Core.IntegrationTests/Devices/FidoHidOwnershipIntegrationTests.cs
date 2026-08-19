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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Hardware-gated confirmation of the exclusive FIDO HID interface ownership contract.
/// </summary>
public class FidoHidOwnershipIntegrationTests
{
    private const uint BroadcastChannel = 0xFFFFFFFF;
    private const byte CtapHidInit = 0x86;
    private const int NonceLength = 8;

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_SingleFidoHidConnection_CompletesCtapHidInit(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var only = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);

        await only.SendAsync(BuildInitPacket(only.PacketSize, nonce), cancellationToken);
        var echo = await ReceiveInitEchoAsync(only, cancellationToken);

        Assert.Equal(nonce, echo.ToArray());
    }

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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ConnectAsync_DisposedFidoHidConnection_PermitsReopen(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        var first = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        await first.DisposeAsync();

        await using var reopened = await state.Device.ConnectAsync<IFidoHidConnection>(cancellationToken);
        Assert.NotNull(reopened);
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
