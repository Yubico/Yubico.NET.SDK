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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Management.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for the <see cref="ManagementSessionFactory" /> registered via
///     <see cref="DependencyInjection.AddYubiKeyManager" />.
/// </summary>
/// <remarks>
///     These tests verify that the DI-registered factory correctly creates working sessions
///     with real YubiKey hardware. They complement the unit tests in
///     <c>Yubico.YubiKit.Management.UnitTests.DependencyInjectionTests</c> which only
///     test registration mechanics without invoking the factory.
/// </remarks>
public class ManagementSession_DependencyInjectionTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(30));

    /// <summary>
    ///     Verifies the DI factory creates a working session that can query the device.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "4.1.0")]
    public async Task Factory_WithRealConnection_CreatesWorkingSession(YubiKeyTestState state) =>
        await state.WithManagementSessionFromDIAsync(
            async session =>
            {
                Assert.NotNull(session);

                var info = await session.GetDeviceInfoAsync();
                Assert.True(info.SerialNumber > 0);
                Assert.Equal(state.SerialNumber, info.SerialNumber);
            },
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the DI factory correctly passes protocol configuration.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "4.1.0")]
    public async Task Factory_WithConfiguration_AppliesSettings(YubiKeyTestState state) =>
        await state.WithManagementSessionFromDIAsync(
            async session =>
            {
                var info = await session.GetDeviceInfoAsync();
                Assert.True(info.SerialNumber > 0);
                Assert.Equal(state.SerialNumber, info.SerialNumber);
            },
            configuration: new ProtocolConfiguration { ForceShortApdus = true },
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies the factory-created session can retrieve device information.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "4.1.0")]
    public async Task Factory_CreatedSession_CanGetDeviceInfo(YubiKeyTestState state) =>
        await state.WithManagementSessionFromDIAsync(
            async session =>
            {
                var info = await session.GetDeviceInfoAsync();

                Assert.True(info.SerialNumber > 0);
                Assert.NotNull(info.FirmwareVersion);
            },
            cancellationToken: CancellationTokenSource.Token);
}
