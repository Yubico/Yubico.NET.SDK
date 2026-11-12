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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Bounds each caller's wait for a shared discovery device-info read.</summary>
internal static class DiscoveryDeviceInfoReader
{
    private static readonly DiscoveryReadScope DefaultScope = new();

    internal static void NotifyTransportActivity() => DefaultScope.Advance();

    /// <summary>
    ///     Opens a short-lived connection over the given interface and reads <see cref="DeviceInfo" />,
    ///     with a budget for the caller's wait.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The budget bounds the caller's <em>wait</em>, not the work: an in-flight native call (e.g.
    ///         <c>SCardTransmit</c> against a card busy with a long applet operation such as RSA key
    ///         generation) cannot observe cancellation. On budget exhaustion the read is therefore
    ///         <em>abandoned, not aborted</em> — this method throws <see cref="TimeoutException" /> so the
    ///         scan can proceed, while the abandoned task keeps running in the background and disposes its
    ///         protocol through <see cref="ProtocolDeviceInfo.ReadAsync" /> and its discovery-owned connection through
    ///         <c>SharedDeviceInfoRead.ConnectAndReadAsync</c> when the native call eventually returns.
    ///     </para>
    ///     <para>
    ///         External cancellation via <paramref name="cancellationToken" /> likewise abandons only that
    ///         caller's wait (propagating <see cref="OperationCanceledException" />). The shared operation's
    ///         lifetime is independent of every waiter and remains the single in-flight read for its stable
    ///         interface/connection key until it completes.
    ///     </para>
    /// </remarks>
    /// <exception cref="TimeoutException">The budget elapsed before the read completed.</exception>
    public static async Task<DeviceInfo> ReadBoundedAsync(
        IYubiKey device,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot = false,
        DiscoveryReadScope? scope = null)
    {
        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        var provider = device as IDiscoveryConnectionProvider;
        return await ReadBoundedCoreAsync(
                interfaceId,
                device.DeviceId,
                provider,
                connection,
                budget,
                logger,
                cancellationToken,
                waitForWorkerSlot,
                scope ?? DefaultScope)
            .ConfigureAwait(false);
    }

    public static async Task<DeviceInfo> ReadSlotBoundedAsync(
        IYubiKeyConnectionSlot slot,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot = false,
        DiscoveryReadScope? scope = null) =>
        await ReadBoundedCoreAsync(
            slot.InterfaceId,
            slot.InterfaceId,
            slot as IDiscoveryConnectionProvider,
            connection,
            budget,
            logger,
            cancellationToken,
            waitForWorkerSlot,
            scope ?? DefaultScope).ConfigureAwait(false);

    private static async Task<DeviceInfo> ReadBoundedCoreAsync(
        string interfaceId,
        string deviceId,
        IDiscoveryConnectionProvider? provider,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot,
        DiscoveryReadScope scope)
    {
        // One atomic epoch capture: the key's generation and the supersede token used while queued are
        // guaranteed to belong to the same hotplug epoch, so a read created against an already-retired
        // epoch always holds a token that has been (or is about to be) cancelled.
        if (!scope.TryCaptureEpoch(out var epoch))
            throw new DiscoveryReadKey(interfaceId, connection, Generation: -1).Superseded();

        var key = new DiscoveryReadKey(interfaceId, connection, epoch.Generation);
        var sharedRead = scope.GetOrAdd(
            key,
            _ => new SharedDeviceInfoRead(scope, key, epoch, deviceId, provider, connection, logger, waitForWorkerSlot));

        try
        {
            return await sharedRead.Task.WaitAsync(budget, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (!sharedRead.Task.IsCompleted)
                sharedRead.RecordAbandonment();
            throw;
        }
        catch (OperationCanceledException)
        {
            if (!sharedRead.Task.IsCompleted)
                sharedRead.RecordAbandonment();
            throw;
        }
    }
}