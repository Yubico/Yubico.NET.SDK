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
namespace Audit20261111.DocExamples.Test260;
public sealed class DocumentationBody260
{
public async Task CompileOnly(YubiKeyTestState state)
{
await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
{
    // cachedDeviceInfo comes from shared device initialization, not helper entry.
    // Use it instead of re-querying if data hasn't changed
    
    var serial = cachedDeviceInfo.SerialNumber; // ✅ Fast
    
    // Only query again if testing consistency
    var freshInfo = await mgmt.GetDeviceInfoAsync(); // ⚠️ APDU overhead
    Assert.Equal(cachedDeviceInfo.SerialNumber, freshInfo.SerialNumber);
});
}
}
