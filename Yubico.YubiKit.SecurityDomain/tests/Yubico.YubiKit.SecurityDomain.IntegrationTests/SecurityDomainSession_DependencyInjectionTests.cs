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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration tests for the <see cref="SecurityDomainSessionFactory" /> registered via
///     <see cref="DependencyInjection.AddYubiKeySecurityDomain" />.
/// </summary>
/// <remarks>
///     These tests verify that the DI-registered factory correctly creates working sessions
///     with real YubiKey hardware. They complement the unit tests in
///     <c>Yubico.YubiKit.SecurityDomain.UnitTests.DependencyInjectionTests</c> which only
///     test registration mechanics without invoking the factory.
/// </remarks>
public class SecurityDomainSession_DependencyInjectionTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Verifies the DI factory creates a working session that can query the device.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_WithRealConnection_CreatesWorkingSession(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            async session =>
            {
                Assert.NotNull(session);
                Assert.True(session.IsAuthenticated);

                var keyInfo = await session.GetKeyInfoAsync(CancellationTokenSource.Token);
                Assert.NotEmpty(keyInfo);
                Assert.Contains(keyInfo, k => k.KeyReference.Kvn == 0xFF);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the DI factory correctly passes SCP03 parameters for authentication.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_WithScpParameters_Authenticates(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            session =>
            {
                Assert.True(session.IsAuthenticated);
                return Task.CompletedTask;
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the DI factory creates unauthenticated sessions when no SCP params provided.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_WithoutScpParameters_CreatesUnauthenticatedSession(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            async session =>
            {
                Assert.False(session.IsAuthenticated);

                var cardData = await session.GetDataAsync(0x66, cancellationToken: CancellationTokenSource.Token);
                Assert.True(cardData.Length > 0);
            },
            scpKeyParams: null,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the DI factory correctly passes protocol configuration.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_WithConfiguration_AppliesSettings(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            async session =>
            {
                Assert.NotNull(session);

                // TODO: This test doesn't currently validate that ForceShortApdus is actually applied.
                // Consider asserting an observable behavior change or removing this test.
                var keyInfo = await session.GetKeyInfoAsync(CancellationTokenSource.Token);
                Assert.NotEmpty(keyInfo);
            },
            configuration: new ProtocolConfiguration { ForceShortApdus = true },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the factory-created session can perform key operations.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_CreatedSession_CanQueryKeyInfo(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            async session =>
            {
                var keyInfo = await session.GetKeyInfoAsync(CancellationTokenSource.Token);

                // TODO: This exact key count may be brittle across firmware/models; prefer asserting invariants.
                Assert.Equal(
                    state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3,
                    keyInfo.Count);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the factory-created session can retrieve card recognition data.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Factory_CreatedSession_CanGetCardRecognitionData(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionFromDIAsync(
            resetBeforeUse: true,
            async session =>
            {
                var cardData = await session.GetCardRecognitionDataAsync(CancellationTokenSource.Token);
                Assert.True(cardData.Length > 0);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);
}
