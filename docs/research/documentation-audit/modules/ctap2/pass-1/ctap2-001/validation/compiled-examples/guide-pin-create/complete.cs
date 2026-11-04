using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Credentials;
using System.Security.Cryptography;

namespace AuditCt001.Prepared.guide_pin_create;

public static class Example
{
public static async Task ExampleAsync(ICtap2Session session, byte[] pin, string rpId, byte[] clientDataHash, MakeCredentialOptions options, PublicKeyCredentialRpEntity rp, PublicKeyCredentialUserEntity user, CancellationToken cancellationToken)
{
// Fragment: session, byte[] pin, string rpId, byte[32] clientDataHash,
// MakeCredentialOptions options, RP/user entities and CancellationToken cancellationToken are supplied.
// Requires mutually supported protocol 2; this constructor does not negotiate it.
using var protocol = new PinUvAuthProtocolV2();
using var clientPin = new ClientPin(session, protocol);

// Get PIN token with permissions. rpId is required for MakeCredential/GetAssertion permissions.
byte[]? pinToken = null;
byte[]? authParam = null;
try
{
    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
        pin,
        PinUvAuthTokenPermissions.MakeCredential,
        rpId,
        cancellationToken);

    // Authenticate command
    authParam = protocol.Authenticate(pinToken, clientDataHash);
    options.WithPinUvAuth(authParam, protocol.Version);
    // Keep options.UserVerification null; even uv:false is mutually exclusive with token auth.
    await session.MakeCredentialAsync(
        clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options,
        cancellationToken);
}
finally
{
    if (pinToken is not null) CryptographicOperations.ZeroMemory(pinToken);
    if (authParam is not null) CryptographicOperations.ZeroMemory(authParam);
}
}
}
