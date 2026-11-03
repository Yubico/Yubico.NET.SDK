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


namespace ManagementSnippet_guide_01;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(
        MinFirmware = "5.3.0",
        Capability = DeviceCapabilities.Piv,
        RequireUsb = true)]
    public async Task ReadMetadata_ForMatchingKey(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        });
    }
}
