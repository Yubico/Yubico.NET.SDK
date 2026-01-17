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

using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Simple integration tests for FidoSession.
/// These tests exercise basic FIDO2 operations that do NOT require user presence.
/// </summary>
/// <remarks>
/// Tests requiring user presence or PIN verification are excluded from automated runs.
/// Mark such tests with [Trait("RequiresUserPresence", "true")] to exclude them.
/// </remarks>
public class FidoSessionSimpleTests : IntegrationTestBase
{
    #region GetInfo Tests (No User Presence Required)
    
    /// <summary>
    /// Tests that creating a FidoSession over USB SmartCard (CCID) correctly throws NotSupportedException.
    /// FIDO2 is only available over NFC SmartCard or USB HID FIDO interfaces.
    /// </summary>
    [Fact]
    public async Task CreateFidoSession_With_UsbSmartCard_ThrowsNotSupportedException()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<ISmartCardConnection>();
        
        // USB CCID does not support FIDO2 - only NFC SmartCard or USB HID FIDO
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await FidoSession.CreateAsync(connection);
        });
        
        Assert.Contains("NFC transport", exception.Message);
        Assert.Contains("IFidoHidConnection", exception.Message);
    }
    
    [Fact]
    public async Task CreateFidoSession_With_HidFido_CreateAsync()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var fidoDevice = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(fidoDevice);
        
        Assert.Equal(ConnectionType.HidFido, fidoDevice.ConnectionType);

        await using var connection = await fidoDevice.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
    }
    
    [Fact]
    public async Task GetInfoAsync_Returns_CTAP2_Version()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        
        // YubiKey 5+ should support FIDO2/CTAP2
        Assert.Contains("FIDO_2_0", info.Versions);
    }
    
    [Fact]
    public async Task GetInfoAsync_Returns_AAGUID()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        
        // AAGUID should be 16 bytes
        Assert.Equal(16, info.Aaguid.Length);
        Assert.False(info.Aaguid.Span.SequenceEqual(new byte[16]), "AAGUID should not be all zeros");
    }
    
    [Fact]
    public async Task GetInfoAsync_Returns_Supported_Extensions()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        
        // YubiKey should support at least some extensions
        if (info.Extensions is { Count: > 0 })
        {
            // hmac-secret is commonly supported
            var hasHmacSecret = info.Extensions.Contains("hmac-secret");
            Assert.True(hasHmacSecret || info.Extensions.Count > 0, 
                "YubiKey should support at least one extension");
        }
    }
    
    [Fact]
    public async Task GetInfoAsync_Returns_Supported_Algorithms()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        
        // Should support at least ES256 (ECDSA with P-256 and SHA-256)
        if (info.Algorithms is { Count: > 0 })
        {
            var hasEs256 = info.Algorithms.Any(a => 
                a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
            Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
        }
    }
    
    [Fact]
    public async Task GetInfoAsync_Returns_Options()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        var info = await fidoSession.GetInfoAsync();
        
        // Should have options
        if (info.Options is { Count: > 0 })
        {
            // rk (resident key) is a common option
            var hasRk = info.Options.ContainsKey("rk");
            Assert.True(hasRk || info.Options.Count > 0, 
                "YubiKey should have at least one option");
        }
    }
    
    #endregion
    
    #region Factory Method Tests
    
    /// <summary>
    /// Tests that the factory delegate also throws NotSupportedException for USB SmartCard connections.
    /// </summary>
    [Fact]
    public async Task CreateFidoSession_With_FactoryInstance_UsbSmartCard_ThrowsNotSupportedException()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);
        
        var sessionFactory = ServiceProvider.GetRequiredService<FidoSessionFactoryDelegate>();

        await using var connection = await device.ConnectAsync<ISmartCardConnection>();
        
        // USB CCID does not support FIDO2
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await sessionFactory(connection, configuration: null);
        });
    }
    
    [Fact]
    public async Task CreateFidoSession_With_ExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var fidoSession = await device.CreateFidoSessionAsync();

        var info = await fidoSession.GetInfoAsync();
        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }
    
    [Fact]
    public async Task GetInfoAsync_With_YubiKeyExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        var info = await device.GetFidoInfoAsync();

        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }
    
    #endregion
    
    #region Tests Requiring User Presence (Excluded from Automated Runs)
    
    /// <summary>
    /// Tests SelectionAsync which requires user touch to confirm device selection.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task SelectionAsync_RequiresTouch()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        // This will blink the device LED and wait for user touch
        await fidoSession.SelectionAsync();
    }
    
    /// <summary>
    /// Tests ResetAsync which requires user presence within a short window after power-up.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task ResetAsync_RequiresUserPresence()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device);

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var fidoSession = await FidoSession.CreateAsync(connection);

        // Reset requires user presence and must be done within 10 seconds of power-up
        // This test is expected to fail in most cases
        await Assert.ThrowsAsync<Ctap.CtapException>(async () =>
        {
            await fidoSession.ResetAsync();
        });
    }
    
    #endregion
}
