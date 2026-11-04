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
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration checks of selected GetInfo fields on a dedicated, approved test device.
/// </summary>
/// <remarks>
/// The shared helper defaults to PIN normalization before the GetInfo action: it can set/change
/// the PIN or consume a retry. These paths require approved PIN work and known device state,
/// even though GetInfo itself needs no PIN or presence. Assertions inspect metadata, not feature
/// execution, complete GetInfo validity or protocol conformance; optional-field checks can be bypassed
/// by absence. A setup skip does not establish that no device mutation occurred.
/// </remarks>
[Trait("Category", "Integration")]
public class Ctap2GetInfoTests
{
    /// <summary>
    /// Tests that GetInfo returns valid CTAP2 version strings.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsValidVersions(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Versions);
            Assert.True(
                info.Versions.Contains("FIDO_2_0") ||
                info.Versions.Contains("FIDO_2_1_PRE") ||
                info.Versions.Contains("FIDO_2_1") ||
                info.Versions.Contains("FIDO_2_3"),
                $"Expected at least one CTAP2 version, got: [{string.Join(", ", info.Versions)}]");
        });

    /// <summary>
    /// Checks length 16 and an additional nonzero-AAGUID test assumption, not authenticator identity verification.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsValidAaguid(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.False(info.Aaguid.IsEmpty, "AAGUID should not be empty");
            Assert.Equal(16, info.Aaguid.Length);
            Assert.False(info.Aaguid.Span.SequenceEqual(new byte[16]),
                "AAGUID should not be all zeros");
        });

    /// <summary>
    /// Requires clientPin key presence and checks plat/rk/up values only if supplied.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsExpectedOptions(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Options);

            if (info.Options.TryGetValue("plat", out var plat))
            {
                Assert.False(plat, "YubiKey should not be a platform authenticator");
            }

            if (info.Options.TryGetValue("rk", out var rk))
            {
                Assert.True(rk, "YubiKey should support resident keys");
            }

            if (info.Options.TryGetValue("up", out var up))
            {
                Assert.True(up, "YubiKey should support user presence");
            }

            Assert.True(info.Options.ContainsKey("clientPin"),
                "YubiKey should have clientPin option");
        });

    /// <summary>
    /// Tests that GetInfo returns PIN/UV auth protocol versions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsPinUvAuthProtocols(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.PinUvAuthProtocols);
            Assert.NotEmpty(info.PinUvAuthProtocols);

            Assert.True(
                info.PinUvAuthProtocols.Contains(1) || info.PinUvAuthProtocols.Contains(2),
                "YubiKey should support PIN/UV auth protocol 1 or 2");
        });

    /// <summary>
    /// Tests that GetInfo returns supported algorithms including ES256.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsSupportedAlgorithms(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Algorithms);
            Assert.NotEmpty(info.Algorithms);

            var hasEs256 = info.Algorithms.Any(a =>
                a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.Es256);
            Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
        });

    /// <summary>
    /// Checks a non-null extension list; the guarded disjunction does not establish hmac-secret support.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsExtensions(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Extensions);

            if (info.Extensions.Count > 0)
            {
                var hasHmacSecret = info.Extensions.Contains("hmac-secret");
                Assert.True(hasHmacSecret || info.Extensions.Count > 0,
                    "YubiKey should support at least one extension");
            }
        });

    /// <summary>
    /// Checks reported firmware against the test's accepted values only when the field is present.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsFirmwareVersion(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.FirmwareVersion is not null)
            {
                var isBetaKey = info.FirmwareVersion is { Major: 0, Minor: 0, Patch: 1 };
                var isProductionKey = info.FirmwareVersion.Major >= 5;
                Assert.True(isBetaKey || isProductionKey,
                    $"Expected firmware 5.0+ or 0.0.1 (beta), got {info.FirmwareVersion}");
            }
        });

    /// <summary>
    /// Checks the reported message-size minimum only when the field is present.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsMaxMsgSize(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.MaxMsgSize.HasValue)
            {
                Assert.True(info.MaxMsgSize.Value >= 1024,
                    "Max message size should be at least 1024 bytes");
            }
        });
}