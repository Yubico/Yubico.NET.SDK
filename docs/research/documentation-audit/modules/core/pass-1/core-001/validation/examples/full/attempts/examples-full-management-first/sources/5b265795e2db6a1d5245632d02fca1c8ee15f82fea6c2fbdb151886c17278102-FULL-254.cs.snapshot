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
namespace Audit20261111.DocExamples.Test254;
public sealed class DocumentationBody254
{
[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state) =>
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();
        
        Assert.True(info.SerialNumber > 0);
        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(state.FirmwareVersion, info.FirmwareVersion);
    });
}
