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
namespace Audit20261111.DocExamples.Test289;
public sealed class DocumentationBody289
{
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.0.0")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        // mgmt = ManagementSession (already initialized)
        // cachedDeviceInfo = shared test-state snapshot, not fresh helper-entry metadata
        
        var info = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(cachedDeviceInfo.SerialNumber, info.SerialNumber);
    });
}
