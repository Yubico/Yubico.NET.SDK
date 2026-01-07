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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal static class Scp11X963Kdf
{
    internal static SessionKeys DeriveSessionKeys(
        ECDiffieHellman eSkOceEcka, // Host ephemeral private key
        ECDiffieHellman skOceEcka, // Host static or ephemeral private key
        ReadOnlyMemory<byte> oceAuthenticateData, // Host Authenticate EC KeyAgreement TLV Bytes
        ECDiffieHellmanPublicKey pkSdEcka, // Yubikey Public Key
        ReadOnlyMemory<byte> ePkSdEcka, // Yubikey Ephemeral SD Public Key Bytes
        ReadOnlyMemory<byte> sdReceipt, // Yubikey receipt
        ReadOnlyMemory<byte> keyUsage,
        ReadOnlyMemory<byte> keyType,
        ReadOnlyMemory<byte> keyLen
    )
    {
        byte[] sharedInfo = [..keyUsage.Span, ..keyType.Span, ..keyLen.Span];
        byte[] keyAgreementData = [..oceAuthenticateData.Span, ..ePkSdEcka.Span];
        var keyMaterial = GetSharedSecret(eSkOceEcka, skOceEcka, pkSdEcka, ePkSdEcka);

        const int keyCount = 5;
        const int keySizeBytes = 16; // 128 bits
        var derivedKeyMaterial = X963Kdf.DeriveKeyMaterial(
            keyMaterial,
            sharedInfo,
            keyCount * keySizeBytes);

        var keys = new List<byte[]>();
        for (var i = 0; i < 5; i++)
            keys.Add(derivedKeyMaterial.AsSpan(i * 16, 16).ToArray());

        CryptographicOperations.ZeroMemory(derivedKeyMaterial);

        // 5 keys were derived: one for verification of receipt, 4 keys to use
        var receiptVerificationKey = keys[0];
        var oceReceipt = GenerateOceReceiptAesCmac(receiptVerificationKey, keyAgreementData);

        return CryptographicOperations.FixedTimeEquals(sdReceipt.Span, oceReceipt)
            ? new SessionKeys(keys[1], keys[2], keys[3], keys[4])
            : throw new BadResponseException("Receipt does not match");
    }

    internal static Span<byte> GetSharedSecret(
        ECDiffieHellman ePkOceEcka, // host ephemeral key
        ECDiffieHellman skOceEcka, // host private key
        ECDiffieHellmanPublicKey pkSdEcka, // Yubikey Public Key
        ReadOnlyMemory<byte> epkSdEckaTlvBytes
    )
    {
        var ePkSdEcka = CreateECDiffieHellmanPublicKey(epkSdEckaTlvBytes);

        // Key agreement 1: ephemeral OCE private key with ephemeral SD public key
        var ka1 = ePkOceEcka.DeriveRawSecretAgreement(ePkSdEcka);

        // Key agreement 2: static/ephemeral OCE private key with static SD public key
        var ka2 = skOceEcka.DeriveRawSecretAgreement(pkSdEcka);

        const int expectedLength = 32; // 256 bits
        if (ka1.Length != expectedLength || ka2.Length != expectedLength)
            throw new InvalidOperationException("Derived key agreement material has unexpected length");

        using var buffer = new DisposableArrayPoolBuffer(expectedLength * 2);
        var keyMaterial = buffer.Span;
        ka1.AsSpan().CopyTo(keyMaterial);
        ka2.AsSpan().CopyTo(keyMaterial[ka1.Length..]);

        CryptographicOperations.ZeroMemory(ka1);
        CryptographicOperations.ZeroMemory(ka2);

        return keyMaterial.ToArray();
    }

    private static ECDiffieHellmanPublicKey CreateECDiffieHellmanPublicKey(ReadOnlyMemory<byte> ePkSdEckaTlv)
    {
        var ePkSdEckaEncodedPoint = TlvHelper.GetValue(0x5F49, ePkSdEckaTlv.Span);
        var ePkSdEcka = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = ePkSdEckaEncodedPoint.Span[1..33].ToArray(), Y = ePkSdEckaEncodedPoint.Span[33..].ToArray()
            }
        };

        return ECDiffieHellman.Create(ePkSdEcka).PublicKey;
    }

    internal static Span<byte> GetKeyAgreementData(
        ReadOnlyMemory<byte> pkOceEcka,
        ReadOnlyMemory<byte> ePkSdEckaTlvBytes)
    {
        var length = pkOceEcka.Length + ePkSdEckaTlvBytes.Length;
        using var buffer = new DisposableArrayPoolBuffer(length);
        var keyAgreementData = buffer.Span;

        // Key Agreement Data: host authenticate TLV + epkSdEcka TLV
        pkOceEcka.Span.CopyTo(keyAgreementData);
        ePkSdEckaTlvBytes.Span.CopyTo(keyAgreementData[pkOceEcka.Length..]);

        return keyAgreementData.ToArray();
    }

    internal static byte[] GenerateOceReceiptAesCmac(ReadOnlySpan<byte> receiptVerificationKey,
        ReadOnlySpan<byte> keyAgreementData)
    {
        // var useOpenSsl = false; // Try AesCmac instead of OpenSSL
        // if (useOpenSsl)
        // {
        //     using var cmacObj =
        //         new CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm.Aes128); // This works in legacy code.
        //
        //     Span<byte> oceReceipt = stackalloc byte[16];
        //     cmacObj.CmacInit(receiptVerificationKey);
        //     cmacObj.CmacUpdate(keyAgreementData);
        //     cmacObj.CmacFinal(oceReceipt); // Our generated receipt
        //     return oceReceipt.ToArray();
        // }

        using var
            mac = new AesCmac(
                receiptVerificationKey); // When we have made a successful SCP11 connection with legacy code, we can try this again.
        mac.AppendData(keyAgreementData);
        return mac.GetHashAndReset();
    }
}