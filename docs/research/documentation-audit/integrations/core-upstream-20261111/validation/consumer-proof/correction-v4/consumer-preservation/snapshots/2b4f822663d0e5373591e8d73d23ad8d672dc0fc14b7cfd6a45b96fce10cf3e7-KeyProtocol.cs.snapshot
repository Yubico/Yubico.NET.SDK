// Copyright 2026 Yubico AB
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

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Keys;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class KeyProtocol
{
    private const byte Cla = 0x00;
    private const byte InsGenerate = 0x47;
    private const byte InsImport = 0xFE;
    private const byte InsMove = 0xF6;
    private const byte InsAttest = 0xF9;
    private const byte DeleteDestination = 0xFF;
    private const byte TagGenerationTemplate = 0xAC;
    private const byte TagAlgorithm = 0x80;
    private const byte TagPinPolicy = 0xAA;
    private const byte TagTouchPolicy = 0xAB;
    private const int TagPublicKeyTemplate = 0x7F49;
    private const byte TagPublicPoint = 0x86;
    private const byte UncompressedPoint = 0x04;
    private const int ShortTlvHeaderLength = 2;
    private const int KeyPolicyLength = 3;
    private const byte SingleByteValueLength = 1;
    private const byte TagRsaP = 0x01;
    private const byte TagRsaQ = 0x02;
    private const byte TagRsaDp = 0x03;
    private const byte TagRsaDq = 0x04;
    private const byte TagRsaInverseQ = 0x05;
    private const byte TagEcScalar = 0x06;
    private const byte TagEd25519PrivateKey = 0x07;
    private const byte TagX25519PrivateKey = 0x08;
    private const byte TagRsaModulus = 0x81;
    private const byte TagRsaExponent = 0x82;
    private static ReadOnlySpan<byte> RsaPublicExponent => [0x01, 0x00, 0x01];

    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(KeyProtocol));

    /// <summary>
    /// Generates a new key pair in the specified slot.
    /// </summary>
    /// <param name="slot">The slot where the key should be generated.</param>
    /// <param name="keyType">The algorithm to use for key generation.</param>
    /// <param name="pinPolicy">The PIN policy for using the generated key.</param>
    /// <param name="touchPolicy">The touch policy for using the generated key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The generated public key.</returns>
    /// <exception cref="InvalidOperationException">If session is not authenticated.</exception>
    /// <exception cref="NotSupportedException">If the algorithm is not supported on this firmware version.</exception>
    internal static async Task<IPublicKey> GenerateKeyAsync(
        IBackend backend,
        Slot slot,
        KeyType keyType,
        PinPolicy pinPolicy = PinPolicy.Default,
        TouchPolicy touchPolicy = TouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Generating key in slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, keyType);

        // Build the command data: TAG 0xAC [ TAG 0x80 (algorithm) + optional policies ]
        // Pre-compute size: 0xAC + len + (0x80,0x01,algo) + optional (0xAA,0x01,pin) + optional (0xAB,0x01,touch)
        int innerLength = KeyPolicyLength; // TAG 0x80 (algorithm) is always present
        if (pinPolicy != PinPolicy.Default)
            innerLength += KeyPolicyLength;
        if (touchPolicy != TouchPolicy.Default)
            innerLength += KeyPolicyLength;

        var data = new byte[ShortTlvHeaderLength + innerLength]; // 0xAC + length byte + inner content
        try
        {
            int offset = 0;

            data[offset++] = TagGenerationTemplate;
            data[offset++] = (byte)innerLength;

            data[offset++] = TagAlgorithm;
            data[offset++] = SingleByteValueLength;
            data[offset++] = KeyTypeEncoding.EncodeAsymmetric(keyType);

            if (pinPolicy != PinPolicy.Default)
            {
                data[offset++] = TagPinPolicy;
                data[offset++] = SingleByteValueLength;
                data[offset++] = (byte)pinPolicy;
            }

            if (touchPolicy != TouchPolicy.Default)
            {
                data[offset++] = TagTouchPolicy;
                data[offset++] = SingleByteValueLength;
                data[offset++] = (byte)touchPolicy;
            }

            var command = new ApduCommand(Cla, InsGenerate, 0, (byte)slot, data);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            if (response.SW == SWConstants.InsNotSupported)
                throw new NotSupportedException("PIV key generation is not supported by this YubiKey.");

            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW, $"Key generation failed for slot 0x{(byte)slot:X2}");
            }

            // Parse public key from response (TAG 0x7F49)
            return ParsePublicKey(response.Data, keyType);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <summary>
    /// Imports a private key into the specified slot.
    /// </summary>
    /// <param name="slot">The slot where the key should be imported.</param>
    /// <param name="privateKey">The private key to import.</param>
    /// <param name="pinPolicy">The PIN policy for using the imported key.</param>
    /// <param name="touchPolicy">The touch policy for using the imported key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes after successful import.</returns>
    /// <exception cref="InvalidOperationException">If session is not authenticated.</exception>
    /// <exception cref="NotSupportedException">If the key type is not supported.</exception>
    internal static async Task ImportKeyAsync(
        IBackend backend,
        Slot slot,
        IPrivateKey privateKey,
        PinPolicy pinPolicy = PinPolicy.Default,
        TouchPolicy touchPolicy = TouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(privateKey);

        Logger.LogDebug("PIV: Importing key into slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, privateKey.KeyType);

        var keyType = privateKey.KeyType;
        byte algorithmId = KeyTypeEncoding.EncodeAsymmetric(keyType);

        // Build TLV-encoded key data based on algorithm
        byte[]? keyData = null;
        try
        {
            keyData = privateKey switch
            {
                RSAPrivateKey rsaKey => EncodeRsaPrivateKey(rsaKey),
                ECPrivateKey ecKey => EncodeEcPrivateKey(ecKey),
                Curve25519PrivateKey curveKey => EncodeCurve25519PrivateKey(curveKey),
                _ => throw new NotSupportedException($"Private key type {privateKey.GetType().Name} is not supported")
            };

            // Add PIN policy TLV if not default
            if (pinPolicy != PinPolicy.Default)
            {
                using var pinPolicyTlv = new Tlv(TagPinPolicy, [(byte)pinPolicy]);
                keyData = AppendTlvZeroingPrevious(keyData!, pinPolicyTlv);
            }

            // Add touch policy TLV if not default
            if (touchPolicy != TouchPolicy.Default)
            {
                using var touchPolicyTlv = new Tlv(TagTouchPolicy, [(byte)touchPolicy]);
                keyData = AppendTlvZeroingPrevious(keyData!, touchPolicyTlv);
            }

            // Send IMPORT KEY command: INS 0xFE, P1 = algorithm, P2 = slot
            var command = new ApduCommand(Cla, InsImport, algorithmId, (byte)slot, keyData);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            if (response.SW == SWConstants.InsNotSupported)
                throw new NotSupportedException("PIV key import is not supported by this YubiKey.");

            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW, $"Key import failed for slot 0x{(byte)slot:X2}");
            }

            Logger.LogDebug("PIV: Key imported successfully into slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, keyType);
        }
        finally
        {
            // Zero sensitive key data
            if (keyData is not null)
            {
                CryptographicOperations.ZeroMemory(keyData);
            }
        }
    }

    /// <summary>
    /// Concatenates <paramref name="previous"/> with the encoded bytes of <paramref name="tlv"/> and
    /// zeroes <paramref name="previous"/> before returning, since <paramref name="previous"/> is
    /// raw encoded private-key material that would otherwise be silently orphaned and left unzeroed
    /// by the caller's reassignment (<c>keyData = AppendTlvZeroingPrevious(keyData, tlv)</c>).
    /// </summary>
    internal static byte[] AppendTlvZeroingPrevious(byte[] previous, Tlv tlv)
    {
        ArgumentNullException.ThrowIfNull(previous);

        var combined = new byte[previous.Length + tlv.TotalLength];
        try
        {
            previous.CopyTo(combined, 0);
            tlv.AsSpan().CopyTo(combined.AsSpan(previous.Length));
            return combined;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(previous);
        }
    }

    /// <summary>
    /// Encodes RSA private key components as TLV for PIV import.
    /// RSA format: TLV(0x01, P) + TLV(0x02, Q) + TLV(0x03, DP) + TLV(0x04, DQ) + TLV(0x05, InverseQ)
    /// </summary>
    private static byte[] EncodeRsaPrivateKey(RSAPrivateKey rsaKey)
    {
        var parameters = rsaKey.Parameters;

        // Validate RSA exponent is 65537 (standard requirement)
        if (parameters.Exponent is null || !parameters.Exponent.AsSpan().SequenceEqual(RsaPublicExponent))
        {
            throw new NotSupportedException("RSA exponent must be 65537 (0x010001)");
        }

        if (parameters.P is null || parameters.Q is null ||
            parameters.DP is null || parameters.DQ is null || parameters.InverseQ is null)
        {
            throw new ArgumentException("RSA private key is missing required CRT parameters (P, Q, DP, DQ, InverseQ)");
        }

        // All components should be half the modulus length
        var componentLength = rsaKey.KeyInfo.KeySizeBytes / 2;

        // Create TLVs for each component, padding to correct length
        using var pTlv = CreatePaddedComponent(TagRsaP, parameters.P, componentLength);
        using var qTlv = CreatePaddedComponent(TagRsaQ, parameters.Q, componentLength);
        using var dpTlv = CreatePaddedComponent(TagRsaDp, parameters.DP, componentLength);
        using var dqTlv = CreatePaddedComponent(TagRsaDq, parameters.DQ, componentLength);
        using var iqTlv = CreatePaddedComponent(TagRsaInverseQ, parameters.InverseQ, componentLength);

        // Concatenate all TLVs
        return [.. pTlv.AsSpan(),
                .. qTlv.AsSpan(),
                .. dpTlv.AsSpan(),
                .. dqTlv.AsSpan(),
                .. iqTlv.AsSpan()];
    }

    /// <summary>
    /// Encodes EC private key D value as TLV for PIV import.
    /// ECC format: TLV(0x06, D)
    /// </summary>
    private static byte[] EncodeEcPrivateKey(ECPrivateKey ecKey)
    {
        var parameters = ecKey.Parameters;

        if (parameters.D is null)
        {
            throw new ArgumentException("EC private key is missing D value");
        }

        // D should be padded to the curve size
        int scalarLength = ecKey.KeyInfo.PrivateValueLengthBytes
            ?? throw new NotSupportedException("The key has no modeled private scalar representation.");
        using var dTlv = CreatePaddedComponent(TagEcScalar, parameters.D, scalarLength);

        return dTlv.AsSpan().ToArray();
    }

    /// <summary>
    /// Encodes Curve25519 private key as TLV for PIV import.
    /// Ed25519 format: TLV(0x07, privateKey)
    /// X25519 format: TLV(0x08, privateKey)
    /// </summary>
    private static byte[] EncodeCurve25519PrivateKey(Curve25519PrivateKey curveKey)
    {
        var tag = curveKey.KeyType == KeyType.Ed25519 ? TagEd25519PrivateKey : TagX25519PrivateKey;
        int privateValueLength = curveKey.KeyInfo.PrivateValueLengthBytes
            ?? throw new NotSupportedException("The key has no modeled private value representation.");
        if (curveKey.PrivateKey.Length != privateValueLength)
            throw new ArgumentException("Private scalar/seed length does not match the key representation.", nameof(curveKey));
        using var tlv = new Tlv(tag, curveKey.PrivateKey.Span);

        return tlv.AsSpan().ToArray();
    }

    /// <summary>
    /// Pads byte array to target length with leading zeros if needed.
    /// </summary>
    private static Tlv CreatePaddedComponent(byte tag, ReadOnlySpan<byte> data, int targetLength)
    {
        if (data.Length > targetLength)
            throw new ArgumentException("Private component exceeds the canonical key size.", nameof(data));
        // Supported PIV components are at most 256 bytes. Tlv takes its own copy; no padded
        // private-material array is abandoned, and caller-owned key parameters are never cleared.
        Span<byte> padded = stackalloc byte[targetLength];
        try
        {
            padded.Clear();
            data.CopyTo(padded[(targetLength - data.Length)..]);
            return new Tlv(tag, padded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
        }
    }

    /// <summary>
    /// Moves a key from one slot to another.
    /// </summary>
    /// <param name="sourceSlot">The slot containing the key to move.</param>
    /// <param name="destinationSlot">The slot where the key should be moved.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="NotSupportedException">If firmware version is less than 5.7.0.</exception>
    internal static async Task MoveKeyAsync(
        IBackend backend,
        Slot sourceSlot,
        Slot destinationSlot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Moving key from slot 0x{Source:X2} to slot 0x{Dest:X2}",
            (byte)sourceSlot, (byte)destinationSlot);

        if (sourceSlot == Slot.Attestation)
        {
            throw new InvalidOperationException("Cannot move attestation key");
        }

        var command = new ApduCommand(Cla, InsMove, (byte)destinationSlot, (byte)sourceSlot);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        if (response.SW == SWConstants.InsNotSupported)
            throw new NotSupportedException("PIV key move is not supported by this YubiKey.");

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW,
                $"Failed to move key from slot 0x{(byte)sourceSlot:X2} to 0x{(byte)destinationSlot:X2}");
        }
    }

    /// <summary>
    /// Deletes a key from the specified slot.
    /// </summary>
    internal static async Task DeleteKeyAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Deleting key from slot 0x{Slot:X2}", (byte)slot);

        var command = new ApduCommand(Cla, InsMove, DeleteDestination, (byte)slot);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        if (response.SW == SWConstants.InsNotSupported)
            throw new NotSupportedException("PIV key deletion is not supported by this YubiKey.");

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW,
                $"Failed to delete key from slot 0x{(byte)slot:X2}");
        }
    }

    /// <summary>
    /// Generates an attestation certificate for a key in the specified slot.
    /// </summary>
    internal static async Task<X509Certificate2> AttestKeyAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Attesting key in slot 0x{Slot:X2}", (byte)slot);

        // The formatter adds a trailing 00 byte for Case 1 commands (no data, no Le)
        var command = new ApduCommand(Cla, InsAttest, (byte)slot, 0);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        if (response.SW == SWConstants.InsNotSupported)
            throw new NotSupportedException("PIV key attestation is not supported by this YubiKey.");

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW,
                $"Failed to attest key in slot 0x{(byte)slot:X2}");
        }

        // Response is a DER-encoded X.509 certificate
#pragma warning disable SYSLIB0057 // X509Certificate2(byte[]) is obsolete
        return new X509Certificate2(response.Data.ToArray());
#pragma warning restore SYSLIB0057
    }

    private static IPublicKey ParsePublicKey(ReadOnlyMemory<byte> data, KeyType keyType)
    {
        // Parse 0x7F49 (Public key template) - Tlv handles 2-byte tags
        using var template = Tlv.Create(data.Span);
        if (template.Tag != TagPublicKeyTemplate)
        {
            throw new ApduException("Invalid public key response format");
        }

        var keyData = template.Value.Span;

        // Parse based on algorithm
        return keyType switch
        {
            KeyType.P256 or KeyType.P384 => ParseEccPublicKey(keyData, keyType),
            KeyType.Ed25519 or KeyType.X25519 => ParseCurve25519PublicKey(keyData, keyType),
            KeyType.Rsa1024 or KeyType.Rsa2048 or KeyType.Rsa3072 or KeyType.Rsa4096
                => ParseRsaPublicKey(keyData),
            _ => throw new NotSupportedException($"Unsupported algorithm: {keyType}")
        };
    }

    private static IPublicKey ParseEccPublicKey(ReadOnlySpan<byte> data, KeyType keyType)
    {
        // ECC public key format: TAG 0x86 (EC point)
        if (data.Length < ShortTlvHeaderLength || data[0] != TagPublicPoint)
        {
            throw new ApduException("Invalid ECC public key format");
        }

        using var pointTlv = Tlv.Create(data);
        var point = pointTlv.Value.Span;

        // Convert to ECPublicKey using the Core cryptography types
        var curveInfo = keyType.GetKeyInfo().Curve
            ?? throw new NotSupportedException($"Key type {keyType} has no modeled curve.");
        var curve = ECCurve.CreateFromOid(new Oid(curveInfo.NamedCurveOid
            ?? throw new NotSupportedException($"Key type {keyType} has no EC curve OID.")));
        int coordinateSize = curveInfo.CoordinateLengthBytes;
        if (point.Length != 1 + 2 * coordinateSize || point[0] != UncompressedPoint)
            throw new ApduException("Invalid uncompressed EC public point length or format.");

        var parameters = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = point.Slice(1, coordinateSize).ToArray(),
                Y = point.Slice(1 + coordinateSize, coordinateSize).ToArray()
            }
        };

        return ECPublicKey.CreateFromParameters(parameters);
    }

    private static IPublicKey ParseCurve25519PublicKey(ReadOnlySpan<byte> data, KeyType keyType)
    {
        // Curve25519 public key format: TAG 0x86 (point)
        if (data.Length < ShortTlvHeaderLength || data[0] != TagPublicPoint)
        {
            throw new ApduException("Invalid Curve25519 public key format");
        }

        using var pointTlv = Tlv.Create(data);
        var point = pointTlv.Value.Span;

        if (point.Length != keyType.GetKeyInfo().PublicValueLengthBytes)
            throw new ApduException("Public point length does not match the key definition.");
        return Curve25519PublicKey.CreateFromValue(point.ToArray(), keyType);
    }

    private static IPublicKey ParseRsaPublicKey(ReadOnlySpan<byte> data)
    {
        // RSA public key format: TAG 0x81 (modulus) + TAG 0x82 (public exponent)
        var tlvDict = TlvHelper.DecodeDictionary(data);

        if (!tlvDict.TryGetValue(TagRsaModulus, out var modulusMemory))
        {
            throw new ApduException("Invalid RSA public key format: missing modulus");
        }

        if (!tlvDict.TryGetValue(TagRsaExponent, out var exponentMemory))
        {
            throw new ApduException("Invalid RSA public key format: missing exponent");
        }

        var parameters = new RSAParameters
        {
            Modulus = modulusMemory.ToArray(),
            Exponent = exponentMemory.ToArray()
        };

        return RSAPublicKey.CreateFromParameters(parameters);
    }
}