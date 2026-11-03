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
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Oath;


namespace ManagementSnippet_shared_guide_oath;

public static class DocumentationExample
{
    public static async Task WithOathAsync(
        this YubiKeyTestState state,
        Func<OathSession, DeviceInfo, Task> action,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);

        await using var session = await OathSession.CreateAsync(
            connection,
            new SessionCreationOptions { ScpKeyParameters = scpKeyParams },
            cancellationToken: cancellationToken);

        await action(session, state.DeviceInfo);
    }
}
