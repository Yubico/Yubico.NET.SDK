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
using Yubico.YubiKit.Core.IntegrationTests;
namespace Audit20261111.DocExamples.Test239;
public class MyIntegrationTests : IntegrationTestBase
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state)
    {
        // The placeholder row binds lazily to its first matching device.
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        // Test logic
    }
}
