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
public static class AsnPrivateKeyWriter
{
    /// <summary>
    /// Converts a private key and key type to ASN.1 DER encoded format in PKCS#8 structure.
    /// </summary>
    /// <param name="privateKey">The private key as a byte array.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    public static byte[] EncodeToPkcs8(ReadOnlyMemory<byte> privateKey, KeyType keyType) // 
    {
        return keyType switch
        {
            KeyType.P256 => CreateEcEncodedKey(privateKey, KeyDefinitions.CryptoOids.P256, null),
            KeyType.P384 => CreateEcEncodedKey(privateKey, KeyDefinitions.CryptoOids.P384, null),
            KeyType.P521 => CreateEcEncodedKey(privateKey, KeyDefinitions.CryptoOids.P521, null),
            KeyType.X25519 => CreateX25519EncodedKey(privateKey.Span),
            KeyType.Ed25519 => CreateEd25519EncodedKey(privateKey.Span),
            _ => throw new NotSupportedException($"Key type {keyType} is not supported for encoding.")
        };
    }

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
            KeyType.P256 => CreateEcEncodedKey(
                privateKey, KeyDefinitions.CryptoOids.P256, publicPoint),
            KeyType.P384 => CreateEcEncodedKey(
                privateKey, KeyDefinitions.CryptoOids.P384, publicPoint),
            KeyType.P521 => CreateEcEncodedKey(
                privateKey, KeyDefinitions.CryptoOids.P521, publicPoint),
            KeyType.X25519 => CreateX25519EncodedKey(privateKey.Span),
            KeyType.Ed25519 => CreateEd25519EncodedKey(privateKey.Span),
            _ => throw new NotSupportedException($"Key type {keyType} is not supported for encoding.")
        };
    }

    /// <summary>
    /// Converts RSA private key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The RSA parameters including private key values.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key in PKCS#8 format.</returns>
    public static byte[] EncodeToPkcs8(RSAParameters parameters)
    {
        // Ensure parameters include private key parts
        if (parameters.D == null || parameters.P == null || parameters.Q == null ||
            parameters.DP == null || parameters.DQ == null || parameters.InverseQ == null)
        {
            throw new ArgumentException("Private key parameters must be provided.");
        }

        var rsaKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        _ = rsaKeyWriter.PushSequence();

        rsaKeyWriter.WriteInteger(0);

        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.Modulus));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.Exponent));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.D));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.P));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.Q));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.DP));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.DQ));
        rsaKeyWriter.WriteInteger(ProcessIntegerBytes(parameters.InverseQ));

        rsaKeyWriter.PopSequence();

        byte[] rsaKeyData = rsaKeyWriter.Encode();

        var writer = new AsnWriter(AsnEncodingRules.DER);

        _ = writer.PushSequence();

        writer.WriteInteger(0);

        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.CryptoOids.RSA);
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
    public static byte[] EncodeToPkcs8(ECParameters parameters)
    {
        // Ensure parameters include private key part
        if (parameters.D == null)
        {
            throw new ArgumentException("Private key parameter D must be provided.");
        }

        string curveOid = parameters.Curve.Oid.Value;
        ReadOnlyMemory<byte> privateKey = parameters.D;

        // Create public point if Q coordinates are available
        ReadOnlyMemory<byte>? publicPoint = null;
        if (parameters.Q.X != null && parameters.Q.Y != null)
        {
            byte[] xCoordinate = parameters.Q.X;
            byte[] yCoordinate = parameters.Q.Y;

            byte[] uncompressedPoint = new byte[1 + xCoordinate.Length + yCoordinate.Length];
            uncompressedPoint[0] = 0x04; // Uncompressed point format
            Buffer.BlockCopy(xCoordinate, 0, uncompressedPoint, 1, xCoordinate.Length);
            Buffer.BlockCopy(yCoordinate, 0, uncompressedPoint, 1 + xCoordinate.Length, yCoordinate.Length);

            publicPoint = uncompressedPoint;
        }

        return CreateEcEncodedKey(privateKey, curveOid, publicPoint);
    }

    /// <summary>
    /// Creates an EC private key encoded in ASN.1 DER format.
    /// </summary>
    private static byte[] CreateEcEncodedKey(
        ReadOnlyMemory<byte> privateKey,
        string curveOid,
        ReadOnlyMemory<byte>? publicPoint)
    {
        // First, create the EC private key structure
        var ecKeyWriter = new AsnWriter(AsnEncodingRules.DER);

        // Start ECPrivateKey SEQUENCE (RFC 5915)
        _ = ecKeyWriter.PushSequence();

        // Version (1)
        ecKeyWriter.WriteInteger(1);

        // Private key as OCTET STRING
        ecKeyWriter.WriteOctetString(privateKey.ToArray());

        // [0] parameters (optional) - omitted since we include the OID in the AlgorithmIdentifier

        // [1] Public key (optional)
        if (publicPoint.HasValue)
        {
            ecKeyWriter.WriteBitString(
                publicPoint.Value.ToArray(),
                0,
                new Asn1Tag(TagClass.ContextSpecific, 1));
        }

        // End ECPrivateKey SEQUENCE
        ecKeyWriter.PopSequence();

        byte[] ecKeyData = ecKeyWriter.Encode();

        // Now create the PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start PrivateKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.CryptoOids.ECDSA);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // PrivateKey as OCTET STRING (contains the EC private key)
        writer.WriteOctetString(ecKeyData);

        // End PrivateKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    /// <summary>
    /// Creates an Ed25519 private key encoded in ASN.1 DER format.
    /// </summary>
    private static byte[] CreateEd25519EncodedKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Ed25519 private key must be 32 bytes.");
        }

        // For Ed25519, create a simple octet string for the private key
        var octetWriter = new AsnWriter(AsnEncodingRules.DER);
        octetWriter.WriteOctetString(privateKey.ToArray());
        byte[] keyData = octetWriter.Encode();

        // Create the PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start PrivateKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.CryptoOids.Ed25519);
        writer.PopSequence();

        // PrivateKey as OCTET STRING
        writer.WriteOctetString(keyData);

        // End PrivateKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    /// <summary>
    /// Creates an X25519 private key encoded in ASN.1 DER format.
    /// </summary>
    private static byte[] CreateX25519EncodedKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("X25519 private key must be 32 bytes.");
        }

        // For X25519, create a simple octet string for the private key
        var octetWriter = new AsnWriter(AsnEncodingRules.DER);
        octetWriter.WriteOctetString(privateKey.ToArray());
        byte[] keyData = octetWriter.Encode();

        // Create the PKCS#8 PrivateKeyInfo structure
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start PrivateKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Version (0)
        writer.WriteInteger(0);

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.CryptoOids.X25519);
        writer.PopSequence();

        // PrivateKey as OCTET STRING
        writer.WriteOctetString(keyData);

        // End PrivateKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    // Process integer bytes for ASN.1 DER encoding
    // 1. Ensures the integer value is treated as positive by adding a leading zero if needed
    // 2. Removes redundant leading zero bytes to avoid ASN.1 encoding errors
    private static byte[] ProcessIntegerBytes(byte[]? value)
    {
        if (value == null || value.Length == 0)
        {
            return [];
        }

        // Make a copy to avoid modifying the original
        byte[] result = value;

        // First, trim any redundant leading zeros
        int startIndex = 0;
        while (startIndex < result.Length - 1 && result[startIndex] == 0)
        {
            startIndex++;
        }

        if (startIndex > 0)
        {
            byte[] trimmed = new byte[result.Length - startIndex];
            Buffer.BlockCopy(result, startIndex, trimmed, 0, trimmed.Length);
            result = trimmed;
        }

        // Then, check if we need to add a leading zero to ensure positive interpretation
        if ((result[0] & 0x80) != 0)
        {
            byte[] padded = new byte[result.Length + 1];
            padded[0] = 0; // Add leading zero
            Buffer.BlockCopy(result, 0, padded, 1, result.Length);
            return padded;
        }

        return result;
    }
}

// Extension class to add methods to IPrivateKeyParameters for ASN.1 DER encoding
public static class PrivateKeyParametersExtensions
{
    /// <summary>
    /// Converts a private key parameter object to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The private key parameters.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded private key.</returns>
    public static byte[] EncodeToPkcs8(this IPrivateKeyParameters parameters) // TODO keep?
    {
        return parameters switch
        {
            RSAPrivateKeyParameters rsaParams => AsnPrivateKeyWriter.EncodeToPkcs8(rsaParams.Parameters),
            ECPrivateKeyParameters ecParams => AsnPrivateKeyWriter.EncodeToPkcs8(ecParams.Parameters),
            Curve25519PrivateKeyParameters edParams when edParams.KeyType == KeyType.Ed25519 
                => AsnPrivateKeyWriter.EncodeToPkcs8(edParams.PrivateKey, KeyType.Ed25519),
            Curve25519PrivateKeyParameters x25519Params when x25519Params.KeyType == KeyType.X25519  
                => AsnPrivateKeyWriter.EncodeToPkcs8(x25519Params.PrivateKey, KeyType.X25519),
            _ => throw new NotSupportedException($"Key type {parameters.GetType().Name} is not supported for encoding.")
        };
    }
}
