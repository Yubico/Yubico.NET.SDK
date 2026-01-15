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

using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration tests for the public <see cref="IYubiKeyExtensions" /> methods.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate the public API entry points that users will call.
///         They serve as both smoke tests and usage examples.
///     </para>
///     <para>
///         Session behavior (GetKeyInfo, PutKey, Reset, etc.) is tested in
///         <see cref="SecurityDomainSession_Scp03Tests" />. These tests focus on verifying
///         the extension method wiring is correct.
///     </para>
/// </remarks>
public class IYubiKeyExtensionsTests
{
    private static readonly CancellationTokenSource Cts = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Verifies <see cref="IYubiKeyExtensions.CreateSecurityDomainSessionAsync" />
    ///     creates a working authenticated session.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateSecurityDomainSessionAsync_WithScp03_CreatesAuthenticatedSession(YubiKeyTestState state)
    {
        // Arrange - Reset SD to default keys
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            _ => Task.CompletedTask,
            cancellationToken: Cts.Token);

        var yubiKey = state.Device;

        // Act - Use PUBLIC extension method (the intended user entry point)
        using var session = await yubiKey.CreateSecurityDomainSessionAsync(
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: Cts.Token);

        // Assert
        Assert.NotNull(session);
        Assert.True(session.IsAuthenticated);
        Assert.True(session.IsInitialized);
    }

    /// <summary>
    ///     Verifies <see cref="IYubiKeyExtensions.CreateSecurityDomainSessionAsync" />
    ///     creates an unauthenticated session when no SCP params provided.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateSecurityDomainSessionAsync_WithoutScp_CreatesUnauthenticatedSession(YubiKeyTestState state)
    {
        // Arrange
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            _ => Task.CompletedTask,
            cancellationToken: Cts.Token);

        var yubiKey = state.Device;

        // Act - No SCP params = unauthenticated
        using var session = await yubiKey.CreateSecurityDomainSessionAsync(
            cancellationToken: Cts.Token);

        // Assert
        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        Assert.True(session.IsInitialized);
    }

    /// <summary>
    ///     Verifies <see cref="IYubiKeyExtensions.CreateSecurityDomainSessionAsync" />
    ///     creates a session that can perform operations.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateSecurityDomainSessionAsync_SessionCanQueryKeyInfo(YubiKeyTestState state)
    {
        // Arrange
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            _ => Task.CompletedTask,
            cancellationToken: Cts.Token);

        var yubiKey = state.Device;

        // Act
        using var session = await yubiKey.CreateSecurityDomainSessionAsync(
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: Cts.Token);

        var keyInfo = await session.GetKeyInfoAsync(Cts.Token);

        // Assert
        Assert.NotEmpty(keyInfo);
        Assert.Contains(keyInfo, k => k.KeyReference.Kvn == 0xFF);
        Assert.Equal(
            state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3,
            keyInfo.Count);
    }

    /// <summary>
    ///     Verifies <see cref="IYubiKeyExtensions.GetSecurityDomainKeyInfoAsync" />
    ///     convenience method works correctly.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetSecurityDomainKeyInfoAsync_ReturnsKeyInfo(YubiKeyTestState state)
    {
        // Arrange
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            _ => Task.CompletedTask,
            cancellationToken: Cts.Token);

        var yubiKey = state.Device;

        // Act - Convenience method: creates session, queries, disposes
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync(
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: Cts.Token);

        // Assert
        Assert.NotEmpty(keyInfo);
        Assert.Contains(keyInfo, k => k.KeyReference.Kvn == 0xFF);
    }

    /// <summary>
    ///     Verifies session disposal releases the connection.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateSecurityDomainSessionAsync_DisposalReleasesConnection(YubiKeyTestState state)
    {
        // Arrange
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            _ => Task.CompletedTask,
            cancellationToken: Cts.Token);

        var yubiKey = state.Device;

        // Act - Create and dispose session
        var session = await yubiKey.CreateSecurityDomainSessionAsync(
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: Cts.Token);
        session.Dispose();

        // Assert - Can create another session (connection was released)
        using var session2 = await yubiKey.CreateSecurityDomainSessionAsync(
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: Cts.Token);

        Assert.True(session2.IsAuthenticated);
    }
}
