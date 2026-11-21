// Copyright 2025 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// A class that converts private key parameters to ASN.1 DER encoding.
/// </summary>
/// <remarks>
/// Returns caller-owned, unencrypted PKCS#8 version-0 encodings; clear them after use. Inputs remain
/// borrowed. Explicit INTEGER and nested encoding temporaries have local clearing owners. AsnWriter
/// storage is separate: Encode copies it, and these paths do not Reset the writers to clear that storage.
/// </remarks>
internal static class AsnPrivateKeyEncoder
{
    /// <summary>
    /// Converts a private key and its corresponding public point to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="privateKey">The private key as a byte array.</param>
    /// <param name="publicPoint">The public key point as a byte array (optional).</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key.</returns>
    /// <remarks>
    /// Both EC overloads validate scalar width against the named curve. Curve25519 encodings retain
    /// the exact imported 32 bytes and RFC 8410's absent algorithm parameters.
    /// </remarks>
    public static byte[] EncodeToPkcs8(
        ReadOnlyMemory<byte> privateKey,
        ReadOnlyMemory<byte>? publicPoint,
        KeyType keyType)
    {
        var keyInfo = keyType.GetKeyInfo();
        return keyInfo.Asn1 switch
        {
            { ParameterKind: Asn1ParameterKind.NamedCurve, NamedCurveOid: { } curveOid } when keyInfo.IsShortWeierstrass => EncodeECKey(privateKey, curveOid, publicPoint),
            { ParameterKind: Asn1ParameterKind.Absent, AlgorithmOid: var keyAlgorithmOid } when keyType.IsCurve25519() => EncodeCurve25519Key(privateKey.Span, keyAlgorithmOid),
            _ => throw new NotSupportedException($"Key type {keyType} is not supported for encoding.")
        };
    }

    /// <summary>
    /// Converts a private key and key type to ASN.1 DER encoded format in PKCS#8 structure.
    /// </summary>
    /// <param name="privateKey">The private key as a byte array.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    public static byte[] EncodeToPkcs8(ReadOnlyMemory<byte> privateKey, KeyType keyType) =>
        EncodeToPkcs8(privateKey, null, keyType);

    /// <summary>
    /// Converts RSA private key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The RSA parameters including private key values.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    public static byte[] EncodeToPkcs8(RSAParameters parameters) =>
        EncodeToPkcs8(parameters, integerContentCreated: null, rsaKeyEncoded: null);

    internal static byte[] EncodeToPkcs8(
        RSAParameters parameters,
        Action<byte[]>? integerContentCreated,
        Action<byte[]>? rsaKeyEncoded)
    {
        // Ensure parameters include private key parts
        if (parameters.D is null ||
            parameters.P is null ||
            parameters.Q is null ||
            parameters.DP is null ||
            parameters.DQ is null ||
            parameters.InverseQ is null)
        {
            throw new ArgumentException("All RSA Private key parameters must be provided.");
        }

        var rsaKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        _ = rsaKeyWriter.PushSequence();

        rsaKeyWriter.WriteInteger(0);

        // The RSAParameters arrays remain caller-owned. WriteInteger creates owned INTEGER content
        // octets for every field and clears them after AsnWriter has consumed them.
        WriteInteger(rsaKeyWriter, parameters.Modulus, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.Exponent, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.D, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.P, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.Q, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.DP, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.DQ, integerContentCreated);
        WriteInteger(rsaKeyWriter, parameters.InverseQ, integerContentCreated);

        rsaKeyWriter.PopSequence();

        var rsaKeyData = rsaKeyWriter.Encode();
        using var rsaKeyDataHandle = new DisposableBufferHandle(rsaKeyData);
        rsaKeyEncoded?.Invoke(rsaKeyData);

        // Start PrivateKeyInfo SEQUENCE
        var writer = new AsnWriter(AsnEncodingRules.DER);
        _ = writer.PushSequence();

        // Version
        writer.WriteInteger(0);

        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.Rsa);
        writer.WriteNull();
        writer.PopSequence();

        writer.WriteOctetString(rsaKeyDataHandle.Data.Span);

        writer.PopSequence();

        return writer.Encode();
    }

    private static void WriteInteger(
        AsnWriter writer,
        ReadOnlySpan<byte> value,
        Action<byte[]>? integerContentCreated)
    {
        var integerContent = AsnUtilities.GetOwnedIntegerContentOctets(value);
        using var integerContentHandle = new DisposableBufferHandle(integerContent);
        integerContentCreated?.Invoke(integerContent);
        writer.WriteInteger(integerContentHandle.Data.Span);
    }

    /// <summary>
    /// Converts EC private key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The EC parameters including private key value.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    /// <exception cref="ArgumentException">
    /// The private key parameter D is null or is not the curve's coordinate size, the curve is not
    /// a modeled short-Weierstrass curve, or the public point is half supplied (exactly one of
    /// <c>Q.X</c> and <c>Q.Y</c> is null).
    /// </exception>
    public static byte[] EncodeToPkcs8(ECParameters parameters)
    {
        if (parameters.D is null)
        {
            throw new ArgumentException("Private key parameter D must be provided.");
        }

        if (parameters.Curve.Oid.Value is null)
            throw new ArgumentException("Curve OID is null.");

        ReadOnlyMemory<byte> privateKey = parameters.D;
        var curveOid = parameters.Curve.Oid.Value;

        // Checks D against the curve and, in doing so, gates the untrusted curve OID at the boundary
        // where it arrives. Neither can be left to the point-shape check: a key with no public point
        // never reaches that check, and the point says nothing about D's width in any case. An
        // unchecked D emits an RFC 5915 ECPrivateKey whose privateKey OCTET STRING contradicts the
        // curve named beside it — malformed, but plausible enough to give the caller no signal.
        AsnUtilities.ValidateEcPrivateScalarArgument(privateKey.Span, curveOid, nameof(parameters));

        // The optional RFC 5915 publicKey field is omitted only when the caller supplied no point
        // at all. Half a point is caller error: silently dropping it would return a valid encoding
        // that has quietly discarded key material the caller asked to include.
        if ((parameters.Q.X is null) != (parameters.Q.Y is null))
        {
            throw new ArgumentException(
                parameters.Q.X is null
                    ? "EC public point is incomplete: Q.Y was supplied but Q.X is null. Supply both coordinates or neither."
                    : "EC public point is incomplete: Q.X was supplied but Q.Y is null. Supply both coordinates or neither.",
                nameof(parameters));
        }

        ReadOnlyMemory<byte>? publicPoint = null;
        if (parameters.Q.X is not null && parameters.Q.Y is not null)
        {
            publicPoint = AsnUtilities.BuildUncompressedEcPoint(
                parameters.Q.X,
                parameters.Q.Y,
                curveOid,
                nameof(parameters));
        }

        return EncodeECKey(privateKey, curveOid, publicPoint);
    }

    /// <summary>
    /// Creates an EC private key encoded in ASN.1 DER format.
    /// </summary>
    private static byte[] EncodeECKey(
        ReadOnlyMemory<byte> privateKey,
        string curveOid,
        ReadOnlyMemory<byte>? publicPoint)
    {
        AsnUtilities.ValidateEcPrivateScalarArgument(privateKey.Span, curveOid, nameof(privateKey));
        if (publicPoint.HasValue)
        {
            AsnUtilities.ValidateEcPointArgument(publicPoint.Value.Span, curveOid, nameof(publicPoint));
        }

        var ecKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        // Start ECPrivateKey SEQUENCE (RFC 5915)
        _ = ecKeyWriter.PushSequence();

        // Version (1)
        ecKeyWriter.WriteInteger(1);

        // Private key
        ecKeyWriter.WriteOctetString(privateKey.Span);

        // The inner [0] parameters are omitted; the outer AlgorithmIdentifier carries the curve OID.
        // Although the ASN.1 field is OPTIONAL, RFC 5915 §3 requires conforming implementations to
        // include it. This encoding therefore differs from that requirement.

        // [1] Public key (optional). RFC 5915 uses EXPLICIT tags, so this is a constructed [1]
        // wrapper around a universal BIT STRING. The primitive legacy form differs from that schema;
        // the encoder tests cover the explicit wrapper and selected BCL import scenarios.
        if (publicPoint.HasValue)
        {
            var publicKeyTag = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
            _ = ecKeyWriter.PushSequence(publicKeyTag);
            ecKeyWriter.WriteBitString(publicPoint.Value.Span);
            ecKeyWriter.PopSequence(publicKeyTag);
        }

        ecKeyWriter.PopSequence();
        using var ecPrivateKeyHandle = new DisposableBufferHandle(ecKeyWriter.Encode());

        // PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.EcPublicKey);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // PrivateKey as OCTET STRING
        writer.WriteOctetString(ecPrivateKeyHandle.Data.Span);
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeCurve25519Key(ReadOnlySpan<byte> privateKey, string keyAlgorithmOid)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 key must be 32 bytes.", nameof(privateKey));
        }

        // Create the PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start PrivateKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(keyAlgorithmOid);
        writer.PopSequence();

        // PrivateKey as OCTET STRING
        var privateKeyWriter = new AsnWriter(AsnEncodingRules.DER);
        privateKeyWriter.WriteOctetString(privateKey);

        using var privateKeyBytesHandle = new DisposableBufferHandle(privateKeyWriter.Encode());
        writer.WriteOctetString(privateKeyBytesHandle.Data.Span);

        // End PrivateKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
}