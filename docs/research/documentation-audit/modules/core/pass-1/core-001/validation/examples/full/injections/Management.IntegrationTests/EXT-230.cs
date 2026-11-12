using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
namespace Audit20261111.DocExamples.External230;
public sealed class DocumentationBody
{
[SkippableTheory]
[WithYubiKey]
public async Task GetDeviceInfo_AllDevices_ReturnsValidData(YubiKeyTestState state)
{
    await state.WithManagementAsync(async (mgmt, cachedInfo) =>
    {
        var info = await mgmt.GetDeviceInfoAsync();

        Assert.Equal(state.SerialNumber, info.SerialNumber);
        Assert.Equal(cachedInfo.FirmwareVersion, info.FirmwareVersion);
    });
}
}
