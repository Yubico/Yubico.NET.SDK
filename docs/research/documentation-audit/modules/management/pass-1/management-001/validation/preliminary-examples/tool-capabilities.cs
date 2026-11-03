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
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;


namespace ManagementSnippet_tool_capabilities;

public static class DocumentationExample
{
    public static async Task Example(IManagementSession session, CancellationToken ct)
    {
        var result = await DeviceConfiguration.SetCapabilitiesAsync(
            session,
            Transport.Usb,
            DeviceCapabilities.Piv | DeviceCapabilities.Fido2,
            lockCode: null,
            reboot: true,
            ct);
    }
}
