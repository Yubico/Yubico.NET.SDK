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
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
namespace Audit20261111.DocExamples.Test12;
public sealed class DocumentationBody12
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
public async Task GetInfoAsync_Returns_Aaguid(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateCtap2SessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido });
    var info = await session.GetInfoAsync();
    Assert.Equal(16, info.Aaguid.Length);
}
}
