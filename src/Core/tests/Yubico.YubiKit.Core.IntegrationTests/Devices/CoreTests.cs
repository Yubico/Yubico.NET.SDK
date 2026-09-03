using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

public class CoreTests : IAsyncLifetime
{
    /// <summary>Resets the static manager so each test starts with an empty device cache.</summary>
    public async Task InitializeAsync() => await YubiKeyManager.ShutdownAsync();

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    /// <summary>Verifies observable delivery of the initial device scan against real hardware.</summary>
    /// <remarks>
    /// <see cref="YubiKeyManager.DeviceChanges"/> invokes observers inline on the monitor's publish
    /// path. The subscription is established before monitoring starts so it observes the initial
    /// scan.
    /// </remarks>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task DeviceChanges_PublishesToObservableSubscribers()
    {
        var observer = new FirstEventObserver();

        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(observer);
        YubiKeyManager.StartMonitoring();

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
        private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnNext(DeviceEvent value) => _first.TrySetResult();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public async Task<bool> WaitForFirstAsync(TimeSpan timeout)
        {
            try
            {
                await _first.Task.WaitAsync(timeout).ConfigureAwait(false);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}