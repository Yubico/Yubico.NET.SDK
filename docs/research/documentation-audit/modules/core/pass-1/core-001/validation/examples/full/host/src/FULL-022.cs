using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.CredentialManagement;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;

namespace Audit20261111.DocExamples.Full;
internal static class Body22
{
internal static async Task CompileOnly(ICtap2Session session, PublicKeyCredentialRpEntity rp, PublicKeyCredentialUserEntity user)
{
byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[]? pinToken = null;
byte[]? authParam = null;
try
{
    using var protocol = new PinUvAuthProtocolV2();
    using var clientPin = new ClientPin(session, protocol);
    byte[] pinRegistrationHash = RandomNumberGenerator.GetBytes(32); // Separate demo creation.
    byte[] pinAuthenticationHash = RandomNumberGenerator.GetBytes(32); // Separate demo assertion.
    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
        pin, PinUvAuthTokenPermissions.MakeCredential, "example.com");
    var options = new MakeCredentialOptions
    {
        ResidentKey = true,
        Extensions = new ExtensionBuilder().WithCredProtect(CredProtectPolicy.UserVerificationRequired).Build()
    };
    authParam = protocol.Authenticate(pinToken, pinRegistrationHash);
    options.WithPinUvAuth(authParam, protocol.Version); // uv remains absent.
    var created = await session.MakeCredentialAsync(
        pinRegistrationHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
    var createdData = created.AuthenticatorData.AttestedCredentialData
        ?? throw new InvalidOperationException("No attested credential data was returned.");

    // The first token requested only MakeCredential permission. Authorize the separate assertion.
    CryptographicOperations.ZeroMemory(authParam);
    CryptographicOperations.ZeroMemory(pinToken);
    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
        pin, PinUvAuthTokenPermissions.GetAssertion, "example.com");
    authParam = protocol.Authenticate(pinToken, pinAuthenticationHash);
    var assertionOptions = new GetAssertionOptions
    {
        AllowList = [new PublicKeyCredentialDescriptor(createdData.CredentialId)]
    };
    assertionOptions.WithPinUvAuth(authParam, protocol.Version);
    var verified = await session.GetAssertionAsync("example.com", pinAuthenticationHash, assertionOptions);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    if (pinToken is not null) CryptographicOperations.ZeroMemory(pinToken);
    if (authParam is not null) CryptographicOperations.ZeroMemory(authParam);
}
}

}
