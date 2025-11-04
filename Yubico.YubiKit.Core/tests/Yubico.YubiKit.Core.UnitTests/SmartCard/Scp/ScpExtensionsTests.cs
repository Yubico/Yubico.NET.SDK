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

using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.UnitTests.SmartCard.Fakes;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Tests firmware version validation in ScpExtensions.WithScpAsync.
///     Verifies that appropriate NotSupportedException is thrown for unsupported firmware versions.
/// </summary>
public class ScpExtensionsTests
{
    private readonly FakeSmartCardConnection _fakeConnection = new();
    private readonly NullLogger<PcscProtocol> _logger = NullLogger<PcscProtocol>.Instance;

    [Fact]
    public async Task WithScpAsync_Scp03_FirmwareBelow530_ThrowsNotSupportedException()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 2, 9)); // Just below 5.3.0

        using var staticKeys = StaticKeys.GetDefaultKeys();
        var keyParams = new Scp03KeyParams(new KeyRef(0x01, 0xFF), staticKeys);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await protocol.WithScpAsync(keyParams));

        Assert.Contains("SCP03", ex.Message);
        Assert.Contains("5.3.0", ex.Message);
    }

    [Fact]
    public async Task WithScpAsync_Scp03_FirmwareExactly530_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 3)); // Exactly 5.3.0

        using var staticKeys = StaticKeys.GetDefaultKeys();
        var keyParams = new Scp03KeyParams(new KeyRef(0x01, 0xFF), staticKeys);

        // Act & Assert
        // Should proceed to initialization (will fail because fake connection doesn't respond,
        // but we're only testing that firmware check passes)
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware
        Assert.IsNotType<NotSupportedException>(ex);
    }

    [Fact]
    public async Task WithScpAsync_Scp03_FirmwareAbove530_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 4, 3)); // Above 5.3.0

        using var staticKeys = StaticKeys.GetDefaultKeys();
        var keyParams = new Scp03KeyParams(new KeyRef(0x01, 0xFF), staticKeys);

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware
        Assert.IsNotType<NotSupportedException>(ex);
    }

    [Fact]
    public async Task WithScpAsync_Scp11_FirmwareBelow572_ThrowsNotSupportedException()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 7, 1)); // Just below 5.7.2

        // Create minimal SCP11b params (simplest variant)
        using var ecdh = ECDiffieHellman.Create();
        var keyParams = new Scp11KeyParams(
            new KeyRef(ScpKid.SCP11b, 0xFF),
            ecdh.PublicKey
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await protocol.WithScpAsync(keyParams));

        Assert.Contains("SCP11", ex.Message);
        Assert.Contains("5.7.2", ex.Message);
    }

    [Fact]
    public async Task WithScpAsync_Scp11_FirmwareExactly572_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 7, 2)); // Exactly 5.7.2

        using var ecdh = ECDiffieHellman.Create();
        var keyParams = new Scp11KeyParams(
            new KeyRef(ScpKid.SCP11b, 0xFF),
            ecdh.PublicKey
        );

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware
        Assert.IsNotType<NotSupportedException>(ex);
    }

    [Fact]
    public async Task WithScpAsync_Scp11_FirmwareAbove572_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        protocol.Configure(new FirmwareVersion(5, 8)); // Above 5.7.2

        using var ecdh = ECDiffieHellman.Create();
        var keyParams = new Scp11KeyParams(
            new KeyRef(ScpKid.SCP11b, 0xFF),
            ecdh.PublicKey
        );

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware
        Assert.IsNotType<NotSupportedException>(ex);
    }

    [Fact]
    public async Task WithScpAsync_Scp03_UnknownFirmware_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        // Don't call Configure - firmware version remains null

        using var staticKeys = StaticKeys.GetDefaultKeys();
        var keyParams = new Scp03KeyParams(new KeyRef(0x01, 0xFF), staticKeys);

        // Act & Assert
        // Should proceed to initialization (will fail, but not due to firmware check)
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware requirements
        if (ex is NotSupportedException notSupported)
            // If it IS NotSupportedException, it should be from device (CLA_NOT_SUPPORTED),
            // not from firmware version check
            Assert.DoesNotContain("5.3.0", notSupported.Message);
    }

    [Fact]
    public async Task WithScpAsync_Scp11_UnknownFirmware_Proceeds()
    {
        // Arrange
        ISmartCardProtocol protocol = new PcscProtocol(_logger, _fakeConnection);
        // Don't call Configure - firmware version remains null

        using var ecdh = ECDiffieHellman.Create();
        var keyParams = new Scp11KeyParams(
            new KeyRef(ScpKid.SCP11b, 0xFF),
            ecdh.PublicKey
        );

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await protocol.WithScpAsync(keyParams));

        // Should NOT be NotSupportedException about firmware requirements
        if (ex is NotSupportedException notSupported) Assert.DoesNotContain("5.7.2", notSupported.Message);
    }
}