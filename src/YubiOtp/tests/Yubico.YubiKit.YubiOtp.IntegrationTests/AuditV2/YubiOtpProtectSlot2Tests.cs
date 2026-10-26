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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiOtp.IntegrationTests.AuditV2;

/// <summary>
///     Audit 26l (YESDK-1634): <see cref="SlotConfiguration.ProtectSlot2" /> must set the PROTECT_CFG2 bit
///     (0x80, yubikit/yubiotp.py TKTFLAG.PROTECT_CFG2) and leave the challenge-response bit (0x40) alone.
///     These tests write both slots of the test key and clear them afterwards.
/// </summary>
public sealed class YubiOtpProtectSlot2Tests
{
    private static readonly byte[] Key = [.. Enumerable.Repeat((byte)0x31, 20)];
    private static readonly byte[] Challenge = [0x01, 0x02, 0x03, 0x04];
    private static readonly byte[] Expected = HMACSHA1.HashData(Key, Challenge);

    /// <summary>
    ///     A challenge-response slot keeps answering with protection on or explicitly off: the protect bit
    ///     is not the chal-resp bit, and ProtectSlot2(false) must not clear the mode bit.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ProtectSlot2_DoesNotDisturbChallengeResponseBit(YubiKeyTestState state)
    {
        // Challenge-response over CCID answers 6985 on this applet; pin the HID transport as the
        // existing CalculateHmacSha1 test does.
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = state.ConnectionType });
        try
        {
            using (var protectedConfig = new HmacSha1SlotConfiguration(Key))
            {
                protectedConfig.ProtectSlot2();
                await session.PutConfigurationAsync(Slot.Two, protectedConfig);
            }

            Assert.Equal(Expected, (await session.CalculateHmacSha1Async(Slot.Two, Challenge)).ToArray());

            using (var unprotectedConfig = new HmacSha1SlotConfiguration(Key))
            {
                unprotectedConfig.ProtectSlot2(false);
                await session.PutConfigurationAsync(Slot.Two, unprotectedConfig);
            }

            Assert.Equal(Expected, (await session.CalculateHmacSha1Async(Slot.Two, Challenge)).ToArray());
        }
        finally
        {
            await DeleteIfConfiguredAsync(session, Slot.Two);
        }
    }

    /// <summary>
    ///     The bit's meaning: set on slot 1, the YubiKey refuses to write slot 2 while slot 2 is not itself
    ///     configured with the same bit. Rewriting slot 1 without it lifts the block.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ProtectSlot2OnSlot1_BlocksSlot2Write(YubiKeyTestState state)
    {
        // Challenge-response over CCID answers 6985 on this applet; pin the HID transport as the
        // existing CalculateHmacSha1 test does.
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = state.ConnectionType });
        try
        {
            await DeleteIfConfiguredAsync(session, Slot.Two);

            using (var slot1Protecting = new HmacSha1SlotConfiguration(Key))
            {
                slot1Protecting.ProtectSlot2();
                await session.PutConfigurationAsync(Slot.One, slot1Protecting);
            }

            using (var slot2Plain = new HmacSha1SlotConfiguration(Key))
            {
                var rejection = await Record.ExceptionAsync(() => session.PutConfigurationAsync(Slot.Two, slot2Plain));
                Assert.NotNull(rejection);
            }

            Assert.False(session.GetConfigState().IsConfigured(Slot.Two),
                "Slot 2 was written although slot 1 carries the protect flag.");

            using (var slot1Open = new HmacSha1SlotConfiguration(Key))
            {
                await session.PutConfigurationAsync(Slot.One, slot1Open);
            }

            using (var slot2Plain = new HmacSha1SlotConfiguration(Key))
            {
                await session.PutConfigurationAsync(Slot.Two, slot2Plain);
            }

            Assert.True(session.GetConfigState().IsConfigured(Slot.Two));
            Assert.Equal(Expected, (await session.CalculateHmacSha1Async(Slot.Two, Challenge)).ToArray());
        }
        finally
        {
            await DeleteIfConfiguredAsync(session, Slot.Two);
            await DeleteIfConfiguredAsync(session, Slot.One);
        }
    }

    // Over HID, deleting an already-empty slot does not answer and times out; only delete what is there.
    private static async Task DeleteIfConfiguredAsync(IYubiOtpSession session, Slot slot)
    {
        if (session.GetConfigState().IsConfigured(slot))
            await session.DeleteSlotAsync(slot);
    }
}