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
namespace Audit20261111.DocExamples.Test241;
public sealed class DocumentationBody241
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
