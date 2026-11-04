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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// A class that converts public key parameters to ASN.1 DER encoding.
/// </summary>
internal static class AsnPublicKeyEncoder
{
    /// <summary>
    /// Converts a public point and key type to ASN.1 DER encoded format.
    /// </summary>
    /// <param name="publicPoint">The public key point as a byte array.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>A byte array containing the ASN.1 DER encoded public key.</returns>
    public static byte[] EncodeToSubjectPublicKeyInfo(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyInfo = keyType.GetKeyInfo();
        return keyInfo.Asn1 switch
        {
            { ParameterKind: Asn1ParameterKind.NamedCurve, NamedCurveOid: { } curveOid } when keyInfo.IsShortWeierstrass => EncodeEcPublicKey(publicPoint, curveOid),
            { ParameterKind: Asn1ParameterKind.Absent } when keyType.IsCurve25519() => EncodeCurve25519PublicKey(publicPoint, keyInfo),
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
        var modulusBytes = modulus.ToArray();
        modulusBytes = AsnUtilities.EnsurePositive(modulusBytes);

        var exponentBytes = exponent.ToArray();
        exponentBytes = AsnUtilities.EnsurePositive(exponentBytes);

        keySequenceWriter.WriteInteger(modulusBytes);
        keySequenceWriter.WriteInteger(exponentBytes);

        keySequenceWriter.PopSequence();

        // Get the encoded RSA key
        var rsaKeyData = keySequenceWriter.Encode();

        // Write the complete ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.Rsa);
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
    public static byte[] EncodeToSubjectPublicKeyInfo(RSAParameters parameters)
    {
        if (parameters.Exponent is null ||
            parameters.Modulus is null)
        {
            throw new InvalidOperationException("Cannot export public key, missing required parameters");
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
        if (parameters.Q.X is null || parameters.Q.Y is null)
        {
            throw new ArgumentException("EC point coordinates cannot be null.", nameof(parameters));
        }

        if (parameters.Curve.Oid.Value is null)
            throw new ArgumentException("Curve OID is null.");

        var curveOid = parameters.Curve.Oid.Value;

        // Create the uncompressed EC point format: 0x04 || X || Y
        var uncompressedPoint = AsnUtilities.BuildUncompressedEcPoint(
            parameters.Q.X,
            parameters.Q.Y,
            curveOid,
            nameof(parameters));

        return EncodeEcPublicKey(uncompressedPoint, curveOid);
    }

    private static byte[] EncodeCurve25519PublicKey(ReadOnlyMemory<byte> publicKey, KeyInfo keyInfo)
    {
        var asn1 = keyInfo.Asn1 ?? throw new ArgumentException("The key has no ASN.1 representation.", nameof(keyInfo));
        if (publicKey.Length != keyInfo.PublicValueLengthBytes)
        {
            throw new ArgumentException("Curve25519 public key must be 32 bytes.");
        }

        // Start SubjectPublicKeyInfo SEQUENCE
        var writer = new AsnWriter(AsnEncodingRules.DER);
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(asn1.AlgorithmOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicKey.Span);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }

    private static byte[] EncodeEcPublicKey(ReadOnlyMemory<byte> publicPoint, string curveOid)
    {
        AsnUtilities.ValidateEcPointArgument(publicPoint.Span, curveOid, nameof(publicPoint));

        // Write ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.EcPublicKey);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicPoint.Span);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }
}