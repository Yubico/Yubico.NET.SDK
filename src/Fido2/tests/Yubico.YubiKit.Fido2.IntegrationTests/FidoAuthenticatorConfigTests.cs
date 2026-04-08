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

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
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
                        FidoTestData.Pin,
                        PinUvAuthTokenPermissions.AuthenticatorConfig);
                }
                else
                {
                    restorePinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
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
    public async Task SetMinPinLength_IncreasesMinimum(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (!info.Options.TryGetValue("authnrCfg", out var configSupported) || !configSupported)
            {
                Skip.If(true, "YubiKey does not support authenticatorConfig (authnrCfg option)");
                return;
            }

            // Check for setMinPINLength support
            if (!info.Options.TryGetValue("setMinPINLength", out var setMinPinSupported) || !setMinPinSupported)
            {
                Skip.If(true, "YubiKey does not support setMinPINLength option");
                return;
            }

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            try
            {
                var currentMinLength = info.MinPinLength ?? 4;
                // Set min PIN length to current + 1 (capped at a reasonable value)
                // Note: this is a one-way operation - min length can only increase
                var newMinLength = Math.Max(currentMinLength + 1, 6);

                // Only proceed if the new length is within acceptable range
                if (newMinLength > 63)
                {
                    Skip.If(true, "Cannot increase minimum PIN length further (at max)");
                    return;
                }

                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);

                // Set the minimum PIN length with our RP ID allowed to see it
                await config.SetMinPinLengthAsync(
                    newMinLength,
                    rpIds: [FidoTestData.RpId]);

                // Verify the change by reading info again
                var updatedInfo = await session.GetInfoAsync();
                Assert.True(updatedInfo.MinPinLength >= newMinLength,
                    $"Min PIN length should be at least {newMinLength}, was {updatedInfo.MinPinLength}");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task SetMinPinLength_ForceChangePin_SetsForcePinChangeFlag(YubiKeyTestState state) =>
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

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] configPinToken;
            if (supportsPermissions)
            {
                configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.AuthenticatorConfig);
            }
            else
            {
                configPinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            try
            {
                var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);

                // Force PIN change with a minimum length
                var currentMinLength = info.MinPinLength ?? 4;
                var newMinLength = Math.Max(currentMinLength, 4);

                await config.SetMinPinLengthAsync(
                    newMinLength,
                    forceChangePin: true);

                // Verify forcePinChange is set
                var updatedInfo = await session.GetInfoAsync();
                Assert.True(updatedInfo.ForcePinChange,
                    "ForcePinChange should be true after SetMinPinLength with forceChangePin=true");
            }
            finally
            {
                // Clear the forcePinChange flag by performing a same-PIN change.
                // This restores the key to a clean state for subsequent tests.
                try
                {
                    await clientPin.ChangePinAsync(FidoTestData.Pin, FidoTestData.Pin);
                }
                catch
                {
                    // Best-effort cleanup — ignore errors if change fails
                }

                CryptographicOperations.ZeroMemory(configPinToken);
            }
        });
}
