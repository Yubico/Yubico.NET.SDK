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

using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Management;

/// <summary>
/// Backend implementation for Management operations over SmartCard (CCID/NFC).
/// Encodes operations as ISO 7816 APDUs.
/// </summary>
internal sealed class SmartCardBackend(ISmartCardProtocol protocol) : IManagementBackend
{
    private readonly ISmartCardProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

    // Instruction bytes for Management APDUs
    private const byte InsGetDeviceInfo = 0x1D;
    private const byte InsSetDeviceInfo = 0x1C;
    private const byte InsSetMode = 0x16;
    private const byte InsDeviceReset = 0x1F;

    public async ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsGetDeviceInfo,
            P1 = (byte)page,
            P2 = 0
        };

        var response = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Data.ToArray();
    }

    public async ValueTask WriteConfigAsync(byte[] config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

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

    public async ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = InsSetMode,
            P1 = 0,
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

    public void Dispose()
    {
        // Backend doesn't own the protocol - ManagementSession handles disposal
    }
}
