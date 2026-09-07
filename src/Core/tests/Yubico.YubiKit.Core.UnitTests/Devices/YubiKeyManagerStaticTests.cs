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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// The two things about <see cref="YubiKeyManager"/> that are only true of the facade: what its
/// public surface does <em>not</em> expose, and the lazy create/recreate policy around its static
/// manager.
/// </summary>
/// <remarks>
/// <para>
/// Everything the facade merely forwards is asserted where fakes can stand in for hardware, and is
/// deliberately not repeated here: discovery and monitoring lifecycle on
/// <c>YubiKeyDeviceManagerTests</c> and <c>YubiKeyDeviceMonitorServiceTests</c>, watcher delivery
/// and cancellation on <c>DeviceEventHubTests</c> and <c>YubiKeyDeviceRepositoryTests</c>, and the
/// real zero-configuration scan on the integration suite's <c>YubiKeyManagerTests</c>. Duplicating
/// those here bought no coverage and cost a real device scan per test.
/// </para>
/// <para>
/// What remains is hermetic. Enumerating <see cref="YubiKeyManager.WatchAsync"/> constructs the
/// manager but starts no listener and runs no scan, so nothing here consumes the process-wide
/// discovery-worker pool that <see cref="DiscoveryWorkerAdmissionCollection"/> serializes — which
/// is why this class no longer joins it.
/// </para>
/// <para>
/// The negative assertions below are the ones compilation cannot make for us: a member that should
/// be absent stays absent only if something checks.
/// </para>
/// </remarks>
public class YubiKeyManagerStaticTests : IAsyncLifetime
{
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        // Enumerating a watcher creates the static manager, so every test here leaves one behind.
        await YubiKeyManager.ShutdownAsync();
    }

    [Fact]
    public void YubiKeyManager_WatchAsync_IsTheOnlyDeviceChangeStream()
    {
        var method = typeof(YubiKeyManager).GetMethod(
            nameof(YubiKeyManager.WatchAsync),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<DeviceEvent>), method.ReturnType);

        // The observable surface was removed outright; no obsolete shim, alias, or overload survives.
        Assert.DoesNotContain(
            typeof(YubiKeyManager).GetMembers(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => m.Name.Contains("DeviceChanges", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(YubiKeyManager).GetMembers(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => m is System.Reflection.PropertyInfo p &&
                 typeof(IObservable<DeviceEvent>).IsAssignableFrom(p.PropertyType));
    }

    [Fact]
    public void DeviceAction_DoesNotContainUpdated()
    {
        // DeviceAction.Updated was removed per PRD
        Assert.False(Enum.IsDefined(typeof(DeviceAction), "Updated"));
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_CanBeEnumeratedWithoutStartingMonitoring()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Enumerating (which is what actually subscribes) must not auto-start monitoring.
        await using var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync();

        Assert.False(YubiKeyManager.IsMonitoring);

        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
    }

    /// <summary>
    /// Shutting down when nothing was ever created must be a no-op rather than a null dereference.
    /// </summary>
    [Fact]
    public async Task YubiKeyManager_ShutdownAsync_IsIdempotent()
    {
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    /// <summary>
    /// The facade tolerates being asked to stop, and being asked whether it is monitoring, before
    /// anything has ever been created.
    /// </summary>
    /// <remarks>
    /// This is the one piece of monitoring behaviour that is the facade's own rather than the
    /// manager's: <c>StopMonitoring</c> and <c>IsMonitoring</c> both read the static field and
    /// null-guard it, so with no manager they must be a no-op and <see langword="false"/> instead
    /// of a null dereference. Everything past that guard forwards to
    /// <c>YubiKeyDeviceManager</c>/<c>YubiKeyDeviceMonitorService</c> and is asserted there,
    /// against fakes, without starting real listeners.
    /// </remarks>
    [Fact]
    public void YubiKeyManager_StopMonitoring_WithNoManager_IsNoOp()
    {
        Assert.False(YubiKeyManager.IsMonitoring);

        YubiKeyManager.StopMonitoring();

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    /// <summary>
    /// After shutdown the static facade must build a new manager rather than hand back the disposed
    /// one, whose repository would end every enumeration immediately.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test creates a manager <em>before</em> shutting down, which is what makes it about
    /// recreation at all. Shutting down first would only prove ordinary first-time lazy creation,
    /// since cleanup already leaves the static field null - the assertion would hold just as well
    /// with the recreation logic removed.
    /// </para>
    /// <para>
    /// Two things are asserted, and both are needed. The pre-shutdown enumeration must end
    /// normally, proving the old manager really was disposed rather than left running. The
    /// post-shutdown enumeration must then be cancellable, which is the liveness claim: a dead
    /// sequence ends with no elements and so never reaches the cancellation, whereas a live one
    /// waits. Nothing here starts monitoring, so the fresh repository has no publisher and cannot
    /// deliver an element that would end the wait for real reasons.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task YubiKeyManager_WatchAsync_AfterShutdown_RecreatesTheContext()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Enumerating is what subscribes, so this is what actually creates the manager.
        var firstEnumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await using (firstEnumerator.ConfigureAwait(false))
        {
            var pendingOnFirst = firstEnumerator.MoveNextAsync();

            await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

            // The disposed manager completes its stream rather than leaving the watcher hanging.
            Assert.False(await pendingOnFirst);
        }

        // A second watcher must be served by a newly built manager, not the disposed one.
        var secondEnumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await using (secondEnumerator.ConfigureAwait(false))
        {
            var pendingOnSecond = secondEnumerator.MoveNextAsync();

            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pendingOnSecond);
        }
    }
}