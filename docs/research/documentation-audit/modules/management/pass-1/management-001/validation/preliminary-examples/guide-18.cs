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


namespace ManagementSnippet_guide_18;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(
        MinFirmware = "5.0.0",
        RequireUsb = true,
        Capability = DeviceCapabilities.Piv)]
    public async Task AdvancedFiltering_ModernUsbPiv_Works(YubiKeyTestState state)
    {
        // Multiple requirements enforced by attribute:
        Assert.True(state.FirmwareVersion.IsAtLeast(5, 0, 0));
        Assert.True(state.IsUsbTransport);
        Assert.True(state.HasCapability(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();

            // RequireUsb and Capability are independent: PIV may be enabled over USB OR NFC.
            Assert.True(info.FirmwareVersion.Major >= 5);
            Assert.True(((info.UsbEnabled | info.NfcEnabled) & DeviceCapabilities.Piv) != 0);
        });
    }
}
