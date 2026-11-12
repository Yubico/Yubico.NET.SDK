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
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Cryptography;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class PrivateKeyOperations
{
    private const byte Cla = 0x00;
    private const byte InsAuthenticate = 0x87;
    private const byte TagDynamicAuth = 0x7C;
    private const byte TagResponse = 0x82;
    private const byte TagChallenge = 0x81;
    private const byte TagExponentiation = 0x85;

    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(PrivateKeyOperations));

    /// <summary>
    /// Signs or decrypts data using the private key in the specified slot.
    /// </summary>
    /// <param name="slot">The slot containing the private key.</param>
    /// <param name="keyType">The algorithm of the key.</param>
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
    internal static Task<byte[]> SignOrDecryptAsync(
        IBackend backend,
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken = default) =>
        SignOrDecryptCoreAsync(
            backend,

            slot,
            keyType,
            data,
            userPresenceNotification,
            cancellationToken);

    private static async Task<byte[]> SignOrDecryptCoreAsync(
        IBackend backend,
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("PIV: Signing/decrypting with slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, keyType);

        // Size the TAG 81 payload for the algorithm; the bytes are written straight into the APDU buffer below.
        int preparedLength = PreparedDataLength(keyType, data.Length);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x81 (challenge) ]
        // Pre-compute lengths to avoid List<byte> resizing (which can leave unzeroed copies)

        // Inner content: TAG 0x82 (2 bytes) + TAG 0x81 + length encoding + data
        int dataLenEncodingSize = BerLength.EncodingSize(preparedLength);
        int innerLength = 2 + 1 + dataLenEncodingSize + preparedLength; // 0x82,0x00 + 0x81 + len + data

        // Template: TAG 0x7C + length encoding + inner content
        int templateLenEncodingSize = BerLength.EncodingSize(innerLength);
        int totalLength = 1 + templateLenEncodingSize + innerLength; // 0x7C + template len + inner

        var commandData = new byte[totalLength];
        try
        {
            int offset = 0;

            commandData[offset++] = TagDynamicAuth;

            // Template length encoding
            offset += BerLength.Write(commandData.AsSpan(offset), innerLength);

            commandData[offset++] = TagResponse;
            commandData[offset++] = 0;

            commandData[offset++] = TagChallenge;
            offset += BerLength.Write(commandData.AsSpan(offset), preparedLength);

            WritePreparedData(keyType, data.Span, commandData.AsSpan(offset, preparedLength));

            var command = new ApduCommand(Cla, InsAuthenticate, KeyTypeEncoding.EncodeAsymmetric(keyType), (byte)slot, commandData);
            await userPresenceNotification.RequestAsync(cancellationToken).ConfigureAwait(false);

            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            byte[]? result = null;
            try
            {
                if (!response.IsOK())
                {
                    // Check if PIN verification is required
                    if (response.SW == SWConstants.SecurityStatusNotSatisfied)
                    {
                        throw new InvalidOperationException(
                            "Security status not satisfied. PIN verification may be required before this operation.");
                    }

                    throw ApduException.FromStatusWord(response.SW,
                        $"Sign/decrypt operation failed for slot 0x{(byte)slot:X2}");
                }

                // Parse response: TAG 0x7C [ TAG 0x82 (response data) ]
                result = ParseCryptoResponse(response.Data);
                byte[] transferredResult = await ResolveAndTransferCryptoResultAsync(
                        result,
                        userPresenceNotification)
                    .ConfigureAwait(false);
                result = null;
                return transferredResult;
            }
            finally
            {
                if (result is not null)
                {
                    CryptographicOperations.ZeroMemory(result);
                }

                response.Clear();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(commandData);
        }
    }

    /// <inheritdoc/>
    internal static async Task<byte[]> DecryptAsync(
        IBackend backend,
        SlotMetadata? metadata,
        Slot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(padding);
        Logger.LogDebug("PIV: DecryptAsync slot 0x{Slot:X2}", (byte)slot);

        if (metadata is null || !metadata.Value.RequireKeyType().IsRsa())
        {
            throw new ArgumentException(
                $"Slot 0x{(byte)slot:X2} does not contain an RSA key.", nameof(slot));
        }

        var keyType = metadata.Value.RequireKeyType();
        int keyBits = keyType.GetKeySizeBits();

        if (cipherText.Length != keyBits / 8)
        {
            throw new ArgumentException(
                $"Cipher text length {cipherText.Length} does not match RSA-{keyBits} key size ({keyBits / 8} bytes).",
                nameof(cipherText));
        }

        // Perform the raw RSA private key operation on the YubiKey
        var rawDecrypted = await SignOrDecryptCoreAsync(
                backend,

                slot,
                keyType,
                cipherText,
                userPresenceNotification,
                cancellationToken)
            .ConfigureAwait(false);

        // Strip padding using a dummy RSA key — same technique as Python yubikey-manager's _unpad_message.
        // We generate a temporary RSA key of the same size, use textbook RSA (encrypt with public key)
        // to re-wrap the raw bytes, then decrypt with the dummy private key and real padding params.
        // This lets .NET's crypto library handle PKCS#1 v1.5 and OAEP unpadding correctly.
        byte[]? reEncrypted = null;
        try
        {
            // Cleanup already owns the raw card output before local RSA setup can fail.
            using var dummy = RSA.Create(keyBits);
            var publicParams = dummy.ExportParameters(false);
            var n = publicParams.Modulus ?? throw new CryptographicException("RSA key export returned null modulus.");
            var e = publicParams.Exponent ?? throw new CryptographicException("RSA key export returned null exponent.");
            reEncrypted = ModPow(rawDecrypted, e, n, keyBits / 8);
            return dummy.Decrypt(reEncrypted, padding);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "Padding removal failed. The cipher text may be corrupt or encrypted with a different padding scheme.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawDecrypted);
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

        // Re-wrapping requires b < mod to preserve the original integer through exponentiation.
        // Valid full-width PKCS#1 v1.5 and OAEP encryption blocks begin with 00 and fit below a
        // same-width, full-bit-length dummy modulus. Reject raw output at or above the modulus
        // before modular reduction could change its value; host unpadding validates the encoding afterward.
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
                return raw.AsSpan().ToArray();
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
    /// <returns>Raw shared-secret bytes: the x-coordinate for NIST ECDH, or the 32-byte X25519 result.</returns>
    internal static async Task<byte[]> CalculateSecretAsync(
        IBackend backend,
        Slot slot,
        IPublicKey peerPublicKey,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Calculating shared secret with slot 0x{Slot:X2}", (byte)slot);

        // Determine algorithm from public key
        var keyType = peerPublicKey switch
        {
            ECPublicKey ecKey when ecKey.KeyType is KeyType.P256 or KeyType.P384 => ecKey.KeyType,
            Curve25519PublicKey curve25519Key when curve25519Key.KeyType == KeyType.X25519 => curve25519Key.KeyType,
            _ => throw new ArgumentException("Unsupported public key type for ECDH", nameof(peerPublicKey))
        };

        // Encode peer public key
        var peerKeyData = EncodePeerPublicKey(peerPublicKey);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x85 (exponentiation) ]
        // Pre-compute lengths to avoid List<byte> resizing (which can leave unzeroed copies)

        // Inner content: TAG 0x82 (2 bytes) + TAG 0x85 + length encoding + data
        int keyLenEncodingSize = BerLength.EncodingSize(peerKeyData.Length);
        int innerLength = 2 + 1 + keyLenEncodingSize + peerKeyData.Length; // 0x82,0x00 + 0x85 + len + data

        // Template: TAG 0x7C + length encoding + inner content
        int templateLenEncodingSize = BerLength.EncodingSize(innerLength);
        int totalLength = 1 + templateLenEncodingSize + innerLength; // 0x7C + template len + inner

        var data = new byte[totalLength];
        try
        {
            int offset = 0;

            data[offset++] = TagDynamicAuth;

            // Template length encoding
            offset += BerLength.Write(data.AsSpan(offset), innerLength);

            data[offset++] = TagResponse;
            data[offset++] = 0;

            data[offset++] = TagExponentiation;
            offset += BerLength.Write(data.AsSpan(offset), peerKeyData.Length);

            peerKeyData.CopyTo(data.AsSpan(offset));

            var command = new ApduCommand(Cla, InsAuthenticate, KeyTypeEncoding.EncodeAsymmetric(keyType), (byte)slot, data);
            await userPresenceNotification.RequestAsync(cancellationToken).ConfigureAwait(false);

            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            byte[]? result = null;
            try
            {
                if (!response.IsOK())
                {
                    if (response.SW == SWConstants.SecurityStatusNotSatisfied)
                    {
                        throw new InvalidOperationException(
                            "Security status not satisfied. PIN verification may be required before this operation.");
                    }

                    throw ApduException.FromStatusWord(response.SW,
                        $"ECDH operation failed for slot 0x{(byte)slot:X2}");
                }

                // Parse response: TAG 0x7C [ TAG 0x82 (shared secret) ]
                result = ParseCryptoResponse(response.Data);
                byte[] transferredResult = await ResolveAndTransferCryptoResultAsync(
                        result,
                        userPresenceNotification)
                    .ConfigureAwait(false);
                result = null;
                return transferredResult;
            }
            finally
            {
                if (result is not null)
                {
                    CryptographicOperations.ZeroMemory(result);
                }

                response.Clear();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <summary>
    ///     Number of bytes TAG 81 carries for <paramref name="keyType" /> when the caller supplies
    ///     <paramref name="dataLength" /> bytes. Pairs with <see cref="WritePreparedData" />.
    /// </summary>
    private static int PreparedDataLength(KeyType keyType, int dataLength) =>
        // RFC 8032: Ed25519 signs the message, not a locally padded/prehashed digest.
        keyType.IsRsa() || keyType.IsShortWeierstrass() ? keyType.GetKeySizeBytes() : dataLength;

    /// <summary>
    ///     Writes the TAG 81 payload for <paramref name="keyType" /> into <paramref name="destination" />,
    ///     whose length is <see cref="PreparedDataLength" />. Throws before writing anything when the input
    ///     cannot be represented without changing it.
    /// </summary>
    /// <exception cref="ArgumentException">An RSA block longer than the modulus.</exception>
    private static void WritePreparedData(KeyType keyType, ReadOnlySpan<byte> data, Span<byte> destination)
    {
        if (keyType.IsRsa())
        {
            // A short encoded RSA block is the same integer left-padded. A longer one cannot be
            // represented faithfully: reject it rather than silently truncating caller input.
            if (data.Length > destination.Length)
                throw new ArgumentException($"Data exceeds the {keyType} encoded block size ({destination.Length} bytes).", nameof(data));
            LeftPad(data, destination);
            return;
        }
        if (keyType.IsShortWeierstrass())
        {
            // FIPS 186-5 §6.4.1 uses the leftmost min(N, outlen) bits of the digest.
            if (data.Length >= destination.Length)
                data[..destination.Length].CopyTo(destination);
            else
                LeftPad(data, destination);
            return;
        }
        data.CopyTo(destination);
    }

    private static void LeftPad(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        destination[..^data.Length].Clear();
        data.CopyTo(destination[^data.Length..]);
    }

    private static byte[] ParseCryptoResponse(ReadOnlyMemory<byte> data)
    {
        // Parse outer TLV (0x7C - Dynamic Auth Template)
        using var outer = Tlv.Create(data.Span);
        if (outer.Tag != TagDynamicAuth)
        {
            throw new ApduException("Invalid crypto response format");
        }

        // Parse inner TLV (0x82 - Response data)
        using var inner = Tlv.Create(outer.Value.Span);
        if (inner.Tag != TagResponse)
        {
            throw new ApduException("Invalid crypto response - expected TAG 0x82");
        }

        // Copy the value before the Tlv objects are disposed. The caller owns this buffer until transfer.
        return inner.Value.ToArray();
    }

    /// <summary>Resolves successful user presence before transferring an owned cryptographic result.</summary>
    internal static async Task<byte[]> ResolveAndTransferCryptoResultAsync(
        byte[] result,
        UserPresenceNotification userPresenceNotification)
    {
        try
        {
            await userPresenceNotification.ResolveAsync(UserPresenceOutcome.Completed).ConfigureAwait(false);
            return result;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(result);
            throw;
        }
    }

    private static byte[] EncodePeerPublicKey(IPublicKey publicKey)
    {
        return publicKey switch
        {
            ECPublicKey ecKey => ecKey.PublicPoint.ToArray(),
            Curve25519PublicKey cv25519 => cv25519.PublicPoint.ToArray(),
            _ => throw new ArgumentException("Unsupported public key type", nameof(publicKey))
        };
    }
}
