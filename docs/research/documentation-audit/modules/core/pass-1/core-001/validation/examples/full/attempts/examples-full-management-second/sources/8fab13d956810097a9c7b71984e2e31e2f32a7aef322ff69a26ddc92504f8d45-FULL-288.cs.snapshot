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
namespace Audit20261111.DocExamples.Test288;
public sealed class DocumentationBody288
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
