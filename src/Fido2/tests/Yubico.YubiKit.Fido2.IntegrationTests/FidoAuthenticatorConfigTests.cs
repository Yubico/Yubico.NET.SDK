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
using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Config;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2.1 authenticatorConfig operations.
/// Tests AlwaysUV toggle, minimum PIN length, and force PIN change.
/// Requires firmware 5.4+ with authenticatorConfig support.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "AuthenticatorConfig")]
public class FidoAuthenticatorConfigTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task ToggleAlwaysUv_TogglesAlwaysUvOption(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Check for authnrCfg option
            if (!info.Options.TryGetValue("authnrCfg", out var configSupported) || !configSupported)
            {
                Skip.If(true, "YubiKey does not support authenticatorConfig (authnrCfg option)");
                return;
            }

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
            }

            try
            {
                // Read current alwaysUv state
                var initialAlwaysUv = info.Options.TryGetValue("alwaysUv", out var alwaysUvValue)
                    && alwaysUvValue;

                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);

                // Toggle alwaysUv
                await config.ToggleAlwaysUvAsync();

                // Read new state
                var updatedInfo = await session.GetInfoAsync();
                var newAlwaysUv = updatedInfo.Options.TryGetValue("alwaysUv", out var newAlwaysUvValue)
                    && newAlwaysUvValue;

                Assert.NotEqual(initialAlwaysUv, newAlwaysUv);

                // Toggle back to restore original state
                // Need a fresh token since the previous one may have been consumed
                byte[] restorePinToken;
                if (supportsPermissions)
                {
                    restorePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.AuthenticatorConfig);
                }
                else
                {
                    restorePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var restoreConfig = new AuthenticatorConfig(session, clientPin.Protocol, restorePinToken);
                await restoreConfig.ToggleAlwaysUvAsync();

                CryptographicOperations.ZeroMemory(restorePinToken);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.PermanentDeviceState)]
    public async Task SetMinPinLength_IncreasesMinimum(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
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

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
            }

            try
            {
                // Fixed target well below our 8-char test PIN. Using a constant
                // avoids the old currentMinLength+1 pattern that accumulated
                // across runs until it exceeded the test PIN length.
                const int testMinPinLength = 6;

                var currentMinLength = info.MinPinLength ?? 4;

                // setMinPINLength is one-way (increase only, factory reset to undo).
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
                    rpIds: [FidoTestData.RpId]);

                var updatedInfo = await session.GetInfoAsync();
                Assert.True(updatedInfo.MinPinLength >= testMinPinLength,
                    $"Min PIN length should be >= {testMinPinLength}, was {updatedInfo.MinPinLength}");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });

    /// <summary>
    /// Tests the full forcePinChange lifecycle: set the flag, verify PIN tokens are
    /// blocked, change PIN to clear the flag, verify tokens work again.
    /// Matches the python-fido2 test_force_pin_change pattern.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task SetMinPinLength_ForceChangePin_FullCycle(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
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

            // Pre-condition: forcePinChange should not be set (NormalizePinAsync clears it)
            Assert.True(info.ForcePinChange != true,
                "ForcePinChange should not be set before test. NormalizePinAsync should have cleared it.");

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.PinUtf8,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
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

                // Step 3: Verify PIN token requests are blocked
                var ex = await Assert.ThrowsAsync<Ctap.CtapException>(
                    () => clientPin.GetPinTokenAsync(FidoTestData.PinUtf8));
                Assert.Equal(Ctap.CtapStatus.PinInvalid, ex.Status);

                // Step 4: Clear flag via PIN change (reverse then restore, matching python-fido2 pattern)
                byte[] reversedPin = FidoTestData.PinUtf8.Reverse().ToArray();
                await clientPin.ChangePinAsync(FidoTestData.PinUtf8, reversedPin);
                await clientPin.ChangePinAsync(reversedPin, FidoTestData.PinUtf8);
                CryptographicOperations.ZeroMemory(reversedPin);

                // Step 5: Verify PIN tokens work again
                var restoredToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                Assert.NotEmpty(restoredToken);
                CryptographicOperations.ZeroMemory(restoredToken);

                // Step 6: Verify flag is cleared
                var clearedInfo = await session.GetInfoAsync();
                Assert.True(clearedInfo.ForcePinChange != true,
                    "ForcePinChange should be cleared after PIN change");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);

                // Safety net: if test failed before step 4, clear forcePinChange.
                // Uses reversed-PIN pattern because Enhanced PIN keys reject same-PIN changes.
                try
                {
                    var checkInfo = await session.GetInfoAsync();
                    if (checkInfo.ForcePinChange == true)
                    {
                        byte[] tempPin = FidoTestData.PinUtf8.Reverse().ToArray();
                        await clientPin.ChangePinAsync(FidoTestData.PinUtf8, tempPin);
                        await clientPin.ChangePinAsync(tempPin, FidoTestData.PinUtf8);
                        CryptographicOperations.ZeroMemory(tempPin);
                    }
                }
                catch
                {
                    // Best-effort — NormalizePinAsync will also attempt recovery
                }
            }
        });
}
