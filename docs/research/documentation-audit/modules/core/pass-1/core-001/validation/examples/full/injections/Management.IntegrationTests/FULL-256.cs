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
namespace Audit20261111.DocExamples.Test256;
public sealed class DocumentationBody256
{
[SkippableTheory]
[WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
public async Task BioFormFactor_MetadataMatchesCache(YubiKeyTestState state)
{
    Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);
    
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(FormFactor.UsbABiometricKeychain, info.FormFactor);
        
        // Metadata checks only; this does not exercise biometric operations or prove MPE eligibility.
        Assert.True(info.FirmwareVersion.Major >= 5);
    });
}
}
