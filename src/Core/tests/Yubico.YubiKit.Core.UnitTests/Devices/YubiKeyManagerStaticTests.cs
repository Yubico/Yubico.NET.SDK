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

using System.Reflection;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>Hermetic facade tests using per-call finders, never global test configuration.</summary>
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class YubiKeyManagerStaticTests
{
    [Fact]
    public void DiscoveryFacade_HasNoGlobalLifetimeOrCache()
    {
        Assert.Empty(typeof(YubiKeyManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic));
        var oldNames = new[] { "ScanAsync", "StartMonitoring", "StopMonitoring", "WatchAsync", "IsMonitoring", "Shutdown", "ShutdownAsync", "DeviceChanges" };
        Assert.DoesNotContain(typeof(YubiKeyManager).GetMembers(BindingFlags.Public | BindingFlags.Static),
            member => oldNames.Contains(member.Name));
        Assert.DoesNotContain(typeof(YubiKeyManager).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.GetParameters().Any(parameter => parameter.Name is "forceRescan" or "type" or "scope"));
        Assert.False(Enum.IsDefined(typeof(DeviceAction), "Updated"));
    }

    [Fact]
    public async Task FindAllAsync_SubsequentCall_ObservesChangedDevices()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var first = Assert.Single(await YubiKeyManager.FindAllAsync(
            ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder));
        rig.Serial = 333;
        var second = Assert.Single(await YubiKeyManager.FindAllAsync(
            ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder));
        Assert.NotSame(first, second);
        Assert.Equal(111, first.SerialNumber);
        Assert.Equal(333, second.SerialNumber);
        Assert.Equal(2, rig.Pcsc.Calls);
        Assert.Equal(2, rig.OpenCalls.Values.Sum());
    }

    [Fact]
    public async Task FindAllAsync_EmptyThenPresent_UsesANewFinderAndEnumeratesAgain()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var empty = true;
        var factories = 0;
        FindYubiKeys CreateFinder(ConnectionType interfaces)
        {
            factories++;
            var finder = rig.CreateFinder(interfaces);
            if (empty) rig.Pcsc.Devices = [];
            return finder;
        }

        Assert.Empty(await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, TestContext.Current.CancellationToken, CreateFinder));
        empty = false;
        Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, TestContext.Current.CancellationToken, CreateFinder));
        Assert.Equal(2, factories);
        Assert.Equal(2, rig.Pcsc.Calls);
    }

    [Fact]
    public async Task FindAllAsync_EmptyInterfaces_PerformsNoBackendInitialization()
    {
        Assert.Empty(await YubiKeyManager.FindAllAsync(ConnectionType.Unknown, TestContext.Current.CancellationToken));
        Assert.Empty(await YubiKeyManager.FindAllAsync((ConnectionType)0x80, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ConnectionType.SmartCard)]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.SmartCard | ConnectionType.HidFido)]
    public async Task FindFirstMethods_UseOneScopedDiscoveryAndStopAtFirstPredicateMatch(ConnectionType interfaces)
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig { IncludeSecondKey = true };
        var calls = 0;
        var predicateCalls = 0;
        FindYubiKeys CreateFinder(ConnectionType scope)
        {
            Assert.Equal(interfaces, scope);
            calls++;
            return rig.CreateFinder(scope);
        }

        var optional = await YubiKeyManager.FindFirstOrDefaultAsync(key =>
        {
            predicateCalls++;
            return true;
        }, interfaces, TestContext.Current.CancellationToken, CreateFinder);
        Assert.NotNull(optional);
        Assert.Equal(1, predicateCalls);
        Assert.Equal(1, calls);
        var required = await YubiKeyManager.FindFirstAsync(key => key.SerialNumber == 222,
            interfaces, TestContext.Current.CancellationToken, CreateFinder);
        Assert.Equal(222, required.SerialNumber);
        Assert.Equal(2, calls);
        Assert.Equal(ConnectionType.Unknown, required.AvailableConnections & ~interfaces.ToConcreteScope());
    }

    [Fact]
    public async Task FindFirstMethods_NullPredicateAndNoMatch_PreserveSelectionSemantics()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        Assert.NotNull(await YubiKeyManager.FindFirstOrDefaultAsync(null, ConnectionType.SmartCard,
            TestContext.Current.CancellationToken, rig.CreateFinder));
        Assert.Equal(111, (await YubiKeyManager.FindFirstAsync(null, ConnectionType.SmartCard,
            TestContext.Current.CancellationToken, rig.CreateFinder)).SerialNumber);
        Assert.Null(await YubiKeyManager.FindFirstOrDefaultAsync(_ => false, ConnectionType.SmartCard,
            TestContext.Current.CancellationToken, rig.CreateFinder));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => YubiKeyManager.FindFirstAsync(
            _ => false, ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder));
        Assert.Equal("No matching YubiKey was found.", error.Message);
        Assert.Null(await YubiKeyManager.FindFirstOrDefaultAsync(null, ConnectionType.Unknown, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => YubiKeyManager.FindFirstAsync(
            interfaces: ConnectionType.Unknown, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindFirstMethods_PropagatePredicateAndDiscoveryExceptions()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var expected = new InvalidDataException("Scripted failure");
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidDataException>(() => YubiKeyManager.FindFirstOrDefaultAsync(
            _ => throw expected, ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder)));
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidDataException>(() => YubiKeyManager.FindFirstAsync(
            null, ConnectionType.All, TestContext.Current.CancellationToken, _ => throw expected)));
    }

    [Fact]
    public async Task FindAllAsync_DoesNotPublishPartialViewsOrChangeAnOwnedMonitor()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var retained = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.All,
            TestContext.Current.CancellationToken, rig.CreateFinder));
        var repository = new YubiKeyDeviceRepository();
        var worker = new YubiKeyMonitorWorker(repository, new FixedFinder(retained),
            [(ConnectionType.Hid, () => new OwnedMonitorFixture.FakeDeviceChangeListener()),
                (ConnectionType.SmartCard, () => new OwnedMonitorFixture.FakeDeviceChangeListener())]);
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await using var watcher = monitor.WatchAsync(TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await watcher.MoveNextAsync());
        Assert.Same(retained, watcher.Current.Device);
        var pending = watcher.MoveNextAsync().AsTask();
        try
        {
            rig.Serial = 333;
            var scoped = Assert.Single(await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard,
                TestContext.Current.CancellationToken, rig.CreateFinder));
            Assert.Equal(333, scoped.SerialNumber);
            Assert.Equal(ConnectionType.SmartCard, scoped.AvailableConnections);
            Assert.Same(retained, Assert.Single(monitor.CurrentDevices));
            Assert.Equal(111, retained.SerialNumber);
            Assert.Equal(ConnectionTypeExtensions.ConcreteConnections, retained.AvailableConnections);
            Assert.False(pending.IsCompleted);
        }
        finally
        {
            await monitor.DisposeAsync();
            // End the outstanding MoveNext before enumerator disposal, even on runner cancellation.
            Assert.False(await pending.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
        }
    }

    [Fact]
    public async Task PublicOverloads_AreUnambiguousAndObserveCancellationBeforeInitialization()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var ct = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindAllAsync(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindAllAsync(cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindAllAsync(interfaces: ConnectionType.All, cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstAsync(null, ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstAsync(cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstAsync(predicate: null, cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstAsync(null, ConnectionType.All, ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstOrDefaultAsync(null, ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstOrDefaultAsync(cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstOrDefaultAsync(predicate: null, cancellationToken: ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindFirstOrDefaultAsync(null, ConnectionType.All, ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.CreateMonitorAsync(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => YubiKeyManager.FindAllAsync(ConnectionType.All, ct,
            _ => throw new InvalidOperationException("Finder must not be constructed")));
    }

    private sealed class FixedFinder(IYubiKey device) : IFindYubiKeys, IMonitoredDiscovery
    {
        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IYubiKey>>([device]);

        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken) =>
            FindAllAsync(ConnectionType.All, cancellationToken);

        public void InvalidateEvidence(ConnectionType transport) { }

        public void Retire() { }
    }
}