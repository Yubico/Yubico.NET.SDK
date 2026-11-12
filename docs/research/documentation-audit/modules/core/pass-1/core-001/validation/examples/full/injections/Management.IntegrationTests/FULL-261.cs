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
namespace Audit20261111.DocExamples.Test261;
public sealed class DocumentationBody261
{
public async Task CompileOnly(YubiKeyTestState state)
{
await state.WithManagementAsync(async (mgmt, info) =>
{
    // Multiple operations in one session
    var info1 = await mgmt.GetDeviceInfoAsync();
    var info2 = await mgmt.GetDeviceInfoAsync();
    var info3 = await mgmt.GetDeviceInfoAsync();
    
    // More efficient than creating 3 separate sessions
});
}
}
