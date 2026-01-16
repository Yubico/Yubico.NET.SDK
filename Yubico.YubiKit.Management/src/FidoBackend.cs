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

using Yubico.YubiKit.Core.Hid.Fido;

namespace Yubico.YubiKit.Management;

/// <summary>
/// Backend implementation for Management operations over FIDO HID.
/// Encodes operations as CTAP vendor commands.
/// </summary>
internal sealed class FidoBackend(IFidoHidProtocol hidProtocol) : IManagementBackend
{
    private readonly IFidoHidProtocol _hidProtocol = hidProtocol ?? throw new ArgumentNullException(nameof(hidProtocol));

    // CTAP vendor command codes
    private const byte CtapYubikeyDeviceConfig = 0xC0;
    private const byte CtapReadConfig = 0xC2;
    private const byte CtapWriteConfig = 0xC3;

    public async ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken cancellationToken)
    {
        var pagePayload = new byte[] { (byte)page };
        var response = await _hidProtocol.SendVendorCommandAsync(CtapReadConfig, pagePayload, cancellationToken)
            .ConfigureAwait(false);
        return response.ToArray();
    }

    public async ValueTask WriteConfigAsync(byte[] config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        await _hidProtocol.SendVendorCommandAsync(CtapWriteConfig, config, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        await _hidProtocol.SendVendorCommandAsync(CtapYubikeyDeviceConfig, data, cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask DeviceResetAsync(CancellationToken cancellationToken)
    {
        // Device reset is only supported over CCID, not HID
        throw new NotSupportedException("Device reset is only available over SmartCard (CCID) connections.");
    }

    public void Dispose()
    {
        // Backend doesn't own the protocol - ManagementSession handles disposal
    }
}
