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
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Unit tests for Scp11X963Kdf class.
/// </summary>
public class Scp11X963KdfTests
{
    [Fact]
    public void TestDeriveKeyMaterial_WithTestVectors()
    {
        // Use test vectors from ansx963_2001.rsp (COUNT = 0, 128-bit output)
        var z = Convert.FromHexString("96c05619d56c328ab95fe84b18264b08725b85e33fd34f08");
        var sharedInfo = ReadOnlySpan<byte>.Empty; // Empty SharedInfo
        var expected = Convert.FromHexString("443024c3dae66b95e6f5670601558f71");

        var result = X963Kdf.DeriveKeyMaterial(z, sharedInfo, 16);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestGetSharedSecret()
    {
        // Mock EC keys for testing GetSharedSecret
        var pkSdEcka = CreateTestSdPublicKey();
        var ephemeralOceEcka = CreateTestOceEphemeralPrivateKey();
        var skOceEcka = CreateTestOceStaticPrivateKey();
        var epkSdEckaTlvBytes = CreateTestEpkSdEckaTlv();

        var sharedSecret = Scp11X963Kdf.GetSharedSecret(ephemeralOceEcka, skOceEcka, pkSdEcka, epkSdEckaTlvBytes);

        // Expected shared secret: concatenation of ka1 and ka2
        // For test purposes, we can compute expected manually or assert length
        Assert.Equal(64, sharedSecret.Length); // 32 bytes each for P-256
    }

    [Fact]
    public void TestGetKeyAgreementData()
    {
        var epkSdEckaTlvBytes = CreateTestEpkSdEckaTlv();
        var hostAuthenticateTlvBytes = CreateTestHostAuthenticateTlv();

        var keyAgreementData = Scp11X963Kdf.GetKeyAgreementData(hostAuthenticateTlvBytes, epkSdEckaTlvBytes);

        // Should be concatenation of hostAuthenticateTlvBytes and epkSdEckaTlvBytes
        var expectedLength = hostAuthenticateTlvBytes.Length + epkSdEckaTlvBytes.Length;
        Assert.Equal(expectedLength, keyAgreementData.Length);
    }

    [Fact]
    public void TestGenerateOceReceipt()
    {
        var receiptVerificationKey = new byte[16]; // Mock 128-bit key
        RandomNumberGenerator.Fill(receiptVerificationKey);
        var keyAgreementData = new byte[32]; // Mock data
        RandomNumberGenerator.Fill(keyAgreementData);

        var receipt = Scp11X963Kdf.GenerateOceReceiptAesCmac(receiptVerificationKey, keyAgreementData);

        Assert.Equal(16, receipt.Length); // CMAC output is 16 bytes for AES-128
    }

    [Fact]
    public void TestDeriveSessionKeys()
    {
        // Generate test inputs
        var pkSdEcka = CreateTestSdPublicKey();
        var ephemeralOceEcka = CreateTestOceEphemeralPrivateKey();
        var skOceEcka = CreateTestOceStaticPrivateKey();
        var epkSdEckaTlvBytes = CreateTestEpkSdEckaTlv();
        var hostAuthenticateTlvBytes = CreateTestHostAuthenticateTlv();

        // Fixed values for SCP11 parameters
        ReadOnlyMemory<byte>
            keyUsage = new byte[] { 0x3C }; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
        ReadOnlyMemory<byte> keyType = new byte[] { 0x88 }; // AES
        ReadOnlyMemory<byte> keyLen = new byte[] { 16 }; // 128-bit

        // Emulate YubiKey receipt generation
        var sdReceipt = GenerateTestSdReceipt(
            pkSdEcka,
            ephemeralOceEcka,
            skOceEcka,
            epkSdEckaTlvBytes,
            hostAuthenticateTlvBytes,
            keyUsage,
            keyType,
            keyLen);

        // Call DeriveSessionKeys - should succeed since receipts match
        var sessionKeys = Scp11X963Kdf.DeriveSessionKeys(ephemeralOceEcka,
            skOceEcka,
            hostAuthenticateTlvBytes,
            pkSdEcka, epkSdEckaTlvBytes, sdReceipt, keyUsage, keyType, keyLen);

        // Assert session keys are returned
        Assert.NotNull(sessionKeys);
    }

    // Helper to emulate YubiKey receipt generation for testing
    private static ReadOnlyMemory<byte> GenerateTestSdReceipt(
        ECDiffieHellmanPublicKey pkSdEcka,
        ECDiffieHellman ephemeralOceEcka,
        ECDiffieHellman skOceEcka,
        ReadOnlyMemory<byte> epkSdEckaTlvBytes,
        ReadOnlyMemory<byte> hostAuthenticateTlvBytes,
        ReadOnlyMemory<byte> keyUsage,
        ReadOnlyMemory<byte> keyType,
        ReadOnlyMemory<byte> keyLen)
    {
        var keyAgreementData = Scp11X963Kdf.GetKeyAgreementData(hostAuthenticateTlvBytes, epkSdEckaTlvBytes);
        var keyMaterial = Scp11X963Kdf.GetSharedSecret(ephemeralOceEcka, skOceEcka, pkSdEcka, epkSdEckaTlvBytes);
        byte[] sharedInfo = [..keyUsage.Span, ..keyType.Span, ..keyLen.Span];


        // Derive keys
        const int keyCount = 5;
        const int keySizeBytes = 16;
        var derivedKeyMaterial = X963Kdf.DeriveKeyMaterial(keyMaterial, sharedInfo, keyCount * keySizeBytes);

        var keys = new List<byte[]>();
        for (var i = 0; i < keyCount; i++)
            keys.Add(derivedKeyMaterial.AsSpan(i * keySizeBytes, keySizeBytes).ToArray());

        CryptographicOperations.ZeroMemory(derivedKeyMaterial);

        // Compute oceReceipt (emulating YubiKey's receipt)
        var receiptVerificationKey = keys[0];
        var oceReceipt = Scp11X963Kdf.GenerateOceReceiptAesCmac(receiptVerificationKey, keyAgreementData);

        return oceReceipt.AsMemory();
    }

    // Helper methods to create test data
    private static ECDiffieHellmanPublicKey CreateTestSdPublicKey()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return ecdh.PublicKey;
    }

    private static ECDiffieHellman CreateTestOceEphemeralPrivateKey()
    {
        // Use the fixed test key from GetECDHs
        var privateKey = ECPrivateKey.CreateFromValue(
            Convert.FromHexString("549D2A8A03E62DC829ADE4D6850DB9568475147C59EF238F122A08CF557CDB91"),
            KeyType.ECP256);
        return privateKey.ToECDiffieHellman();
    }

    private static ECDiffieHellman CreateTestOceStaticPrivateKey()
    {
        // Another mock private key
        var privateKey = ECPrivateKey.CreateFromValue(
            Convert.FromHexString("1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF"),
            KeyType.ECP256);
        return privateKey.ToECDiffieHellman();
    }

    private static ReadOnlyMemory<byte> CreateTestEpkSdEckaTlv()
    {
        // Generate a valid EC public key point
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdh.ExportParameters(false);
        var point = new byte[65];
        point[0] = 0x04; // Uncompressed
        parameters.Q.X.CopyTo(point.AsSpan(1, 32));
        parameters.Q.Y.CopyTo(point.AsSpan(33, 32));

        // TLV: Tag 0x5F49 (2 bytes), length 1 byte (65), value 65 bytes, total 68 bytes
        var tlv = new byte[68];
        tlv[0] = 0x5F;
        tlv[1] = 0x49;
        tlv[2] = 65;
        point.CopyTo(tlv.AsSpan(3));
        return tlv;
    }

    private static ReadOnlyMemory<byte> CreateTestHostAuthenticateTlv()
    {
        // Mock host authenticate TLV data
        var data = new byte[32];
        RandomNumberGenerator.Fill(data);
        return data;
    }
}