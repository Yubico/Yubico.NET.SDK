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


namespace ManagementSnippet_guide_15;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task DeviceInfo_Firmware57Plus_HasVersionDisplay(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedInfo) =>
        {
            // This test only runs on firmware >= 5.7.0
            Assert.True(state.FirmwareVersion.IsAtLeast(5, 7, 0));

            // Check the reported display value, not generic firmware feature support.
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.False(string.IsNullOrWhiteSpace(info.VersionName));
            Assert.Equal(cachedInfo.SerialNumber, info.SerialNumber);
        });
}
