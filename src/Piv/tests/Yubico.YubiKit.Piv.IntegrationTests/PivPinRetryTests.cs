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

using Xunit;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
/// Integration tests for PIN retry counter behavior.
///
/// Verifies that PIN retry counters decrement correctly after failed attempts,
/// that metadata reflects the current retry state, and that SetPinAttemptsAsync
/// with custom retry counts is enforced during actual PIN failures.
/// </summary>
public class PivPinRetryTests
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    /// <summary>
    /// Verifies that each failed PIN attempt decrements the retry counter by exactly one,
    /// and that both GetPinAttemptsAsync and GetPinMetadataAsync (on 5.3+) reflect the
    /// updated count consistently.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task PinRetries_DecrementAfterEachFailedAttempt_MetadataMatches(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        // Default state: 3 PIN retries
        var initialMetadata = await session.GetPinMetadataAsync();
        Assert.Equal(3, initialMetadata.TotalRetries);
        Assert.Equal(3, initialMetadata.RetriesRemaining);

        var wrongPin = "000000"u8.ToArray();

        try
        {
            // First wrong attempt: 3 -> 2
            var ex1 = await Assert.ThrowsAsync<InvalidPinException>(
                () => session.VerifyPinAsync(wrongPin));
            Assert.Equal(2, ex1.RetriesRemaining);

            var attemptsAfter1 = await session.GetPinAttemptsAsync();
            Assert.Equal(2, attemptsAfter1);

            var metadataAfter1 = await session.GetPinMetadataAsync();
            Assert.Equal(3, metadataAfter1.TotalRetries);
            Assert.Equal(2, metadataAfter1.RetriesRemaining);

            // Second wrong attempt: 2 -> 1
            var ex2 = await Assert.ThrowsAsync<InvalidPinException>(
                () => session.VerifyPinAsync(wrongPin));
            Assert.Equal(1, ex2.RetriesRemaining);

            var attemptsAfter2 = await session.GetPinAttemptsAsync();
            Assert.Equal(1, attemptsAfter2);

            var metadataAfter2 = await session.GetPinMetadataAsync();
            Assert.Equal(1, metadataAfter2.RetriesRemaining);

            // Correct PIN restores the counter
            await session.VerifyPinAsync(DefaultPin);

            var attemptsRestored = await session.GetPinAttemptsAsync();
            Assert.Equal(3, attemptsRestored);

            var metadataRestored = await session.GetPinMetadataAsync();
            Assert.Equal(3, metadataRestored.RetriesRemaining);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    /// <summary>
    /// Sets custom PIN retry counts (e.g. 5 PIN / 4 PUK), then exercises actual failed
    /// PIN attempts to verify the custom limit is enforced. After 4 wrong attempts,
    /// there should be 1 retry remaining, not the default 3-attempt behavior.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task SetPinAttempts_CustomLimit_EnforcedDuringFailedAttempts(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        // SetPinAttemptsAsync requires both PIN verification and management key auth
        await session.VerifyPinAsync(DefaultPin);
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        try
        {
            // Set 5 PIN retries, 4 PUK retries
            await session.SetPinAttemptsAsync(5, 4);

            var metadata = await session.GetPinMetadataAsync();
            Assert.Equal(5, metadata.TotalRetries);
            Assert.Equal(5, metadata.RetriesRemaining);

            // Note: SetPinAttemptsAsync resets PIN to default, so re-verify
            var wrongPin = "000000"u8.ToArray();

            // Fail 4 times: 5 -> 4 -> 3 -> 2 -> 1
            for (var i = 0; i < 4; i++)
            {
                var ex = await Assert.ThrowsAsync<InvalidPinException>(
                    () => session.VerifyPinAsync(wrongPin));
                Assert.Equal(5 - i - 1, ex.RetriesRemaining);
            }

            // Verify 1 retry remaining (not blocked yet -- 5 total, 4 used)
            var attemptsRemaining = await session.GetPinAttemptsAsync();
            Assert.Equal(1, attemptsRemaining);

            // Correct PIN resets counter to 5
            await session.VerifyPinAsync(DefaultPin);

            var restoredAttempts = await session.GetPinAttemptsAsync();
            Assert.Equal(5, restoredAttempts);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    /// <summary>
    /// Verifies that PIN metadata correctly reports IsDefault after reset,
    /// and that changing the PIN causes IsDefault to become false.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task PinMetadata_IsDefault_ReflectsChanges(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        try
        {
            // After reset, PIN is default
            var metadataDefault = await session.GetPinMetadataAsync();
            Assert.True(metadataDefault.IsDefault);

            // Change PIN
            var newPin = "654321"u8.ToArray();
            await session.ChangePinAsync(DefaultPin, newPin);

            // After change, PIN is no longer default
            var metadataChanged = await session.GetPinMetadataAsync();
            Assert.False(metadataChanged.IsDefault);
        }
        finally
        {
            await session.ResetAsync();
        }
    }
}
