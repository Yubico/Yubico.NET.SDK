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
namespace Audit20261111.DocExamples.Test134;
public sealed class DocumentationBody134
{
[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_DefaultManagementSelection(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
    {
        var deviceInfo = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    });
}
}
