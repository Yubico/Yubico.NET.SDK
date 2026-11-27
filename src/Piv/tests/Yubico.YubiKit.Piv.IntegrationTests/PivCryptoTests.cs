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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivCryptoTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SignOrDecryptAsync_EccP256Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.P256,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = SHA256.HashData("test data"u8);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.P256,
            dataToSign);

        Assert.NotEmpty(signature.ToArray());
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        // PIV returns DER-encoded signatures (RFC 3279 format)
        Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CalculateSecretAsync_ECDH_ProducesMatchingSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var devicePublicKey = await session.GenerateKeyAsync(
            Slot.KeyManagement,
            KeyType.P256);
        await session.VerifyPinAsync(DefaultPin);

        using var peerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKeyBytes = peerKey.PublicKey.ExportSubjectPublicKeyInfo();
        var peerPublicKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(peerPublicKeyBytes);

        var sharedSecret = await session.CalculateSecretAsync(
            Slot.KeyManagement,
            peerPublicKey);

        Assert.Equal(CryptographicDefinitions.Keys.P256.KeySizeBytes, sharedSecret.Length);

        // Verify shared secrets match using software ECDH
        using var deviceEcdh = ECDiffieHellman.Create();
        deviceEcdh.ImportSubjectPublicKeyInfo(((ECPublicKey)devicePublicKey).ExportSubjectPublicKeyInfo(), out _);
        var softwareSecret = peerKey.DeriveRawSecretAgreement(deviceEcdh.PublicKey);

        Assert.Equal(softwareSecret, sharedSecret.ToArray());
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait("Audit", "YESDK-1632")]
    public async Task SignOrDecryptAsync_Ed25519_ProducesSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Ed25519,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "test data"u8.ToArray();

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Ed25519,
            dataToSign);

        Assert.Equal(CryptographicDefinitions.Keys.Ed25519.KeySizeBytes * 2, signature.Length);

        var curve25519Key = (Curve25519PublicKey)publicKey;
        Assert.Equal(CryptographicDefinitions.Keys.Ed25519.KeySizeBytes, curve25519Key.PublicPoint.Length);

        // Compare short/padded and long/common-prefix message pairs, then repeat one message.
        // These differential checks catch the selected 32-byte padding/truncation regressions and
        // check repeatability; a deterministic hash-first implementation can also distinguish the pairs.
        // See PivAuditHardwareReproTests.YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages
        // in AuditV2/PivAuditHardwareReproTests.cs for independent OpenSSL message/key verification.
        byte[] paddedTo32 = new byte[32];
        dataToSign.CopyTo(paddedTo32, 0);
        var paddedSignature = await session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, paddedTo32);
        Assert.NotEqual(signature.ToArray(), paddedSignature.ToArray());

        byte[] long1 = Enumerable.Range(1, 64).Select(i => (byte)i).ToArray();
        byte[] long2 = (byte[])long1.Clone();
        long2[63] ^= 0xFF;
        var longSignature1 = await session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, long1);
        var longSignature2 = await session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, long2);
        Assert.NotEqual(longSignature1.ToArray(), longSignature2.ToArray());
        Assert.Equal(longSignature1.ToArray(), (await session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, long1)).ToArray());
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.0.0")]
    public async Task SignOrDecryptAsync_EccP384Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.P384,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        // P384 uses SHA384 (48-byte hash)
        var dataToSign = SHA384.HashData("test data"u8);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.P384,
            dataToSign);

        Assert.NotEmpty(signature.ToArray());
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        // PIV returns DER-encoded signatures (RFC 3279 format)
        Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task CalculateSecretAsync_X25519_ProducesSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            Slot.KeyManagement,
            KeyType.X25519);
        await session.VerifyPinAsync(DefaultPin);

        var curve25519Key = (Curve25519PublicKey)publicKey;
        Assert.Equal(CryptographicDefinitions.Keys.Ed25519.KeySizeBytes, curve25519Key.PublicPoint.Length);

        // This method checks generation and public-point width only; it performs no agreement call.
        // TODO: Add a compatible peer X25519 implementation and compare raw shared secrets.
    }

    // SHA-256 DigestInfo prefix for PKCS#1 v1.5 padding
    private static readonly byte[] Sha256DigestInfo =
    [
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20
    ];

    /// <summary>
    /// Creates PKCS#1 v1.5 padding for RSA signing.
    /// Format: 0x00 0x01 [0xFF padding] 0x00 [DigestInfo] [Hash]
    /// </summary>
    private static byte[] CreatePkcs1v15SigningPadding(byte[] digestInfo, byte[] hash, int modulusBytes)
    {
        var paddedData = new byte[modulusBytes];

        // 0x00 0x01
        paddedData[0] = 0x00;
        paddedData[1] = 0x01;

        // Calculate padding length: total - 3 (0x00, 0x01, 0x00) - digestInfo - hash
        var paddingLength = modulusBytes - 3 - digestInfo.Length - hash.Length;

        // Fill with 0xFF
        for (var i = 2; i < 2 + paddingLength; i++)
        {
            paddedData[i] = 0xFF;
        }

        // 0x00 separator
        paddedData[2 + paddingLength] = 0x00;

        // Copy DigestInfo
        Array.Copy(digestInfo, 0, paddedData, 3 + paddingLength, digestInfo.Length);

        // Copy hash
        Array.Copy(hash, 0, paddedData, 3 + paddingLength + digestInfo.Length, hash.Length);

        return paddedData;
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignOrDecryptAsync_Rsa2048Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "test data for RSA"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        // PIV RSA performs raw RSA - we need PKCS#1 v1.5 padding
        var paddedData = CreatePkcs1v15SigningPadding(Sha256DigestInfo, hash, CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes, signature.Length);

        // Verify signature using RSA
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignOrDecryptAsync_Rsa1024Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa1024,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "test data for RSA 1024"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha256DigestInfo, hash, CryptographicDefinitions.Keys.Rsa1024.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa1024,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa1024.KeySizeBytes, signature.Length);

        // Verify signature using RSA
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task SignOrDecryptAsync_Rsa3072Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa3072,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "test data for RSA 3072"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha256DigestInfo, hash, CryptographicDefinitions.Keys.Rsa3072.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa3072,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa3072.KeySizeBytes, signature.Length);

        // Verify signature using RSA
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task SignOrDecryptAsync_Rsa4096Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa4096,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "test data for RSA 4096"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha256DigestInfo, hash, CryptographicDefinitions.Keys.Rsa4096.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa4096,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa4096.KeySizeBytes, signature.Length);

        // Verify signature using RSA
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignOrDecryptAsync_Rsa2048Decrypt_DecryptsCorrectly(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.KeyManagement,
            KeyType.Rsa2048);
        await session.VerifyPinAsync(DefaultPin);

        // Encrypt test data with public key
        var plaintext = "secret message"u8.ToArray();
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        var encrypted = rsa.Encrypt(plaintext, RSAEncryptionPadding.Pkcs1);

        // Decrypt with YubiKey (returns raw RSA output with PKCS#1 v1.5 encryption padding)
        var decrypted = await session.SignOrDecryptAsync(
            Slot.KeyManagement,
            KeyType.Rsa2048,
            encrypted);

        // PKCS#1 v1.5 encryption padding: 0x00 0x02 [random bytes] 0x00 [message]
        // Find the 0x00 separator after the padding
        var paddedOutput = decrypted.ToArray();
        Assert.Equal(0x00, paddedOutput[0]);
        Assert.Equal(0x02, paddedOutput[1]);

        var separatorIndex = Array.IndexOf(paddedOutput, (byte)0x00, 2);
        Assert.True(separatorIndex >= 10, "PKCS#1 padding should have at least 8 bytes of random data");

        var extractedPlaintext = paddedOutput[(separatorIndex + 1)..];
        Assert.Equal(plaintext, extractedPlaintext);
    }
}