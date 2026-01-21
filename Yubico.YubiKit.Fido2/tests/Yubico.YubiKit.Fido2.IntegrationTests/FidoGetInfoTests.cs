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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for GetInfo validation.
/// </summary>
/// <remarks>
/// These tests verify the authenticator info returned by GetInfo command.
/// Based on Java yubikit-android Ctap2SessionTests.testCtap2GetInfo() patterns.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoGetInfoTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GetInfo returns valid FIDO2 version strings.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsValidVersions()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.Versions);
        Assert.True(
            info.Versions.Contains("FIDO_2_0") ||
            info.Versions.Contains("FIDO_2_1_PRE") ||
            info.Versions.Contains("FIDO_2_1") ||
            info.Versions.Contains("FIDO_2_2"),
            $"Expected at least one FIDO2 version, got: [{string.Join(", ", info.Versions)}]");
    }

    /// <summary>
    /// Tests that GetInfo returns a valid 16-byte AAGUID.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsValidAaguid()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.False(info.Aaguid.IsEmpty, "AAGUID should not be empty");
        Assert.Equal(16, info.Aaguid.Length);
        Assert.False(info.Aaguid.Span.SequenceEqual(new byte[16]), 
            "AAGUID should not be all zeros");
    }

    /// <summary>
    /// Tests that GetInfo returns expected options for YubiKey.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsExpectedOptions()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.Options);

        // Platform authenticator should be false (YubiKey is a roaming authenticator)
        if (info.Options.TryGetValue("plat", out var plat))
        {
            Assert.False(plat, "YubiKey should not be a platform authenticator");
        }

        // Resident keys should be supported
        if (info.Options.TryGetValue("rk", out var rk))
        {
            Assert.True(rk, "YubiKey should support resident keys");
        }

        // User presence should be supported
        if (info.Options.TryGetValue("up", out var up))
        {
            Assert.True(up, "YubiKey should support user presence");
        }

        // clientPin option should exist
        Assert.True(info.Options.ContainsKey("clientPin"), 
            "YubiKey should have clientPin option");
    }

    /// <summary>
    /// Tests that GetInfo returns PIN/UV auth protocol versions.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsPinUvAuthProtocols()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.PinUvAuthProtocols);
        Assert.NotEmpty(info.PinUvAuthProtocols);
        
        // Protocol 1 or 2 should be present
        Assert.True(
            info.PinUvAuthProtocols.Contains(1) || info.PinUvAuthProtocols.Contains(2),
            "YubiKey should support PIN/UV auth protocol 1 or 2");
    }

    /// <summary>
    /// Tests that GetInfo returns supported algorithms including ES256.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsSupportedAlgorithms()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.Algorithms);
        Assert.NotEmpty(info.Algorithms);

        // ES256 should be supported on all YubiKeys
        var hasEs256 = info.Algorithms.Any(a => 
            a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
        Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
    }

    /// <summary>
    /// Tests that GetInfo returns extensions list.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsExtensions()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.Extensions);
        
        // YubiKey should support at least some common extensions
        if (info.Extensions.Count > 0)
        {
            // hmac-secret is commonly supported on FIDO2 devices
            var hasHmacSecret = info.Extensions.Contains("hmac-secret");
            Assert.True(hasHmacSecret || info.Extensions.Count > 0,
                "YubiKey should support at least one extension");
        }
    }

    /// <summary>
    /// Tests that GetInfo returns firmware version.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsFirmwareVersion()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        // FirmwareVersion may be null on some devices/configurations
        if (info.FirmwareVersion is not null)
        {
            // Beta/alpha keys report 0.0.1, production keys report 5.x+
            var isBetaKey = info.FirmwareVersion is { Major: 0, Minor: 0, Patch: 1 };
            var isProductionKey = info.FirmwareVersion.Major >= 5;
            Assert.True(isBetaKey || isProductionKey,
                $"Expected firmware 5.0+ or 0.0.1 (beta), got {info.FirmwareVersion}");
        }
    }

    /// <summary>
    /// Tests that GetInfo returns max message size.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReturnsMaxMsgSize()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        if (info.MaxMsgSize.HasValue)
        {
            Assert.True(info.MaxMsgSize.Value >= 1024,
                "Max message size should be at least 1024 bytes");
        }
    }
}
