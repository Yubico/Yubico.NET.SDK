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
namespace Audit20261111.DocExamples.Test257;
public sealed class DocumentationBody257
{
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv)]
public async Task PivCapability_EnabledDevices_Accessible(YubiKeyTestState state)
{
    Assert.True(state.HasCapability(DeviceCapabilities.Piv));
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        
        // Verify PIV is enabled on USB or NFC
        bool pivEnabled = (info.UsbEnabled & DeviceCapabilities.Piv) != 0 ||
                         (info.NfcEnabled & DeviceCapabilities.Piv) != 0;
        Assert.True(pivEnabled);
    });
}
}
