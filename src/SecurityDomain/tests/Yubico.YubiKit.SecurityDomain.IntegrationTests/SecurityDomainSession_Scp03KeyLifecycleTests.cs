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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration tests for SCP03 key lifecycle operations: import, delete, and replace (rotate).
/// </summary>
public class SecurityDomainSession_Scp03KeyLifecycleTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Imports two custom SCP03 key sets, then deletes them one at a time, verifying
    ///     each deletion. Follows the pattern from the Java SDK's testDeleteKey: to delete
    ///     a key you must authenticate with a different key that remains on the device.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutKeyAsync_ThenDeleteKeyAsync_ImportsAndRemovesKey(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;

        // Use key material distinct from the default keys (0x40..0x4F) to avoid ambiguity.
        byte[] keyBytes1 =
        [
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF
        ];

        byte[] keyBytes2 =
        [
            0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7,
            0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF
        ];

        var keyRef1 = new KeyReference(0x01, 0x10);
        var keyRef2 = new KeyReference(0x01, 0x55);
        using var staticKeys1 = new StaticKeys(keyBytes1, keyBytes1, keyBytes1);
        using var staticKeys2 = new StaticKeys(keyBytes2, keyBytes2, keyBytes2);
        var keyParams1 = new Scp03KeyParameters(keyRef1, staticKeys1);
        var keyParams2 = new Scp03KeyParameters(keyRef2, staticKeys2);

        // Session 1: Reset and import first custom key set using default keys
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                await session.PutKeyAsync(keyRef1, staticKeys1, 0, ct);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: ct);

        // Session 2: Authenticate with first key, import second key set
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                await session.PutKeyAsync(keyRef2, staticKeys2, 0, ct);
            }, scpKeyParams: keyParams1, cancellationToken: ct);

        // Session 3: Authenticate with second key, delete first key
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                await session.DeleteKeyAsync(keyRef1, cancellationToken: ct);
            }, scpKeyParams: keyParams2, cancellationToken: ct);

        // Session 4: Verify first key no longer works
        await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: keyParams1,
                cancellationToken: ct);
        });

        // Session 5: Verify second key still works, and first key is gone from key info
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                var keyInfo = await session.GetKeyInfoAsync(ct);
                Assert.DoesNotContain(keyInfo,
                    entry => entry.KeyReference.Kvn == keyRef1.Kvn);
                Assert.Contains(keyInfo,
                    entry => entry.KeyReference.Kvn == keyRef2.Kvn);
            }, scpKeyParams: keyParams2, cancellationToken: ct);

        // Session 6: Delete the last remaining custom key
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                await session.DeleteKeyAsync(keyRef2, deleteLast: true, cancellationToken: ct);
            }, scpKeyParams: keyParams2, cancellationToken: ct);

        // Session 7: Verify second key no longer works
        await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: keyParams2,
                cancellationToken: ct);
        });
    }

    /// <summary>
    ///     Imports a custom SCP03 key set, then replaces (rotates) it with a new key set using the replaceKvn parameter.
    ///     Verifies that the original key no longer works and the replacement key authenticates successfully.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutKeyAsync_WithReplaceKvn_RotatesKey(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;

        byte[] originalKeyBytes =
        [
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        byte[] rotatedKeyBytes =
        [
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
            0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F
        ];

        var originalKeyRef = new KeyReference(0x01, 0x01);
        var rotatedKeyRef = new KeyReference(0x01, 0x02);

        using var originalStaticKeys = new StaticKeys(originalKeyBytes, originalKeyBytes, originalKeyBytes);
        using var rotatedStaticKeys = new StaticKeys(rotatedKeyBytes, rotatedKeyBytes, rotatedKeyBytes);

        // Session 1: Import original custom key set using default keys
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                await session.PutKeyAsync(originalKeyRef, originalStaticKeys, 0, ct);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: ct);

        // Session 2: Authenticate with original key, then replace it with a rotated key
        var originalKeyParams = new Scp03KeyParameters(originalKeyRef, originalStaticKeys);
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                // PutKeyAsync with replaceKvn=originalKeyRef.Kvn replaces the original key
                await session.PutKeyAsync(rotatedKeyRef, rotatedStaticKeys, originalKeyRef.Kvn, ct);
            }, scpKeyParams: originalKeyParams, cancellationToken: ct);

        // Session 3: Verify the rotated key works for authentication
        var rotatedKeyParams = new Scp03KeyParameters(rotatedKeyRef, rotatedStaticKeys);
        await state.WithSecurityDomainSessionAsync(false,
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            }, scpKeyParams: rotatedKeyParams, cancellationToken: ct);

        // Session 4: Verify the original key no longer works
        await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: originalKeyParams,
                cancellationToken: ct);
        });
    }
}
