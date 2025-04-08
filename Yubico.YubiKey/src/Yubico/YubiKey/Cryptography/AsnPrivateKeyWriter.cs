// Copyright 2024 Yubico AB
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

using System;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

/// <summary>
/// A class that converts private key parameters to ASN.1 DER encoding.
/// </summary>
internal static class AsnPrivateKeyWriter
{
    /// <summary>
    /// Converts a private key and its corresponding public point to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="privateKey">The private key as a byte array.</param>
    /// <param name="publicPoint">The public key point as a byte array (optional).</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key.</returns>
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
    public static byte[] EncodeToPkcs8(RSAParameters parameters)
    {
        // Ensure parameters include private key parts
        if (parameters.D == null ||
            parameters.P == null ||
            parameters.Q == null ||
            parameters.DP == null ||
            parameters.DQ == null ||
            parameters.InverseQ == null)
        {
            throw new ArgumentException("All RSA Private key parameters must be provided.");
        }

        var rsaKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        _ = rsaKeyWriter.PushSequence();

        rsaKeyWriter.WriteInteger(0);

        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.Modulus));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.Exponent));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.D));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.P));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.Q));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.DP));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.DQ));
        rsaKeyWriter.WriteInteger(AsnUtilities.GetIntegerBytes(parameters.InverseQ));

        rsaKeyWriter.PopSequence();

        byte[] rsaKeyData = rsaKeyWriter.Encode();

        // Start PrivateKeyInfo SEQUENCE
        var writer = new AsnWriter(AsnEncodingRules.DER);
        _ = writer.PushSequence();

        // Version
        writer.WriteInteger(0);

        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.RSA);
        writer.WriteNull();
        writer.PopSequence();

        writer.WriteOctetString(rsaKeyData);

        writer.PopSequence();

        return writer.Encode();
    }

    /// <summary>
    /// Converts EC private key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The EC parameters including private key value.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    /// <exception cref="ArgumentException">Thrown when the private key parameter D is null.</exception>
    public static byte[] EncodeToPkcs8(ECParameters parameters)
    {
        if (parameters.D == null)
        {
            throw new ArgumentException("Private key parameter D must be provided.");
        }

        string curveOid = parameters.Curve.Oid.Value;
        ReadOnlyMemory<byte> privateKey = parameters.D;

        // Create public point if Q coordinates are available
        ReadOnlyMemory<byte>? publicPoint = null;
        if (parameters.Q is { X: not null, Y: not null })
        {
            byte[] xCoordinate = parameters.Q.X;
            byte[] yCoordinate = parameters.Q.Y;

            Memory<byte> uncompressedPoint = new byte[1 + xCoordinate.Length + yCoordinate.Length];
            uncompressedPoint.Span[0] = 0x04; // Uncompressed point format
            xCoordinate.CopyTo(uncompressedPoint[1..]);
            yCoordinate.CopyTo(uncompressedPoint[(1 + xCoordinate.Length)..]);
            publicPoint = uncompressedPoint;
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
        var ecKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        // Start ECPrivateKey SEQUENCE (RFC 5915)
        _ = ecKeyWriter.PushSequence();

        // Version (1)
        ecKeyWriter.WriteInteger(1);

        // Private key
        ecKeyWriter.WriteOctetString(privateKey.Span);

        // [0] parameters (optional) - omitted since we include the OID in the AlgorithmIdentifier

        // [1] Public key (optional)
        if (publicPoint.HasValue)
        {
            ecKeyWriter.WriteBitString(
                publicPoint.Value.Span,
                0,
                new Asn1Tag(TagClass.ContextSpecific, 1));
        }
        ecKeyWriter.PopSequence();
        using var ecPrivateKeyHandle = new ZeroingMemoryHandle(ecKeyWriter.Encode());

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
        writer.WriteOctetString(ecPrivateKeyHandle.Data);
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeCurve25519Key(ReadOnlySpan<byte> privateKey, string algorithmOid)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 key must be 32 bytes.");
        }

        if (algorithmOid == null)
        {
            throw new ArgumentException("Curve OID is null.");
        }

        if (!Oids.IsCurve25519Algorithm(algorithmOid))
        {
            throw new ArgumentException("Algorithm OID is not supported.", nameof(algorithmOid));
        }

        if (algorithmOid == Oids.X25519)
        {
            AsnUtilities.VerifyX25519PrivateKey(privateKey);
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
        
        using var privateKeyBytesHandle = new ZeroingMemoryHandle(privateKeyWriter.Encode());
        writer.WriteOctetString(privateKeyBytesHandle.Data);

        // End PrivateKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
}
