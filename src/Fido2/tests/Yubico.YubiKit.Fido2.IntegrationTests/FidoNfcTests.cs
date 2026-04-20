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

using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 over NFC SmartCard (CCID) transport.
/// </summary>
/// <remarks>
/// These tests require a physical NFC reader connected to the system.
/// FIDO2 over SmartCard is only supported via NFC - USB CCID is intentionally blocked
/// because YubiKey exposes FIDO2 via USB HID FIDO interface, not USB CCID.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("RequiresNfc", "true")]
public class FidoNfcTests
{
    /// <summary>
    /// Tests that creating a FidoSession over NFC SmartCard succeeds and returns valid info.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task CreateFidoSession_With_NfcSmartCard_SucceedsAndReturnsInfo(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info);
            Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
            Assert.Equal(16, info.Aaguid.Length);
        });

    /// <summary>
    /// Tests that GetInfo over NFC SmartCard returns valid FIDO2 versions.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task GetInfo_Over_NfcSmartCard_ReturnsValidFido2Version(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Versions);
            Assert.True(
                info.Versions.Contains("FIDO_2_0") ||
                info.Versions.Contains("FIDO_2_1_PRE") ||
                info.Versions.Contains("FIDO_2_1") ||
                info.Versions.Contains("FIDO_2_2"),
                $"Expected at least one FIDO2 version, got: [{string.Join(", ", info.Versions)}]");
        });

    /// <summary>
    /// Tests that GetInfo over NFC returns supported algorithms including ES256.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task GetInfo_Over_NfcSmartCard_ReturnsSupportedAlgorithms(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Algorithms);
            Assert.NotEmpty(info.Algorithms);

            var hasEs256 = info.Algorithms.Any(a =>
                a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
            Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
        });
}
