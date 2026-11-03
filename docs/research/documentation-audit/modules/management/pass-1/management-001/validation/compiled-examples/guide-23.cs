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


namespace ManagementSnippet_guide_23;

public static class DocumentationExample
{
    public static async Task Example(IManagementSession session, CancellationToken cancellationToken)
    {
        var config = new DeviceConfig
        {
            UsbEnabled = DeviceCapabilities.Piv | DeviceCapabilities.Oath,
            AutoEjectTimeout = 30,
            DeviceFlags = DeviceFlags.TouchEject,
            ChallengeResponseTimeout = 15
        };

        // The session serializes the configuration and clears the encoded buffer after transmission.
        await session.SetDeviceConfigAsync(
            config,
            new SetDeviceConfigOptions { Reboot = true },
            cancellationToken);
    }
}
