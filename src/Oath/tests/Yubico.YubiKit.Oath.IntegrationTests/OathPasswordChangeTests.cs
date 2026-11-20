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
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

/// <summary>
///     Integration tests for OATH password (access key) change workflows.
///     Validates setting a password, changing it, and verifying the new password works.
/// </summary>
/// <remarks>
///     These tests observe lock state from a <em>fresh</em> session, which is the whole point — a
///     session that already holds the applet cannot tell you what a new caller would see. Each such
///     session is therefore scoped and disposed before the next one opens. Each convenience session owns a
///     connection to the physical key, and overlapping connections are refused with
///     <c>ConnectionInUseException</c>.
/// </remarks>
public class OathPasswordChangeTests
{
    /// <summary>
    ///     Runs <paramref name="action" /> against a freshly opened OATH session and disposes it before
    ///     returning, so the caller can immediately open the next one.
    /// </summary>
    private static async Task WithFreshSessionAsync(
        YubiKeyTestState state, CancellationToken token, Func<OathSession, Task> action)
    {
        await state.WithOathSessionAsync(action, resetBeforeUse: false, cancellationToken: token);
    }

    /// <summary>
    ///     Derives an access key from <paramref name="password" />, hands it to <paramref name="action" />,
    ///     and zeroes it afterwards.
    /// </summary>
    private static async Task WithDerivedKeyAsync(OathSession session, string password, Func<byte[], Task> action)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[]? key = null;
        try
        {
            key = session.DeriveKey(passwordBytes);
            await action(key);
        }
        finally
        {
            if (key is not null)
                CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    /// <summary>
    ///     Sets a password, changes it to a new password, then verifies the new password
    ///     works for unlocking the OATH application.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task PasswordChange_SetThenChange_NewPasswordUnlocks(YubiKeyTestState state)
    {
        const string originalPassword = "original-password-123";
        const string newPassword = "changed-password-456";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        string? activePassword = null;
        Exception? primaryFailure = null;

        try
        {
            // Reset the applet and set the initial password.
            await state.WithOathSessionAsync(
                session => WithDerivedKeyAsync(session, originalPassword, async key =>
                {
                    await session.SetKeyAsync(key, token);
                    activePassword = originalPassword;
                }),
                cancellationToken: token);

            // Fresh session: must see the lock, unlock with the original, then rotate the key.
            await WithFreshSessionAsync(state, token, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, originalPassword, async key =>
                {
                    await session.ValidateAsync(key, token);
                    Assert.False(session.IsLocked);
                });

                await WithDerivedKeyAsync(session, newPassword, async key =>
                {
                    await session.SetKeyAsync(key, token);
                    activePassword = newPassword;
                });
            });

            // Fresh session: the NEW password unlocks.
            await WithFreshSessionAsync(state, token, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, newPassword, async key =>
                {
                    await session.ValidateAsync(key, token);
                    Assert.False(session.IsLocked);
                });
            });

            // Fresh session: require device rejection of the old key, not cancellation or a failed exchange.
            await WithFreshSessionAsync(state, token, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, originalPassword, async key =>
                {
                    var rejection = await Assert.ThrowsAsync<OathException>(() => session.ValidateAsync(key, token));
                    Assert.Equal(OathFailureReason.WrongPassword, rejection.Reason);
                });
            });
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            // Only a confirmed SET CODE tells us which key to try. A failed exchange can leave state uncertain.
            if (activePassword is not null)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try
                {
                    await WithFreshSessionAsync(state, cleanup.Token, async session =>
                    {
                        if (session.IsLocked)
                            await WithDerivedKeyAsync(session, activePassword, key => session.ValidateAsync(key, cleanup.Token));
                        await session.UnsetKeyAsync(cleanup.Token);
                    });
                }
                catch when (primaryFailure is not null) { }
            }
        }
    }

    /// <summary>
    ///     Verifies that setting and then removing a password restores
    ///     the OATH application to an unlocked state.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task PasswordRemoval_SetThenUnset_RestoresUnlockedState(YubiKeyTestState state)
    {
        const string password = "temporary-password-789";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        bool keyInstalled = false;
        Exception? primaryFailure = null;

        try
        {
            // Track confirmed installation before fixture disposal can fail.
            await state.WithOathSessionAsync(
                session => WithDerivedKeyAsync(session, password, async key =>
                {
                    await session.SetKeyAsync(key, token);
                    keyInstalled = true;
                }),
                cancellationToken: token);

            // Confirm the lock independently, then remove the password.
            await WithFreshSessionAsync(state, token, async session =>
            {
                Assert.True(session.IsLocked);
                await WithDerivedKeyAsync(session, password, async key =>
                {
                    await session.ValidateAsync(key, token);
                    await session.UnsetKeyAsync(token);
                    keyInstalled = false;
                });
            });

            await WithFreshSessionAsync(state, token, session =>
            {
                Assert.False(session.IsLocked);
                return Task.CompletedTask;
            });
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            if (keyInstalled)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try
                {
                    await WithFreshSessionAsync(state, cleanup.Token, async session =>
                    {
                        if (session.IsLocked)
                            await WithDerivedKeyAsync(session, password, key => session.ValidateAsync(key, cleanup.Token));
                        await session.UnsetKeyAsync(cleanup.Token);
                    });
                }
                catch when (primaryFailure is not null) { }
            }
        }
    }
}