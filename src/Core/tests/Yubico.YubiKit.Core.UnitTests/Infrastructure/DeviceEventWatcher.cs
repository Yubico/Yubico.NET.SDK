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

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>
/// Drains a <c>WatchAsync</c> enumeration on a background task and records what it receives.
/// </summary>
/// <remarks>
/// <para>
/// Device events are delivered asynchronously through a per-watcher buffer, so a test cannot publish
/// and assert on the same thread the way an inline observer allowed. This helper owns the pump, waits
/// for the subscription to be live before returning from <c>StartAsync</c>, and exposes the terminal
/// outcome (normal completion, cancellation, or overflow fault) for assertion.
/// </para>
/// <para>
/// Use <see cref="DrainAsync"/> rather than a sleep when asserting that <em>no</em> event was
/// published: it pushes a sentinel through the repository and waits for it, and per-watcher delivery
/// is ordered, so the sentinel's arrival proves everything published earlier has already landed.
/// </para>
/// </remarks>
internal sealed class DeviceEventWatcher : IAsyncDisposable
{
    private readonly Lock _gate = new();
    private readonly List<DeviceEvent> _received = [];
    private readonly CancellationTokenSource _cts;
    private readonly Task _pump;

    private DeviceEventWatcher(
        Func<CancellationToken, IAsyncEnumerable<DeviceEvent>> watch,
        CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var token = _cts.Token;
        _pump = Task.Run(
            async () =>
            {
                await foreach (var deviceEvent in watch(token).ConfigureAwait(false))
                {
                    lock (_gate)
                    {
                        _received.Add(deviceEvent);
                    }
                }
            },
            CancellationToken.None);
    }

    /// <summary>Snapshot of everything received so far, in delivery order.</summary>
    public IReadOnlyList<DeviceEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return [.. _received];
            }
        }
    }

    /// <summary>Number of events received so far.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _received.Count;
            }
        }
    }

    /// <summary>
    /// The enumeration itself: completes normally when the source completes, faults with
    /// <see cref="InvalidOperationException"/> on buffer overflow, and cancels with the watcher token.
    /// </summary>
    public Task Completion => _pump;

    /// <summary>Whether the enumeration ended normally (source completed).</summary>
    public bool EndedNormally => _pump.IsCompletedSuccessfully;

    /// <summary>Starts a watcher over <paramref name="repository"/> and waits for it to subscribe.</summary>
    public static Task<DeviceEventWatcher> StartAsync(
        YubiKeyDeviceRepository repository,
        CancellationToken cancellationToken) =>
        StartAsync(repository.WatchAsync, () => repository.WatcherCount, cancellationToken);

    /// <summary>Starts a watcher over <paramref name="hub"/> and waits for it to subscribe.</summary>
    public static Task<DeviceEventWatcher> StartAsync(
        DeviceEventHub hub,
        CancellationToken cancellationToken) =>
        StartAsync(hub.WatchAsync, () => hub.WatcherCount, cancellationToken);

    /// <summary>
    /// Starts a watcher and does not return until <paramref name="liveWatcherCount"/> shows the
    /// subscription is live, because <c>WatchAsync</c> subscribes on first enumeration and publishing
    /// before that point would race it.
    /// </summary>
    public static async Task<DeviceEventWatcher> StartAsync(
        Func<CancellationToken, IAsyncEnumerable<DeviceEvent>> watch,
        Func<int> liveWatcherCount,
        CancellationToken cancellationToken)
    {
        var before = liveWatcherCount();
        var watcher = new DeviceEventWatcher(watch, cancellationToken);

        try
        {
            await AsyncWait.WaitUntilAsync(
                () => liveWatcherCount() > before,
                "watcher did not subscribe",
                TimeSpan.FromSeconds(10),
                cancellationToken);
        }
        catch
        {
            await watcher.DisposeAsync();
            throw;
        }

        return watcher;
    }

    /// <summary>Waits until at least <paramref name="count"/> events have been received.</summary>
    public Task WaitForCountAsync(
        int count,
        string because,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null) =>
        AsyncWait.WaitUntilAsync(() => Count >= count, because, timeout ?? TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>
    /// Waits for the first event matching <paramref name="match"/> and returns its delivery index.
    /// </summary>
    public async Task<int> WaitForAsync(
        Func<DeviceEvent, bool> match,
        string because,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        await AsyncWait.WaitUntilAsync(
            () => Events.Any(match),
            because,
            timeout ?? TimeSpan.FromSeconds(10),
            cancellationToken);

        var events = Events;
        for (var i = 0; i < events.Count; i++)
        {
            if (match(events[i]))
            {
                return i;
            }
        }

        throw new InvalidOperationException(because);
    }

    /// <summary>
    /// Returns every event delivered before a freshly published sentinel arrival, which is the
    /// deterministic way to assert "nothing else was published" without a timing guess.
    /// </summary>
    /// <remarks>
    /// The sentinel is appended to the repository's current contents so the diff emits exactly one
    /// <see cref="DeviceAction.Added"/> and disturbs no other cache entry.
    /// </remarks>
    public async Task<IReadOnlyList<DeviceEvent>> DrainAsync(
        YubiKeyDeviceRepository repository,
        CancellationToken cancellationToken)
    {
        var sentinel = new FakeYubiKey($"sentinel:{Guid.NewGuid():N}");
        repository.UpdateCache([.. repository.GetAll(), sentinel]);

        var index = await WaitForAsync(
            e => ReferenceEquals(e.Device, sentinel),
            "the drain sentinel never reached the watcher",
            cancellationToken);

        return [.. Events.Take(index)];
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        try
        {
            await _pump;
        }
#pragma warning disable CA1031 // Teardown must not mask the assertion that already ran.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Cancellation and overflow are both expected terminal outcomes here; tests that care
            // assert on Completion directly.
        }

        _cts.Dispose();
    }
}