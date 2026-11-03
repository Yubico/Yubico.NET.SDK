using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

namespace ManagementSnippet_shared_readme_5;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.2", Capability = DeviceCapabilities.SecurityDomain)]
    public async Task Scp11_ModernDevice_ReturnsKeyInfo(YubiKeyTestState state)
    {
        using var scpKeyParams = Scp03KeyParameters.Default;
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            async session =>
            {
                var keyInfo = await session.ListKeyInformationAsync();
                Assert.NotEmpty(keyInfo);
            },
            scpKeyParams: scpKeyParams);
    }
}
