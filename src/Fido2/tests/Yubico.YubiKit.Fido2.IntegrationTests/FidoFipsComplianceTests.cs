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
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIPS compliance verification.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "FIPS")]
public class FidoFipsComplianceTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task FipsDevice_SupportsPinUvAuthProtocolV2(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.FirmwareVersion is null || info.FirmwareVersion.Major < 5 ||
                (info.FirmwareVersion.Major == 5 && info.FirmwareVersion.Minor < 4))
            {
                return;
            }

            Assert.NotNull(info.PinUvAuthProtocols);
            Assert.Contains(2, info.PinUvAuthProtocols);
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task FipsApproved_ChecksAlwaysUvOption(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.Options.TryGetValue("alwaysUv", out var alwaysUv))
            {
                if (alwaysUv)
                {
                    Assert.True(info.Options.TryGetValue("clientPin", out var pinConfigured),
                        "Device with alwaysUv should have clientPin option");
                }
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsCertifications(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Certifications);

            if (info.Certifications.Count > 0)
            {
                foreach (var cert in info.Certifications)
                {
                    Assert.False(string.IsNullOrEmpty(cert.Key), "Certification key should not be empty");
                }
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReturnsMinPinLength(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            if (info.MinPinLength.HasValue)
            {
                Assert.True(info.MinPinLength.Value >= 4,
                    $"Minimum PIN length should be at least 4, got {info.MinPinLength.Value}");
            }
        });
}
