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
public static class AsnPublicKeyWriter
{
    /// <summary>
    /// Converts a public point and key type to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="publicPoint">The public key point as a byte array.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSpki(ReadOnlyMemory<byte> publicPoint, KeyDefinitions.KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        int coordinateLength = keyDefinition.LengthInBytes;
        return keyType switch
        {
            KeyDefinitions.KeyType.P256 => CreateEcEncodedKey(publicPoint, KeyDefinitions.KeyOids.Curve.P256, coordinateLength),
            KeyDefinitions.KeyType.P384 => CreateEcEncodedKey(publicPoint, KeyDefinitions.KeyOids.Curve.P384, coordinateLength),
            KeyDefinitions.KeyType.P521 => CreateEcEncodedKey(publicPoint, KeyDefinitions.KeyOids.Curve.P521, coordinateLength),
            KeyDefinitions.KeyType.X25519 => CreateCurve25519ToSpki(publicPoint, keyType),
            KeyDefinitions.KeyType.Ed25519 => CreateCurve25519ToSpki(publicPoint, keyType),
            _ => throw new NotSupportedException($"Key type {keyType} is not supported for encoding.")
        };
    }

    /// <summary>
    /// Converts an RSA public key from modulus and exponent to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="modulus">The modulus of the RSA key as a byte array.</param>
    /// <param name="exponent">The exponent of the RSA key as a byte array.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSpki(
        ReadOnlyMemory<byte> modulus,
        ReadOnlyMemory<byte> exponent)
    {
        // Create RSA key sequence first
        var keySequenceWriter = new AsnWriter(AsnEncodingRules.DER);

        // Start RSAPublicKey SEQUENCE
        _ = keySequenceWriter.PushSequence();

        // Write modulus and exponent as INTEGER values
        byte[] modulusBytes = modulus.ToArray();
        modulusBytes = EnsurePositive(modulusBytes);
        
        byte[] exponentBytes = exponent.ToArray();
        exponentBytes = EnsurePositive(exponentBytes);

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
        writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Rsa);
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
    public static byte[] EncodeToSpki(RSAParameters parameters)
    {
        // Ensure parameters are only public
        if (parameters.D != null || parameters.P != null || parameters.Q != null ||
            parameters.DP != null || parameters.DQ != null || parameters.InverseQ != null)
        {
            throw new ArgumentException("Only public key parameters should be provided.");
        }

        return EncodeToSpki(parameters.Modulus, parameters.Exponent);
    }

    /// <summary>
    /// Converts EC public key parameters to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The EC public key parameters.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSpki(ECParameters parameters)
    {
        // Ensure parameters are only public
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
        Buffer.BlockCopy(xCoordinate, 0, uncompressedPoint, 1, xCoordinate.Length);
        Buffer.BlockCopy(yCoordinate, 0, uncompressedPoint, 1 + xCoordinate.Length, yCoordinate.Length);

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(uncompressedPoint);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
    
    private static byte[] CreateCurve25519ToSpki(ReadOnlyMemory<byte> publicKey, KeyDefinitions.KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (keyDefinition.AlgorithmOid is null)
        {
            throw new ArgumentException("Curve OID is null.");
        }

        if (keyDefinition.AlgorithmOid != KeyDefinitions.KeyOids.Algorithm.X25519 && 
            keyDefinition.AlgorithmOid != KeyDefinitions.KeyOids.Algorithm.Ed25519)
        {
            throw new ArgumentException("Invalid curve OID."); 
        }
        
        if (publicKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 public key must be 32 bytes.");
        }

        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
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

    private static byte[] CreateEd25519ToSpki(ReadOnlyMemory<byte> publicKey)
    {
        if (publicKey.Length != 32)
        {
            throw new ArgumentException("Ed25519 public key must be 32 bytes.");
        }

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Ed25519);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicKey.ToArray());

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    } 

    private static byte[] CreateX255519ToSpki(ReadOnlyMemory<byte> publicKey) 
    {
        if (publicKey.Length != 32)
        {
            throw new ArgumentException("X25519 public key must be 32 bytes.");
        }

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.X25519);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicKey.ToArray());

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    // Creates an EC encoded key from a public point (which should be in the format: 0x04 + X + Y for uncompressed form)
    private static byte[] CreateEcEncodedKey(ReadOnlyMemory<byte> publicPoint, string curveOid, int coordinateSize)
    {
        // Validate the public point format
        if (publicPoint.Length == 0 || publicPoint.Span[0] != 0x04)
        {
            throw new ArgumentException("EC public point must be in uncompressed format (starting with 0x04).");
        }

        // Expected length for uncompressed point: 1 + (coordinateSize * 2)
        if (publicPoint.Length != 1 + (coordinateSize * 2))
        {
            throw new ArgumentException(
                $"Invalid EC public point size for the specified curve. Expected {1 + (coordinateSize * 2)} bytes.");
        }

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicPoint.ToArray());

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
    
    // Ensures the integer value is treated as positive by adding a leading zero if needed
    private static byte[] EnsurePositive(byte[]? value)
    {
        if (value == null || value.Length == 0)
        {
            return [];
        }

        // Check if the most significant bit is set, indicating a negative number in two's complement
        if ((value[0] & 0x80) != 0)
        {
            byte[] padded = new byte[value.Length + 1];
            padded[0] = 0; // Add leading zero
            Buffer.BlockCopy(value, 0, padded, 1, value.Length);
            return padded;
        }

        return value;
    }
}

// Extension class to add methods to IPublicKeyParameters for ASN.1 DER encoding
public static class PublicKeyParametersExtensions
{
    /// <summary>
    /// Converts a public key parameter object to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="parameters">The public key parameters.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] ToEncodedKey(this IPublicKeyParameters parameters)
    {
        return parameters switch
        {
            RSAPublicKeyParameters rsaParams => AsnPublicKeyWriter.EncodeToSpki(rsaParams.Parameters),
            ECPublicKeyParameters ecParams => AsnPublicKeyWriter.EncodeToSpki(ecParams.Parameters),
            EDsaPublicKeyParameters edParams => AsnPublicKeyWriter.EncodeToSpki(edParams.GetPublicPoint(), KeyDefinitions.KeyType.Ed25519),
            ECX25519PublicKeyParameters x25519Params => AsnPublicKeyWriter.EncodeToSpki(x25519Params.GetPublicPoint(), KeyDefinitions.KeyType.X25519),
            _ => throw new NotSupportedException($"Key type {parameters.GetType().Name} is not supported for encoding.")
        };
    }
}
