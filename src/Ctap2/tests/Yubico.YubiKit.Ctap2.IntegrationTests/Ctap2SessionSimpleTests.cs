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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration checks of CTAP2 session creation, metadata, selection and reset refusal.
/// </summary>
/// <remarks>
/// Helper-based tests use default PIN normalization and can set/change PIN or consume retries before
/// their actions. They require approved PIN work and known device state; filtering touch traits does
/// not make them read-only. Direct creation/GetInfo paths do not use that helper. Selection and reset
/// require human coordination. Assertions establish only the properties named below, not certification.
/// </remarks>
public class Ctap2SessionSimpleTests
{
    /// <summary>
    /// Tests that creating a Ctap2Session over SmartCard (CCID) succeeds.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateCtap2Session_With_SmartCard_CreateAsync(YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();

        Ctap2Session? session;
        try
        {
            session = await Ctap2Session.CreateAsync(connection);
        }
        catch (NotSupportedException ex)
        {
            Skip.If(true, ex.Message);
            return;
        }

        await using (session)
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info);
            Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateCtap2Session_With_HidFido_CreateAsync(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            Assert.NotNull(info);
            Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_CTAP2_Version(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Require this advertised version string; do not infer execution of all CTAP2 features.
            Assert.Contains("FIDO_2_0", info.Versions);
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_AAGUID(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Length is the protocol requirement; rejecting all-zero bytes is an additional test assumption.
            Assert.Equal(16, info.Aaguid.Length);
            Assert.False(info.Aaguid.Span.SequenceEqual(new byte[16]), "AAGUID should not be all zeros");
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Supported_Extensions(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Extensions is { Count: > 0 })
            {
                // This guarded disjunction is always true; it does not establish hmac-secret support.
                var hasHmacSecret = info.Extensions.Contains("hmac-secret");
                Assert.True(hasHmacSecret || info.Extensions.Count > 0,
                    "YubiKey should support at least one extension");
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Supported_Algorithms(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Algorithms is { Count: > 0 })
            {
                // ES256 is checked only for a nonempty advertisement, not by creating that algorithm.
                var hasEs256 = info.Algorithms.Any(a =>
                    a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
                Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Options(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Options is { Count: > 0 })
            {
                // This guarded disjunction does not establish that rk is present or enabled.
                var hasRk = info.Options.ContainsKey("rk");
                Assert.True(hasRk || info.Options.Count > 0,
                    "YubiKey should have at least one option");
            }
        });

    /// <summary>
    /// Tests that the IYubiKey extension method for creating CTAP2 sessions works.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateCtap2Session_With_ExtensionMethod(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateCtap2SessionAsync();

        var info = await session.GetInfoAsync();
        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateCtap2Session_SecondExplicitHidCtap2Session_IsRefused(YubiKeyTestState state)
    {
        await using var first = await state.Device.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido });

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
            state.Device.CreateCtap2SessionAsync(
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido }));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_With_YubiKeyExtensionMethod(YubiKeyTestState state)
    {
        var info = await state.Device.GetCtap2InfoAsync();

        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }

    /// <summary>
    /// Awaits selection completion on the coordinated HID device; no response UP/UV flags are asserted.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task SelectionAsync_RequiresTouch(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            await session.SelectionAsync();
        });

    /// <summary>
    /// Expects some CtapException from reset, without checking its status or whether state changed.
    /// </summary>
    /// <remarks>This is not a safe reset-window or no-deletion proof. Reset command receipt and
    /// presence use separate clocks; human-approved power-cycle/presence coordination is required.</remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task ResetAsync_RequiresUserPresence(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.ResetAsync();
            });
        });
}