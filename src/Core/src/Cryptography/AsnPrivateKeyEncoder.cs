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
    /// Unlike <see cref="EncodeToPkcs8(ECParameters)"/>, the EC branch here does not length-check
    /// <paramref name="privateKey"/> against the curve. It takes raw values rather than a structure
    /// that names its own curve, and Curve25519 — the only key types any production caller reaches
    /// this overload with — is checked exactly in <c>EncodeCurve25519Key</c>. The EC branch is
    /// exercised only by tests today; if a production caller appears, it should be brought under
    /// the same scalar check as the <see cref="ECParameters"/> path.
    /// </remarks>
    public static byte[] EncodeToPkcs8(
        ReadOnlyMemory<byte> privateKey,
        ReadOnlyMemory<byte>? publicPoint,
        KeyType keyType)
    {
        return keyType switch
        {
            KeyType.ECP256 => EncodeECKey(privateKey, Oids.ECP256, publicPoint),
            KeyType.ECP384 => EncodeECKey(privateKey, Oids.ECP384, publicPoint),
            KeyType.ECP521 => EncodeECKey(privateKey, Oids.ECP521, publicPoint),
            KeyType.X25519 => EncodeCurve25519Key(privateKey.Span, Oids.X25519),
            KeyType.Ed25519 => EncodeCurve25519Key(privateKey.Span, Oids.Ed25519),
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
        writer.WriteObjectIdentifier(Oids.RSA);
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
    /// one of P-256, P-384 or P-521, or the public point is half supplied (exactly one of
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

        // [0] parameters (optional) - omitted since we include the OID in the AlgorithmIdentifier

        // [1] Public key (optional). RFC 5915 uses EXPLICIT tags, so this is a constructed [1]
        // wrapper around a universal BIT STRING. An implicitly tagged BIT STRING here is rejected
        // by standards-compliant importers, including ECDsa.ImportPkcs8PrivateKey.
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
        writer.WriteObjectIdentifier(Oids.ECDSA);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // PrivateKey as OCTET STRING
        writer.WriteOctetString(ecPrivateKeyHandle.Data.Span);
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeCurve25519Key(ReadOnlySpan<byte> privateKey, string algorithmOid)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 key must be 32 bytes.", nameof(privateKey));
        }

        if (algorithmOid is null)
        {
            throw new ArgumentException("Curve OID is null.");
        }

        if (!Oids.IsCurve25519Algorithm(algorithmOid))
        {
            throw new ArgumentException("Algorithm OID is not supported.", nameof(algorithmOid));
        }

        // Create the PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start PrivateKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(algorithmOid);
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