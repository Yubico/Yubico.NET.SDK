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
using Xunit;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivImportTests
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ImportKeyAsync_Rsa2048_CanSignAndVerify(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        using var softwareKey = RSA.Create(2048);
        var pkcs8 = softwareKey.ExportPkcs8PrivateKey();
        var privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8);

        var detectedAlgorithm = await session.ImportKeyAsync(
            PivSlot.Authentication,
            privateKey);

        Assert.Equal(PivAlgorithm.Rsa2048, detectedAlgorithm);

        // Sign with YubiKey
        await session.VerifyPinAsync(DefaultPin);
        var dataToSign = "RSA 2048 import test"u8.ToArray();
        // RSA signing on the YubiKey expects PKCS#1 v1.5 padded input of key size
        var hash = SHA256.HashData(dataToSign);

        // Pad hash with PKCS#1 v1.5 DigestInfo for SHA-256 before sending to YubiKey
        byte[] digestInfo =
        [
            0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
            0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05,
            0x00, 0x04, 0x20, .. hash
        ];

        // PKCS#1 v1.5 padding: 0x00 0x01 [0xFF padding] 0x00 [DigestInfo]
        var padded = new byte[256]; // 2048 bits = 256 bytes
        padded[0] = 0x00;
        padded[1] = 0x01;
        var paddingLength = 256 - digestInfo.Length - 3;
        for (int i = 2; i < 2 + paddingLength; i++)
        {
            padded[i] = 0xFF;
        }
        padded[2 + paddingLength] = 0x00;
        digestInfo.CopyTo(padded.AsSpan(3 + paddingLength));

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.Rsa2048,
            padded);

        // Verify the raw RSA signature with the software public key
        Assert.False(signature.IsEmpty);
        Assert.Equal(256, signature.Length);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task ImportKeyAsync_Rsa3072_ReturnsCorrectAlgorithm(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        using var softwareKey = RSA.Create(3072);
        var pkcs8 = softwareKey.ExportPkcs8PrivateKey();
        var privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8);

        var detectedAlgorithm = await session.ImportKeyAsync(
            PivSlot.Signature,
            privateKey);

        Assert.Equal(PivAlgorithm.Rsa3072, detectedAlgorithm);

        // Verify slot metadata shows key is present
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Signature);
        Assert.NotNull(metadata);
        Assert.Equal(PivAlgorithm.Rsa3072, metadata.Value.Algorithm);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task ImportKeyAsync_Ed25519_CanSignAndVerify(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Generate Ed25519 private key (32 random bytes)
        var ed25519Bytes = new byte[32];
        RandomNumberGenerator.Fill(ed25519Bytes);
        var privateKey = Curve25519PrivateKey.CreateFromValue(ed25519Bytes, Core.Cryptography.KeyType.Ed25519);

        var detectedAlgorithm = await session.ImportKeyAsync(
            PivSlot.Authentication,
            privateKey);

        Assert.Equal(PivAlgorithm.Ed25519, detectedAlgorithm);

        // Sign with YubiKey
        await session.VerifyPinAsync(DefaultPin);
        var dataToSign = SHA256.HashData("Ed25519 import test"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.Ed25519,
            dataToSign);

        Assert.False(signature.IsEmpty);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task ImportKeyAsync_X25519_ReturnsCorrectAlgorithm(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Generate X25519 private key with proper RFC 7748 bit clamping
        var x25519Bytes = new byte[32];
        RandomNumberGenerator.Fill(x25519Bytes);
        x25519Bytes[0] &= 248;   // Clear bits 0, 1, 2
        x25519Bytes[31] &= 127;  // Clear bit 255 (most significant)
        x25519Bytes[31] |= 64;   // Set bit 254
        var privateKey = Curve25519PrivateKey.CreateFromValue(x25519Bytes, Core.Cryptography.KeyType.X25519);

        var detectedAlgorithm = await session.ImportKeyAsync(
            PivSlot.KeyManagement,
            privateKey);

        Assert.Equal(PivAlgorithm.X25519, detectedAlgorithm);

        // Verify slot metadata
        var metadata = await session.GetSlotMetadataAsync(PivSlot.KeyManagement);
        Assert.NotNull(metadata);
        Assert.Equal(PivAlgorithm.X25519, metadata.Value.Algorithm);
    }
}
