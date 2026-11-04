using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using System.Collections.Generic;

namespace AuditCt001.Prepared.guide_extensions;

public static class Example
{
public static void Run(ReadOnlyMemory<byte> blobData, IPinUvAuthProtocol protocol, byte[] sharedSecret, IReadOnlyDictionary<int, object?> keyAgreement, byte[] salt1, byte[] salt2)
{
// Builder fragment: ReadOnlyMemory<byte> blobData and the hmac-secret inputs below are caller-supplied.
// Build extensions for MakeCredential
var extensions = new ExtensionBuilder()
    .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
    .WithCredBlob(blobData)
    .Build();

var options = new MakeCredentialOptions
{
    Extensions = extensions,
    ResidentKey = true
};

// For GetAssertion: encrypted salts under the shared secret from key agreement
// protocol/sharedSecret/keyAgreement and 32-byte salt1/salt2 belong to the caller.
var assertionExtensions = new ExtensionBuilder()
    .WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, salt2)
    .Build();
}
}
