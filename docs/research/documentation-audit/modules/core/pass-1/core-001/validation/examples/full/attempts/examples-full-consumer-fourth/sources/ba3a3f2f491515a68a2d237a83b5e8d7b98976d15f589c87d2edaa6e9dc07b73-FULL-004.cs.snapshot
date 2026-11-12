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
internal static class Body4
{
internal static async Task CompileOnly(ICtap2Session session, byte[] pin, string rpId, byte[] clientDataHash, MakeCredentialOptions options, PublicKeyCredentialRpEntity rp, PublicKeyCredentialUserEntity user, CancellationToken cancellationToken)
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
