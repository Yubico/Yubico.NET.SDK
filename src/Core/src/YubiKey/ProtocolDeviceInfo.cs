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

using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Reads <see cref="DeviceInfo"/> over an already-open connection by building the matching Core protocol.
/// </summary>
/// <remarks>
///     Takes ownership of the supplied connection: it builds a protocol over the connection and disposes the
///     protocol (which disposes the connection) before returning. The caller must not dispose the connection
///     separately. Shared by discovery's serial-disambiguation read and the composite metadata read.
/// </remarks>
internal static class ProtocolDeviceInfo
{
    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        switch (connection)
        {
            case ISmartCardConnection smartCard:
                {
                    var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(smartCard);
                    try
                    {
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IFidoHidConnection fido:
                {
                    var protocol = FidoProtocolFactory.Create().Create(fido);
                    try
                    {
                        // Initializes the HID channel; the application id is unused for HID.
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IOtpHidConnection otp:
                {
                    var protocol = OtpProtocolFactory.Create().Create(otp);
                    try
                    {
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            default:
                throw new NotSupportedException(
                    $"Connection type {connection.GetType().Name} is not supported for reading device info.");
        }
    }
}