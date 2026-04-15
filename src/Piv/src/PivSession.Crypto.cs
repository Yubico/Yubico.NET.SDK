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
using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Signs or decrypts data using the private key in the specified slot.
    /// </summary>
    /// <param name="slot">The slot containing the private key.</param>
    /// <param name="algorithm">The algorithm of the key.</param>
    /// <param name="data">The data to sign or decrypt.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The signature or decrypted data.</returns>
    /// <remarks>
    /// <para>
    /// For RSA signatures, the data should be pre-hashed and padded according to the signature scheme.
    /// For ECC signatures, the data should be the hash to sign.
    /// For RSA decryption, the data should be the encrypted message.
    /// </para>
    /// <para>
    /// PIN verification may be required before this operation depending on the key's PIN policy.
    /// </para>
    /// </remarks>
    public Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default) =>
        SignOrDecryptCoreAsync(slot, algorithm, data, cancellationToken);

    /// <summary>
    /// Signs or decrypts data using the private key in the specified slot, auto-detecting the algorithm.
    /// </summary>
    /// <param name="slot">The slot containing the private key.</param>
    /// <param name="data">The data to sign or decrypt.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The signature or decrypted data.</returns>
    /// <remarks>
    /// <para>
    /// <b>Requires YubiKey firmware 5.3+.</b> This overload queries slot metadata to determine
    /// the key algorithm automatically, eliminating the need to track algorithms separately.
    /// </para>
    /// <para>
    /// For YubiKeys with firmware older than 5.3, use the overload that accepts an explicit
    /// algorithm parameter.
    /// </para>
    /// <para>
    /// PIN verification may be required before this operation depending on the key's PIN policy.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">YubiKey firmware is older than 5.3 and does not support metadata retrieval.</exception>
    /// <exception cref="InvalidOperationException">Slot is empty (no key present).</exception>
    public async Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: SignOrDecryptAsync auto-detecting algorithm for slot 0x{Slot:X2}", (byte)slot);

        // Check firmware version supports metadata
        if (!IsSupported(PivFeatures.Metadata))
        {
            throw new NotSupportedException(
                $"Auto-detecting algorithm requires YubiKey firmware 5.3 or later. " +
                $"Current firmware: {FirmwareVersion}. Use the overload that accepts an explicit algorithm parameter.");
        }

        // Query slot metadata to get the algorithm
        var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                $"Slot 0x{(byte)slot:X2} is empty. Generate or import a key before signing/decrypting.");
        }

        var slotMetadata = metadata.Value;
        Logger.LogDebug("PIV: Auto-detected algorithm {Algorithm} for slot 0x{Slot:X2}", slotMetadata.Algorithm, (byte)slot);

        return await SignOrDecryptCoreAsync(slot, slotMetadata.Algorithm, data, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<byte>> SignOrDecryptCoreAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("PIV: Signing/decrypting with slot 0x{Slot:X2}, algorithm {Algorithm}", (byte)slot, algorithm);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Notify user if touch may be required
        await NotifyTouchIfRequiredAsync(slot, cancellationToken).ConfigureAwait(false);

        // Prepare data according to algorithm key size
        var preparedData = PrepareDataForCrypto(algorithm, data);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x81 (challenge) ]
        // Pre-compute lengths to avoid List<byte> resizing (which can leave unzeroed copies)

        // Inner content: TAG 0x82 (2 bytes) + TAG 0x81 + length encoding + data
        int dataLenEncodingSize = preparedData.Length > 255 ? 3
            : preparedData.Length > 127 ? 2 : 1;
        int innerLength = 2 + 1 + dataLenEncodingSize + preparedData.Length; // 0x82,0x00 + 0x81 + len + data

        // Template: TAG 0x7C + length encoding + inner content
        int templateLenEncodingSize = innerLength > 255 ? 3
            : innerLength > 127 ? 2 : 1;
        int totalLength = 1 + templateLenEncodingSize + innerLength; // 0x7C + template len + inner

        var commandData = new byte[totalLength];
        try
        {
            int offset = 0;

            // TAG 0x7C
            commandData[offset++] = 0x7C;

            // Template length encoding
            if (innerLength > 255)
            {
                commandData[offset++] = 0x82;
                commandData[offset++] = (byte)(innerLength >> 8);
                commandData[offset++] = (byte)(innerLength & 0xFF);
            }
            else if (innerLength > 127)
            {
                commandData[offset++] = 0x81;
                commandData[offset++] = (byte)innerLength;
            }
            else
            {
                commandData[offset++] = (byte)innerLength;
            }

            // TAG 0x82 (Expected response - empty)
            commandData[offset++] = 0x82;
            commandData[offset++] = 0x00;

            // TAG 0x81 (Challenge/data to sign/decrypt)
            commandData[offset++] = 0x81;
            if (preparedData.Length > 255)
            {
                commandData[offset++] = 0x82;
                commandData[offset++] = (byte)(preparedData.Length >> 8);
                commandData[offset++] = (byte)(preparedData.Length & 0xFF);
            }
            else if (preparedData.Length > 127)
            {
                commandData[offset++] = 0x81;
                commandData[offset++] = (byte)preparedData.Length;
            }
            else
            {
                commandData[offset++] = (byte)preparedData.Length;
            }

            preparedData.CopyTo(commandData.AsSpan(offset));

            // INS 0x87 (AUTHENTICATE), P1 = algorithm, P2 = slot
            var command = new ApduCommand(0x00, 0x87, (byte)algorithm, (byte)slot, commandData);
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                // Check if PIN verification is required
                if (response.SW == 0x6982)
                {
                    throw new InvalidOperationException(
                        "Security status not satisfied. PIN verification may be required before this operation.");
                }

                throw ApduException.FromStatusWord(response.SW,
                    $"Sign/decrypt operation failed for slot 0x{(byte)slot:X2}");
            }

            // Parse response: TAG 0x7C [ TAG 0x82 (response data) ]
            return ParseCryptoResponse(response.Data);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(preparedData);
            CryptographicOperations.ZeroMemory(commandData);
        }
    }

    /// <inheritdoc/>
    public async Task<ReadOnlyMemory<byte>> DecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(padding);
        Logger.LogDebug("PIV: DecryptAsync slot 0x{Slot:X2}", (byte)slot);

        var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);
        if (metadata is null || !metadata.Value.Algorithm.IsRsa())
        {
            throw new ArgumentException(
                $"Slot 0x{(byte)slot:X2} does not contain an RSA key.", nameof(slot));
        }

        var algorithm = metadata.Value.Algorithm;
        int keyBits = algorithm switch
        {
            PivAlgorithm.Rsa1024 => 1024,
            PivAlgorithm.Rsa2048 => 2048,
            PivAlgorithm.Rsa3072 => 3072,
            PivAlgorithm.Rsa4096 => 4096,
            _ => throw new ArgumentException("Slot does not contain an RSA key.", nameof(slot))
        };

        if (cipherText.Length != keyBits / 8)
        {
            throw new ArgumentException(
                $"Cipher text length {cipherText.Length} does not match RSA-{keyBits} key size ({keyBits / 8} bytes).",
                nameof(cipherText));
        }

        // Perform the raw RSA private key operation on the YubiKey
        var rawDecrypted = await SignOrDecryptCoreAsync(slot, algorithm, cipherText, cancellationToken).ConfigureAwait(false);

        // Strip padding using a dummy RSA key — same technique as Python yubikey-manager's _unpad_message.
        // We generate a temporary RSA key of the same size, use textbook RSA (encrypt with public key)
        // to re-wrap the raw bytes, then decrypt with the dummy private key and real padding params.
        // This lets .NET's crypto library handle PKCS#1 v1.5 and OAEP unpadding correctly.
        using var dummy = RSA.Create(keyBits);
        var publicParams = dummy.ExportParameters(false);
        var n = publicParams.Modulus ?? throw new CryptographicException("RSA key export returned null modulus.");
        var e = publicParams.Exponent ?? throw new CryptographicException("RSA key export returned null exponent.");

        // rawBytes and reEncrypted are sensitive — zeroed in finally regardless of outcome.
        // The try starts before ModPow so rawBytes is zeroed even if ModPow throws.
        var rawBytes = rawDecrypted.Span.ToArray();
        byte[]? reEncrypted = null;
        try
        {
            reEncrypted = ModPow(rawBytes, e, n, keyBits / 8);
            return dummy.Decrypt(reEncrypted, padding);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "Padding removal failed. The cipher text may be corrupt or encrypted with a different padding scheme.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawBytes);
            if (reEncrypted is not null)
            {
                CryptographicOperations.ZeroMemory(reEncrypted);
            }
        }
    }

    /// <summary>
    /// Raw modular exponentiation for re-wrapping RSA output: result = base^exp mod modulus.
    /// </summary>
    private static byte[] ModPow(byte[] baseBytes, byte[] expBytes, byte[] modBytes, int outputLength)
    {
        var b = new System.Numerics.BigInteger(baseBytes, isUnsigned: true, isBigEndian: true);
        var exp = new System.Numerics.BigInteger(expBytes, isUnsigned: true, isBigEndian: true);
        var mod = new System.Numerics.BigInteger(modBytes, isUnsigned: true, isBigEndian: true);

        // The re-wrap technique requires b < mod. If the raw YubiKey output (interpreted as an integer)
        // is >= the dummy modulus, ModPow silently reduces it and the result won't unpad correctly.
        // This is astronomically unlikely with a fresh random modulus of equal bit length, but we
        // guard explicitly so any failure produces a clear error rather than a corrupt padding message.
        if (b >= mod)
        {
            throw new CryptographicException(
                "RSA re-wrap invariant violated: raw decryption output >= dummy modulus. Retry the operation.");
        }

        var result = System.Numerics.BigInteger.ModPow(b, exp, mod);
        var raw = result.ToByteArray(isUnsigned: true, isBigEndian: true);

        try
        {
            if (raw.Length > outputLength)
            {
                throw new CryptographicException(
                    $"ModPow result ({raw.Length} bytes) exceeds expected output length ({outputLength} bytes).");
            }

            if (raw.Length == outputLength)
            {
                return raw;
            }

            // Pad with leading zeros if result is shorter than key size (leading zeros lost in BigInteger)
            var padded = new byte[outputLength];
            raw.CopyTo(padded.AsSpan(outputLength - raw.Length));
            return padded;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    /// <summary>
    /// Performs Elliptic Curve Diffie-Hellman (ECDH) key agreement.
    /// </summary>
    /// <param name="slot">The slot containing the private key.</param>
    /// <param name="peerPublicKey">The peer's public key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The shared secret (x-coordinate for NIST curves, point for Curve25519).</returns>
    public async Task<ReadOnlyMemory<byte>> CalculateSecretAsync(
        PivSlot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Calculating shared secret with slot 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Notify user if touch may be required
        await NotifyTouchIfRequiredAsync(slot, cancellationToken).ConfigureAwait(false);

        // Determine algorithm from public key
        var algorithm = peerPublicKey switch
        {
            ECPublicKey ecKey when ecKey.KeyDefinition.KeyType == KeyType.ECP256 => PivAlgorithm.EccP256,
            ECPublicKey ecKey when ecKey.KeyDefinition.KeyType == KeyType.ECP384 => PivAlgorithm.EccP384,
            Curve25519PublicKey cv25519 when cv25519.KeyDefinition.KeyType == KeyType.X25519 => PivAlgorithm.X25519,
            _ => throw new ArgumentException("Unsupported public key type for ECDH", nameof(peerPublicKey))
        };

        // Encode peer public key
        var peerKeyData = EncodePeerPublicKey(peerPublicKey);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x85 (exponentiation) ]
        // Pre-compute lengths to avoid List<byte> resizing (which can leave unzeroed copies)

        // Inner content: TAG 0x82 (2 bytes) + TAG 0x85 + length encoding + data
        int keyLenEncodingSize = peerKeyData.Length > 127 ? 2 : 1;
        int innerLength = 2 + 1 + keyLenEncodingSize + peerKeyData.Length; // 0x82,0x00 + 0x85 + len + data

        // Template: TAG 0x7C + length encoding + inner content
        int templateLenEncodingSize = innerLength > 127 ? 2 : 1;
        int totalLength = 1 + templateLenEncodingSize + innerLength; // 0x7C + template len + inner

        var data = new byte[totalLength];
        try
        {
            int offset = 0;

            // TAG 0x7C
            data[offset++] = 0x7C;

            // Template length encoding
            if (innerLength > 127)
            {
                data[offset++] = 0x81;
                data[offset++] = (byte)innerLength;
            }
            else
            {
                data[offset++] = (byte)innerLength;
            }

            // TAG 0x82 (Expected response - empty)
            data[offset++] = 0x82;
            data[offset++] = 0x00;

            // TAG 0x85 (Exponentiation data - peer public key)
            data[offset++] = 0x85;
            if (peerKeyData.Length > 127)
            {
                data[offset++] = 0x81;
                data[offset++] = (byte)peerKeyData.Length;
            }
            else
            {
                data[offset++] = (byte)peerKeyData.Length;
            }

            peerKeyData.CopyTo(data.AsSpan(offset));

            // INS 0x87 (AUTHENTICATE), P1 = algorithm, P2 = slot
            var command = new ApduCommand(0x00, 0x87, (byte)algorithm, (byte)slot, data);
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                if (response.SW == 0x6982)
                {
                    throw new InvalidOperationException(
                        "Security status not satisfied. PIN verification may be required before this operation.");
                }

                throw ApduException.FromStatusWord(response.SW,
                    $"ECDH operation failed for slot 0x{(byte)slot:X2}");
            }

            // Parse response: TAG 0x7C [ TAG 0x82 (shared secret) ]
            return ParseCryptoResponse(response.Data);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    private byte[] PrepareDataForCrypto(PivAlgorithm algorithm, ReadOnlyMemory<byte> data)
    {
        int expectedLength = algorithm switch
        {
            PivAlgorithm.Rsa1024 => 128,
            PivAlgorithm.Rsa2048 => 256,
            PivAlgorithm.Rsa3072 => 384,
            PivAlgorithm.Rsa4096 => 512,
            PivAlgorithm.EccP256 => 32,
            PivAlgorithm.EccP384 => 48,
            PivAlgorithm.Ed25519 => 32,
            _ => data.Length // No padding needed for other algorithms
        };

        var span = data.Span;

        // RSA: Pad with leading zeros if too short, truncate if too long
        if (algorithm == PivAlgorithm.Rsa1024 || algorithm == PivAlgorithm.Rsa2048 ||
            algorithm == PivAlgorithm.Rsa3072 || algorithm == PivAlgorithm.Rsa4096)
        {
            if (span.Length == expectedLength)
            {
                return span.ToArray();
            }
            else if (span.Length < expectedLength)
            {
                var padded = new byte[expectedLength];
                span.CopyTo(padded.AsSpan(expectedLength - span.Length));
                return padded;
            }
            else
            {
                // Truncate from left
                return span.Slice(span.Length - expectedLength).ToArray();
            }
        }

        // ECC/Ed25519: Truncate or pad to expected hash length
        if (span.Length == expectedLength)
        {
            return span.ToArray();
        }
        else if (span.Length > expectedLength)
        {
            // Truncate to match key size
            return span.Slice(0, expectedLength).ToArray();
        }
        else
        {
            // Pad with zeros at the end (shouldn't normally happen)
            var padded = new byte[expectedLength];
            span.CopyTo(padded);
            return padded;
        }
    }

    private ReadOnlyMemory<byte> ParseCryptoResponse(ReadOnlyMemory<byte> data)
    {
        // Parse outer TLV (0x7C - Dynamic Auth Template)
        var outer = Tlv.Create(data.Span);
        if (outer.Tag != 0x7C)
        {
            throw new ApduException("Invalid crypto response format");
        }

        // Parse inner TLV (0x82 - Response data)
        var inner = Tlv.Create(outer.Value.Span);
        if (inner.Tag != 0x82)
        {
            throw new ApduException("Invalid crypto response - expected TAG 0x82");
        }

        return inner.Value;
    }

    private byte[] EncodePeerPublicKey(IPublicKey publicKey)
    {
        return publicKey switch
        {
            ECPublicKey ecKey => ecKey.PublicPoint.ToArray(),
            Curve25519PublicKey cv25519 => cv25519.PublicPoint.ToArray(),
            _ => throw new ArgumentException("Unsupported public key type", nameof(publicKey))
        };
    }
}