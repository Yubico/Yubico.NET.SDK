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

using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for enhanced PIN complexity requirements.
/// </summary>
/// <remarks>
/// These tests verify FIDO2 behavior on YubiKeys with enhanced PIN complexity requirements.
/// Enhanced PIN complexity requires firmware 5.8.0+ and may not be enabled on all devices.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoEnhancedPinTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that a compliant PIN (mixed case + numbers) succeeds on devices with
    /// enhanced PIN complexity enabled.
    /// </summary>
    /// <remarks>
    /// This test uses FidoTestData.Pin which meets enhanced complexity requirements.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task EnhancedPin_CompliantPin_Succeeds()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check if enhanced PIN complexity is available (firmware 5.8.0+)
        var info = await session.GetInfoAsync();
        if (info.FirmwareVersion == null || info.FirmwareVersion.Major < 5 ||
            (info.FirmwareVersion.Major == 5 && info.FirmwareVersion.Minor < 8))
        {
            // Skip test for devices without enhanced PIN support
            return;
        }

        // Use the enhanced-complexity-compliant PIN
        await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin); // "Abc12345"

        // Verify PIN was accepted by checking clientPin option
        var updatedInfo = await session.GetInfoAsync();
        Assert.True(updatedInfo.Options.TryGetValue("clientPin", out var pinSet) && pinSet,
            "PIN should be configured after SetOrVerifyPinAsync");
    }

    /// <summary>
    /// Tests that PIN complexity info can be retrieved.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsPinComplexityInfo()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Check if setMinPINLength option exists (FIDO2.1 feature)
        if (info.Options.TryGetValue("setMinPINLength", out var setMinPinLength))
        {
            // If true, authenticator supports setting minimum PIN length
            Assert.IsType<bool>(setMinPinLength);
        }

        // Check if minPINLength is present
        if (info.MinPinLength.HasValue)
        {
            // Standard FIDO2 minimum is 4
            Assert.True(info.MinPinLength.Value >= 4,
                $"Minimum PIN length should be at least 4, got {info.MinPinLength.Value}");
        }
    }

    /// <summary>
    /// Tests that forcePINChange option is respected.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsForcePinChangeOption()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Check if forcePINChange is present (FIDO2.1 feature)
        // This indicates if PIN change is required
        if (info.ForcePinChange.HasValue)
        {
            Assert.IsType<bool>(info.ForcePinChange.Value);
            // Just verify we can read the value - don't assert specific value
        }
    }

    /// <summary>
    /// Tests that maxRPIDsForSetMinPINLength is returned correctly.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsMaxRpIdsForSetMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // This FIDO2.1 field indicates max RPs for which min PIN length can be set
        if (info.MaxRpidsForSetMinPinLength.HasValue)
        {
            Assert.True(info.MaxRpidsForSetMinPinLength.Value >= 0,
                "MaxRpidsForSetMinPinLength should be non-negative");
        }
    }

    /// <summary>
    /// Tests that PIN retry counter can be retrieved.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task ClientPin_RetrievesPinRetries()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check PIN is configured
        var info = await session.GetInfoAsync();
        if (!info.Options.TryGetValue("clientPin", out var pinConfigured) || !pinConfigured)
        {
            // PIN not configured - set it first
            await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        }

        // Get PIN retries
        IPinUvAuthProtocol protocol = info.PinUvAuthProtocols.Contains(2)
            ? new PinUvAuthProtocolV2()
            : new PinUvAuthProtocolV1();
        var clientPin = new ClientPin(session, protocol);
        var (retries, powerCycleState) = await clientPin.GetPinRetriesAsync();

        // Assert
        Assert.True(retries >= 0, "PIN retries should be non-negative");
        Assert.True(retries <= 8, "PIN retries should not exceed 8");
        // powerCycleState may or may not be present depending on device state
    }
}
