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
/// A class that converts public key parameters to ASN.1 DER encoding.
/// </summary>
internal static class AsnPublicKeyWriter
{
    /// <summary>
    /// Converts a public point and key type to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="publicPoint">The public key point as a byte array.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSubjectPublicKeyInfo(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        int coordinateLength = keyDefinition.LengthInBytes;
        return keyType switch
        {
            KeyType.ECP256 => EncodeECDsaPublicKey(publicPoint, Oids.ECP256, coordinateLength),
            KeyType.ECP384 => EncodeECDsaPublicKey(publicPoint, Oids.ECP384, coordinateLength),
            KeyType.ECP521 => EncodeECDsaPublicKey(publicPoint, Oids.ECP521, coordinateLength),
            KeyType.X25519 or KeyType.Ed25519 => EncodeCurve25519PublicKey(publicPoint, keyType),
            _ => throw new NotSupportedException($"Key type {keyType} is not supported for encoding.")
        };
    }

    /// <summary>
    /// Converts an RSA public key from modulus and exponent to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="modulus">The modulus of the RSA key as a byte array.</param>
    /// <param name="exponent">The exponent of the RSA key as a byte array.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSubjectPublicKeyInfo(
        ReadOnlyMemory<byte> modulus,
        ReadOnlyMemory<byte> exponent)
    {
        // Create RSA key sequence first
        var keySequenceWriter = new AsnWriter(AsnEncodingRules.DER);

        // Start RSAPublicKey SEQUENCE
        _ = keySequenceWriter.PushSequence();

        // Write modulus and exponent as INTEGER values
        byte[] modulusBytes = modulus.ToArray();
        modulusBytes = AsnUtilities.EnsurePositive(modulusBytes);

        byte[] exponentBytes = exponent.ToArray();
        exponentBytes = AsnUtilities.EnsurePositive(exponentBytes);

        keySequenceWriter.WriteInteger(modulusBytes);
        keySequenceWriter.WriteInteger(exponentBytes);

        keySequenceWriter.PopSequence();

        // Get the encoded RSA key
        byte[] rsaKeyData = keySequenceWriter.Encode();

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.RSA);
        writer.WriteNull();
        writer.PopSequence();

        // Write subject public key as BIT STRING - directly using the RSA key data
        writer.WriteBitString(rsaKeyData);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    /// <summary>
    /// Converts RSA public key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The RSA public key parameters.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    /// <remarks>
    /// Only public key parameters are supported. The method will throw an <see cref="ArgumentException"/> if any of the private key parameters are set.
    /// </remarks>
    public static byte[] EncodeToSubjectPublicKeyInfo(RSAParameters parameters)
    {
        if (parameters.D != null ||
            parameters.P != null ||
            parameters.Q != null ||
            parameters.DP != null ||
            parameters.DQ != null ||
            parameters.InverseQ != null)
        {
            throw new ArgumentException("Only public key parameters should be provided.");
        }

        return EncodeToSubjectPublicKeyInfo(parameters.Modulus, parameters.Exponent);
    }

    /// <summary>
    /// Converts EC public key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The EC public key parameters.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSubjectPublicKeyInfo(ECParameters parameters)
    {
        if (parameters.D != null)
        {
            throw new ArgumentException("Only public key parameters should be provided.", nameof(parameters));
        }

        if (parameters.Q.X == null)
        {
            throw new ArgumentException("EC point coordinates cannot be null.", nameof(parameters));
        }

        if (parameters.Q.Y == null)
        {
            throw new ArgumentException("EC point coordinates cannot be null.", nameof(parameters));
        }

        string curveOid = parameters.Curve.Oid.Value;

        // Create the uncompressed EC point format: 0x04 || X || Y
        byte[] xCoordinate = parameters.Q.X;
        byte[] yCoordinate = parameters.Q.Y;

        byte[] uncompressedPoint = new byte[1 + xCoordinate.Length + yCoordinate.Length];
        uncompressedPoint[0] = 0x04; // Uncompressed point format
        xCoordinate.CopyTo(uncompressedPoint, 1);
        yCoordinate.CopyTo(uncompressedPoint, 1 + xCoordinate.Length);

        // Write ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.ECDSA);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(uncompressedPoint);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeCurve25519PublicKey(ReadOnlyMemory<byte> publicKey, KeyType keyType)
    {
        var keyDefinition = keyType.GetKeyDefinition();
        if (keyDefinition.AlgorithmOid is null)
        {
            throw new ArgumentException("Curve OID is null.");
        }

        if (!Oids.IsCurve25519Algorithm(keyDefinition.AlgorithmOid))
        {
            throw new ArgumentException("Invalid curve OID.");
        }

        if (publicKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 public key must be 32 bytes.");
        }

        // Start SubjectPublicKeyInfo SEQUENCE
        var writer = new AsnWriter(AsnEncodingRules.DER);
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(keyDefinition.AlgorithmOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicKey.ToArray());

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeECDsaPublicKey(ReadOnlyMemory<byte> publicPoint, string curveOid, int coordinateSize)
    {
        if (publicPoint.Length == 0 || publicPoint.Span[0] != 0x04)
        {
            throw new ArgumentException("EC public point must be in uncompressed format (starting with 0x04).");
        }

        bool isValidLength = publicPoint.Length == 1 + (coordinateSize * 2);
        if (!isValidLength)
        {
            throw new ArgumentException(
                $"Invalid EC public point size for the specified curve. Expected {1 + (coordinateSize * 2)} bytes.");
        }

        // Write ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.ECDSA);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicPoint.ToArray());

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
}
