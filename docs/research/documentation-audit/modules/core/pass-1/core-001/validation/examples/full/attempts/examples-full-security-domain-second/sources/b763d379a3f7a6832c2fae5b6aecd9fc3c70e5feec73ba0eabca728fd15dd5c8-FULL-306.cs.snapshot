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
namespace Audit20261111.DocExamples.Test306;
public sealed class DocumentationBody306
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task MyTest(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.ListKeyInformationAsync(ct);
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
}
