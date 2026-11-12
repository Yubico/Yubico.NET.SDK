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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.IntegrationTests;
namespace Audit20261111.DocExamples.Test37;
public sealed class DocumentationBody37
{
[SkippableTheory]
[WithYubiKey]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
public async Task Registration_WithPreviewSign_ReturnsGeneratedSigningKey(YubiKeyTestState state)
{
    // Test setup mutates PIN state; run only with the approval described above.
    await using (var setupSession = await state.Device.CreateCtap2SessionAsync())
    {
        await WebAuthnTestHelpers.NormalizePinAsync(setupSession);
    }

    if (!WebAuthnOrigin.TryParse(WebAuthnTestHelpers.TestOriginUrl, out var origin))
        throw new InvalidOperationException("Unsupported test origin.");
    await using var client = await state.Device.CreateWebAuthnClientAsync(
        origin,
        new WebAuthnClientOptions
        {
            RpIdValidator = (requestOrigin, rpId) =>
                requestOrigin.StringValue == WebAuthnTestHelpers.TestOriginUrl && rpId == WebAuthnTestHelpers.TestRpId
        });

    // Test logic...
}
}
