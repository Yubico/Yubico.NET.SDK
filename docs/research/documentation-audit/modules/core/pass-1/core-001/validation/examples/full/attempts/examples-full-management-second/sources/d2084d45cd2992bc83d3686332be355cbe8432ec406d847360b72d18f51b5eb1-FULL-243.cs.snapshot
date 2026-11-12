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
namespace Audit20261111.DocExamples.Test243;
public sealed class DocumentationBody243
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
