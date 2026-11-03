using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Management;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using (var session = await device.CreateManagementSessionAsync())
{
    var info = await session.GetDeviceInfoAsync();
    Console.WriteLine($"Serial {info.SerialNumber}, firmware {info.FirmwareVersion}, {info.FormFactor}");
    Console.WriteLine($"USB enabled: {info.UsbEnabled}");
}
