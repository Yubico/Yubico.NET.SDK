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
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    /// <summary>
    ///     Runs <paramref name="action" /> against a freshly opened OATH session and disposes it before
    ///     returning, so the caller can immediately open the next one.
    /// </summary>
    private static async Task WithFreshSessionAsync(YubiKeyTestState state, Func<OathSession, Task> action)
    {
        await using var session = await state.Device.CreateOathSessionAsync(cancellationToken: NewToken());
        await action(session);
    }

    /// <summary>
    ///     Derives an access key from <paramref name="password" />, hands it to <paramref name="action" />,
    ///     and zeroes it afterwards.
    /// </summary>
    private static async Task WithDerivedKeyAsync(OathSession session, string password, Func<byte[], Task> action)
    {
        var key = session.DeriveKey(Encoding.UTF8.GetBytes(password));
        try
        {
            await action(key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
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

        try
        {
            // Reset the applet and set the initial password.
            await state.WithOathSessionAsync(
                session => WithDerivedKeyAsync(session, originalPassword,
                    key => session.SetKeyAsync(key, NewToken())),
                cancellationToken: NewToken());

            // Fresh session: must see the lock, unlock with the original, then rotate the key.
            await WithFreshSessionAsync(state, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, originalPassword, async key =>
                {
                    await session.ValidateAsync(key, NewToken());
                    Assert.False(session.IsLocked);
                });

                await WithDerivedKeyAsync(session, newPassword,
                    key => session.SetKeyAsync(key, NewToken()));
            });

            // Fresh session: the NEW password unlocks.
            await WithFreshSessionAsync(state, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, newPassword, async key =>
                {
                    await session.ValidateAsync(key, NewToken());
                    Assert.False(session.IsLocked);
                });
            });

            // Fresh session: the OLD password no longer does.
            await WithFreshSessionAsync(state, async session =>
            {
                Assert.True(session.IsLocked);

                await WithDerivedKeyAsync(session, originalPassword, key =>
                    Assert.ThrowsAnyAsync<Exception>(() => session.ValidateAsync(key, NewToken())));
            });
        }
        finally
        {
            // Leave the applet unlocked for whatever runs next.
            await WithFreshSessionAsync(state, async session =>
            {
                await WithDerivedKeyAsync(session, newPassword, async key =>
                {
                    if (session.IsLocked)
                    {
                        await session.ValidateAsync(key, NewToken());
                    }

                    await session.UnsetKeyAsync(NewToken());
                });
            });
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

        // Reset the applet and set a password.
        await state.WithOathSessionAsync(
            session => WithDerivedKeyAsync(session, password,
                key => session.SetKeyAsync(key, NewToken())),
            cancellationToken: NewToken());

        // Fresh session: the lock is visible, and removing the password requires unlocking first —
        // the session that set the key is gone, so its authenticated state went with it.
        await WithFreshSessionAsync(state, async session =>
        {
            Assert.True(session.IsLocked);

            await WithDerivedKeyAsync(session, password, async key =>
            {
                await session.ValidateAsync(key, NewToken());
                await session.UnsetKeyAsync(NewToken());
            });
        });

        // Fresh session: no lock remains.
        await WithFreshSessionAsync(state, session =>
        {
            Assert.False(session.IsLocked);
            return Task.CompletedTask;
        });
    }
}
