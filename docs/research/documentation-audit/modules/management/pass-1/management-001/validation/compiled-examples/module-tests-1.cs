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


namespace ManagementSnippet_module_tests_1;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(
        MinFirmware = "5.3.0",                     // Only firmware >= 5.3.0
        Capability = DeviceCapabilities.Piv,      // PIV enabled over USB OR NFC
        RequireUsb = true                         // Independent USB availability requirement
    )]
    public async Task MyTest(YubiKeyTestState state)
    {
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }
}
