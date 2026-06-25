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

/// <summary>
/// Integration tests for cross-key-type slot overwriting.
///
/// Verifies that generating a new key in a slot that already contains a key
/// of a different algorithm type correctly overwrites the old key and updates
/// the slot metadata to reflect the new algorithm.
/// </summary>
public class PivSlotOverwriteTests
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

    /// <summary>
    /// Generate RSA 2048 in a slot, then overwrite with ECC P-256. Verify via
    /// slot metadata that the algorithm changed, and that the new ECC key is
    /// functional for signing.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_RsaThenEcc_OverwritesWithEcc(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Step 1: Generate RSA 2048 key
        var rsaPublicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication,
            PivAlgorithm.Rsa2048);

        Assert.NotNull(rsaPublicKey);
        Assert.IsType<RSAPublicKey>(rsaPublicKey);

        // Verify metadata shows RSA 2048
        var metadataRsa = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadataRsa);
        Assert.Equal(PivAlgorithm.Rsa2048, metadataRsa.Value.Algorithm);

        // Step 2: Overwrite with ECC P-256
        var eccPublicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256);

        Assert.NotNull(eccPublicKey);
        Assert.IsType<ECPublicKey>(eccPublicKey);

        // Verify metadata now shows ECC P-256
        var metadataEcc = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadataEcc);
        Assert.Equal(PivAlgorithm.EccP256, metadataEcc.Value.Algorithm);

        // Verify the ECC key is functional
        await session.VerifyPinAsync(DefaultPin);
        var hash = SHA256.HashData("overwrite test"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            hash);

        Assert.False(signature.IsEmpty);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)eccPublicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    /// <summary>
    /// Generate ECC P-256 in a slot, then overwrite with RSA 2048. Verify via
    /// slot metadata that the algorithm changed, and that the new RSA key is
    /// functional for signing.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_EccThenRsa_OverwritesWithRsa(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Step 1: Generate ECC P-256 key
        var eccPublicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256);

        Assert.NotNull(eccPublicKey);
        Assert.IsType<ECPublicKey>(eccPublicKey);

        // Verify metadata shows ECC P-256
        var metadataEcc = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadataEcc);
        Assert.Equal(PivAlgorithm.EccP256, metadataEcc.Value.Algorithm);

        // Step 2: Overwrite with RSA 2048
        var rsaPublicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication,
            PivAlgorithm.Rsa2048);

        Assert.NotNull(rsaPublicKey);
        Assert.IsType<RSAPublicKey>(rsaPublicKey);

        // Verify metadata now shows RSA 2048
        var metadataRsa = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadataRsa);
        Assert.Equal(PivAlgorithm.Rsa2048, metadataRsa.Value.Algorithm);

        // Verify the RSA key is functional
        await session.VerifyPinAsync(DefaultPin);

        // SHA-256 DigestInfo prefix
        byte[] sha256DigestInfo =
        [
            0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
            0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05,
            0x00, 0x04, 0x20
        ];

        var dataToSign = "overwrite RSA test"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        // Build PKCS#1 v1.5 padding
        var padded = new byte[KeyDefinitions.RSA2048.LengthInBytes];
        padded[0] = 0x00;
        padded[1] = 0x01;
        var paddingLength = padded.Length - 3 - sha256DigestInfo.Length - hash.Length;
        for (var i = 2; i < 2 + paddingLength; i++)
        {
            padded[i] = 0xFF;
        }
        padded[2 + paddingLength] = 0x00;
        Array.Copy(sha256DigestInfo, 0, padded, 3 + paddingLength, sha256DigestInfo.Length);
        Array.Copy(hash, 0, padded, 3 + paddingLength + sha256DigestInfo.Length, hash.Length);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.Rsa2048,
            padded);

        Assert.Equal(KeyDefinitions.RSA2048.LengthInBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)rsaPublicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
}
