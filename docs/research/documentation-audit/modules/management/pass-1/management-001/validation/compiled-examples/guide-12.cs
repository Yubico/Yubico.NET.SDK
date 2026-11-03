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


namespace ManagementSnippet_guide_12;

public static class DocumentationExample
{
    public static async Task Example(YubiKeyTestState state)
    {
        // Session helper: automatic cleanup and cached device info.
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var freshInfo = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(deviceInfo.SerialNumber, freshInfo.SerialNumber);
        });

        // A one-shot alternative, after the helper's session has been disposed:
        var oneShotInfo = await state.Device.GetDeviceInfoAsync();
    }
}
