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


namespace ManagementSnippet_guide_20;

public static class DocumentationExample
{
    public static async Task Example(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // cachedDeviceInfo comes from shared device initialization, not helper entry.
            // Use it instead of re-querying if data hasn't changed

            var serial = cachedDeviceInfo.SerialNumber; // ✅ Fast

            // Only query again if testing consistency
            var freshInfo = await mgmt.GetDeviceInfoAsync(); // ⚠️ APDU overhead
            Assert.Equal(cachedDeviceInfo.SerialNumber, freshInfo.SerialNumber);
        });
    }
}
