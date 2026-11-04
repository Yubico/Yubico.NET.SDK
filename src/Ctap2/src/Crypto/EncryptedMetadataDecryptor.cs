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

using System.Security.Cryptography;

namespace Yubico.YubiKit.Ctap2.Crypto;

/// <summary>
/// Decrypts optional GetInfo device-identifier and credential-store-state fields using
/// keys derived from a persistent PIN/UV auth token (PPUAT).
/// </summary>
/// <remarks>
/// <para>
/// CTAP 2.3 §6.4 defines encIdentifier as a 128-bit device identifier and encCredStoreState as
/// an opaque 128-bit credential-store state. Each output is a fresh 16-byte IV followed by
/// 16 bytes of AES-128-CBC ciphertext. They are not per-credential identifiers. Use the matching
/// persistent token, not an arbitrary ordinary PIN/UV token. These offline helpers do not check
/// device capabilities, token permissions or the standard field lengths; they accept generic
/// block-aligned ciphertext, including IV-only input.
/// </para>
/// <para>
/// CBC without padding is not authenticated encryption: a wrong token can return plausible bytes.
/// A null result is not reliable wrong-token detection. Inputs are borrowed for the call; returned
/// plaintext and derived keys are caller-owned. Clear those arrays with
/// CryptographicOperations.ZeroMemory when finished, including after later processing fails.
/// Clearing the helper's stack key does not guarantee cleanup of all provider or failure-output copies.
/// </para>
/// <para>
/// HKDF parameters:
/// <list type="bullet">
///   <item><description>Algorithm: SHA-256</description></item>
///   <item><description>Secret: PPUAT (PIN/UV Auth Token)</description></item>
///   <item><description>Salt: 32 zero bytes</description></item>
///   <item><description>Info: "encIdentifier" or "encCredStoreState"</description></item>
///   <item><description>Output length: 16 bytes (AES-128 key)</description></item>
/// </list>
/// </para>
/// </remarks>
public static class EncryptedMetadataDecryptor
{
    private static readonly byte[] ZeroSalt = new byte[32];

    /// <summary>
    /// Decrypts the encIdentifier field using PPUAT-derived key.
    /// </summary>
    /// <param name="ppuat">The Persistent PIN/UV Auth Token.</param>
    /// <param name="encIdentifier">Borrowed IV-prefixed encIdentifier bytes from GetInfo.</param>
    /// <returns>Caller-owned plaintext, or null for empty/short input or a caught cryptographic failure.</returns>
    /// <remarks>
    /// <para>
    /// The plaintext is a device identifier, not a credential ID. Randomized encryption changes the
    /// ciphertext on each GetInfo output; it does not make the decrypted identifier session-specific.
    /// No authenticity or correct-token check is performed. Clear the returned plaintext when done.
    /// </para>
    /// </remarks>
    public static byte[]? DecryptIdentifier(ReadOnlySpan<byte> ppuat, ReadOnlySpan<byte> encIdentifier)
    {
        return DecryptWithInfo(ppuat, encIdentifier, "encIdentifier"u8);
    }

    /// <summary>
    /// Decrypts the encCredStoreState field using PPUAT-derived key.
    /// </summary>
    /// <param name="ppuat">The Persistent PIN/UV Auth Token.</param>
    /// <param name="encCredStoreState">Borrowed IV-prefixed encCredStoreState bytes from GetInfo.</param>
    /// <returns>Caller-owned plaintext, or null for empty/short input or a caught cryptographic failure.</returns>
    /// <remarks>
    /// <para>
    /// The plaintext describes opaque credential-store state, not an individual credential's metadata.
    /// No integer or status interpretation, authenticity check or correct-token check is performed.
    /// Clear the returned plaintext when done.
    /// </para>
    /// </remarks>
    public static byte[]? DecryptCredStoreState(ReadOnlySpan<byte> ppuat, ReadOnlySpan<byte> encCredStoreState)
    {
        return DecryptWithInfo(ppuat, encCredStoreState, "encCredStoreState"u8);
    }

    /// <summary>
    /// Derives an AES-128 key from PPUAT using HKDF-SHA256.
    /// </summary>
    /// <param name="ppuat">The Persistent PIN/UV Auth Token.</param>
    /// <param name="info">The context info string (e.g., "encIdentifier").</param>
    /// <returns>A caller-owned 16-byte secret AES key; clear it when finished, including on later failure.</returns>
    /// <remarks>
    /// This can be used for custom decryption scenarios. For standard 
    /// encIdentifier/encCredStoreState, prefer the specific decrypt methods.
    /// </remarks>
    public static byte[] DeriveKey(ReadOnlySpan<byte> ppuat, ReadOnlySpan<byte> info)
    {
        if (ppuat.IsEmpty)
        {
            throw new ArgumentException("PPUAT cannot be empty.", nameof(ppuat));
        }

        var key = new byte[16];
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ppuat,
            key,
            salt: ZeroSalt,
            info: info);

        return key;
    }

    private static byte[]? DecryptWithInfo(
        ReadOnlySpan<byte> ppuat,
        ReadOnlySpan<byte> encryptedData,
        ReadOnlySpan<byte> info)
    {
        if (ppuat.IsEmpty || encryptedData.IsEmpty)
        {
            return null;
        }

        // Encrypted data format: [IV (16 bytes) | ciphertext]
        // This generic helper accepts IV-only input; standard GetInfo fields also have 16 ciphertext bytes.
        if (encryptedData.Length < 16)
        {
            return null;
        }

        // Extract IV from first 16 bytes
        ReadOnlySpan<byte> iv = encryptedData[..16];
        ReadOnlySpan<byte> ciphertext = encryptedData[16..];

        // Derive AES-128 key using HKDF-SHA256
        Span<byte> key = stackalloc byte[16];
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ppuat,
            key,
            salt: ZeroSalt,
            info: info);

        try
        {
            // AES-128-CBC without padding provides no authentication or wrong-token detection.
            using var aes = Aes.Create();
            aes.SetKey(key);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var plaintext = new byte[ciphertext.Length];
            aes.DecryptCbc(ciphertext, iv, plaintext, PaddingMode.None);

            return plaintext;
        }
        catch (CryptographicException)
        {
            return null;
        }
        finally
        {
            // Clear the key from stack
            CryptographicOperations.ZeroMemory(key);
        }
    }
}