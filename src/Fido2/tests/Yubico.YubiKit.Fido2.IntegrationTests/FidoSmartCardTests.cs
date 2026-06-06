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
/// Integration tests for FIDO2 over SmartCard (CCID) transport.
/// </summary>
/// <remarks>
/// These tests exercise the SmartCard FIDO2 APDU path when the connected authenticator exposes the FIDO2 AID.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoSmartCardTests
{
    /// <summary>
    /// Tests that creating a FidoSession over SmartCard succeeds and returns valid info.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateFidoSession_With_SmartCard_SucceedsAndReturnsInfo(YubiKeyTestState state)
    {
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires a SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}.");
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
            Skip.If(true, "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
        }
    }

    /// <summary>
    /// Tests that GetInfo over SmartCard returns valid FIDO2 versions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetInfo_Over_SmartCard_ReturnsValidFido2Version(YubiKeyTestState state)
    {
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires a SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}.");
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
            Skip.If(true, "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
        }
    }

    /// <summary>
    /// Tests that GetInfo over SmartCard returns supported algorithms including ES256.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetInfo_Over_SmartCard_ReturnsSupportedAlgorithms(YubiKeyTestState state)
    {
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires a SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}.");
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
            Skip.If(true, "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
        }
    }
}