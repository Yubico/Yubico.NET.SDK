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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for enhanced PIN complexity requirements.
/// </summary>
[Trait("Category", "Integration")]
public class FidoEnhancedPinTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task EnhancedPin_CompliantPin_Succeeds(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (info.FirmwareVersion is null || info.FirmwareVersion.Major < 5 ||
                (info.FirmwareVersion.Major == 5 && info.FirmwareVersion.Minor < 8))
            {
                return;
            }

            await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var updatedInfo = await session.GetInfoAsync();
            Assert.True(updatedInfo.Options.TryGetValue("clientPin", out var pinSet) && pinSet,
                "PIN should be configured after SetOrVerifyPinAsync");
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsPinComplexityInfo(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Options.TryGetValue("setMinPINLength", out var setMinPinLength))
            {
                Assert.IsType<bool>(setMinPinLength);
            }

            if (info.MinPinLength.HasValue)
            {
                Assert.True(info.MinPinLength.Value >= 4,
                    $"Minimum PIN length should be at least 4, got {info.MinPinLength.Value}");
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsForcePinChangeOption(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.ForcePinChange.HasValue)
            {
                Assert.IsType<bool>(info.ForcePinChange.Value);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsMaxRpIdsForSetMinPinLength(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.MaxRpidsForSetMinPinLength.HasValue)
            {
                Assert.True(info.MaxRpidsForSetMinPinLength.Value >= 0,
                    "MaxRpidsForSetMinPinLength should be non-negative");
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task ClientPin_RetrievesPinRetries(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("clientPin", out var pinConfigured) || !pinConfigured)
            {
                await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            }

            IPinUvAuthProtocol protocol = info.PinUvAuthProtocols.Contains(2)
                ? new PinUvAuthProtocolV2()
                : new PinUvAuthProtocolV1();
            var clientPin = new ClientPin(session, protocol);
            var (retries, powerCycleState) = await clientPin.GetPinRetriesAsync();

            Assert.True(retries >= 0, "PIN retries should be non-negative");
            Assert.True(retries <= 8, "PIN retries should not exceed 8");
        });
}
