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

namespace Yubico.YubiKit.Fido2.Crypto;

/// <summary>
/// Decrypts encrypted credential metadata (encIdentifier, encCredStoreState) 
/// using PPUAT-derived keys. Requires YubiKey 5.7+.
/// </summary>
/// <remarks>
/// <para>
/// YubiKey firmware 5.7+ can return encrypted metadata in credential management
/// operations. These fields can be decrypted by the client using a key derived
/// from the PPUAT (Persistent PIN/UV Auth Token) via HKDF.
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
    /// <param name="encIdentifier">The encrypted identifier bytes.</param>
    /// <returns>The decrypted identifier, or null if decryption fails.</returns>
    /// <remarks>
    /// <para>
    /// The encIdentifier is a unique identifier for the credential that is
    /// encrypted to prevent tracking across sessions. Available on YubiKey 5.7+.
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
    /// <param name="encCredStoreState">The encrypted credential store state bytes.</param>
    /// <returns>The decrypted state, or null if decryption fails.</returns>
    /// <remarks>
    /// <para>
    /// The encCredStoreState contains metadata about the credential's storage
    /// state, encrypted to prevent tracking. Available on YubiKey 5.8+.
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
    /// <returns>The derived 16-byte AES key.</returns>
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
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> info)
    {
        if (ppuat.IsEmpty || ciphertext.IsEmpty)
        {
            return null;
        }
        
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
            // Decrypt using AES-128-ECB (YubiKey uses ECB for these small values)
            using var aes = Aes.Create();
            aes.SetKey(key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            
            var plaintext = new byte[ciphertext.Length];
            aes.DecryptEcb(ciphertext, plaintext, PaddingMode.None);
            
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
