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
using Yubico.YubiKit.Core.Utils;

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
            pkSdEcka, epkSdEckaTlvBytes, sdReceipt);

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

    [Fact]
    public void TestDeriveSessionKeys_WithDeterministicData()
    {
        // Deterministic test using fixed cryptographic material captured from a real SCP11b session
        // This validates the entire SCP11b key derivation pipeline: ECDH + X9.63 KDF + AES-CMAC
        //
        // Test setup (artificial but deterministic):
        // - Both OCE and YubiKey use the SAME static keypair (private key imported into YubiKey SD)
        // - SCP11b uses same key for ephemeral and static OCE (both use the private key below)
        // - YubiKey generates its own ephemeral key for the session
        // - With fixed keys, ECDH produces deterministic shared secrets → deterministic receipt

        // Private key (same for both OCE ephemeral, OCE static, and YubiKey static)
        var privateKeyBytes =
            Convert.FromHexString("549D2A8A03E62DC829ADE4D6850DB9568475147C59EF238F122A08CF557CDB91");

        // Public key corresponding to the private key above
        // This is used for: OCE ephemeral, OCE static, and YubiKey static
        var publicKey =
            Convert.FromHexString(
                "04680103F07EBE8E9F8C56A39BA96CC7A0F236D94F68410A05C62A1675C14712079A93453F9A52F76EB87E75C5A0D600AD88C843B260820A6DF978205B2B388BAC");

        // YubiKey's ephemeral public key TLV (from AUTHENTICATE response in the captured session)
        // This is a different key that the YubiKey generated for this specific session
        var epkSdEckaTlvBytes = Convert.FromHexString(
            "5F494104182899E51687194868973A16A397EE4BED7DC76800B5614747828DF04446BB4E01AE0614B481317115EEC5A5D09F1E18119BE57B716D87D739288D15BF051167");

        // Expected sdReceipt computed from these inputs (captured from real YubiKey)
        var expectedSdReceipt = Convert.FromHexString("171632BF1F7B2CCC8A7BA3254F987AAA");

        // Reconstruct ECDH keys
        var ephemeralOceEcka = CreateECDiffieHellmanFromPrivateKey(privateKeyBytes); // OCE ephemeral
        var skOceEcka =
            CreateECDiffieHellmanFromPrivateKey(privateKeyBytes); // OCE static (same as ephemeral in SCP11b)

        // YubiKey's static public key (same as OCE public key in this artificial test)
        var pkSdEcka = CreatePublicKeyFromUncompressedPoint(publicKey);

        // SCP11b parameters
        byte[] keyUsage = [0x3C]; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
        byte[] keyType = [0x88]; // AES
        byte[] keyLen = [16]; // 128-bit
        const byte scpTypeParam = 0x00; // SCP11b

        var hostAuthenticateTlvBytes = TlvHelper.EncodeList(
        [
            new Tlv(0xA6, TlvHelper.EncodeList(
            [
                new Tlv(0x90, [0x11, scpTypeParam]),
                new Tlv(0x95, keyUsage),
                new Tlv(0x80, keyType),
                new Tlv(0x81, keyLen)
            ])),
            new Tlv(0x5F49, publicKey)
        ]);

        // Call DeriveSessionKeys - it should compute the same receipt as expectedSdReceipt
        var sessionKeys = Scp11X963Kdf.DeriveSessionKeys(
            ephemeralOceEcka,
            skOceEcka,
            hostAuthenticateTlvBytes,
            pkSdEcka,
            epkSdEckaTlvBytes,
            expectedSdReceipt);

        // Assert session keys were derived successfully
        Assert.NotNull(sessionKeys);

        // The fact that DeriveSessionKeys didn't throw means the computed receipt matched expectedSdReceipt
        // This validates the entire ECDH + X9.63 KDF + AES-CMAC pipeline
    }

    private static ECDiffieHellman CreateECDiffieHellmanFromPrivateKey(byte[] privateKeyBytes)
    {
        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = privateKeyBytes };
        return ECDiffieHellman.Create(parameters);
    }

    private static ECDiffieHellmanPublicKey CreatePublicKeyFromUncompressedPoint(byte[] uncompressedPoint)
    {
        // Uncompressed point format: 0x04 || X (32 bytes) || Y (32 bytes)
        if (uncompressedPoint[0] != 0x04 || uncompressedPoint.Length != 65)
            throw new ArgumentException("Invalid uncompressed point format");

        var x = uncompressedPoint.AsSpan(1, 32).ToArray();
        var y = uncompressedPoint.AsSpan(33, 32).ToArray();

        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = x, Y = y } };

        using var ecdh = ECDiffieHellman.Create(parameters);
        return ecdh.PublicKey;
    }

    private static ECDiffieHellmanPublicKey CreatePublicKeyFromCoordinates(byte[] x, byte[] y)
    {
        var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = x, Y = y } };

        using var ecdh = ECDiffieHellman.Create(parameters);
        return ecdh.PublicKey;
    }
}