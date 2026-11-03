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


namespace ManagementSnippet_readme_03;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey device)
    {
        var info = await device.GetDeviceInfoAsync();
        bool pivOverUsb = (info.UsbSupported & DeviceCapabilities.Piv) != 0;
        bool oathEnabledOverNfc = (info.NfcEnabled & DeviceCapabilities.Oath) != 0;
        Console.WriteLine($"Configuration locked: {info.IsLocked}");
    }
}
