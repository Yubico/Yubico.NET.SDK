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


namespace ManagementSnippet_guide_14;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();

            Assert.True(info.SerialNumber > 0);
            Assert.Equal(state.SerialNumber, info.SerialNumber);
            Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
        });
}
