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
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Simple integration tests for FidoSession.
/// These tests exercise basic FIDO2 operations that do NOT require user presence.
/// </summary>
/// <remarks>
/// Tests requiring user presence or PIN verification are excluded from automated runs.
/// Mark such tests with [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)] to exclude them.
/// </remarks>
public class FidoSessionSimpleTests
{
    /// <summary>
    /// Tests that creating a FidoSession over USB SmartCard (CCID) correctly throws NotSupportedException.
    /// FIDO2 is only available over NFC SmartCard or USB HID FIDO interfaces.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateFidoSession_With_UsbSmartCard_ThrowsNotSupportedException(YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();

        // USB CCID does not support FIDO2 - only NFC SmartCard or USB HID FIDO
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await FidoSession.CreateAsync(connection);
        });

        Assert.Contains("NFC transport", exception.Message);
        Assert.Contains("IFidoHidConnection", exception.Message);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateFidoSession_With_HidFido_CreateAsync(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            Assert.NotNull(info);
            Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_CTAP2_Version(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // YubiKey 5+ should support FIDO2/CTAP2
            Assert.Contains("FIDO_2_0", info.Versions);
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_AAGUID(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // AAGUID should be 16 bytes
            Assert.Equal(16, info.Aaguid.Length);
            Assert.False(info.Aaguid.Span.SequenceEqual(new byte[16]), "AAGUID should not be all zeros");
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Supported_Extensions(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Extensions is { Count: > 0 })
            {
                var hasHmacSecret = info.Extensions.Contains("hmac-secret");
                Assert.True(hasHmacSecret || info.Extensions.Count > 0,
                    "YubiKey should support at least one extension");
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Supported_Algorithms(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Algorithms is { Count: > 0 })
            {
                var hasEs256 = info.Algorithms.Any(a =>
                    a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
                Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_Returns_Options(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Options is { Count: > 0 })
            {
                var hasRk = info.Options.ContainsKey("rk");
                Assert.True(hasRk || info.Options.Count > 0,
                    "YubiKey should have at least one option");
            }
        });

    /// <summary>
    /// Tests that the IYubiKey extension method for creating FIDO sessions works.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateFidoSession_With_ExtensionMethod(YubiKeyTestState state)
    {
        await using var fidoSession = await state.Device.CreateFidoSessionAsync();

        var info = await fidoSession.GetInfoAsync();
        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfoAsync_With_YubiKeyExtensionMethod(YubiKeyTestState state)
    {
        var info = await state.Device.GetFidoInfoAsync();

        Assert.NotNull(info);
        Assert.True(info.Versions.Count > 0);
    }

    /// <summary>
    /// Tests SelectionAsync which requires user touch to confirm device selection.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task SelectionAsync_RequiresTouch(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            await session.SelectionAsync();
        });

    /// <summary>
    /// Tests ResetAsync which requires user presence within a short window after power-up.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task ResetAsync_RequiresUserPresence(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            await Assert.ThrowsAsync<Ctap.CtapException>(async () =>
            {
                await session.ResetAsync();
            });
        });
}
