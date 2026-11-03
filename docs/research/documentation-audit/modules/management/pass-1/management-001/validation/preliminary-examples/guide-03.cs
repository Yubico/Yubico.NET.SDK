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


namespace ManagementSnippet_guide_03;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task MultiFormFactor_Test(YubiKeyTestState state)
    {
        // Three independently bound rows; each row requires its attribute's form factor.
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.FormFactor, info.FormFactor);
    }
}
