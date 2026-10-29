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
/// Integration tests for CTAP2 over SmartCard (CCID) transport.
/// </summary>
/// <remarks>
/// These tests exercise the SmartCard CTAP2 APDU path when the connected authenticator exposes the FIDO AID.
/// </remarks>
[Trait("Category", "Integration")]
public class Ctap2SmartCardTests
{
    /// <summary>
    /// Tests that creating a Ctap2Session over SmartCard succeeds and returns valid info.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateCtap2Session_With_SmartCard_SucceedsAndReturnsInfo(YubiKeyTestState state)
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
            await state.WithCtap2SessionAsync(async session =>
            {
                var info = await session.GetInfoAsync();

                Assert.NotNull(info);
                Assert.True(info.Versions.Count > 0, "AuthenticatorInfo.Versions should not be empty");
                Assert.Equal(16, info.Aaguid.Length);
            }, preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true, "CTAP2 SmartCard session failed because the connected authenticator did not expose the FIDO AID or does not support USB SmartCard CTAP2 on this firmware.");
        }
    }

    /// <summary>
    /// Tests that GetInfo over SmartCard returns valid CTAP2 versions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetInfo_Over_SmartCard_ReturnsValidCtap2Version(YubiKeyTestState state)
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
            }, preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true, "CTAP2 SmartCard session failed because the connected authenticator did not expose the FIDO AID or does not support USB SmartCard CTAP2 on this firmware.");
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
            await state.WithCtap2SessionAsync(async session =>
            {
                var info = await session.GetInfoAsync();

                Assert.NotNull(info.Algorithms);
                Assert.NotEmpty(info.Algorithms);

                var hasEs256 = info.Algorithms.Any(a =>
                    a.Type == "public-key" && a.Algorithm == CoseAlgorithmIdentifier.ES256);
                Assert.True(hasEs256, "YubiKey should support ES256 algorithm");
            }, preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true, "CTAP2 SmartCard session failed because the connected authenticator did not expose the FIDO AID or does not support USB SmartCard CTAP2 on this firmware.");
        }
    }
}