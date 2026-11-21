// Copyright 2024 Yubico AB
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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Management.Backend;

/// <summary>
/// Backend implementation for Management operations over SmartCard (CCID/NFC).
/// Encodes operations as ISO 7816 APDUs.
/// </summary>
internal sealed class SmartCardBackend(ISmartCardProtocol protocol) : IManagementBackend
{
    private readonly ISmartCardProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

    // Instruction bytes for Management APDUs
    private const byte InsSetDeviceInfo = 0x1C;
    private const byte InsSetMode = 0x16;
    private const byte InsDeviceReset = 0x1F;

    public static async ValueTask<FirmwareVersion?> SelectApplicationAsync(
        ISmartCardProtocol protocol,
        CancellationToken cancellationToken)
    {
        var versionBytes = await protocol.SelectAsync(ApplicationIds.Management, cancellationToken)
            .ConfigureAwait(false);

        return FirmwareVersion.FromSelectResponse(versionBytes.Span);
    }

    public async ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsSetDeviceInfo,
            P1 = 0,
            P2 = 0,
            Data = config
        };

        await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // No production caller. Firmware 5.0+ and development firmware use DeviceConfig instead.
    internal async ValueTask SetModeAsync(
        FirmwareVersion firmwareVersion, byte mode, byte challengeResponseTimeout, ushort autoEjectTimeout,
        CancellationToken cancellationToken)
    {
        if (firmwareVersion.IsAtLeast(5, 0, 0))
            throw new NotSupportedException("Use DeviceConfig instead of SET_MODE on this firmware.");

        byte[] data = [mode, challengeResponseTimeout, 0, 0];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2), autoEjectTimeout);

        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsSetMode,
            P1 = 0x11,
            P2 = 0,
            Data = data
        };

        await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DeviceResetAsync(CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsDeviceReset,
            P1 = 0,
            P2 = 0
        };

        await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

}