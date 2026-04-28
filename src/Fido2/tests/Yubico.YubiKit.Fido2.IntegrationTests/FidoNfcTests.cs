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
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task CreateFidoSession_With_NfcSmartCard_SucceedsAndReturnsInfo(YubiKeyTestState state)
    {
        // The RequireNfc filter checks if the device supports NFC, not if it's
        // currently connected via NFC. A USB-connected YubiKey 5 NFC will pass
        // the filter but fail when attempting SmartCard transport over USB.
        // Skip at runtime when the actual connection type is USB (not NFC SmartCard).
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires an NFC SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}. Connect the YubiKey via NFC to run this test.");
            return;
        }

        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                var info = await session.GetInfoAsync();

                Assert.NotNull(info);
                Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
                Assert.Equal(16, info.Aaguid.Length);
            });
        }
        catch (NotSupportedException)
        {
            // FIDO2 over USB CCID is intentionally not supported. If the device
            // is connected via USB and matched as SmartCard, the session creation
            // throws here. Skip the test in that case.
            Skip.If(true, "FIDO2 over USB CCID is not supported; this test requires NFC.");
        }
    }

    /// <summary>
    /// Tests that GetInfo over NFC SmartCard returns valid FIDO2 versions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task GetInfo_Over_NfcSmartCard_ReturnsValidFido2Version(YubiKeyTestState state)
    {
        // The RequireNfc filter checks if the device supports NFC, not if it's
        // currently connected via NFC. A USB-connected YubiKey 5 NFC will pass
        // the filter but fail when attempting SmartCard transport over USB.
        // Skip at runtime when the actual connection type is USB (not NFC SmartCard).
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires an NFC SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}. Connect the YubiKey via NFC to run this test.");
            return;
        }

        try
        {
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
        }
        catch (NotSupportedException)
        {
            // FIDO2 over USB CCID is intentionally not supported. If the device
            // is connected via USB and matched as SmartCard, the session creation
            // throws here. Skip the test in that case.
            Skip.If(true, "FIDO2 over USB CCID is not supported; this test requires NFC.");
        }
    }

    /// <summary>
    /// Tests that GetInfo over NFC returns supported algorithms including ES256.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, RequireNfc = true)]
    public async Task GetInfo_Over_NfcSmartCard_ReturnsSupportedAlgorithms(YubiKeyTestState state)
    {
        // The RequireNfc filter checks if the device supports NFC, not if it's
        // currently connected via NFC. A USB-connected YubiKey 5 NFC will pass
        // the filter but fail when attempting SmartCard transport over USB.
        // Skip at runtime when the actual connection type is USB (not NFC SmartCard).
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires an NFC SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}. Connect the YubiKey via NFC to run this test.");
            return;
        }

        try
        {
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
        catch (NotSupportedException)
        {
            // FIDO2 over USB CCID is intentionally not supported. If the device
            // is connected via USB and matched as SmartCard, the session creation
            // throws here. Skip the test in that case.
            Skip.If(true, "FIDO2 over USB CCID is not supported; this test requires NFC.");
        }
    }
}
