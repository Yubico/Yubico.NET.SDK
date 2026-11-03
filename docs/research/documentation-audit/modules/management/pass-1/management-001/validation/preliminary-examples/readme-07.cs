using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;


namespace ManagementSnippet_readme_07;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey device)
    {
        await using var session = await device.CreateManagementSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

        // Destructive: the caller has confirmed Bio MPE applicability and application-data erasure.
        await session.ResetDeviceAsync();
    }
}
