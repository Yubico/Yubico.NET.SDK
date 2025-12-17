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
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal static class Scp11X963Kdf
{
    internal static SessionKeys DeriveSessionKeys(
        ECDiffieHellmanPublicKey pkSdEcka, // Yubikey Public Key
        ECDiffieHellman ephemeralOceEcka, // host ephemeral key
        ECDiffieHellman skOceEcka, // host private key
        ReadOnlyMemory<byte> sdReceipt, // Yubikey receipt
        ReadOnlyMemory<byte> epkSdEckaTlvBytes, // Yubikey Ephemeral SD Public Key Bytes
        ReadOnlyMemory<byte> hostAuthenticateTlvBytes,
        ReadOnlyMemory<byte> keyUsage,
        ReadOnlyMemory<byte> keyType,
        ReadOnlyMemory<byte> keyLen
    )
    {
        var keyAgreementData = GetKeyAgreementData(epkSdEckaTlvBytes, hostAuthenticateTlvBytes);
        var keyMaterial = GetSharedSecret(
            pkSdEcka,
            ephemeralOceEcka,
            skOceEcka,
            epkSdEckaTlvBytes);

        // Create SharedInfo
        var sharedInfo = GetSharedInfo(keyUsage, keyType, keyLen);

        const int keyCount = 5;
        const int keySizeBytes = 16; // 128 bits
        var derivedKeyMaterial = X964Kdf.DeriveKeyMaterial(
            keyMaterial,
            sharedInfo,
            keyCount * keySizeBytes);

        var keys = new List<byte[]>();
        for (var i = 0; i < 5; i++)
            keys.Add(derivedKeyMaterial.AsSpan(i * 16, 16).ToArray());

        CryptographicOperations.ZeroMemory(derivedKeyMaterial);

        // 5 keys were derived: one for verification of receipt, 4 keys to use
        var receiptVerificationKey = keys[0];
        var oceReceipt = GenerateOceReceipt(receiptVerificationKey, keyAgreementData);

        // Debug logging: Compare receipts to identify mismatch cause
        Console.WriteLine($"OCE Receipt: {Convert.ToHexString(oceReceipt)} ({oceReceipt.Length})");
        Console.WriteLine($"SD Receipt: {Convert.ToHexString(sdReceipt.Span)} ({sdReceipt.Length})");

        if (!CryptographicOperations.FixedTimeEquals(sdReceipt.Span, oceReceipt))
            throw new BadResponseException("Receipt does not match");

        return new SessionKeys(keys[1], keys[2], keys[3], keys[4]);
    }

    internal static ReadOnlySpan<byte> GetSharedInfo(
        ReadOnlyMemory<byte> keyUsage,
        ReadOnlyMemory<byte> keyType,
        ReadOnlyMemory<byte> keyLen)
    {
        Span<byte> sharedInfo = new byte[keyUsage.Length + keyType.Length + keyLen.Length];
        
        keyUsage.Span.CopyTo(sharedInfo);
        keyType.Span.CopyTo(sharedInfo[keyUsage.Length..]);
        keyLen.Span.CopyTo(sharedInfo[(keyUsage.Length + keyType.Length)..]);

        return sharedInfo;
    }

    internal static ReadOnlySpan<byte> GetSharedSecret(
        ECDiffieHellmanPublicKey pkSdEcka,
        ECDiffieHellman ephemeralOceEcka,
        ECDiffieHellman skOceEcka,
        ReadOnlyMemory<byte> epkSdEckaTlvBytes)
    {
        byte[]? rentedKeyMaterial = null;
        var epkSdEcka = CreateECDiffieHellmanPublicKey(epkSdEckaTlvBytes);

        try
        {
            // Key agreement 1: ephemeral OCE private key with ephemeral SD public key
            var ka1 = ephemeralOceEcka.DeriveKeyMaterial(epkSdEcka);

            // Key agreement 2: static/ephemeral OCE private key with static SD public key
            var ka2 = skOceEcka.DeriveKeyMaterial(pkSdEcka);

            var keyMaterialLen = ka1.Length + ka2.Length;
            rentedKeyMaterial = ArrayPool<byte>.Shared.Rent(keyMaterialLen);
            var keyMaterial = rentedKeyMaterial.AsSpan(0, keyMaterialLen);

            ka1.AsSpan().CopyTo(keyMaterial);
            ka2.AsSpan().CopyTo(keyMaterial[ka1.Length..]);

            CryptographicOperations.ZeroMemory(ka1);
            CryptographicOperations.ZeroMemory(ka2);

            return keyMaterial;
        }
        finally
        {
            if (rentedKeyMaterial != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyMaterial);
                ArrayPool<byte>.Shared.Return(rentedKeyMaterial);
            }
        }
    }

    private static ECDiffieHellmanPublicKey CreateECDiffieHellmanPublicKey(ReadOnlyMemory<byte> epkSdEckaTlv)
    {
        var epkSdEckaEncodedPoint = TlvHelper.GetValue(0x5F49, epkSdEckaTlv.Span);
        var epkSdEcka = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = epkSdEckaEncodedPoint.Span[1..33].ToArray(), Y = epkSdEckaEncodedPoint.Span[33..].ToArray()
            }
        };

        return ECDiffieHellman.Create(epkSdEcka).PublicKey;

        // return ECPublicKey.CreateFromParameters(epkSdEcka);
    }

    internal static ReadOnlySpan<byte> GetKeyAgreementData(
        ReadOnlyMemory<byte> epkSdEckaTlvBytes,
        ReadOnlyMemory<byte> hostAuthenticateTlvBytes)
    {
        byte[]? rentedKeyAgreementData = null;

        try
        {
            // Key Agreement Data: host authenticate TLV + epkSdEcka TLV
            var keyAgreementDataLen = hostAuthenticateTlvBytes.Length + epkSdEckaTlvBytes.Length;
            rentedKeyAgreementData = ArrayPool<byte>.Shared.Rent(keyAgreementDataLen);
            var keyAgreementData = rentedKeyAgreementData.AsSpan(0, keyAgreementDataLen);

            hostAuthenticateTlvBytes.Span.CopyTo(keyAgreementData);
            epkSdEckaTlvBytes.Span.CopyTo(keyAgreementData[hostAuthenticateTlvBytes.Length..]);

            return keyAgreementData;
        }
        finally
        {
            if (rentedKeyAgreementData != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyAgreementData);
                ArrayPool<byte>.Shared.Return(rentedKeyAgreementData);
            }
        }
    }

    internal static byte[] GenerateOceReceipt(byte[] receiptVerificationKey, ReadOnlySpan<byte> keyAgreementData)
    {
        var useOpenSsl = false; // Try AesCmac instead of OpenSSL
        if (useOpenSsl)
        {
            using var cmacObj =
                new CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm.Aes128); // This works in legacy code.

            Span<byte> oceReceipt = stackalloc byte[16];
            cmacObj.CmacInit(receiptVerificationKey);
            cmacObj.CmacUpdate(keyAgreementData);
            cmacObj.CmacFinal(oceReceipt); // Our generated receipt
            return oceReceipt.ToArray();
        }

        using var
            mac = new AesCmac(
                receiptVerificationKey); // When we have made a successful SCP11 connection with legacy code, we can try this again.
        mac.AppendData(keyAgreementData);
        return mac.GetHashAndReset();
    }
}