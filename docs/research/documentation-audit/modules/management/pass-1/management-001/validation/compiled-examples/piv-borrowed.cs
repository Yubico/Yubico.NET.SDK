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
using Yubico.YubiKit.Piv;


namespace ManagementSnippet_piv_borrowed;

public static class DocumentationExample
{
    public static async Task Example(ISmartCardConnection connection)
    {
        await using (var management = await ManagementSession.CreateAsync(connection))
        {
            var info = await management.GetDeviceInfoAsync();
            Console.WriteLine(info.FirmwareVersion);
        }
        await using var piv = await PivSession.CreateAsync(connection);
    }
}
