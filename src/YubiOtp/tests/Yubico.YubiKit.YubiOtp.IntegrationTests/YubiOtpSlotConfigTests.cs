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

using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using Xunit;

namespace Yubico.YubiKit.YubiOtp.IntegrationTests;

/// <summary>
///     Integration tests for YubiOTP slot configurations: static password,
///     Yubico OTP, HOTP, and touch-triggered OTP modes.
/// </summary>
public class YubiOtpSlotConfigTests
{
    /// <summary>
    ///     Programs slot 2 with a static password configuration using keyboard scan codes,
    ///     verifies the slot is configured, then deletes it and verifies cleanup.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task PutConfiguration_StaticPassword_ConfiguresAndDeletesSlot(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync<IOtpHidConnection>();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Simple scan codes representing a static password (US keyboard layout)
        // These are USB HID scan codes, not ASCII characters
        byte[] scanCodes = [0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B]; // "abcdefgh"

        using var config = new StaticPasswordSlotConfiguration(scanCodes);

        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            // Verify slot 2 is now configured
            var configState = session.GetConfigState();
            Assert.True(configState.IsConfigured(Slot.Two));
        }
        finally
        {
            // Clean up: delete slot 2
            await session.DeleteSlotAsync(Slot.Two);

            var stateAfterDelete = session.GetConfigState();
            Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
        }
    }

    /// <summary>
    ///     Programs slot 2 with a classic Yubico OTP configuration using a public ID,
    ///     private ID, and AES key. Verifies the slot is configured, then cleans up.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task PutConfiguration_YubicoOtp_ConfiguresAndDeletesSlot(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync<IOtpHidConnection>();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Public ID (modhex-encoded, up to 16 bytes)
        byte[] publicId = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];

        // Private ID (exactly 6 bytes)
        byte[] privateId = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66];

        // AES-128 key (exactly 16 bytes)
        byte[] aesKey =
        [
            0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8,
            0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8
        ];

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);

        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            var configState = session.GetConfigState();
            Assert.True(configState.IsConfigured(Slot.Two));
        }
        finally
        {
            await session.DeleteSlotAsync(Slot.Two);

            var stateAfterDelete = session.GetConfigState();
            Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
        }
    }

    /// <summary>
    ///     Programs slot 2 with an HOTP (counter-based OTP) configuration.
    ///     Verifies the slot is configured, then cleans up.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task PutConfiguration_Hotp_ConfiguresAndDeletesSlot(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync<IOtpHidConnection>();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // 20-byte HMAC key for HOTP
        byte[] hmacKey =
        [
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30
        ];

        using var config = new HotpSlotConfiguration(hmacKey);

        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            var configState = session.GetConfigState();
            Assert.True(configState.IsConfigured(Slot.Two));
        }
        finally
        {
            await session.DeleteSlotAsync(Slot.Two);

            var stateAfterDelete = session.GetConfigState();
            Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
        }
    }

    /// <summary>
    ///     Programs slot 2 with an HOTP configuration using 8 digits mode.
    ///     Verifies the slot is configured with the 8-digit option, then cleans up.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task PutConfiguration_Hotp8Digits_ConfiguresAndDeletesSlot(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync<IOtpHidConnection>();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        byte[] hmacKey =
        [
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30
        ];

        using var config = new HotpSlotConfiguration(hmacKey).Use8Digits();

        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            var configState = session.GetConfigState();
            Assert.True(configState.IsConfigured(Slot.Two));
        }
        finally
        {
            await session.DeleteSlotAsync(Slot.Two);

            var stateAfterDelete = session.GetConfigState();
            Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
        }
    }
}
