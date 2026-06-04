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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Represents a derived public key produced by ARKG-P256 derivation.
/// </summary>
/// <remarks>
/// <para>
/// This class contains the derived public key and handles needed for the
/// ESP256-split-ARKG previewSign helper path. Instances are obtained
/// by calling <see cref="PreviewSignGeneratedKey.DerivePublicKey"/> with
/// application-provided input keying material and a context string.
/// </para>
/// <para>
/// The derived public key can be used to verify signatures produced by the
/// YubiKey when signing with the corresponding ARKG key handle and context.
/// Use <see cref="VerifySignature"/> to validate signatures against this key.
/// </para>
/// <para>
/// To request a signature from the YubiKey using this derived key, pass the
/// <see cref="DeviceKeyHandle"/>, <see cref="ArkgKeyHandle"/>, and
/// <see cref="Context"/> properties to the previewSign authentication extension input.
/// </para>
/// </remarks>
public sealed class PreviewSignDerivedKey
{
    /// <summary>
    /// Gets the derived public key (65-byte SEC1 uncompressed point).
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; init; }

    /// <summary>
    /// Gets the ARKG key handle.
    /// </summary>
    public ReadOnlyMemory<byte> ArkgKeyHandle { get; init; }

    /// <summary>
    /// Gets the device key handle from the original registration.
    /// </summary>
    public ReadOnlyMemory<byte> DeviceKeyHandle { get; init; }

    /// <summary>
    /// Gets the context string used for derivation.
    /// </summary>
    public ReadOnlyMemory<byte> Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewSignDerivedKey"/> class.
    /// </summary>
    /// <param name="publicKey">The derived public key (65-byte SEC1 uncompressed point).</param>
    /// <param name="arkgKeyHandle">The ARKG key handle.</param>
    /// <param name="deviceKeyHandle">The device key handle.</param>
    /// <param name="context">The context string.</param>
    internal PreviewSignDerivedKey(
        ReadOnlyMemory<byte> publicKey,
        ReadOnlyMemory<byte> arkgKeyHandle,
        ReadOnlyMemory<byte> deviceKeyHandle,
        ReadOnlyMemory<byte> context)
    {
        PublicKey = publicKey;
        ArkgKeyHandle = arkgKeyHandle;
        DeviceKeyHandle = deviceKeyHandle;
        Context = context;
    }

    /// <summary>
    /// Verifies a signature against the derived public key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method verifies that a signature produced by the ESP256-split-ARKG
    /// previewSign path is valid for the given message using the derived public key
    /// from ARKG-P256 derivation.
    /// </para>
    /// <para>
    /// The signature must be in DER-encoded ECDSA format, as returned by the
    /// YubiKey's previewSign extension. The message should be the original raw data,
    /// not a pre-hashed value.
    /// </para>
    /// </remarks>
    /// <param name="message">
    /// The message that was signed. This method will hash the message internally
    /// before verifying the signature.
    /// </param>
    /// <param name="signature">
    /// The DER-encoded ESP256-split-ARKG previewSign signature to verify.
    /// </param>
    /// <returns>
    /// <c>true</c> if the signature is valid for the message using the derived
    /// public key; otherwise, <c>false</c>.
    /// </returns>
    public bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        // PublicKey is SEC1 uncompressed: 0x04 || X(32) || Y(32).
        if (PublicKey.Length != 65 || PublicKey.Span[0] != 0x04)
        {
            return false;
        }

        try
        {
            // Import the SEC1 uncompressed point into ECDsa
            using var ecdsa = ECDsa.Create();
            var parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = PublicKey.Slice(1, 32).ToArray(),
                    Y = PublicKey.Slice(33, 32).ToArray()
                }
            };
            ecdsa.ImportParameters(parameters);

            // Convert DER signature to IEEE P1363 format (r||s) for VerifyData
            var ieeeSignature = ConvertDerToIeee(signature);

            // Verify using SHA-256 (P-256 standard)
            return ecdsa.VerifyData(message, ieeeSignature, HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ConvertDerToIeee(ReadOnlySpan<byte> derSignature)
    {
        // Parse DER: SEQUENCE { r INTEGER, s INTEGER }
        var reader = new AsnReader(derSignature.ToArray(), AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        var r = sequence.ReadIntegerBytes();
        var s = sequence.ReadIntegerBytes();

        // Pad or trim to 32 bytes each (P-256 coordinate size)
        byte[] ieee = new byte[64];
        CopyWithPadding(r.Span, ieee.AsSpan(0, 32));
        CopyWithPadding(s.Span, ieee.AsSpan(32, 32));
        return ieee;
    }

    private static void CopyWithPadding(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        // Remove leading zero byte if present (DER encoding of positive integers)
        if (source.Length > destination.Length && source[0] == 0x00)
        {
            source = source[1..];
        }

        if (source.Length > destination.Length)
        {
            throw new InvalidOperationException("Signature component too large");
        }

        // Pad with leading zeros if needed
        int padding = destination.Length - source.Length;
        destination[..padding].Clear();
        source.CopyTo(destination[padding..]);
    }
}
