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


namespace ManagementSnippet_tool_reset;

public static class DocumentationExample
{
    public static async Task Example(IManagementSession session, CancellationToken ct)
    {
        // Prerequisite: caller confirmed Bio MPE and authorized reset; session uses SmartCard.
        // Metadata below supplies the helper's firmware check, not a product-family check.
        var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
        if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
        {
            Console.WriteLine("Failed to get device info");
            return;
        }

        // Request reset; the helper returns failure results for operation exceptions.
        var resetResult = await DeviceReset.ResetDeviceAsync(
            session,
            infoResult.DeviceInfo.Value,
            ct);

        if (resetResult.Success)
        {
            Console.WriteLine("Device reset successfully");
        }
        else
        {
            Console.WriteLine($"Reset failed: {resetResult.ErrorMessage}");
        }
    }
}
