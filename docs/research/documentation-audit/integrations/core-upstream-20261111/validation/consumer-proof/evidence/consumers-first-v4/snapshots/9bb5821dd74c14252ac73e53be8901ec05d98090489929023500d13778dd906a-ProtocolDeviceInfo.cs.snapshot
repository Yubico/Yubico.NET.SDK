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
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads <see cref="DeviceInfo"/> over an already-open connection by building the matching Core protocol.
/// </summary>
/// <remarks>
///     Borrows the supplied connection. It builds and disposes the matching protocol before returning, but
///     protocol disposal does not dispose the connection. The caller retains ownership and must dispose the
///     connection. Shared by discovery's serial-disambiguation read and the composite metadata read.
/// </remarks>
internal static class ProtocolDeviceInfo
{
    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        // Borrows: the caller owns this connection and disposes it (see the type remarks). Upstream
        // disposed it here on failure, which was correct only in a model where protocol disposal
        // cascaded to the connection. This branch removed that cascade, so disposing here would be a
        // premature dispose of a connection the caller still holds and will dispose itself.
        var protocol = ProtocolFactory.Create(connection);

        try
        {
            if (protocol is ISmartCardProtocol smartCard)
            {
                _ = await smartCard.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            }
            else if (protocol is IFidoHidProtocol fidoHid)
            {
                await fidoHid.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            protocol.Dispose();
        }
    }
}