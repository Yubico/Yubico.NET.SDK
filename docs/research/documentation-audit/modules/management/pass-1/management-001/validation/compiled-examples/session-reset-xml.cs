using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Management;

namespace ManagementSnippet_session_reset_xml;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey device, CancellationToken cancellationToken)
    {
        await using var session = await device.CreateManagementSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
            cancellationToken);

        // Destructive: confirm that FIDO/PIV data and the shared PIN may be removed.
        await session.ResetDeviceAsync(cancellationToken);
    }
}
