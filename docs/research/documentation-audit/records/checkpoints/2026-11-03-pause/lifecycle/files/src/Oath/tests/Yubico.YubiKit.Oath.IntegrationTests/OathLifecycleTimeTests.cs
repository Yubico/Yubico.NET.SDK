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
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public sealed class OathLifecycleTimeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, CustomFilter = typeof(OathProtocolResponseTests.Before563))]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task Timestamps_ExactRfcVectorsAndResetRefresh(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await WithCleanupAsync(state, () => state.WithOathSessionAsync(async session =>
        {
            output.WriteLine($"Executed device serial={state.SerialNumber}, firmware={state.FirmwareVersion}");
            byte[] seed = "12345678901234567890"u8.ToArray();
            try
            {
                using var data = new CredentialData
                {
                    Name = "time",
                    OathType = OathType.Totp,
                    HashAlgorithm = OathHashAlgorithm.Sha1,
                    Secret = seed,
                    Digits = 8
                };
                await session.PutCredentialAsync(data, timeout.Token);
                var credential = Assert.Single(await session.ListCredentialsAsync(timeout.Token));
                // RFC 6238 Appendix B, plus epoch and exact step boundaries from RFC 4226.
                foreach ((long timestamp, string expected, long from, long to) in new (long, string, long, long)[]
                {
                    (0, "84755224", 0, 30), (29, "84755224", 0, 30),
                    (30, "94287082", 30, 60), (59, "94287082", 30, 60),
                    (60, "37359152", 60, 90), (20000000000, "65353130", 19999999980, 20000000010)
                })
                {
                    var single = await session.CalculateCodeAsync(credential, timestamp, timeout.Token);
                    var aggregate = Assert.Single(await session.CalculateAllAsync(timestamp, timeout.Token));
                    Assert.Equal(expected, single.Value);
                    Assert.Equal(from, single.ValidFrom);
                    Assert.Equal(to, single.ValidTo);
                    Assert.Equal(single, aggregate.Value);
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateCodeAsync(credential, -1, timeout.Token));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateAllAsync(long.MaxValue, timeout.Token));
                string before = session.DeviceId;
                await session.ResetAsync(timeout.Token);
                Assert.NotEqual(before, session.DeviceId);
                Assert.True(session.IsInitialized);
                Assert.False(session.IsPasswordProtected);
                Assert.Empty(await session.ListCredentialsAsync(timeout.Token));
                output.WriteLine("Epoch, step boundaries, future RFC vector, aggregate equality and post-reset LIST passed.");
            }
            finally { CryptographicOperations.ZeroMemory(seed); }
        }, cancellationToken: timeout.Token));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ResetWithScp03_DefaultKeys_RequiresProtectedOperationAfterReset(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var parameters = Scp03KeyParameters.Default;
        output.WriteLine($"SCP03 probe device serial={state.SerialNumber}, firmware={state.FirmwareVersion}; no Security Domain reset or key changes.");
        // Establish SCP before any reset. If default credentials are unavailable, this is a
        // credential/environment limitation, not evidence about reset recovery.
        OathSession session;
        try
        {
            session = await state.Device.CreateOathSessionAsync(
                new SessionCreationOptions { ScpKeyParameters = parameters }, timeout.Token);
        }
        catch (ApduException exception) when (exception.SW == unchecked((short)0x6A88) &&
                                             exception.Cla == 0x80 && exception.Ins == 0x50)
        {
            output.WriteLine("Default SCP03 key reference unavailable (6A88) before RESET; recovery was not exercised.");
            Skip.If(true, "Default SCP03 key reference unavailable (6A88); SCP reset recovery not exercised.");
            throw;
        }
        await using var sessionLifetime = session;
        Assert.True(session.IsAuthenticated);
        await session.ListCredentialsAsync(timeout.Token);
        output.WriteLine("Default SCP03 establishment and initial protected LIST passed.");
        Exception? primaryFailure = null;
        try
        {
            await session.ResetAsync(timeout.Token);
            Assert.True(session.IsAuthenticated);
            Assert.Empty(await session.ListCredentialsAsync(timeout.Token));
            output.WriteLine("SCP03 RESET and subsequent protected LIST passed.");
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            // End the SCP session before independent bounded OATH-only cleanup.
            try { await session.DisposeAsync(); }
            catch (Exception exception) when (primaryFailure is not null)
            {
                output.WriteLine($"SCP session teardown also failed ({exception.GetType().Name}); preserving the primary failure.");
            }
            finally { await CleanupAsync(state, primaryFailure); }
        }
    }

    private async Task WithCleanupAsync(YubiKeyTestState state, Func<Task> operation)
    {
        Exception? primaryFailure = null;
        try { await operation(); }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally { await CleanupAsync(state, primaryFailure); }
    }

    private async Task CleanupAsync(YubiKeyTestState state, Exception? primaryFailure)
    {
        // A cancelled operation can leave OATH changed. Cleanup has its own finite budget.
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await state.WithOathSessionAsync(_ => Task.CompletedTask, cancellationToken: cleanup.Token); }
        catch (Exception exception) when (primaryFailure is not null)
        {
            output.WriteLine($"OATH cleanup also failed ({exception.GetType().Name}); preserving the primary failure.");
        }
    }
}
