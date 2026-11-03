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


namespace ManagementSnippet_guide_29;

public static class DocumentationExample
{
    public static async Task Example(CancellationToken cancellationToken)
    {
        // Find all connected YubiKeys
        var devices = await YubiKeyManager.FindAllAsync(forceRescan: true, cancellationToken: cancellationToken);
        foreach (var device in devices)
        {
            var info = await device.GetDeviceInfoAsync(cancellationToken: cancellationToken);
            Console.WriteLine($"Serial: {info.SerialNumber}");
            Console.WriteLine($"Firmware: {info.FirmwareVersion}");
            Console.WriteLine($"USB enabled: {info.UsbEnabled}");
        }

        // Find specific device
        var yubiKey = devices.SingleOrDefault(device => device.SerialNumber == 12345678);
    }
}
