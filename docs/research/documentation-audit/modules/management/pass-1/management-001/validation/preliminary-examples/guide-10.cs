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


namespace ManagementSnippet_guide_10;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey yubiKey, CancellationToken cancellationToken, ProtocolConfiguration customProtocolConfig)
    {
        // Requires a SmartCard-capable key configured with the supplied SCP03 keys.
        using var scpParameters = Scp03KeyParameters.Default;
        await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(
            new SessionCreationOptions
            {
                ProtocolConfiguration = customProtocolConfig,
                ScpKeyParameters = scpParameters
            },
            cancellationToken: cancellationToken);

        // Multiple operations in same session
        var info1 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
        var info2 = await mgmtSession.GetDeviceInfoAsync(cancellationToken);
        // Session stays open for both calls

        // At scope end, the session is disposed before the caller-owned SCP parameters.
    }
}
