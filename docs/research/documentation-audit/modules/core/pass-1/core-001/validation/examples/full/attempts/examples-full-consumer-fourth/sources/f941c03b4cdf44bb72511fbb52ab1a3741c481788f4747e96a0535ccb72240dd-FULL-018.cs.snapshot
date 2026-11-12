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
internal static class Body18
{
internal static async Task CompileOnly(ICtap2Session session)
{
byte[] registrationHash = RandomNumberGenerator.GetBytes(32); // Demo only, not collected client data.
var rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp");
var user = new PublicKeyCredentialUserEntity(
    RandomNumberGenerator.GetBytes(32), "user@example.com", "Example User");

var response = await session.MakeCredentialAsync(
    registrationHash, rp, user, [PublicKeyCredentialParameters.CreateES256()],
    new MakeCredentialOptions { ResidentKey = false });
var attested = response.AuthenticatorData.AttestedCredentialData
    ?? throw new InvalidOperationException("No attested credential data was returned.");
ReadOnlyMemory<byte> credentialId = attested.CredentialId;
ReadOnlyMemory<byte> publicKeyCbor = attested.CredentialPublicKey;
}

}
