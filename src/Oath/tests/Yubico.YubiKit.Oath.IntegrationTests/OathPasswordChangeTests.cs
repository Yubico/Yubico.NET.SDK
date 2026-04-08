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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

/// <summary>
///     Integration tests for OATH password (access key) change workflows.
///     Validates setting a password, changing it, and verifying the new password works.
/// </summary>
public class OathPasswordChangeTests
{
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    /// <summary>
    ///     Sets a password, changes it to a new password, then verifies the new password
    ///     works for unlocking the OATH application.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task PasswordChange_SetThenChange_NewPasswordUnlocks(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            string originalPassword = "original-password-123";
            string newPassword = "changed-password-456";

            byte[] originalKey = session.DeriveKey(originalPassword);

            try
            {
                // Set the initial password
                await session.SetKeyAsync(originalKey, NewToken());

                // Verify the device is now locked on a fresh session
                await using var lockedSession1 = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.True(lockedSession1.IsLocked);

                // Unlock with original password
                byte[] validateKey1 = lockedSession1.DeriveKey(originalPassword);
                try
                {
                    await lockedSession1.ValidateAsync(validateKey1, NewToken());
                    Assert.False(lockedSession1.IsLocked);

                    // Change the password: set a new key on the unlocked session
                    byte[] newKey = lockedSession1.DeriveKey(newPassword);
                    try
                    {
                        await lockedSession1.SetKeyAsync(newKey, NewToken());
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(newKey);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(validateKey1);
                }

                // Verify the NEW password works on a fresh session
                await using var lockedSession2 = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.True(lockedSession2.IsLocked);

                byte[] validateKey2 = lockedSession2.DeriveKey(newPassword);
                try
                {
                    await lockedSession2.ValidateAsync(validateKey2, NewToken());
                    Assert.False(lockedSession2.IsLocked);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(validateKey2);
                }

                // Verify the OLD password no longer works
                await using var lockedSession3 = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.True(lockedSession3.IsLocked);

                byte[] oldKey = lockedSession3.DeriveKey(originalPassword);
                try
                {
                    await Assert.ThrowsAnyAsync<Exception>(async () =>
                        await lockedSession3.ValidateAsync(oldKey, NewToken()));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(oldKey);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(originalKey);

                // Clean up: remove the password so other tests are unaffected.
                // The original session was created with reset, so it should still be valid.
                // We need to unlock with the new password first since we changed it.
                byte[] cleanupKey = session.DeriveKey(newPassword);
                try
                {
                    // The original session may have lost its auth state, so unlock again
                    // by setting the key to nothing (unset). Since we changed the password
                    // on a different session, the original session's auth may be stale.
                    // Best approach: open a new session, validate, then unset.
                    await using var cleanupSession = await state.Device
                        .CreateOathSessionAsync(cancellationToken: NewToken());

                    if (cleanupSession.IsLocked)
                    {
                        await cleanupSession.ValidateAsync(cleanupKey, NewToken());
                    }

                    await cleanupSession.UnsetKeyAsync(NewToken());
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(cleanupKey);
                }
            }
        }, cancellationToken: NewToken());

    /// <summary>
    ///     Verifies that setting and then removing a password restores
    ///     the OATH application to an unlocked state.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task PasswordRemoval_SetThenUnset_RestoresUnlockedState(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            string password = "temporary-password-789";
            byte[] key = session.DeriveKey(password);

            try
            {
                // Set a password
                await session.SetKeyAsync(key, NewToken());

                // Verify it's locked on a fresh session
                await using var lockedSession = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.True(lockedSession.IsLocked);

                // Remove the password from the original (still-authenticated) session
                await session.UnsetKeyAsync(NewToken());

                // Verify the device is now unlocked on a fresh session
                await using var unlockedSession = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.False(unlockedSession.IsLocked);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }, cancellationToken: NewToken());
}
