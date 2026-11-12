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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
///     Register row D3: a key physically removed while a session is open.
/// </summary>
/// <remarks>
///     <para>
///         Exercises removal failure, connection disposal and a later open. Checked native teardown releases
///         the grouped physical-key claim only with release proof; uncertain release can retain/quarantine it.
///         Best-effort native finalization is not deterministic session or connection cleanup.
///     </para>
///     <para>
///         The poll accepts any read exception, then awaits disposal and checks that a later open does not
///         report ConnectionInUseException. It does not independently identify removal as the failure cause
///         or observe native release. A token is not a native I/O deadline, and the polling window is checked
///         between awaits. The existing reopening expectation needs separate review for unproven release.
///     </para>
///     <para>
///         Requires a human to remove the key during execution, so it is marked
///         <see cref="TestCategories.RequiresUserPresence" /> and excluded from smoke runs. Run it
///         explicitly and pull the key when the run begins.
///     </para>
/// </remarks>
public class PivHotplugContentionTests
{
    private static readonly TimeSpan RemovalWindow = TimeSpan.FromSeconds(150);

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease(
        YubiKeyTestState state)
    {
        var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        Exception? removalFailure;

        try
        {
            await using var session = await PivSession.CreateAsync(connection);

            // Liveness before removal: if this throws, the rig is wrong and the result means nothing.
            _ = await session.GetSerialNumberAsync();

            removalFailure = await PollUntilRemovedAsync(session);
        }
        finally
        {
            // Await checked teardown. Its outcome is separate from proof of native release;
            // an uncertain release can retain the original claim rather than permit an unsafe reopen.
            await connection.DisposeAsync();
        }

        Assert.True(
            removalFailure is not null,
            $"The key was not removed within {RemovalWindow.TotalSeconds:F0}s, so this run proves nothing. " +
            "Re-run and physically unplug the key while it is executing.");

        // Retain this test's existing no-ConnectionInUse expectation. It does not establish a general
        // unconditional-release policy; genuinely unproven native release has a different recovery boundary.
        var reopen = await Record.ExceptionAsync(async () =>
        {
            await using var second = await state.Device.ConnectAsync<ISmartCardConnection>();
        });

        Assert.False(
            reopen is ConnectionInUseException,
            "The physical-key lease was stranded by hotplug removal: reopening reported the key as still in " +
            $"use by this process. Removal failure was: {removalFailure}");
    }

    /// <summary>
    ///     Calls a PIV serial read until any exception or the between-await window check expires.
    ///     Returns that exception, or null if no read failed; a failure alone does not identify physical removal.
    /// </summary>
    private static async Task<Exception?> PollUntilRemovedAsync(PivSession session)
    {
        var deadline = DateTime.UtcNow + RemovalWindow;

        while (DateTime.UtcNow < deadline)
        {
            // Expiry can prevent exchange admission; it does not interrupt an admitted native call
            // or bound its completion. The loop's deadline is checked only after awaited work returns.
            using var attemptTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var failure = await Record.ExceptionAsync(
                () => session.GetSerialNumberAsync(attemptTimeout.Token));

            if (failure is not null)
                return failure;

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return null;
    }
}
