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


namespace ManagementSnippet_tool_query;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey device, CancellationToken ct)
    {
        await using var connection = await device.ConnectAsync<ISmartCardConnection>(ct);
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: ct);

        var result = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
        if (result.Success && result.DeviceInfo.HasValue)
        {
            var info = result.DeviceInfo.Value;
            Console.WriteLine($"Serial: {info.SerialNumber}");
            Console.WriteLine($"Firmware: {info.FirmwareVersion}");
        }
    }
}
