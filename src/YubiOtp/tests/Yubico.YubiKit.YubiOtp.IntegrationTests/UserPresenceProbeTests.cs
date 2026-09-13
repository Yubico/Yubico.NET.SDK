// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiOtp.IntegrationTests;

public class UserPresenceProbeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "3.0.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task InspectPrerequisites_ReadOnly_PrintsNonSecretState(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });

        ConfigState configState = session.GetConfigState();

        output.WriteLine($"Firmware={configState.FirmwareVersion}");
        output.WriteLine(
            $"Slot1: IsConfigured={configState.IsConfigured(Slot.One)}, " +
            $"IsTouchTriggered={configState.IsTouchTriggered(Slot.One)}");
        output.WriteLine(
            $"Slot2: IsConfigured={configState.IsConfigured(Slot.Two)}, " +
            $"IsTouchTriggered={configState.IsTouchTriggered(Slot.Two)}");
    }

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "3.0.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task CalculateHmacSha1Async_TouchRequired_CompletesOnceAndReturnsExpectedResponse(
        YubiKeyTestState state)
    {
        var prompt = new RecordingUserPresencePrompt(output);
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.HidOtp,
                UserPresencePrompt = prompt
            });

        ConfigState initialState = session.GetConfigState();
        output.WriteLine(
            $"Prerequisites: Firmware={initialState.FirmwareVersion}, " +
            $"Slot1Configured={initialState.IsConfigured(Slot.One)}, " +
            $"Slot2Configured={initialState.IsConfigured(Slot.Two)}, " +
            $"Slot2TouchTriggered={initialState.IsTouchTriggered(Slot.Two)}");
        Skip.If(
            initialState.IsConfigured(Slot.Two),
            "YubiOTP slot 2 is configured; refusing to overwrite it. Clear slot 2 deliberately before running this probe.");

        byte[] key =
        [
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13
        ];
        byte[] challenge = "YubiOTP touch probe"u8.ToArray();
        byte[]? expectedResponse = null;
        ReadOnlyMemory<byte> response = default;
        var slotCreated = false;

        try
        {
            using var configuration = new HmacSha1SlotConfiguration(key).RequireTouch();
            try
            {
                await session.PutConfigurationAsync(Slot.Two, configuration);
                slotCreated = true;
            }
            catch
            {
                output.WriteLine(
                    "The slot 2 write did not complete unambiguously. This test will not delete slot 2 because it " +
                    "cannot prove ownership; inspect slot state before retrying.");
                throw;
            }

            ConfigState configuredState = session.GetConfigState();
            Assert.True(configuredState.IsConfigured(Slot.Two));
            output.WriteLine(
                $"Post-configuration status: Slot2IsTouchTriggered={configuredState.IsTouchTriggered(Slot.Two)} " +
                "(reported for inspection; HID wait behavior is authoritative for this probe).");

            Assert.Empty(prompt.Requested);
            Assert.Empty(prompt.Resolved);
            expectedResponse = HMACSHA1.HashData(key, challenge);

            using var operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            response = await session.CalculateHmacSha1Async(Slot.Two, challenge, operationCts.Token);

            Assert.True(CryptographicOperations.FixedTimeEquals(expectedResponse, response.Span));

            UserPresenceContext requested = Assert.Single(prompt.Requested);
            Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
            Assert.Equal("YubiOTP", requested.Application);
            Assert.Equal(Slot.Two.ToString(), requested.Scope);

            (UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken) resolved =
                Assert.Single(prompt.Resolved);
            Assert.Same(requested, resolved.Context);
            Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
            Assert.Equal(CancellationToken.None, resolved.CancellationToken);
        }
        finally
        {
            if (!response.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
            }

            if (expectedResponse is not null)
            {
                CryptographicOperations.ZeroMemory(expectedResponse);
            }

            CryptographicOperations.ZeroMemory(challenge);
            CryptographicOperations.ZeroMemory(key);

            if (slotCreated)
            {
                await session.DeleteSlotAsync(Slot.Two);
                Assert.False(session.GetConfigState().IsConfigured(Slot.Two));
                output.WriteLine("Cleanup: deleted the test-created configuration from YubiOTP slot 2.");
            }
        }
    }

    private sealed class RecordingUserPresencePrompt(ITestOutputHelper output) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];

        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)>
            Resolved
        { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] YubiOTP user-presence request: Basis={context.Basis}, " +
                $"Slot={context.Scope}. Touch the YubiKey now.");
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] YubiOTP user-presence resolution: Outcome={outcome}.");
            return default;
        }
    }
}