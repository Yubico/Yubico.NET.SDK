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
/// PIN/UV authentication protocol version 1 implementation (legacy).
/// </summary>
/// <remarks>
/// <para>
/// V1 uses:
/// <list type="bullet">
///   <item><description>ECDH P-256 for key agreement</description></item>
///   <item><description>SHA-256 of raw ECDH output as shared secret (32 bytes)</description></item>
///   <item><description>AES-256-CBC with zero IV for encryption</description></item>
///   <item><description>HMAC-SHA-256 for authentication (truncated to 16 bytes)</description></item>
/// </list>
/// </para>
/// <para>
/// V1 is considered legacy and has security limitations compared to V2.
/// New implementations should prefer V2 when supported by the authenticator.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#pinProto1
/// </para>
/// </remarks>
public sealed class PinUvAuthProtocolV1 : IPinUvAuthProtocol
{
    private const int AesKeyLength = 32;
    private const int AesBlockSize = 16;
    private const int SharedSecretLength = 32;
    private const int V1AuthTagLength = 16;
    
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
    public int Version => 1;
    
    /// <inheritdoc />
    public int AuthenticationTagLength => V1AuthTagLength;
    
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
        
        // V1 KDF: SHA-256(z)
        var sharedSecret = new byte[SharedSecretLength];
        SHA256.HashData(z, sharedSecret);
        return sharedSecret;
    }
    
    /// <inheritdoc />
    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be {SharedSecretLength} bytes.", nameof(key));
        }
        
        if (plaintext.Length == 0 || plaintext.Length % AesBlockSize != 0)
        {
            throw new ArgumentException(
                $"Plaintext must be a non-empty multiple of {AesBlockSize} bytes.", nameof(plaintext));
        }
        
        using var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        // V1 uses zero IV
        aes.IV = new byte[AesBlockSize];
        
        // Encrypt
        var ciphertext = new byte[plaintext.Length];
        
        using (var encryptor = aes.CreateEncryptor())
        {
            var inputArray = plaintext.ToArray();
            encryptor.TransformBlock(inputArray, 0, inputArray.Length, ciphertext, 0);
            CryptographicOperations.ZeroMemory(inputArray);
        }
        
        // V1 returns ciphertext only (no IV prefix)
        return ciphertext;
    }
    
    /// <inheritdoc />
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be {SharedSecretLength} bytes.", nameof(key));
        }
        
        // V1 ciphertext format: encrypted data only (no IV prefix)
        if (ciphertext.Length == 0 || ciphertext.Length % AesBlockSize != 0)
        {
            throw new ArgumentException(
                $"Ciphertext must be a non-empty multiple of {AesBlockSize}.", 
                nameof(ciphertext));
        }
        
        using var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        // V1 uses zero IV
        aes.IV = new byte[AesBlockSize];
        
        var plaintext = new byte[ciphertext.Length];
        
        using (var decryptor = aes.CreateDecryptor())
        {
            var inputArray = ciphertext.ToArray();
            decryptor.TransformBlock(inputArray, 0, inputArray.Length, plaintext, 0);
            CryptographicOperations.ZeroMemory(inputArray);
        }
        
        return plaintext;
    }
    
    /// <inheritdoc />
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // V1 PIN token can be 16 or 32 bytes
        if (key.Length != 16 && key.Length != SharedSecretLength)
        {
            throw new ArgumentException(
                $"Key must be 16 or {SharedSecretLength} bytes.", nameof(key));
        }
        
        // Compute HMAC-SHA-256
        Span<byte> fullHash = stackalloc byte[32];
        HMACSHA256.HashData(key, message, fullHash);
        
        // V1 returns only first 16 bytes
        return fullHash[..V1AuthTagLength].ToArray();
    }
    
    /// <inheritdoc />
    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (signature.Length != V1AuthTagLength)
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
