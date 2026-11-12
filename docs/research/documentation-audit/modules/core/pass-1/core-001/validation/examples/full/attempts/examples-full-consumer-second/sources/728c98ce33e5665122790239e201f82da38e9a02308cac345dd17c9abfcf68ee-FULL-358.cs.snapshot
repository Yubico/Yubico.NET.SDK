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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.OpenPgp;

namespace Audit20261111.DocExamples.Full;
internal static class Body358
{
static ECParameters DecodeP256PublicKey(ReadOnlyMemory<byte> template)
{
    using var outer = Tlv.Create(template.Span);
    if (outer.Tag != 0x7F49)
        throw new CryptographicException("Expected a card public-key template.");
    var point = TlvHelper.DecodeDictionary(outer.Value.Span)[0x86];
    if (point.Length != 65 || point.Span[0] != 0x04)
        throw new CryptographicException("Expected an uncompressed P-256 point.");
    return new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = point.Slice(1, 32).ToArray(), Y = point.Slice(33, 32).ToArray() }
    };
}

static async Task<bool> SignAndVerifyP256Async(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> message,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    var attributes = await session.GetAlgorithmAttributesAsync(KeyRef.Sig, cancellationToken);
    if (attributes is not EcAttributes { KeyType: KeyType.P256 })
        throw new InvalidOperationException("Provision a P-256 signing key first.");
    using var verifier = ECDsa.Create(DecodeP256PublicKey(
        await session.GetPublicKeyAsync(KeyRef.Sig, cancellationToken)));
    await session.VerifyPinAsync(userPin, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> signature = await session.SignAsync(message, HashAlgorithmName.SHA256, cancellationToken);
    try
    {
        return verifier.VerifyData(message.Span, signature.Span, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(signature).Span);
    }
}
}
