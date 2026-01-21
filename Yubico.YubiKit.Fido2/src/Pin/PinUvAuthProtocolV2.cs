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

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN/UV authentication protocol version 2 implementation.
/// </summary>
/// <remarks>
/// <para>
/// V2 uses:
/// <list type="bullet">
///   <item><description>ECDH P-256 for key agreement</description></item>
///   <item><description>HKDF-SHA-256 to derive separate HMAC and AES keys (64 bytes total)</description></item>
///   <item><description>AES-256-CBC with random IV for encryption</description></item>
///   <item><description>HMAC-SHA-256 for authentication (full 32-byte output)</description></item>
/// </list>
/// </para>
/// <para>
/// The shared secret structure for V2:
/// <code>
/// sharedSecret[0..31]  = HMAC key (32 bytes)
/// sharedSecret[32..63] = AES key (32 bytes)
/// </code>
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#pinProto2
/// </para>
/// </remarks>
public sealed class PinUvAuthProtocolV2 : IPinUvAuthProtocol
{
    private const int AesKeyLength = 32;
    private const int AesBlockSize = 16;
    private const int HmacKeyLength = 32;
    private const int SharedSecretLength = HmacKeyLength + AesKeyLength;
    
    // HKDF info strings as per CTAP2.1 spec
    private static ReadOnlySpan<byte> HkdfInfoHmacKey => "CTAP2 HMAC key"u8;
    private static ReadOnlySpan<byte> HkdfInfoAesKey => "CTAP2 AES key"u8;
    
    // COSE key parameter labels
    private const int CoseKeyType = 1;
    private const int CoseAlgorithm = 3;
    private const int CoseEC2Curve = -1;
    private const int CoseEC2X = -2;
    private const int CoseEC2Y = -3;
    
    // COSE values
    private const int CoseKeyTypeEC2 = 2;
    private const int CoseAlgEcdhEsHkdf256 = -25;
    private const int CoseEC2CurveP256 = 1;
    
    private bool _disposed;
    
    /// <inheritdoc />
    public int Version => 2;
    
    /// <inheritdoc />
    public int AuthenticationTagLength => 32;
    
    /// <inheritdoc />
    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(peerCoseKey);
        
        // Validate and extract peer's public key coordinates
        if (!peerCoseKey.TryGetValue(CoseEC2X, out var xObj) || xObj is not byte[] peerX)
        {
            throw new ArgumentException("Peer COSE key missing or invalid X coordinate (-2).", nameof(peerCoseKey));
        }
        
        if (!peerCoseKey.TryGetValue(CoseEC2Y, out var yObj) || yObj is not byte[] peerY)
        {
            throw new ArgumentException("Peer COSE key missing or invalid Y coordinate (-3).", nameof(peerCoseKey));
        }
        
        // Validate coordinate lengths (P-256 uses 32-byte coordinates)
        if (peerX.Length != 32)
        {
            throw new ArgumentException($"Invalid X coordinate length: expected 32, got {peerX.Length}.", nameof(peerCoseKey));
        }
        
        if (peerY.Length != 32)
        {
            throw new ArgumentException($"Invalid Y coordinate length: expected 32, got {peerY.Length}.", nameof(peerCoseKey));
        }
        
        // Generate ephemeral ECDH key pair
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ourParams = ecdh.ExportParameters(includePrivateParameters: false);
        
        // Import peer's public key
        var peerParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = peerX, Y = peerY }
        };
        
        using var peerEcdh = ECDiffieHellman.Create(peerParams);
        
        // Derive raw shared secret (X coordinate of ECDH shared point)
        byte[] z;
        try
        {
            z = ecdh.DeriveRawSecretAgreement(peerEcdh.PublicKey);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Failed to derive shared secret. Peer key may be invalid.", nameof(peerCoseKey), ex);
        }
        
        // Apply KDF to get shared secret
        var sharedSecret = Kdf(z);
        
        // Clear raw shared secret
        CryptographicOperations.ZeroMemory(z);
        
        // Build our COSE key for key agreement
        var keyAgreement = new Dictionary<int, object?>
        {
            { CoseKeyType, CoseKeyTypeEC2 },
            { CoseAlgorithm, CoseAlgEcdhEsHkdf256 },
            { CoseEC2Curve, CoseEC2CurveP256 },
            { CoseEC2X, ourParams.Q.X },
            { CoseEC2Y, ourParams.Q.Y }
        };
        
        return (keyAgreement, sharedSecret);
    }
    
    /// <inheritdoc />
    public byte[] Kdf(ReadOnlySpan<byte> z)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (z.IsEmpty)
        {
            throw new ArgumentException("Shared secret cannot be empty.", nameof(z));
        }
        
        // Derive HMAC key and AES key separately using HKDF
        var sharedSecret = new byte[SharedSecretLength];
        
        try
        {
            // Derive HMAC key: HKDF(z, salt=empty, info="CTAP2 HMAC key")
            HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                z,
                sharedSecret.AsSpan(0, HmacKeyLength),
                salt: [],
                info: HkdfInfoHmacKey);
            
            // Derive AES key: HKDF(z, salt=empty, info="CTAP2 AES key")
            HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                z,
                sharedSecret.AsSpan(HmacKeyLength, AesKeyLength),
                salt: [],
                info: HkdfInfoAesKey);
            
            return sharedSecret;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
            throw;
        }
    }
    
    /// <inheritdoc />
    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be {SharedSecretLength} bytes (HMAC key + AES key).", nameof(key));
        }
        
        if (plaintext.Length == 0 || plaintext.Length % AesBlockSize != 0)
        {
            throw new ArgumentException(
                $"Plaintext must be a non-empty multiple of {AesBlockSize} bytes.", nameof(plaintext));
        }
        
        // Extract AES key from shared secret
        var aesKey = key.Slice(HmacKeyLength, AesKeyLength);
        
        using var aes = Aes.Create();
        aes.Key = aesKey.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        // Generate random IV
        var iv = RandomNumberGenerator.GetBytes(AesBlockSize);
        aes.IV = iv;
        
        // Encrypt
        var ciphertext = new byte[plaintext.Length];
        
        using (var encryptor = aes.CreateEncryptor())
        {
            var inputArray = plaintext.ToArray();
            encryptor.TransformBlock(inputArray, 0, inputArray.Length, ciphertext, 0);
            CryptographicOperations.ZeroMemory(inputArray);
        }
        
        // Return IV || ciphertext
        var result = new byte[AesBlockSize + ciphertext.Length];
        iv.CopyTo(result, 0);
        ciphertext.CopyTo(result, AesBlockSize);
        
        return result;
    }
    
    /// <inheritdoc />
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be {SharedSecretLength} bytes (HMAC key + AES key).", nameof(key));
        }
        
        // V2 ciphertext format: IV (16 bytes) || encrypted data
        if (ciphertext.Length < AesBlockSize * 2 || ciphertext.Length % AesBlockSize != 0)
        {
            throw new ArgumentException(
                $"Ciphertext must be at least {AesBlockSize * 2} bytes and a multiple of {AesBlockSize}.", 
                nameof(ciphertext));
        }
        
        // Extract AES key from shared secret
        var aesKey = key.Slice(HmacKeyLength, AesKeyLength);
        var iv = ciphertext[..AesBlockSize];
        var encrypted = ciphertext[AesBlockSize..];
        
        using var aes = Aes.Create();
        aes.Key = aesKey.ToArray();
        aes.IV = iv.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        var plaintext = new byte[encrypted.Length];
        
        using (var decryptor = aes.CreateDecryptor())
        {
            var inputArray = encrypted.ToArray();
            decryptor.TransformBlock(inputArray, 0, inputArray.Length, plaintext, 0);
            CryptographicOperations.ZeroMemory(inputArray);
        }
        
        return plaintext;
    }
    
    /// <inheritdoc />
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Key can be either:
        // - 32-byte PIN token (used for pinUvAuthParam computation)
        // - 64-byte shared secret (HMAC key + AES key)
        // In both cases, we use the first 32 bytes as the HMAC key
        if (key.Length != HmacKeyLength && key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be {HmacKeyLength} bytes (PIN token) or {SharedSecretLength} bytes (shared secret).", nameof(key));
        }
        
        // Use first 32 bytes as HMAC key (works for both token and shared secret)
        var hmacKey = key[..HmacKeyLength];
        
        // Compute HMAC-SHA-256 (full 32 bytes for V2)
        var hash = new byte[AuthenticationTagLength];
        HMACSHA256.HashData(hmacKey, message, hash);
        
        return hash;
    }
    
    /// <inheritdoc />
    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (signature.Length != AuthenticationTagLength)
        {
            return false;
        }
        
        var expected = Authenticate(key, message);
        return CryptographicOperations.FixedTimeEquals(expected, signature);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
