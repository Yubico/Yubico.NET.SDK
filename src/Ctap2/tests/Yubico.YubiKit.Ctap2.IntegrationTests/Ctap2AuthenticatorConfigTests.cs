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
using Yubico.YubiKit.Ctap2.Config;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Device tests for alwaysUv state, a minimum-PIN target and force-change behavior.
/// </summary>
/// <remarks>
/// All callbacks use default known-PIN normalization before their capability checks;
/// setup can set/change a PIN or consume retries. These require human-approved PIN work
/// and known device state, not a read-only GetInfo lane. Support is checked per option,
/// not qualified by a firmware threshold. Restoration is partial/best-effort: a failed
/// assertion can leave alwaysUv enabled, the minimum is not reduced, and a failed second
/// PIN change can leave the temporary PIN. These tests require authorized fixture state;
/// the finally blocks clear local buffers, not device state.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "AuthenticatorConfig")]
public class Ctap2AuthenticatorConfigTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.PermanentDeviceState)]
    public async Task ToggleAlwaysUv_TogglesAlwaysUvOption(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Check for authnrCfg option
            if (!info.Options.TryGetValue("authnrCfg", out var configSupported) || !configSupported)
            {
                Skip.If(true, "YubiKey does not support authenticatorConfig (authnrCfg option)");
                return;
            }

            if (!info.Options.ContainsKey("alwaysUv"))
            {
                Skip.If(true, "YubiKey does not advertise alwaysUv support.");
                return;
            }

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    Ctap2TestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
            }

            try
            {
                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);
                await config.SetAlwaysUvAsync(false);
                var baseline = await session.GetInfoAsync();
                Assert.False(baseline.Options["alwaysUv"]);

                await config.SetAlwaysUvAsync(true);
                var updatedInfo = await session.GetInfoAsync();
                Assert.True(updatedInfo.Options["alwaysUv"]);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.PermanentDeviceState)]
    public async Task SetMinPinLength_IncreasesMinimum(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.Options.TryGetValue("authnrCfg", out var configSupported) || !configSupported)
            {
                Skip.If(true, "YubiKey does not support authenticatorConfig (authnrCfg option)");
                return;
            }

            if (!info.Options.TryGetValue("setMinPINLength", out var setMinPinSupported) || !setMinPinSupported)
            {
                Skip.If(true, "YubiKey does not support setMinPINLength option");
                return;
            }

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    Ctap2TestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
            }

            try
            {
                // Fixed target below the eight-ASCII-code-point test PIN avoids accumulating increases.
                // Equality with the current minimum is allowed; this does not always prove an increase.
                const int testMinPinLength = 6;

                var currentMinLength = info.MinPinLength ?? 4;

                // The minimum cannot decrease; FIDO reset restores the preconfigured minimum and invalidates credentials.
                // If already above our target, skip with diagnostic.
                if (currentMinLength > testMinPinLength)
                {
                    Skip.If(true,
                        $"minPinLength is already {currentMinLength} (> target {testMinPinLength}). " +
                        "Factory reset required: ykman fido reset");
                    return;
                }

                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);

                await config.SetMinPinLengthAsync(
                    testMinPinLength,
                    rpIds: [Ctap2TestData.RpId]);

                var updatedInfo = await session.GetInfoAsync();
                // Asserts only the minimum bound, not RP-list storage or minPinLength extension output.
                Assert.True(updatedInfo.MinPinLength >= testMinPinLength,
                    $"Min PIN length should be >= {testMinPinLength}, was {updatedInfo.MinPinLength}");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });

    /// <summary>
    /// Exercises the force-change flag, blocked token and explicit PIN-change cycle.
    /// </summary>
    /// <remarks>
    /// The token-block assertion uses legacy GetPinTokenAsync (0x05) and expects PinInvalid,
    /// as specified by CTAP 2.1 (2022-06-21 errata), Section 6.5.5.7.1, for forcePINChange.
    /// The permission-based 0x09 method instead returns PinPolicyViolation (Section 6.5.5.7.2)
    /// and is not exercised by that assertion. Later successful steps assert a nonempty
    /// legacy token and a cleared flag, not every restored setting or permission-token behavior.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task SetMinPinLength_ForceChangePin_FullCycle(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.Options.TryGetValue("authnrCfg", out var configSupported) || !configSupported)
            {
                Skip.If(true, "YubiKey does not support authenticatorConfig (authnrCfg option)");
                return;
            }

            if (!info.Options.TryGetValue("setMinPINLength", out var setMinPinSupported) || !setMinPinSupported)
            {
                Skip.If(true, "YubiKey does not support setMinPINLength option");
                return;
            }

            // Require a clear flag after normalization's attempted recovery; setup can fail before this callback.
            Assert.True(info.ForcePinChange != true,
                "ForcePinChange should not be set before test. NormalizePinAsync should have cleared it.");

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    Ctap2TestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
            }

            try
            {
                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);
                var currentMinLength = info.MinPinLength ?? 4;

                // Step 1: Set forcePinChange flag
                await config.SetMinPinLengthAsync(currentMinLength, forceChangePin: true);

                // Step 2: Verify flag is set
                var flaggedInfo = await session.GetInfoAsync();
                Assert.True(flaggedInfo.ForcePinChange,
                    "ForcePinChange should be true after SetMinPinLength with forceChangePin=true");

                // Step 3: Legacy 0x05 returns PinInvalid for forcePINChange in CTAP 2.1 Section 6.5.5.7.1.
                var ex = await Assert.ThrowsAsync<CtapException>(
                    () => clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8));
                Assert.Equal(CtapStatus.PinInvalid, ex.Status);

                // Step 4: Change the PIN as part of the exercise, not in teardown.
                byte[] reversedPin = Ctap2TestData.PinUtf8.Reverse().ToArray();
                try
                {
                    await clientPin.ChangePinAsync(Ctap2TestData.PinUtf8, reversedPin);
                    await clientPin.ChangePinAsync(reversedPin, Ctap2TestData.PinUtf8);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(reversedPin);
                }

                // Step 5: Verify PIN tokens work again
                var restoredToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                try
                {
                    Assert.NotEmpty(restoredToken);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(restoredToken);
                }

                // Step 6: Verify flag is cleared
                var clearedInfo = await session.GetInfoAsync();
                Assert.True(clearedInfo.ForcePinChange != true,
                    "ForcePinChange should be cleared after PIN change");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });
}
