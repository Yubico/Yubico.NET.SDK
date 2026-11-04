using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Credentials;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;

namespace AuditCt001.Prepared.hardware_create;

public sealed class Example
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
public async Task MakeCredential_TouchRequired_Succeeds(YubiKeyTestState state)
{
    await state.WithCtap2SessionAsync(async session =>
    {
        using var protocol = new PinUvAuthProtocolV2(); // Test device must support V2.
        using var clientPin = new ClientPin(session, protocol);
        byte[]? token = null;
        byte[]? authParam = null;
        try
        {
            byte[] clientDataHash = RandomNumberGenerator.GetBytes(32); // Protocol demo, not client data.
            var rp = new PublicKeyCredentialRpEntity("example.com", "Example");
            var user = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(32), "test@example.com", "Test");
            token = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                Ctap2TestStateExtensions.KnownTestPin,
                PinUvAuthTokenPermissions.MakeCredential, rp.Id);
            authParam = protocol.Authenticate(token, clientDataHash);
            var options = new MakeCredentialOptions { ResidentKey = true };
            options.WithPinUvAuth(authParam, protocol.Version); // uv absent.
            var response = await session.MakeCredentialAsync(
                clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
            var attested = Assert.IsType<AttestedCredentialData>(response.AuthenticatorData.AttestedCredentialData);
            Assert.False(attested.CredentialId.IsEmpty);
        }
        finally
        {
            if (token is not null) CryptographicOperations.ZeroMemory(token);
            if (authParam is not null) CryptographicOperations.ZeroMemory(authParam);
        }
    }, preferredConnection: ConnectionType.HidFido);
}
}
