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

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal class X964Kdf
{
    public static SessionKeys X963KDF(ECPublicKey pkSdEcka, // Yubikey Public Key
        ECDiffieHellman ephemeralOceEcka, // host ephemeral key
        ECDiffieHellman skOceEcka, // host private key
        ReadOnlyMemory<byte> sdReceipt, // Yubikey receipt
        ReadOnlyMemory<byte> epkSdEckaTlvBytes, // Yubikey Ephemeral SD Public Key Bytes
        ReadOnlyMemory<byte> hostAuthenticateTlvEncodedData,
        ReadOnlyMemory<byte> keyUsage,
        ReadOnlyMemory<byte> keyType,
        ReadOnlyMemory<byte> keyLen
    )
    {
        byte[]? rentedKeyAgreementData = null;
        byte[]? rentedKeyMaterial = null;

        try
        {
            var keyAgreementDataLen = hostAuthenticateTlvEncodedData.Length + epkSdEckaTlvBytes.Length;
            rentedKeyAgreementData = ArrayPool<byte>.Shared.Rent(keyAgreementDataLen);
            var keyAgreementData = rentedKeyAgreementData.AsSpan(0, keyAgreementDataLen);

            hostAuthenticateTlvEncodedData.Span.CopyTo(keyAgreementData);
            epkSdEckaTlvBytes.Span.CopyTo(keyAgreementData[hostAuthenticateTlvEncodedData.Length..]);

            Span<byte> sharedInfo = stackalloc byte[keyUsage.Length + keyType.Length + keyLen.Length];
            keyUsage.Span.CopyTo(sharedInfo);
            keyType.Span.CopyTo(sharedInfo[keyUsage.Length..]);
            keyLen.Span.CopyTo(sharedInfo[(keyUsage.Length + keyType.Length)..]);

            // Key agreement 1: ephemeral OCE private key with ephemeral SD public key
            var epkSdEcka = ExtractPublicKey(epkSdEckaTlvBytes);
            var ka1 = epkSdEcka.DeriveKeyMaterial(ephemeralOceEcka);

            // Key agreement 2: static/ephemeral OCE private key with static SD public key
            var ka2 = pkSdEcka.DeriveKeyMaterial(skOceEcka);

            var keyMaterialLen = ka1.Length + ka2.Length;
            rentedKeyMaterial = ArrayPool<byte>.Shared.Rent(keyMaterialLen);
            var keyMaterial = rentedKeyMaterial.AsSpan(0, keyMaterialLen);
            ka1.AsSpan().CopyTo(keyMaterial);
            ka2.AsSpan().CopyTo(keyMaterial[ka1.Length..]);
            CryptographicOperations.ZeroMemory(ka1);
            CryptographicOperations.ZeroMemory(ka2);

            var keys = new List<byte[]>();
            var counter = 1;

            // We need 5 16-byte keys, which requires 3 iterations of SHA256
            Span<byte> counterBytes = stackalloc byte[4];
            Span<byte> digest = stackalloc byte[32];
            for (var i = 0; i < 3; i++)
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                BinaryPrimitives.WriteInt32BigEndian(counterBytes, counter++);

                hash.AppendData(keyMaterial);
                hash.AppendData(counterBytes);
                hash.AppendData(sharedInfo);

                // Each iteration gives us 2 keys
                var bytesWritten = hash.GetHashAndReset(digest);
                if (bytesWritten != 32) throw new InvalidOperationException("Hash computation failed");

                var key1 = digest[..16].ToArray();
                var key2 = digest[16..32].ToArray();
                keys.Add(key1);
                keys.Add(key2);
                CryptographicOperations.ZeroMemory(digest);
            }

            // 6 keys were derived: one for verification of receipt, 4 keys to use, and 1 which is discarded
            var receiptVerificationKey = keys[0];
            // using var mac = new AesCmac(key);
            // mac.AppendData(keyAgreementData);
            // var genReceipt = mac.GetHashAndReset();

            using var cmacObj = new CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm.Aes128);

            Span<byte> oceReceipt = stackalloc byte[16];
            cmacObj.CmacInit(receiptVerificationKey);
            cmacObj.CmacUpdate(keyAgreementData);
            cmacObj.CmacFinal(oceReceipt); // Our generated receipt

            if (!CryptographicOperations.FixedTimeEquals(sdReceipt.Span, oceReceipt))
                throw new BadResponseException("Receipt does not match");

            return new SessionKeys(keys[1], keys[2], keys[3], keys[4]);
        }
        finally
        {
            if (rentedKeyAgreementData != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyAgreementData);
                ArrayPool<byte>.Shared.Return(rentedKeyAgreementData);
            }

            if (rentedKeyMaterial != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyMaterial);
                ArrayPool<byte>.Shared.Return(rentedKeyMaterial);
            }
        }

        static ECPublicKey ExtractPublicKey(ReadOnlyMemory<byte> epkSdEckaTlv)
        {
            var epkSdEckaEncodedPoint = TlvHelper.GetValue(0x5F49, epkSdEckaTlv.Span);
            var epkSdEcka = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = epkSdEckaEncodedPoint.Span[1..33].ToArray(),
                    Y = epkSdEckaEncodedPoint.Span[33..].ToArray()
                }
            };

            return ECPublicKey.CreateFromParameters(epkSdEcka);
        }
    }

    /// <summary>
    /// Core X9.63-2001 KDF implementation using SHA-256.
    /// Derives key material from shared secret Z and optional SharedInfo.
    /// </summary>
    /// <param name="z">Shared secret material (input keying material)</param>
    /// <param name="sharedInfo">Optional shared information (can be empty)</param>
    /// <param name="keyDataLength">Desired output length in bytes</param>
    /// <returns>Derived key material of exactly keyDataLength bytes</returns>
    /// <exception cref="ArgumentException">Thrown if keyDataLength is invalid</exception>
    internal static byte[] DeriveKeyMaterial(
        ReadOnlySpan<byte> z,
        ReadOnlySpan<byte> sharedInfo,
        int keyDataLength)
    {
        if (keyDataLength <= 0)
            throw new ArgumentException("Key data length must be positive", nameof(keyDataLength));

        // ANS X9.63-2001 KDF with SHA-256
        // Output = Hash(Z || Counter || SharedInfo) for Counter = 1, 2, 3, ...
        // Counter is 32-bit big-endian starting at 1

        const int hashSize = 32; // SHA-256 output size
        var result = new byte[keyDataLength];
        var counter = 1;
        var bytesGenerated = 0;

        Span<byte> counterBytes = stackalloc byte[4];
        Span<byte> hash = stackalloc byte[hashSize];

        while (bytesGenerated < keyDataLength)
        {
            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            
            // Write counter as big-endian 32-bit integer
            BinaryPrimitives.WriteInt32BigEndian(counterBytes, counter);

            // Hash(Z || Counter || SharedInfo)
            sha256.AppendData(z);
            sha256.AppendData(counterBytes);
            sha256.AppendData(sharedInfo);

            var bytesWritten = sha256.GetHashAndReset(hash);
            if (bytesWritten != hashSize)
                throw new InvalidOperationException("Hash computation failed");

            // Copy hash bytes to result (may be partial for last iteration)
            var bytesToCopy = Math.Min(hashSize, keyDataLength - bytesGenerated);
            hash[..bytesToCopy].CopyTo(result.AsSpan(bytesGenerated, bytesToCopy));

            bytesGenerated += bytesToCopy;
            counter++;
        }

        return result;
    }
}