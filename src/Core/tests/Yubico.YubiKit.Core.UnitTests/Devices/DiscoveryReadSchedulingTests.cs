// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using static Yubico.YubiKit.Core.UnitTests.Devices.FindYubiKeysScopeTests;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
[Trait("Category", "RuntimeResilience")]
public class DiscoveryReadSchedulingTests
{
    [Fact]
    public async Task ReadScopeRetirement_QueuedPidAdmission_DoesNotInlineReadCompletion()
    {
        var gates = DiscoveryReadAdmission.GatesFor(0x0407);
        using var owner = await DiscoveryReadAdmission.AcquireAsync(gates, TestContext.Current.CancellationToken);
        var scope = ProtocolDeviceInfo.CreateScope();
        Assert.True(scope.TryCaptureEpoch(out var epoch));
        var ingress = new Lock();
        var queued = DiscoveryReadAdmission.AcquireAsync(gates, epoch.Superseded);
        Assert.False(queued.IsCompleted);

        // A queued read's continuation can perform completion logging or finish a scan. Neither
        // may run inline under the worker's ingress/finder retirement locks during internal Cancel.
        async Task<bool> ObserveCompletionAsync()
        {
            try
            {
                using var admission = await queued;
                return ingress.IsHeldByCurrentThread;
            }
            catch (OperationCanceledException)
            {
                return ingress.IsHeldByCurrentThread;
            }
        }

        var completion = ObserveCompletionAsync();
        try
        {
            lock (ingress)
                ProtocolDeviceInfo.Retire(scope);

            Assert.False(await completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.True(queued.IsCanceled);
        }
        finally
        {
            ProtocolDeviceInfo.Retire(scope);
            // Retirement must finish the queued read before releasing its admission owner, even
            // after runner cancellation; this never releases an already-open native read early.
            await completion.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindAllAsync_PotentialSiblingReads_WaitForNativeDisposal(bool unparsedCcid)
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig
        {
            IncludeSecondKey = true,
            PrimaryReader = unparsedCcid ? "Yubico YubiKey future USB reader" : ScopeRig.Reader
        };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (held, siblingOpened) = HoldFirstDisposal(rig, release.Task);
        var scan = rig.CreateFinder(ConnectionType.All).FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        try
        {
            await held.DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            // Metadata has already returned, but native disposal is still active. A sibling must not open.
            await Assert.ThrowsAsync<TimeoutException>(() =>
                siblingOpened.WaitAsync(TimeSpan.FromMilliseconds(150), TestContext.Current.CancellationToken));
            Assert.Equal(0, held.DisposeCount);

            release.SetResult();
            AssertComplete(await scan, ConnectionTypeExtensions.ConcreteConnections);
            Assert.Equal(6, rig.OpenCalls.Values.Sum());
            Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
        }
        finally
        {
            release.TrySetResult();
            // Native-owner fakes must drain even if the runner has canceled this test.
            await scan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScanAsync_ExitKeepsNativeAdmissionAndRetiresQueuedSiblings(bool cancel)
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig { IncludeSecondKey = true };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (held, _) = HoldFirstDisposal(rig, release.Task);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var scan = new YubiKeyDiscovery(rig.CreateFinder).FindAllAsync(ConnectionType.All, cancellation.Token);
        try
        {
            await held.DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (cancel)
            {
                await cancellation.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scan);
            }
            else
            {
                // The original 2s identity budget still bounds the scan, not native disposal.
                _ = await scan.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            }

            Assert.Equal(rig.PrimaryReader, Assert.Single(rig.OpenCalls).Key);
            var id = rig.InterfaceId(rig.PrimaryReader);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
            var recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(id);
            Assert.False(recovery.IsCompleted);

            // A fresh independent finder must also respect the abandoned native owner. Its queued reads
            // retire at exit; neither scope may open a sibling when the original owner finally drains.
            var fresh = new ScopeRig { IncludeSecondKey = true, PrimaryReader = "Yubico YubiKey future USB reader" };
            _ = await new YubiKeyDiscovery(fresh.CreateFinder).FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.Empty(fresh.OpenCalls);

            // Retiring the unknown-PID waiter must complete without waiting for the native owner;
            // unrelated PID admission remains available throughout.
            var unrelated = new ScopeRig { PrimaryReader = "Yubico YubiKey CCID", ProductId = 0x0404 };
            Assert.NotNull(Assert.Single(await unrelated.CreateFinder(ConnectionType.SmartCard)
                .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)).CachedDeviceInfo);

            release.SetResult();
            await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, rig.OpenCalls.Values.Sum());
            Assert.Empty(fresh.OpenCalls);
            Assert.Equal(1, held.DisposeCount);

            rig.OpenConnection = null;
            AssertComplete(await rig.CreateFinder(ConnectionType.All)
                .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken), ConnectionTypeExtensions.ConcreteConnections);
        }
        finally
        {
            release.TrySetResult();
            await cancellation.CancelAsync();
            // Observe cancellation and drain the detached owner before this collection can run another test.
            _ = await Record.ExceptionAsync(() => scan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task FindAllAsync_LiveUnknownPidWaiter_DoesNotBlockDifferentPidMetadata()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (held, _) = HoldFirstDisposal(rig, release.Task);
        var owner = rig.CreateFinder(ConnectionType.SmartCard);
        var scan = owner.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        var unknown = new ScopeRig { PrimaryReader = "Yubico YubiKey future USB reader" };
        var waiter = unknown.CreateFinder(ConnectionType.SmartCard);
        var unrelated = new ScopeRig { PrimaryReader = "Yubico YubiKey CCID", ProductId = 0x0404 };
        var healthyFinder = unrelated.CreateFinder(ConnectionType.SmartCard);
        Task<IReadOnlyList<IYubiKey>>? pendingScan = null;
        Task<IReadOnlyList<IYubiKey>>? healthyScan = null;
        try
        {
            // Hold 0407 before starting the unknown read. Unlike ScanAsync, this finder stays live
            // after its bounded identity wait returns, so retirement cannot mask partial reservations.
            await held.DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var unread = Assert.Single(await waiter.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.Null(unread.CachedDeviceInfo);
            Assert.Single(unknown.CreatedSlots);
            Assert.Empty(unknown.OpenCalls);

            // A second scan joins the still-pending read. Prove the scope has not retired before
            // asking an independent 0404 finder for real metadata, with 0407 still held throughout.
            pendingScan = waiter.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<TimeoutException>(() =>
                pendingScan.WaitAsync(TimeSpan.FromMilliseconds(150), TestContext.Current.CancellationToken));
            healthyScan = healthyFinder.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            var healthy = Assert.Single(await healthyScan
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.Equal(111, healthy.CachedDeviceInfo?.SerialNumber);
            Assert.Single(unrelated.OpenCalls);
            Assert.False(pendingScan.IsCompleted);
            Assert.Empty(unknown.OpenCalls);
            Assert.Equal(0, held.DisposeCount);

            waiter.RetireReadScope();
            _ = await pendingScan.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            release.SetResult();
            await scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
            Assert.Empty(unknown.OpenCalls);
        }
        finally
        {
            waiter.RetireReadScope();
            owner.RetireReadScope();
            healthyFinder.RetireReadScope();
            release.TrySetResult();
            // Retire queued work and drain the held native fake even if the runner canceled the test.
            if (pendingScan is not null)
                _ = await Record.ExceptionAsync(() => pendingScan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            if (healthyScan is not null)
                _ = await Record.ExceptionAsync(() => healthyScan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            _ = await Record.ExceptionAsync(() => scan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task FindAllAsync_DifferentPidMetadata_ProgressesWhileNativeDisposalIsHeld()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig { IncludeSecondKey = true };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (held, _) = HoldFirstDisposal(rig, release.Task);
        var scan = rig.CreateFinder(ConnectionType.All).FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        try
        {
            await held.DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var unrelated = new ScopeRig { PrimaryReader = "Yubico YubiKey CCID", ProductId = 0x0404 };
            var key = Assert.Single(await unrelated.CreateFinder(ConnectionType.SmartCard)
                .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.Equal(111, key.CachedDeviceInfo?.SerialNumber);
            Assert.Equal(0, held.DisposeCount);
        }
        finally
        {
            release.TrySetResult();
            // Complete the admitted owner before leaving the serialized discovery-test collection.
            await scan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    private static (IdentityConnection Connection, Task SiblingOpened) HoldFirstDisposal(ScopeRig rig, Task release)
    {
        var held = rig.CreateIdentityConnection(111);
        held.PendingDisposal = release;
        var siblingOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.OpenConnection = name =>
        {
            if (name == rig.PrimaryReader) return Task.FromResult<IConnection>(held);
            siblingOpened.TrySetResult();
            return Task.FromResult<IConnection>(rig.CreateIdentityConnection(
                name.EndsWith('2') || name.EndsWith(" 01", StringComparison.Ordinal) ? 222 : 111));
        };
        return (held, siblingOpened.Task);
    }

    private static void AssertComplete(IReadOnlyList<IYubiKey> devices, ConnectionType expected)
    {
        Assert.Equal(2, devices.Count);
        Assert.Equal(new int?[] { 111, 222 }, devices.Select(device => device.SerialNumber).Order());
        Assert.All(devices, device =>
        {
            Assert.Equal(expected, device.AvailableConnections);
            Assert.NotNull(device.CachedDeviceInfo);
        });
    }
}