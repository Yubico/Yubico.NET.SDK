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
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
namespace Audit20261111.DocExamples.Test302;
public sealed class DocumentationBody302
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task ListAfterReset_ContainsFactoryVersion(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keys = await session.ListKeyInformationAsync(ct);
            Assert.Contains(keys, entry => entry.KeyReference.Kvn == 0xFF);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
}
