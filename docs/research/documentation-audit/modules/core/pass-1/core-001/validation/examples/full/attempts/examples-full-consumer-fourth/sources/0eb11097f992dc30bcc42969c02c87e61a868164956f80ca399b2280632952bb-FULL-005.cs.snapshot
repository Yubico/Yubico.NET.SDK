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
internal static class Body5
{
internal static void CompileOnly(ReadOnlyMemory<byte> blobData, IPinUvAuthProtocol protocol, byte[] sharedSecret, IReadOnlyDictionary<int, object?> keyAgreement, byte[] salt1, byte[] salt2)
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
