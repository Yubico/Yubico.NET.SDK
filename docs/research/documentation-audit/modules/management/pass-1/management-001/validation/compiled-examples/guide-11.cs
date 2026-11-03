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


namespace ManagementSnippet_guide_11;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey yubiKey, CancellationToken cancellationToken)
    {
        await using var mgmt = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken);
        var info = await mgmt.GetDeviceInfoAsync(cancellationToken);
        var remaining = info.UsbEnabled & ~DeviceCapabilities.Otp;
        if ((remaining & info.UsbSupported) == DeviceCapabilities.None)
        {
            throw new InvalidOperationException("Preserve a supported USB application.");
        }
        await mgmt.SetDeviceConfigAsync(
            new DeviceConfig { UsbEnabled = remaining },
            new SetDeviceConfigOptions { Reboot = true },
            cancellationToken);
        // End this scope; rediscover the intended key and open a fresh connection after reboot.
    }
}
