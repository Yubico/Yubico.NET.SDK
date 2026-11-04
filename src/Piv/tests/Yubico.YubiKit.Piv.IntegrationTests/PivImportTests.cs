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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ImportKeyAsync_Rsa2048_CanSignAndVerify(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        using var softwareKey = RSA.Create(2048);
        using var privateKey = ExportForImport(softwareKey);

        await session.ImportKeyAsync(
            Slot.Authentication,
            privateKey);


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
            Slot.Authentication,
            KeyType.Rsa2048,
            padded);

        // Verify the raw RSA signature with the software public key
        Assert.False(signature.IsEmpty);
        Assert.Equal(256, signature.Length);
        Assert.True(softwareKey.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task ImportKeyAsync_Rsa3072_MetadataMatchesImportedKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        using var softwareKey = RSA.Create(3072);
        using var privateKey = ExportForImport(softwareKey);

        await session.ImportKeyAsync(
            Slot.Signature,
            privateKey);


        // Verify slot metadata shows key is present
        var metadata = await session.GetSlotMetadataAsync(Slot.Signature);
        Assert.NotNull(metadata);
        Assert.Equal(KeyType.Rsa3072, metadata.Value.KeyType);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task ImportKeyAsync_Ed25519_CanSignAndVerify(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var ed25519Bytes = new byte[32];
        RandomNumberGenerator.Fill(ed25519Bytes);
        try
        {
            using var privateKey = Curve25519PrivateKey.CreateFromValue(
                ed25519Bytes,
                Core.Cryptography.KeyType.Ed25519);

            await session.ImportKeyAsync(
                Slot.Authentication,
                privateKey);


            await session.VerifyPinAsync(DefaultPin);
            var dataToSign = SHA256.HashData("Ed25519 import test"u8);
            var signature = await session.SignOrDecryptAsync(
                Slot.Authentication,
                KeyType.Ed25519,
                dataToSign);

            Assert.False(signature.IsEmpty);
            var metadata = await session.GetSlotMetadataAsync(Slot.Authentication);
            Assert.NotNull(metadata);
            Assert.Equal(KeyType.Ed25519, metadata.Value.KeyType);
            Assert.False(metadata.Value.IsGenerated);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ed25519Bytes);
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task ImportKeyAsync_X25519_MetadataMatchesImportedKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var x25519Bytes = Convert.FromHexString(
            "77076D0A7318A57D3C16C17251B26645DF4C2F87EBC0992AB177FBA51DB92C2A");
        try
        {
            using var privateKey = Curve25519PrivateKey.CreateFromValue(
                x25519Bytes,
                Core.Cryptography.KeyType.X25519);

            await session.ImportKeyAsync(
                Slot.KeyManagement,
                privateKey);


            // Verify slot metadata
            var metadata = await session.GetSlotMetadataAsync(Slot.KeyManagement);
            Assert.NotNull(metadata);
            Assert.Equal(KeyType.X25519, metadata.Value.KeyType);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(x25519Bytes);
        }
    }
    private static RSAPrivateKey ExportForImport(RSA rsa)
    {
        byte[] encoded = rsa.ExportPkcs8PrivateKey();
        try
        {
            return RSAPrivateKey.CreateFromPkcs8(encoded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
        }
    }
}