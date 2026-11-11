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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

internal static class Scp11X963Kdf
{
    internal static SessionKeys DeriveSessionKeys(
        ECDiffieHellman eSkOceEcka, // Host ephemeral private key
        ECDiffieHellman skOceEcka, // Host static or ephemeral private key
        ReadOnlyMemory<byte> oceAuthenticateData, // Host Authenticate EC KeyAgreement TLV Bytes
        ECDiffieHellmanPublicKey pkSdEcka, // Yubikey Public Key
        ReadOnlyMemory<byte> ePkSdEcka, // Yubikey Ephemeral SD Public Key Bytes
        ReadOnlyMemory<byte> sdReceipt // Yubikey receipt
    )
    {
        // Extract keyUsage, keyType, keyLen from oceAuthenticateData
        // Structure: A6 [ 90 [11 scpType], 95 [keyUsage], 80 [keyType], 81 [keyLen] ], 5F49 [epkOce]
        if (!TlvHelper.TryFindValue(0xA6, oceAuthenticateData.Span, out var a6Value))
            throw new InvalidOperationException("Missing A6 tag in oceAuthenticateData");

        if (!TlvHelper.TryFindValue(0x95, a6Value.Span, out var keyUsage) ||
            !TlvHelper.TryFindValue(0x80, a6Value.Span, out var keyType) ||
            !TlvHelper.TryFindValue(0x81, a6Value.Span, out var keyLen))
            throw new InvalidOperationException("Missing required tags (95, 80, 81) in A6 container");

        byte[] sharedInfo = [.. keyUsage.Span, .. keyType.Span, .. keyLen.Span];
        byte[] keyAgreementData = [.. oceAuthenticateData.Span, .. ePkSdEcka.Span];
        const int keyCount = 5;
        const int keySizeBytes = 16; // 128 bits
        byte[]? keyMaterial = null;
        byte[]? derivedKeyMaterial = null;
        byte[]? oceReceipt = null;
        try
        {
            keyMaterial = GetSharedSecret(eSkOceEcka, skOceEcka, pkSdEcka, ePkSdEcka);
            derivedKeyMaterial = X963Kdf.DeriveKeyMaterial(
                keyMaterial,
                sharedInfo,
                keyCount * keySizeBytes);

            oceReceipt = GenerateOceReceiptAesCmac(derivedKeyMaterial.AsSpan(0, keySizeBytes), keyAgreementData);
            if (!CryptographicOperations.FixedTimeEquals(sdReceipt.Span, oceReceipt))
                throw new BadResponseException("Receipt does not match");

            return new SessionKeys(
                derivedKeyMaterial.AsSpan(keySizeBytes, keySizeBytes),
                derivedKeyMaterial.AsSpan(keySizeBytes * 2, keySizeBytes),
                derivedKeyMaterial.AsSpan(keySizeBytes * 3, keySizeBytes),
                derivedKeyMaterial.AsSpan(keySizeBytes * 4, keySizeBytes));
        }
        finally
        {
            if (keyMaterial is not null)
                CryptographicOperations.ZeroMemory(keyMaterial);
            if (derivedKeyMaterial is not null)
                CryptographicOperations.ZeroMemory(derivedKeyMaterial);
            if (oceReceipt is not null)
                CryptographicOperations.ZeroMemory(oceReceipt);
        }
    }

    internal static byte[] GetSharedSecret(
        ECDiffieHellman ePkOceEcka, // host ephemeral key
        ECDiffieHellman skOceEcka, // host private key
        ECDiffieHellmanPublicKey pkSdEcka, // Yubikey Public Key
        ReadOnlyMemory<byte> epkSdEckaTlvBytes
    ) => GetSharedSecret(
        ePkOceEcka,
        skOceEcka,
        pkSdEcka,
        epkSdEckaTlvBytes,
        static key => key.ExportParameters(true),
        static parameters => ECDiffieHellman.Create(parameters));

    internal static byte[] GetSharedSecret(
        ECDiffieHellman ePkOceEcka,
        ECDiffieHellman skOceEcka,
        ECDiffieHellmanPublicKey pkSdEcka,
        ReadOnlyMemory<byte> epkSdEckaTlvBytes,
        Func<ECDiffieHellman, ECParameters> privateKeyExporter) => GetSharedSecret(
        ePkOceEcka,
        skOceEcka,
        pkSdEcka,
        epkSdEckaTlvBytes,
        privateKeyExporter,
        static parameters => ECDiffieHellman.Create(parameters));

    internal static byte[] GetSharedSecret(
        ECDiffieHellman ePkOceEcka,
        ECDiffieHellman skOceEcka,
        ECDiffieHellmanPublicKey pkSdEcka,
        ReadOnlyMemory<byte> epkSdEckaTlvBytes,
        Func<ECParameters, ECDiffieHellman> ephemeralSdKeyFactory) => GetSharedSecret(
        ePkOceEcka,
        skOceEcka,
        pkSdEcka,
        epkSdEckaTlvBytes,
        static key => key.ExportParameters(true),
        ephemeralSdKeyFactory);

    private static byte[] GetSharedSecret(
        ECDiffieHellman ePkOceEcka,
        ECDiffieHellman skOceEcka,
        ECDiffieHellmanPublicKey pkSdEcka,
        ReadOnlyMemory<byte> epkSdEckaTlvBytes,
        Func<ECDiffieHellman, ECParameters> privateKeyExporter,
        Func<ECParameters, ECDiffieHellman> ephemeralSdKeyFactory)
    {
        using ECDiffieHellman ePkSdEckaOwner = ephemeralSdKeyFactory(
            ParseECDiffieHellmanParameters(epkSdEckaTlvBytes));
        using ECDiffieHellmanPublicKey ePkSdEcka = ePkSdEckaOwner.PublicKey;
        IEcdhPrimitives ecdh = CryptographyProviders.EcdhPrimitivesCreator();
        ECParameters ephemeralParameters = default;
        ECParameters staticParameters = default;

        byte[]? ka1 = null;
        byte[]? ka2 = null;
        try
        {
            ephemeralParameters = privateKeyExporter(ePkOceEcka);
            staticParameters = privateKeyExporter(skOceEcka);

            if (ephemeralParameters.D is null)
                throw new CryptographicException("The ephemeral ECDH key has no private value.");
            if (staticParameters.D is null)
                throw new CryptographicException("The static ECDH key has no private value.");

            // Key agreement 1: ephemeral OCE private key with ephemeral SD public key.
            // ephemeralParameters carries the local key's own matching Q/D pair; the SD's
            // ephemeral public key is the remote party.
            ka1 = ecdh.ComputeSharedSecret(ephemeralParameters, ePkSdEcka.ExportParameters());

            // Key agreement 2: static/ephemeral OCE private key with static SD public key.
            ka2 = ecdh.ComputeSharedSecret(staticParameters, pkSdEcka.ExportParameters());

            const int expectedLength = 32; // 256 bits
            if (ka1.Length != expectedLength || ka2.Length != expectedLength)
                throw new InvalidOperationException("Derived key agreement material has unexpected length");

            using var buffer = new DisposableArrayPoolBuffer(expectedLength * 2);
            var keyMaterial = buffer.Span;
            ka1.AsSpan().CopyTo(keyMaterial);
            ka2.AsSpan().CopyTo(keyMaterial[ka1.Length..]);
            return keyMaterial.ToArray();
        }
        finally
        {
            if (ephemeralParameters.D is not null)
                CryptographicOperations.ZeroMemory(ephemeralParameters.D);
            if (staticParameters.D is not null)
                CryptographicOperations.ZeroMemory(staticParameters.D);
            if (ka1 is not null)
                CryptographicOperations.ZeroMemory(ka1);
            if (ka2 is not null)
                CryptographicOperations.ZeroMemory(ka2);
        }
    }

    private static ECParameters ParseECDiffieHellmanParameters(ReadOnlyMemory<byte> ePkSdEckaTlv)
    {
        var ePkSdEckaEncodedPoint = TlvHelper.GetValue(0x5F49, ePkSdEckaTlv.Span);
        var ePkSdEcka = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = ePkSdEckaEncodedPoint.Span[1..33].ToArray(),
                Y = ePkSdEckaEncodedPoint.Span[33..].ToArray()
            }
        };

        return ePkSdEcka;
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
        => GenerateOceReceiptAesCmac(
            receiptVerificationKey,
            keyAgreementData,
            static length => new byte[length]);

    internal static byte[] GenerateOceReceiptAesCmac(
        ReadOnlySpan<byte> receiptVerificationKey,
        ReadOnlySpan<byte> keyAgreementData,
        Func<int, byte[]> receiptBufferFactory)
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

        byte[]? receipt = receiptBufferFactory(16);
        try
        {
            ICmacPrimitives mac = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            try
            {
                mac.CmacInit(receiptVerificationKey);
                mac.CmacUpdate(keyAgreementData);
                mac.CmacFinal(receipt);
            }
            finally
            {
                mac.Dispose();
            }

            byte[] result = receipt;
            receipt = null;
            return result;
        }
        finally
        {
            if (receipt is not null)
            {
                CryptographicOperations.ZeroMemory(receipt);
            }
        }
    }
}