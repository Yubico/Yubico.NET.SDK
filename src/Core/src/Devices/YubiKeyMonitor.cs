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

using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Owns continuous discovery and the latest published membership snapshot of connected YubiKeys.
/// </summary>
/// <remarks>
/// Each instance has independent discovery evidence, listeners, and subscriptions. Dispose the monitor
/// when its owner no longer needs tracking; this does not dispose caller-owned sessions or connections.
/// Membership snapshots are read-only, but their device objects can receive later last-known metadata.
/// </remarks>
public sealed class YubiKeyMonitor : IAsyncDisposable
{
    internal static readonly TimeSpan DefaultMonitoringInterval = TimeSpan.FromSeconds(5);

    private readonly DisposalGate _disposal = new();
    private int _disposed;

    internal YubiKeyDeviceRepository Repository { get; }
    internal YubiKeyDeviceMonitorService Service { get; }
    internal Func<Task>? RepositoryTeardownReachedForTest;

    internal YubiKeyMonitor(YubiKeyDeviceRepository repository, YubiKeyDeviceMonitorService service)
    {
        Repository = repository;
        Service = service;
    }

    /// <summary>
    /// Gets a coherent, read-only membership snapshot from the latest completed publication, without I/O.
    /// </summary>
    /// <remarks>
    /// Previously obtained lists retain their membership after updates or disposal. Contained devices
    /// retain last-known metadata rather than guaranteeing live configuration or hardware availability.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The monitor has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Monitoring terminated unexpectedly.</exception>
    public IReadOnlyList<IYubiKey> CurrentDevices
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return Repository.GetAll();
        }
    }

    /// <summary>
    /// Replays current devices as Added events, then streams ordered changes with an independent bounded delta buffer.
    /// </summary>
    /// <param name="cancellationToken">Cancels only this subscription, not the monitor.</param>
    /// <returns>A lazy stream that completes on disposal and faults on terminal monitoring failure.</returns>
    /// <remarks>
    /// On the first MoveNextAsync, membership capture and delta registration share one short publication
    /// boundary. Replay runs outside that boundary and does not occupy the bounded delta buffer. Changes
    /// arriving during replay can overflow that buffer; clear the consumer's projection and re-enumerate
    /// this same monitor to recover. Cancellation and overflow affect only their own subscription.
    /// Replay and events already captured before disposal may still drain. Enumerating after disposal ends empty.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The subscription is canceled.</exception>
    /// <exception cref="InvalidOperationException">The subscriber overflowed or monitoring failed terminally.</exception>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        WatchCoreAsync(cancellationToken);

    private async IAsyncEnumerable<DeviceEvent> WatchCoreAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
            yield break;

        await foreach (var change in Repository.WatchWithSnapshotAsync(cancellationToken).ConfigureAwait(false))
            yield return change;
    }

    internal static YubiKeyMonitor CreateUninitialized()
    {
        var repository = new YubiKeyDeviceRepository();
        var service = new YubiKeyDeviceMonitorService(repository, FindYubiKeys.Create());
        return new YubiKeyMonitor(repository, service);
    }

    internal static Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return InitializeAsync(CreateUninitialized(), cancellationToken, DefaultMonitoringInterval);
    }

    internal static Task<YubiKeyMonitor> CreateAsync(
        YubiKeyDeviceRepository repository,
        YubiKeyDeviceMonitorService service,
        CancellationToken cancellationToken,
        TimeSpan interval) => InitializeAsync(new YubiKeyMonitor(repository, service), cancellationToken, interval);

    private static async Task<YubiKeyMonitor> InitializeAsync(
        YubiKeyMonitor monitor, CancellationToken cancellationToken, TimeSpan interval)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await monitor.Service.StartInitializedAsync(interval, cancellationToken).ConfigureAwait(false);
            return monitor;
        }
        catch
        {
            await monitor.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Stops tracking, closes subscriptions, and releases the repository. Idempotent with shared completion.
    /// </summary>
    /// <remarks>
    /// Teardown uses the existing bounded monitor drain. Already-admitted native work may outlive this
    /// call and retains its lease until actual teardown; its late result cannot repopulate the repository.
    /// Device descriptors and caller-owned connections remain usable independently of this monitor.
    /// </remarks>
    public ValueTask DisposeAsync() => _disposal.DisposeAsync(DisposeCoreAsync);

    private async ValueTask DisposeCoreAsync()
    {
        Volatile.Write(ref _disposed, 1);
        try
        {
            await Service.DisposeAsync().ConfigureAwait(false);
            var hook = RepositoryTeardownReachedForTest;
            if (hook is not null)
                await hook().ConfigureAwait(false);
        }
        finally
        {
            // Dispose first: never expose a live-but-emptied cache to an abandoned publication.
            Repository.Dispose();
        }
    }
}