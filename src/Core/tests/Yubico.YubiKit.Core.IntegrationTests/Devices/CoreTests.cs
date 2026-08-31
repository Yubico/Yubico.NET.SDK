using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

public class CoreTests : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        YubiKeyManager.StartMonitoring();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    /// <summary>
    /// Covers the <see cref="IObservable{T}"/> surface against real hardware.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not <c>WatchAsync</c>, which <c>YubiKeyTests</c> already covers. The two
    /// surfaces differ where it matters: a <c>WatchAsync</c> consumer is buffered by
    /// <c>DeviceEventStream</c> and runs decoupled from the publisher, whereas an observer is
    /// invoked <strong>inline on the publishing thread</strong>, inside the monitor's publish gate.
    /// Only this test exercises that path end to end.
    /// </para>
    /// <para>
    /// The observer is hand-written on purpose. The SDK has no reactive dependency and this project
    /// does not reference <c>System.Reactive</c>, so this doubles as the reference example of
    /// consuming <c>DeviceChanges</c> with nothing but the BCL.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceChanges_PublishesToObservableSubscribers()
    {
        var observer = new FirstEventObserver();
        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(observer);

        // Plug in or remove a YubiKey to trigger an event. You have 10 seconds to do this.
        var observed = await observer.WaitForFirstAsync(TimeSpan.FromSeconds(10));

        Assert.True(observed, "Expected at least one device event to reach the observable subscriber.");
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task GetPcscDevices()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);
    }

    /// <summary>Signals as soon as the first device event arrives.</summary>
    private sealed class FirstEventObserver : IObserver<DeviceEvent>
    {
        // RunContinuationsAsynchronously matters: OnNext runs inline on the monitor's publishing
        // thread, and resuming the test there would block device monitoring - the exact hazard the
        // observable surface is documented to have.
        private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnNext(DeviceEvent value) => _first.TrySetResult();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public async Task<bool> WaitForFirstAsync(TimeSpan timeout) =>
            await Task.WhenAny(_first.Task, Task.Delay(timeout)).ConfigureAwait(false) == _first.Task;
    }
}