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
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="IPivSession.DecryptAsync"/>.
///
/// DecryptAsync models the Python yubikey-manager PivSession.decrypt() API:
/// the session owns the full decrypt-and-unpad operation, returning clean
/// plaintext rather than raw RSA output with padding bytes intact.
///
/// COVERAGE:
///   - PKCS#1 v1.5 decryption (RSA 2048)
///   - OAEP SHA-256 decryption (RSA 2048)
///   - Wrong key type rejection (ECC slot → ArgumentException before APDU)
///   - Wrong ciphertext length rejection (ArgumentException before APDU)
/// </summary>
public class PivDecryptTests
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
    /// Verifies that DecryptAsync strips PKCS#1 v1.5 encryption padding and
    /// returns the exact original plaintext — mirroring Python's session.decrypt().
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task DecryptAsync_Rsa2048Pkcs1_ReturnsExactPlaintext(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();

        await EncryptThenDecryptRoundTrip(
            session,
            state.FirmwareVersion,
            "Hello from decrypt test!"u8.ToArray(),
            RSAEncryptionPadding.Pkcs1);
    }

    /// <summary>
    /// Verifies OAEP SHA-256 padding is also correctly stripped at the session layer.
    /// OAEP is the recommended modern padding; PKCS#1 v1.5 is provided for legacy compatibility.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task DecryptAsync_Rsa2048OaepSha256_ReturnsExactPlaintext(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();

        await EncryptThenDecryptRoundTrip(
            session,
            state.FirmwareVersion,
            "OAEP test payload"u8.ToArray(),
            RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// Verifies that DecryptAsync rejects ECC slots with ArgumentException before
    /// sending any APDU to the YubiKey, matching the Python API's slot validation.
    /// </summary>
    [Theory]
    // ECC P256 is supported since firmware 4.0.0 (no MinFirmware constraint needed — ECC is universally available)
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.0.0")]
    public async Task DecryptAsync_EccSlot_ThrowsArgumentException(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await ResetAndAuthenticate(session, state.FirmwareVersion);

        // Generate an ECC key so the slot has metadata (not empty)
        await session.GenerateKeyAsync(PivSlot.KeyManagement, PivAlgorithm.EccP256);

        var fakeCiphertext = new byte[32]; // wrong size, wrong key type -- error fires on type check

        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.DecryptAsync(
                PivSlot.KeyManagement,
                fakeCiphertext,
                RSAEncryptionPadding.Pkcs1));
    }

    /// <summary>
    /// Verifies that DecryptAsync rejects ciphertext whose length does not match
    /// the RSA key size, preventing malformed inputs from reaching the YubiKey.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task DecryptAsync_WrongCiphertextLength_ThrowsArgumentException(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await ResetAndAuthenticate(session, state.FirmwareVersion);

        await session.GenerateKeyAsync(PivSlot.KeyManagement, PivAlgorithm.Rsa2048);

        // RSA 2048 expects exactly 256 bytes; pass 100 instead
        var shortCiphertext = new byte[100];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.DecryptAsync(
                PivSlot.KeyManagement,
                shortCiphertext,
                RSAEncryptionPadding.Pkcs1));
    }

    /// <summary>
    /// Verifies DecryptAsync works for RSA 4096 keys, exercising the key-size branch in
    /// the session-layer padding removal (complementary to the RSA 2048 round-trip tests).
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DecryptAsync_Rsa4096Pkcs1_ReturnsExactPlaintext(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await ResetAndAuthenticate(session, state.FirmwareVersion);

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement,
            PivAlgorithm.Rsa4096);
        await session.VerifyPinAsync(DefaultPin);

        var plaintext = "RSA 4096 decrypt test"u8.ToArray();

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        byte[] ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.Pkcs1);

        var decrypted = await session.DecryptAsync(
            PivSlot.KeyManagement,
            ciphertext,
            RSAEncryptionPadding.Pkcs1);

        Assert.Equal(plaintext, decrypted.ToArray());
    }

    /// <summary>
    /// Resets the PIV application and authenticates with the default management key.
    /// Shared setup for all tests that need a clean, authenticated session.
    /// </summary>
    private static async Task ResetAndAuthenticate(IPivSession session, FirmwareVersion firmwareVersion)
    {
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(firmwareVersion));
    }

    /// <summary>
    /// Generates an RSA 2048 key, encrypts plaintext with the given padding using
    /// the host-side public key, then calls DecryptAsync and asserts round-trip equality.
    /// </summary>
    private static async Task EncryptThenDecryptRoundTrip(
        IPivSession session,
        FirmwareVersion firmwareVersion,
        byte[] plaintext,
        RSAEncryptionPadding padding)
    {
        await ResetAndAuthenticate(session, firmwareVersion);

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement,
            PivAlgorithm.Rsa2048);
        await session.VerifyPinAsync(DefaultPin);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        byte[] ciphertext = rsa.Encrypt(plaintext, padding);

        var decrypted = await session.DecryptAsync(
            PivSlot.KeyManagement,
            ciphertext,
            padding);

        Assert.Equal(plaintext, decrypted.ToArray());
    }
}