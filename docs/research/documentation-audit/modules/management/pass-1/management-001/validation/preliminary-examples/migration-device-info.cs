using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace ManagementSnippet_migration_device_info;

public static class DocumentationExample
{
    public static async Task Example(CancellationToken cancellationToken)
    {
        var device = await YubiKeyManager.FindFirstAsync(cancellationToken: cancellationToken);
        await using var session = await device.CreateManagementSessionAsync(
            cancellationToken: cancellationToken);

        var info = await session.GetDeviceInfoAsync(cancellationToken);
        Console.WriteLine(info.SerialNumber);
        Console.WriteLine(info.VersionName);
    }
}
