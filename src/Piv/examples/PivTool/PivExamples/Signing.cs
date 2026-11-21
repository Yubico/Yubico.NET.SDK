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

using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV signing operations using the YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples of signing data with private keys stored in PIV slots.
/// Keys must be generated or imported into the slot before signing.
/// </para>
/// <para>
/// PIN verification may be required depending on the slot's PIN policy.
/// Touch may be required depending on the slot's touch policy.
/// This helper uses PKCS#1 v1.5 for RSA and passes the original message to Ed25519.
/// X25519 supports agreement, not signing.
/// </para>
/// </remarks>
public static class Signing
{
    /// <summary>
    /// Signs data using the private key in a PIV slot.
    /// </summary>
    /// <param name="session">A metadata-capable PIV session with PIN already verified if the key policy requires it.</param>
    /// <param name="slot">The slot containing the signing key.</param>
    /// <param name="dataToSign">The original message.</param>
    /// <param name="hashAlgorithm">SHA256, SHA384, or SHA512 (default: SHA256 for RSA and ECDSA).
    /// For Ed25519, pass the default value; an explicit hash is not allowed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing signature or error information.</returns>
    /// <example>
    /// <code>
    /// var options = new SessionCreationOptions { UserPresencePrompt = userPresencePrompt };
    /// // Requires an application-owned prompt, caller-owned PIN/data and a provisioned P-256/P-384 slot.
    /// await using var session = await device.CreatePivSessionAsync(options, ct);
    /// await session.VerifyPinAsync(pin, ct);
    /// 
    /// var result = await Signing.SignDataAsync(
    ///     session, 
    ///     Slot.Signature,
    ///     dataBytes,
    ///     HashAlgorithmName.SHA256,
    ///     ct);
    /// 
    /// if (result.Success)
    /// {
    ///     var signature = result.Signature;
    /// }
    /// </code>
    /// </example>
    public static async Task<SigningResult> SignDataAsync(
        IPivSession session,
        Slot slot,
        ReadOnlyMemory<byte> dataToSign,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            // Check if slot has a key
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return SigningResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Sign using explicit algorithm from the already-fetched metadata.
            var keyType = metadata.Value.KeyType ?? throw new NotSupportedException(
                $"Unknown PIV algorithm 0x{metadata.Value.RawAlgorithmId:X2}.");
            var input = PrepareSigningInput(keyType, dataToSign, hashAlgorithm);
            var signature = await session.SignOrDecryptAsync(slot, keyType, input, cancellationToken);

            stopwatch.Stop();
            return SigningResult.Succeeded(signature, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            return SigningResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
        }
        catch (Exception ex)
        {
            return SigningResult.Failed($"Signing failed: {ex.Message}");
        }
    }

    internal static ReadOnlyMemory<byte> PrepareSigningInput(
        KeyType keyType, ReadOnlyMemory<byte> message, HashAlgorithmName hashAlgorithm)
    {
        if (keyType == KeyType.X25519)
            throw new NotSupportedException("X25519 is a key-agreement algorithm and cannot sign.");
        if (keyType == KeyType.Ed25519)
        {
            if (hashAlgorithm.Name is not null)
                throw new ArgumentException("Do not select a hash for Ed25519; it signs the original message.", nameof(hashAlgorithm));
            return message;
        }

        if (hashAlgorithm.Name is null)
            hashAlgorithm = HashAlgorithmName.SHA256;
        byte[] digest = hashAlgorithm.Name switch
        {
            "SHA256" => SHA256.HashData(message.Span),
            "SHA384" => SHA384.HashData(message.Span),
            "SHA512" => SHA512.HashData(message.Span),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm.Name}", nameof(hashAlgorithm))
        };
        return keyType is KeyType.Rsa1024 or KeyType.Rsa2048 or KeyType.Rsa3072 or KeyType.Rsa4096
            ? YubiKeyRsaSignatureGenerator.EncodeSignatureInput(digest, hashAlgorithm, keyType.GetKeySizeBytes())
            : digest;
    }
}
