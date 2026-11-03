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


namespace ManagementSnippet_guide_07;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey yubiKey, CancellationToken cancellationToken)
    {
        await using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var mgmt = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);
        var deviceInfo = await mgmt.GetDeviceInfoAsync(cancellationToken);
    }
}
