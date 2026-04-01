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
using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Crypto;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP 2.3 encrypted metadata fields.
/// </summary>
[Trait("Category", "Integration")]
[Trait("RequiresFirmware", "5.7+")]
public class FidoEncryptedMetadataTests
{
    /// <summary>
    /// Tests that encIdentifier can be decrypted with PPUAT and yields the same
    /// plaintext across two separate sessions with different PPUATs.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido, MinFirmware = "5.7.0")]
    public async Task DecryptIdentifier_TwoSessions_SamePlaintext(YubiKeyTestState state)
    {
        // Session 1: Get encIdentifier and decrypt with PPUAT
        var (encIdentifier1, decryptedIdentifier1) = await GetEncryptedIdentifierWithDecryptionAsync(state);

        if (encIdentifier1 is null || decryptedIdentifier1 is null)
        {
            return;
        }

        // Session 2: Get encIdentifier and decrypt with different PPUAT
        var (encIdentifier2, decryptedIdentifier2) = await GetEncryptedIdentifierWithDecryptionAsync(state);

        Assert.NotNull(decryptedIdentifier2);

        Assert.False(
            encIdentifier1.Value.Span.SequenceEqual(encIdentifier2!.Value.Span),
            "Encrypted identifiers should differ across sessions due to different IVs");

        Assert.True(
            decryptedIdentifier1.AsSpan().SequenceEqual(decryptedIdentifier2),
            "Decrypted identifiers should match across sessions");
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido, MinFirmware = "5.8.0")]
    [Trait("RequiresFirmware", "5.8+")]
    public async Task DecryptCredStoreState_WithPpuat_Succeeds(YubiKeyTestState state)
    {
        var (encCredStoreState, decryptedState) = await GetEncryptedCredStoreStateWithDecryptionAsync(state);

        if (encCredStoreState is null || decryptedState is null)
        {
            return;
        }

        Assert.True(decryptedState.Length > 0, "Decrypted credential store state should not be empty");
        Assert.True(encCredStoreState.Value.Length >= 16,
            "Encrypted credential store state should contain at least IV");
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido, MinFirmware = "5.8.0")]
    [Trait("RequiresFirmware", "5.8+")]
    public async Task GetInfo_Firmware58Plus_BothEncryptedFieldsPresent(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.FirmwareVersion is null || info.FirmwareVersion.IsLessThan(5, 8, 0))
            {
                return;
            }

            Assert.True(info.EncIdentifier.HasValue, "EncIdentifier should be present on 5.8+ firmware");
            Assert.True(info.EncCredStoreState.HasValue, "EncCredStoreState should be present on 5.8+ firmware");

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            var ppuat = await GetPpuatAsync(session, clientPin);

            try
            {
                var decryptedIdentifier = EncryptedMetadataDecryptor.DecryptIdentifier(
                    ppuat,
                    info.EncIdentifier.Value.Span);

                var decryptedState = EncryptedMetadataDecryptor.DecryptCredStoreState(
                    ppuat,
                    info.EncCredStoreState.Value.Span);

                Assert.NotNull(decryptedIdentifier);
                Assert.NotNull(decryptedState);
                Assert.True(decryptedIdentifier.Length > 0, "Decrypted identifier should not be empty");
                Assert.True(decryptedState.Length > 0, "Decrypted credential store state should not be empty");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ppuat);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido, MinFirmware = "5.7.0")]
    public async Task DecryptIdentifier_WrongPpuat_ReturnsGarbage(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.EncIdentifier.HasValue)
            {
                return;
            }

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            var realPpuat = await GetPpuatAsync(session, clientPin);

            try
            {
                var correctDecryption = EncryptedMetadataDecryptor.DecryptIdentifier(
                    realPpuat,
                    info.EncIdentifier.Value.Span);

                Assert.NotNull(correctDecryption);

                Span<byte> fakePpuat = stackalloc byte[32];
                fakePpuat.Clear();

                var wrongDecryption = EncryptedMetadataDecryptor.DecryptIdentifier(
                    fakePpuat,
                    info.EncIdentifier.Value.Span);

                Assert.NotNull(wrongDecryption);
                Assert.False(
                    correctDecryption.AsSpan().SequenceEqual(wrongDecryption),
                    "Decryption with wrong PPUAT should produce different (garbage) output");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(realPpuat);
            }
        });

    private static async Task<(ReadOnlyMemory<byte>? EncIdentifier, byte[]? DecryptedIdentifier)>
        GetEncryptedIdentifierWithDecryptionAsync(YubiKeyTestState state)
    {
        ReadOnlyMemory<byte>? encIdentifierCopy = null;
        byte[]? decrypted = null;

        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.EncIdentifier.HasValue)
            {
                return;
            }

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            var ppuat = await GetPpuatAsync(session, clientPin);

            try
            {
                decrypted = EncryptedMetadataDecryptor.DecryptIdentifier(
                    ppuat,
                    info.EncIdentifier.Value.Span);

                encIdentifierCopy = info.EncIdentifier.Value.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ppuat);
            }
        });

        return (encIdentifierCopy, decrypted);
    }

    private static async Task<(ReadOnlyMemory<byte>? EncCredStoreState, byte[]? DecryptedState)>
        GetEncryptedCredStoreStateWithDecryptionAsync(YubiKeyTestState state)
    {
        ReadOnlyMemory<byte>? encCredStoreStateCopy = null;
        byte[]? decrypted = null;

        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.EncCredStoreState.HasValue)
            {
                return;
            }

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            var ppuat = await GetPpuatAsync(session, clientPin);

            try
            {
                decrypted = EncryptedMetadataDecryptor.DecryptCredStoreState(
                    ppuat,
                    info.EncCredStoreState.Value.Span);

                encCredStoreStateCopy = info.EncCredStoreState.Value.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ppuat);
            }
        });

        return (encCredStoreStateCopy, decrypted);
    }

    private static async Task<byte[]> GetPpuatAsync(IFidoSession session, ClientPin clientPin)
    {
        var info = await session.GetInfoAsync();

        var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                   info.Versions.Contains("FIDO_2_1_PRE");

        if (supportsPermissions)
        {
            return await clientPin.GetPinUvAuthTokenUsingPinAsync(
                FidoTestData.Pin,
                PinUvAuthTokenPermissions.CredentialManagementRO);
        }

        return await clientPin.GetPinTokenAsync(FidoTestData.Pin);
    }
}
