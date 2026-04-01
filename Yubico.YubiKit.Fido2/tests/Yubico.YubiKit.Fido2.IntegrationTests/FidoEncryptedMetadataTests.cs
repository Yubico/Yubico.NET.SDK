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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Crypto;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP 2.3 encrypted metadata fields.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that encIdentifier and encCredStoreState can be decrypted
/// using PPUAT-derived keys, and that decryption across different sessions yields
/// identical plaintext (proving the fields con1tain stable device identifiers).
/// </para>
/// <para>
/// Requires YubiKey firmware 5.7+ for encIdentifier, 5.8+ for encCredStoreState.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("RequiresFirmware", "5.7+")]
public class FidoEncryptedMetadataTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that encIdentifier can be decrypted with PPUAT and yields the same
    /// plaintext across two separate sessions with different PPUATs.
    /// </summary>
    /// <remarks>
    /// The encIdentifier is a unique device identifier that is encrypted to prevent
    /// tracking. When decrypted with the session's PPUAT, it should reveal the same
    /// underlying identifier regardless of which session decrypts it.
    /// </remarks>
    [Fact]
    public async Task DecryptIdentifier_TwoSessions_SamePlaintext()
    {
        // Session 1: Get encIdentifier and decrypt with PPUAT
        var (encIdentifier1, decryptedIdentifier1) = await GetEncryptedIdentifierWithDecryptionAsync();
        
        if (encIdentifier1 is null || decryptedIdentifier1 is null)
        {
            // Device doesn't support encrypted metadata (firmware < 5.7)
            return;
        }
        
        // Session 2: Get encIdentifier and decrypt with different PPUAT
        var (encIdentifier2, decryptedIdentifier2) = await GetEncryptedIdentifierWithDecryptionAsync();
        
        Assert.NotNull(decryptedIdentifier2);
        
        // The encrypted values should differ (different IVs per session)
        Assert.False(
            encIdentifier1.Value.Span.SequenceEqual(encIdentifier2!.Value.Span),
            "Encrypted identifiers should differ across sessions due to different IVs");
        
        // But the decrypted plaintext should be identical
        Assert.True(
            decryptedIdentifier1.AsSpan().SequenceEqual(decryptedIdentifier2),
            "Decrypted identifiers should match across sessions");
    }
    
    /// <summary>
    /// Tests that encCredStoreState can be decrypted with PPUAT successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per CTAP 2.3 spec: "Platforms can use encCredStoreState to tell if the state
    /// of the credential store in an authenticator has changed since the last time
    /// they were cached." Unlike encIdentifier (a stable device identifier), 
    /// encCredStoreState is a change-detection mechanism that may differ across
    /// sessions if credentials have been added, removed, or the device was reset.
    /// </para>
    /// <para>
    /// Therefore we only verify decryption succeeds, not that the plaintext 
    /// matches across sessions.
    /// </para>
    /// <para>
    /// Requires YubiKey 5.8+.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait("RequiresFirmware", "5.8+")]
    public async Task DecryptCredStoreState_WithPpuat_Succeeds()
    {
        var (encCredStoreState, decryptedState) = await GetEncryptedCredStoreStateWithDecryptionAsync();
        
        if (encCredStoreState is null || decryptedState is null)
        {
            // Device doesn't support encCredStoreState (firmware < 5.8)
            return;
        }
        
        // Verify decryption produced non-empty plaintext
        Assert.True(decryptedState.Length > 0, "Decrypted credential store state should not be empty");
        
        // The encrypted value should have IV (16 bytes) + ciphertext
        Assert.True(encCredStoreState.Value.Length >= 16, 
            "Encrypted credential store state should contain at least IV");
    }
    
    /// <summary>
    /// Tests that both encIdentifier and encCredStoreState are present on 5.8+ devices
    /// and can be decrypted successfully.
    /// </summary>
    [Fact]
    [Trait("RequiresFirmware", "5.8+")]
    public async Task GetInfo_Firmware58Plus_BothEncryptedFieldsPresent()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        if (device is null)
        {
            return; // No device available
        }
        
        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);
        
        var info = await session.GetInfoAsync();
        
        // Check firmware version
        if (info.FirmwareVersion is null || info.FirmwareVersion.IsLessThan(5, 8, 0))
        {
            // Not a 5.8+ device
            return;
        }
        
        // Both fields should be present
        Assert.True(info.EncIdentifier.HasValue, "EncIdentifier should be present on 5.8+ firmware");
        Assert.True(info.EncCredStoreState.HasValue, "EncCredStoreState should be present on 5.8+ firmware");
        
        // Get PPUAT and decrypt both
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
    }
    
    /// <summary>
    /// Tests that decryption with wrong PPUAT fails gracefully.
    /// </summary>
    [Fact]
    public async Task DecryptIdentifier_WrongPpuat_ReturnsGarbage()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        if (device is null)
        {
            return;
        }
        
        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);
        
        var info = await session.GetInfoAsync();
        
        if (!info.EncIdentifier.HasValue)
        {
            // Device doesn't support encrypted metadata
            return;
        }
        
        // Get the real PPUAT for comparison
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        var realPpuat = await GetPpuatAsync(session, clientPin);
        
        try
        {
            // Decrypt with real PPUAT
            var correctDecryption = EncryptedMetadataDecryptor.DecryptIdentifier(
                realPpuat,
                info.EncIdentifier.Value.Span);
            
            Assert.NotNull(correctDecryption);
            
            // Try with a fake PPUAT (all zeros)
            Span<byte> fakePpuat = stackalloc byte[32];
            fakePpuat.Clear();
            
            var wrongDecryption = EncryptedMetadataDecryptor.DecryptIdentifier(
                fakePpuat,
                info.EncIdentifier.Value.Span);
            
            // Decryption shouldn't fail (AES-CBC/NoPadding doesn't validate),
            // but the result should be garbage (different from correct decryption)
            Assert.NotNull(wrongDecryption);
            Assert.False(
                correctDecryption.AsSpan().SequenceEqual(wrongDecryption),
                "Decryption with wrong PPUAT should produce different (garbage) output");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(realPpuat);
        }
    }
    
    /// <summary>
    /// Helper: Creates a new session, gets AuthenticatorInfo, decrypts encIdentifier with PPUAT.
    /// </summary>
    private async Task<(ReadOnlyMemory<byte>? EncIdentifier, byte[]? DecryptedIdentifier)> 
        GetEncryptedIdentifierWithDecryptionAsync()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        if (device is null)
        {
            return (null, null);
        }
        
        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);
        
        var info = await session.GetInfoAsync();
        
        if (!info.EncIdentifier.HasValue)
        {
            return (null, null);
        }
        
        // Get PPUAT
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        var ppuat = await GetPpuatAsync(session, clientPin);
        
        try
        {
            var decrypted = EncryptedMetadataDecryptor.DecryptIdentifier(
                ppuat, 
                info.EncIdentifier.Value.Span);
            
            // Copy the encrypted value since connection will be disposed
            var encIdentifierCopy = info.EncIdentifier.Value.ToArray();
            
            return (encIdentifierCopy, decrypted);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ppuat);
        }
    }
    
    /// <summary>
    /// Helper: Creates a new session, gets AuthenticatorInfo, decrypts encCredStoreState with PPUAT.
    /// </summary>
    private async Task<(ReadOnlyMemory<byte>? EncCredStoreState, byte[]? DecryptedState)> 
        GetEncryptedCredStoreStateWithDecryptionAsync()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        if (device is null)
        {
            return (null, null);
        }
        
        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);
        
        var info = await session.GetInfoAsync();
        
        if (!info.EncCredStoreState.HasValue)
        {
            return (null, null);
        }
        
        // Get PPUAT
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        var ppuat = await GetPpuatAsync(session, clientPin);
        
        try
        {
            var decrypted = EncryptedMetadataDecryptor.DecryptCredStoreState(
                ppuat, 
                info.EncCredStoreState.Value.Span);
            
            // Copy the encrypted value since connection will be disposed
            var encCredStoreStateCopy = info.EncCredStoreState.Value.ToArray();
            
            return (encCredStoreStateCopy, decrypted);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ppuat);
        }
    }
    
    /// <summary>
    /// Helper: Gets the PPUAT (Persistent PIN/UV Auth Token) for the current session.
    /// </summary>
    /// <remarks>
    /// Uses CredentialManagementRO (0x40) permission which is sufficient for reading
    /// encrypted metadata (encIdentifier, encCredStoreState) without requiring full
    /// credential management permissions.
    /// </remarks>
    private static async Task<byte[]> GetPpuatAsync(IFidoSession session, ClientPin clientPin)
    {
        var info = await session.GetInfoAsync();
        
        // Check if device supports CTAP 2.1 permissions
        var supportsPermissions = info.Versions.Contains("FIDO_2_1") || 
                                   info.Versions.Contains("FIDO_2_1_PRE");
        
        if (supportsPermissions)
        {
            // Get PIN/UV auth token with read-only credential management permission
            // This is sufficient for decrypting encrypted metadata
            return await clientPin.GetPinUvAuthTokenUsingPinAsync(
                FidoTestData.Pin,
                PinUvAuthTokenPermissions.CredentialManagementRO);
        }
        
        // Fallback to basic PIN token
        return await clientPin.GetPinTokenAsync(FidoTestData.Pin);
    }
}
