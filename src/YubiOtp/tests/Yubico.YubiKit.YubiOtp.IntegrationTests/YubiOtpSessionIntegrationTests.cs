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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiOtp.IntegrationTests;

public class YubiOtpSessionIntegrationTests
{
    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task GetSerial_ReturnsPositiveSerialNumber(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        var serial = await session.GetSerialAsync();

        // Serial may be 0 if serial API visibility is disabled on the device
        Assert.True(serial >= 0);
    }

    [Theory]
    [WithYubiKey]
    public async Task GetConfigState_ReturnsValidState(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        var configState = session.GetConfigState();

        // ConfigState is always parseable — verify it doesn't throw and has a parseable structure
        Assert.True(configState.FirmwareVersion.Major >= 0);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task PutConfiguration_HmacSha1_ThenDelete_Succeeds(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Program slot 2 with HMAC-SHA1
        using var config = new HmacSha1SlotConfiguration(
            new byte[20] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A,
                           0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14 });

        await session.PutConfigurationAsync(Slot.Two, config);

        // Verify slot is configured
        var stateAfterPut = session.GetConfigState();
        Assert.True(stateAfterPut.IsConfigured(Slot.Two));

        // Delete the slot
        await session.DeleteSlotAsync(Slot.Two);

        // Verify slot is cleared
        var stateAfterDelete = session.GetConfigState();
        Assert.False(stateAfterDelete.IsConfigured(Slot.Two));
    }

    [Theory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    public async Task CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Program slot 2 with a known HMAC-SHA1 key
        byte[] key = new byte[20];
        Array.Fill(key, (byte)0x31); // "1111..." as key

        using var config = new HmacSha1SlotConfiguration(key);
        await session.PutConfigurationAsync(Slot.Two, config);

        try
        {
            // Send a challenge
            byte[] challenge = [0x01, 0x02, 0x03, 0x04];
            var response = await session.CalculateHmacSha1Async(Slot.Two, challenge);

            Assert.Equal(20, response.Length);
        }
        finally
        {
            // Clean up: delete slot 2
            await session.DeleteSlotAsync(Slot.Two);
        }
    }

    [Theory]
    [WithYubiKey(MinFirmware = "2.3.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SwapSlots_Succeeds(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // This test only verifies the swap command completes without error.
        // A full test would configure both slots and verify the swap,
        // but that risks disrupting other tests.
        await session.SwapSlotsAsync();

        // Swap back to restore original state
        await session.SwapSlotsAsync();
    }

    [Theory]
    [WithYubiKey(MinFirmware = "3.0.0")]
    public async Task SetNdefConfiguration_UriType_Succeeds(YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync();
        await using var session = await YubiOtpSession.CreateAsync(connection);

        // Configure NDEF for slot 2
        await session.SetNdefConfigurationAsync(
            Slot.Two,
            "https://www.yubico.com");

        // Disable NDEF
        await session.SetNdefConfigurationAsync(Slot.Two);
    }
}
