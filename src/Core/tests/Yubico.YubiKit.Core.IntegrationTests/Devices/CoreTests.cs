using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

public class CoreTests : IAsyncLifetime
{
    /// <summary>
    /// Tears down any manager left behind by an earlier test class, rather than starting monitoring.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>YubiKeyManager</c> is static, so its device cache survives across test classes. That
    /// matters for <see cref="DeviceChanges_PublishesToObservableSubscribers"/>, which observes the
    /// <see cref="DeviceAction.Added"/> events produced when the initial rescan diffs an empty cache
    /// against the connected hardware. If a previous class already populated the cache, that rescan
    /// diffs the device against itself and publishes nothing, and the test times out. Shutting down
    /// here disposes the manager so the next access rebuilds it with an empty cache.
    /// </para>
    /// <para>
    /// This is not hypothetical: the test passed 8/8 in isolation and still failed roughly one run
    /// in four inside the full suite, whenever <c>YubiKeyTests</c> or
    /// <c>FidoHidOwnershipIntegrationTests</c> happened to run first.
    /// </para>
    /// <para>
    /// Monitoring is deliberately not started here — the test needs to subscribe before the initial
    /// rescan, and <c>GetPcscDevices</c> uses <c>FindAllAsync</c>, which does not need monitoring.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync() => await YubiKeyManager.ShutdownAsync();

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
    /// <para>
    /// No hot-plug required: <c>StartMonitoring</c> performs an initial rescan, and a YubiKey that
    /// is already connected is diffed in as <see cref="DeviceAction.Added"/>. That is why this is
    /// <c>RequiresHardware</c> rather than <c>RequiresUserPresence</c> — it runs unattended once a
    /// key is present, so it stays in the smoke suite where the coverage is actually useful.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task DeviceChanges_PublishesToObservableSubscribers()
    {
        var observer = new FirstEventObserver();

        // Subscribe BEFORE monitoring starts. The event under test is emitted by the initial
        // rescan, so starting first would race the subscription and drop it. This mirrors the
        // ordering YubiKeyTests documents for WatchAsync.
        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(observer);
        YubiKeyManager.StartMonitoring();

        // A generous ceiling, not an expectation. Measured over 8 consecutive local runs the initial
        // rescan landed anywhere between 218 ms and 3 s - PC/SC enumeration latency varies a lot -
        // so the margin is deliberate rather than arbitrary.
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