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

using System.Buffers;
using Microsoft.Extensions.Logging;
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
        if (FirmwareVersion < PivFeatures.Metadata.Version)
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

        return await SignOrDecryptCoreAsync(slot, slotMetadata.Algorithm, data, cancellationToken).ConfigureAwait(false);;
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

        // Prepare data according to algorithm key size
        var preparedData = PrepareDataForCrypto(algorithm, data);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x81 (challenge) ]
        var dataList = new List<byte>();
        
        // TAG 0x7C (Dynamic Auth Template)
        var templateStart = dataList.Count;
        dataList.Add(0x7C);
        dataList.Add(0x00); // Length placeholder

        // TAG 0x82 (Expected response length - empty for "give me everything")
        dataList.Add(0x82);
        dataList.Add(0x00);

        // TAG 0x81 (Challenge/data to sign/decrypt)
        dataList.Add(0x81);
        if (preparedData.Length > 255)
        {
            // Two-byte length encoding: 0x82 followed by 2 length bytes (big-endian)
            dataList.Add(0x82);
            dataList.Add((byte)(preparedData.Length >> 8));
            dataList.Add((byte)(preparedData.Length & 0xFF));
        }
        else if (preparedData.Length > 127)
        {
            // One-byte length encoding: 0x81 followed by 1 length byte
            dataList.Add(0x81);
            dataList.Add((byte)preparedData.Length);
        }
        else
        {
            dataList.Add((byte)preparedData.Length);
        }
        dataList.AddRange(preparedData);

        // Update template length
        int templateLength = dataList.Count - templateStart - 2;
        if (templateLength > 255)
        {
            // Two-byte length encoding for template
            dataList.Insert(templateStart + 2, (byte)(templateLength & 0xFF));
            dataList.Insert(templateStart + 2, (byte)(templateLength >> 8));
            dataList[templateStart + 1] = 0x82;
        }
        else if (templateLength > 127)
        {
            // One-byte length encoding for template
            dataList.Insert(templateStart + 2, (byte)templateLength);
            dataList[templateStart + 1] = 0x81;
        }
        else
        {
            dataList[templateStart + 1] = (byte)templateLength;
        }

        // INS 0x87 (AUTHENTICATE), P1 = algorithm, P2 = slot
        var command = new ApduCommand(0x00, 0x87, (byte)algorithm, (byte)slot, dataList.ToArray());
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
        var dataList = new List<byte>();
        
        // TAG 0x7C (Dynamic Auth Template)
        var templateStart = dataList.Count;
        dataList.Add(0x7C);
        dataList.Add(0x00); // Length placeholder

        // TAG 0x82 (Expected response length - empty)
        dataList.Add(0x82);
        dataList.Add(0x00);

        // TAG 0x85 (Exponentiation data - peer public key)
        dataList.Add(0x85);
        if (peerKeyData.Length > 127)
        {
            dataList.Add(0x81);
            dataList.Add((byte)peerKeyData.Length);
        }
        else
        {
            dataList.Add((byte)peerKeyData.Length);
        }
        dataList.AddRange(peerKeyData);

        // Update template length
        int templateLength = dataList.Count - templateStart - 2;
        if (templateLength > 127)
        {
            dataList.Insert(templateStart + 2, (byte)templateLength);
            dataList[templateStart + 1] = 0x81;
        }
        else
        {
            dataList[templateStart + 1] = (byte)templateLength;
        }

        // INS 0x87 (AUTHENTICATE), P1 = algorithm, P2 = slot
        var command = new ApduCommand(0x00, 0x87, (byte)algorithm, (byte)slot, dataList.ToArray());
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
