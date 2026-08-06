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
///         The risk this effort owns is not that the operation fails — of course it fails, the card is gone.
///         It is that the CCID interface lease could be STRANDED. The lease is released in the connection's
///         disposal path, and there is deliberately no finalizer backstop, so if removal made disposal hang
///         or throw before the release, the interface would stay marked in-use for the process lifetime and
///         every later open would be refused with <c>ConnectionInUseException</c> — on a key the user has
///         already plugged back in.
///     </para>
///     <para>
///         The test therefore asserts three things after the operator pulls the key: the in-flight API fails
///         within a bounded window rather than hanging, disposal still completes, and a subsequent connect
///         attempt reports something OTHER than <c>ConnectionInUseException</c>. That last assertion is the
///         real content: it distinguishes "device is gone" from "we stranded our own lease".
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
            // Must complete even though the card vanished; this is what releases the interface lease.
            await connection.DisposeAsync();
        }

        Assert.True(
            removalFailure is not null,
            $"The key was not removed within {RemovalWindow.TotalSeconds:F0}s, so this run proves nothing. " +
            "Re-run and physically unplug the key while it is executing.");

        // The lease must be gone. A second open may fail because the device is absent, but it must never
        // fail because this process still believes it holds the interface.
        var reopen = await Record.ExceptionAsync(async () =>
        {
            await using var second = await state.Device.ConnectAsync<ISmartCardConnection>();
        });

        Assert.False(
            reopen is ConnectionInUseException,
            "The CCID lease was stranded by hotplug removal: reopening reported the interface as still in " +
            $"use by this process. Removal failure was: {removalFailure}");
    }

    /// <summary>
    ///     Calls a cheap PIV read until it fails (the key was pulled) or the window expires. Returns the
    ///     failure, or <see langword="null" /> if the key was never removed.
    /// </summary>
    private static async Task<Exception?> PollUntilRemovedAsync(PivSession session)
    {
        var deadline = DateTime.UtcNow + RemovalWindow;

        while (DateTime.UtcNow < deadline)
        {
            // Bound each attempt so a wedged native call surfaces as a hang we can see, not an infinite wait.
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