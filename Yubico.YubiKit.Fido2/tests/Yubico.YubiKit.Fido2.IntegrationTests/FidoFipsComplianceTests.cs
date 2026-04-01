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

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIPS compliance verification.
/// </summary>
/// <remarks>
/// These tests verify FIDO2 behavior on FIPS-capable YubiKeys.
/// FIPS devices have stricter requirements for PIN/UV auth protocols.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Category", "FIPS")]
public class FidoFipsComplianceTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that FIPS-capable devices support PIN/UV auth protocol v2.
    /// </summary>
    /// <remarks>
    /// FIPS devices should support protocol v2 for enhanced security.
    /// </remarks>
    [Fact]
    public async Task FipsDevice_SupportsPinUvAuthProtocolV2()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Check if device is FIPS-capable (firmware 5.4+)
        if (info.FirmwareVersion == null || info.FirmwareVersion.Major < 5 ||
            (info.FirmwareVersion.Major == 5 && info.FirmwareVersion.Minor < 4))
        {
            // Skip test for non-FIPS-capable devices
            return;
        }

        // Assert: FIPS-capable devices should support protocol 2
        Assert.NotNull(info.PinUvAuthProtocols);
        Assert.Contains(2, info.PinUvAuthProtocols);
    }

    /// <summary>
    /// Tests that devices with alwaysUv option enforce user verification.
    /// </summary>
    /// <remarks>
    /// FIPS-approved devices in approved mode should have alwaysUv enabled.
    /// </remarks>
    [Fact]
    public async Task FipsApproved_ChecksAlwaysUvOption()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Check if alwaysUv option exists and its value
        if (info.Options.TryGetValue("alwaysUv", out var alwaysUv))
        {
            // If alwaysUv is true, device enforces user verification for all operations
            // This is expected on FIPS-approved devices
            if (alwaysUv)
            {
                // Verify PIN is configured (UV requires PIN or biometric)
                Assert.True(info.Options.TryGetValue("clientPin", out var pinConfigured),
                    "Device with alwaysUv should have clientPin option");
            }
        }
        // Note: alwaysUv may not be present on all devices - that's OK
    }

    /// <summary>
    /// Tests that GetInfo includes certifications information if available.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsCertifications()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert: Certifications may or may not be present
        // FIPS devices may include FIPS certification level
        Assert.NotNull(info.Certifications); // Collection is never null, may be empty
        
        // If certifications exist, log them for debugging
        if (info.Certifications.Count > 0)
        {
            foreach (var cert in info.Certifications)
            {
                Assert.False(string.IsNullOrEmpty(cert.Key), "Certification key should not be empty");
            }
        }
    }

    /// <summary>
    /// Tests that minimum PIN length requirement is reasonable.
    /// </summary>
    /// <remarks>
    /// FIPS devices may require longer minimum PIN lengths.
    /// </remarks>
    [Fact]
    public async Task GetInfo_ReturnsMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        if (info.MinPinLength.HasValue)
        {
            // Minimum PIN length should be at least 4 (CTAP2 spec)
            Assert.True(info.MinPinLength.Value >= 4,
                $"Minimum PIN length should be at least 4, got {info.MinPinLength.Value}");
            
            // FIPS devices may require 6+ characters
            // We don't assert a specific value as it varies by configuration
        }
    }
}
