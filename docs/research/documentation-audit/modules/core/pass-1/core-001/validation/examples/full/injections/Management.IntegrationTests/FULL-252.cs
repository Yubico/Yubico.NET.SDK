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
namespace Audit20261111.DocExamples.Test252;
public sealed class DocumentationBody252
{
public async Task CompileOnly(YubiKeyTestState state)
{
// Session helper: automatic cleanup and cached device info.
await state.WithManagementAsync(async (mgmt, deviceInfo) =>
{
    var freshInfo = await mgmt.GetDeviceInfoAsync();
    Assert.Equal(deviceInfo.SerialNumber, freshInfo.SerialNumber);
});

// A one-shot alternative, after the helper's session has been disposed:
var oneShotInfo = await state.Device.GetDeviceInfoAsync();
}
}
