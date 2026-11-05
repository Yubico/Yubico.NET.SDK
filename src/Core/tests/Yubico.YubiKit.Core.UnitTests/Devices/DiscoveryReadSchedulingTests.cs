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
        var scan = YubiKeyManager.ScanAsync(ConnectionType.All, cancellation.Token, rig.CreateFinder);
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
            _ = await YubiKeyManager.ScanAsync(ConnectionType.All, TestContext.Current.CancellationToken, fresh.CreateFinder)
                .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.Empty(fresh.OpenCalls);

            // The unknown-PID waiter acquired earlier PID gates before blocking on the native owner's
            // gate. Retirement must roll those partial acquisitions back without waiting for that owner.
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