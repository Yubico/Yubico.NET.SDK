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


namespace ManagementSnippet_guide_09;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey yubiKey, CancellationToken cancellationToken)
    {
        var config = new DeviceConfig { UsbEnabled = DeviceCapabilities.Piv };
        await using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
        await mgmt.SetDeviceConfigAsync(
            config,
            new SetDeviceConfigOptions { Reboot = true },
            cancellationToken);
    }
}
