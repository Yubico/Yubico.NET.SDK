using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
namespace Audit20261111.DocExamples.Test258;
public sealed class DocumentationBody258
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
