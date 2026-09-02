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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class DeviceConnectionOwnershipTests
{
    [Fact]
    public async Task ConnectAsync_OwnsInterfaceBeforePhysicalConnectionCreation()
    {
        var factory = new BlockingCreationFactory();
        var slot = CreateSlot(factory);
        var device = CreateDevice(slot);

        var sessionTask = device.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken);
        await factory.FirstCreateStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var discoveryTask = DiscoveryIdentityReader.TryReadAsync(
            slot,
            ConnectionType.SmartCard,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);
        var skippedBeforePhysicalConnect = discoveryTask.IsCompletedSuccessfully;

        factory.ReleaseCreates.TrySetResult();
        await using var session = await sessionTask;

        Assert.True(skippedBeforePhysicalConnect, "Discovery did not skip the session-owned interface immediately.");
        Assert.Null(await discoveryTask);
        Assert.Equal(1, factory.CreateCalls);
    }

    [Fact]
    public async Task ConnectAsync_SessionStartingImmediatelyBeforeDiscoverySelect_CannotCrossOwnership()
    {
        var factory = new DiscoveryFirstFactory();
        var device = CreateDevice(factory);

        var discoveryTask = ProtocolDeviceInfo.ReadBoundedAsync(
            device,
            ConnectionType.SmartCard,
            TimeSpan.FromSeconds(5),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);
        await factory.DiscoveryConnection.TransmitStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, factory.DiscoveryConnection.WireTransmitCalls);
        Assert.False(DeviceConnectionRegistry.IsInUse(device.DeviceId));

        var sessionTask = device.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken);
        var crossedDiscovery = factory.SecondCreateStarted.Task.IsCompleted;
        Assert.False(DeviceConnectionRegistry.IsInUse(device.DeviceId));

        factory.DiscoveryConnection.ReleaseTransmit.TrySetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryTask);
        var session = await sessionTask;

        Assert.False(crossedDiscovery, "Session physical connection creation crossed an active discovery read.");
        Assert.Equal(2, factory.CreateCalls);
        Assert.True(DeviceConnectionRegistry.IsInUse(device.DeviceId));

        await session.DisposeAsync();
        Assert.False(DeviceConnectionRegistry.IsInUse(device.DeviceId));
    }

    [Fact]
    public async Task Coordinator_CanceledWaiterDecrementsCount_AndRemainingConnectionHasPriority()
    {
        var deviceId = $"test:waiter-priority:{Guid.NewGuid():N}";
        using var discovery = DeviceConnectionRegistry.TryAcquireDiscovery(deviceId);
        Assert.NotNull(discovery);

        using var canceledWaiterToken = new CancellationTokenSource();
        var canceledWaiter = DeviceConnectionRegistry
            .AcquireConnectionAsync([deviceId], canceledWaiterToken.Token)
            .AsTask();
        var remainingWaiter = DeviceConnectionRegistry
            .AcquireConnectionAsync([deviceId], TestContext.Current.CancellationToken)
            .AsTask();

        canceledWaiterToken.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWaiter);

        discovery.Dispose();
        Assert.Null(DeviceConnectionRegistry.TryAcquireDiscovery(deviceId));

        var session = await remainingWaiter;
        Assert.True(DeviceConnectionRegistry.IsInUse(deviceId));
        Assert.Null(DeviceConnectionRegistry.TryAcquireDiscovery(deviceId));

        session.Dispose();
        Assert.False(DeviceConnectionRegistry.IsInUse(deviceId));
        using var nextDiscovery = DeviceConnectionRegistry.TryAcquireDiscovery(deviceId);
        Assert.NotNull(nextDiscovery);
    }

    [Fact]
    public async Task ConnectAsync_FailedPhysicalCreation_ReleasesSessionOwnership()
    {
        var factory = new FailThenSucceedFactory();
        var device = CreateDevice(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            device.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken));

        var discoveryTask = ProtocolDeviceInfo.ReadBoundedAsync(
            device,
            ConnectionType.SmartCard,
            TimeSpan.FromSeconds(1),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryTask);
        Assert.Equal(2, factory.CreateCalls);
    }

    [Fact]
    public async Task ConnectAsync_DifferentInterfaces_CreatePhysicalConnectionsConcurrently()
    {
        var firstFactory = new BlockingCreationFactory();
        var secondFactory = new BlockingCreationFactory();
        var first = CreateDevice(firstFactory);
        var second = CreateDevice(secondFactory);

        var firstTask = first.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken);
        var secondTask = second.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken);

        await Task.WhenAll(
            firstFactory.FirstCreateStarted.Task,
            secondFactory.FirstCreateStarted.Task).WaitAsync(TestContext.Current.CancellationToken);

        firstFactory.ReleaseCreates.TrySetResult();
        secondFactory.ReleaseCreates.TrySetResult();
        await using var firstConnection = await firstTask;
        await using var secondConnection = await secondTask;

        Assert.Equal(1, firstFactory.CreateCalls);
        Assert.Equal(1, secondFactory.CreateCalls);
    }

    private static YubiKeyDevice CreateDevice(ISmartCardConnectionFactory factory) =>
        CreateDevice(CreateSlot(factory));

    private static DeviceConnectionSlot CreateSlot(ISmartCardConnectionFactory factory) =>
        new(
            new PcscDevice { ReaderName = $"test-reader-{Guid.NewGuid():N}", Atr = null },
            factory);

    private static YubiKeyDevice CreateDevice(DeviceConnectionSlot slot)
    {
        return new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
    }

    private sealed class BlockingCreationFactory : ISmartCardConnectionFactory
    {
        private int _createCalls;

        public TaskCompletionSource FirstCreateStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseCreates { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CreateCalls => Volatile.Read(ref _createCalls);

        public async Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            _ = Interlocked.Increment(ref _createCalls);
            FirstCreateStarted.TrySetResult();
            await ReleaseCreates.Task.WaitAsync(cancellationToken);
            return new ThrowingSmartCardConnection();
        }
    }

    private sealed class DiscoveryFirstFactory : ISmartCardConnectionFactory
    {
        private int _createCalls;

        public BlockingTransmitConnection DiscoveryConnection { get; } = new();

        public TaskCompletionSource SecondCreateStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CreateCalls => Volatile.Read(ref _createCalls);

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _createCalls);
            if (call == 1)
                return Task.FromResult<ISmartCardConnection>(DiscoveryConnection);

            SecondCreateStarted.TrySetResult();
            return Task.FromResult<ISmartCardConnection>(new ThrowingSmartCardConnection());
        }
    }

    private sealed class FailThenSucceedFactory : ISmartCardConnectionFactory
    {
        private int _createCalls;

        public int CreateCalls => Volatile.Read(ref _createCalls);

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default) =>
            Interlocked.Increment(ref _createCalls) == 1
                ? Task.FromException<ISmartCardConnection>(new InvalidOperationException("Expected creation failure."))
                : Task.FromResult<ISmartCardConnection>(new ThrowingSmartCardConnection());
    }

    private class ThrowingSmartCardConnection : ISmartCardConnection
    {
        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public virtual Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ReadOnlyMemory<byte>>(new InvalidOperationException("Expected transmit failure."));

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class BlockingTransmitConnection : ThrowingSmartCardConnection
    {
        private int _wireTransmitCalls;

        public TaskCompletionSource TransmitStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseTransmit { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int WireTransmitCalls => Volatile.Read(ref _wireTransmitCalls);

        public override async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            TransmitStarted.TrySetResult();
            await ReleaseTransmit.Task;
            _ = Interlocked.Increment(ref _wireTransmitCalls);
            throw new InvalidOperationException("Expected discovery transmit failure.");
        }
    }
}