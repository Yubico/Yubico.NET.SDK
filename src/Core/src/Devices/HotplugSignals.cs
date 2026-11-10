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
using System.Threading.Channels;
using Yubico.YubiKit.Core.Transports;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Starts optional transport listeners and coalesces their rescan hints.</summary>
internal sealed class HotplugSignals : IDisposable
{
    private readonly ILogger _logger;
    private readonly List<(ConnectionType Transport, IDeviceChangeListener Listener)> _listeners = [];
    private readonly Channel<ConnectionType> _channel = Channel.CreateBounded<ConnectionType>(
        new BoundedChannelOptions(1)
        {
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });
    private bool _disposed;

    /// <summary>Creates and starts each listener, logging failures without preventing other listeners from starting.</summary>
    internal HotplugSignals(
        IReadOnlyList<ListenerSource> listeners,
        ILogger logger)
    {
        _logger = logger;
        foreach (var (transport, create) in listeners)
            TryStart(transport, create);

        if (_listeners.Count == 0)
            _logger.LogWarning("No device-change listener could start; monitoring will rely on the interval rescan only.");
    }

    internal ChannelReader<ConnectionType> Signals => _channel.Reader;

    private void TryStart(ConnectionType transport, Func<IDeviceChangeListener> create)
    {
        IDeviceChangeListener? listener = null;
        try
        {
            listener = create();
            listener.Changed = () => _channel.Writer.TryWrite(transport);
            listener.Start();
            if (listener.Status == DeviceListenerStatus.Started)
            {
                _listeners.Add((transport, listener));
                return;
            }

            _logger.LogWarning(
                "{Transport} device-change listener did not start (status: {Status}); changes will be detected on the interval rescan",
                transport, listener.Status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "{Transport} device-change listener failed to start; changes will be detected on the interval rescan", transport);
        }

        if (listener is not null)
        {
            BestEffort(() => listener.Changed = null, $"clear {transport} listener callback");
            BestEffort(listener.Stop, $"stop {transport} listener");
            BestEffort(listener.Dispose, $"dispose {transport} listener");
        }
    }

    /// <summary>Clears all callbacks, stops all listeners, disposes all listeners, then completes the signal channel.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var (transport, listener) in _listeners)
            BestEffort(() => listener.Changed = null, $"clear {transport} listener callback");
        foreach (var (transport, listener) in _listeners)
            BestEffort(listener.Stop, $"stop {transport} listener");
        foreach (var (transport, listener) in _listeners)
            BestEffort(listener.Dispose, $"dispose {transport} listener");

        _channel.Writer.TryComplete();
        _logger.LogDebug("Device listeners torn down");
    }

    private void BestEffort(Action cleanup, string operation)
    {
        try
        {
            cleanup();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to {CleanupOperation} during monitor teardown", operation);
        }
    }
}

/// <summary>Pairs a transport hint with its listener factory.</summary>
internal readonly record struct ListenerSource(ConnectionType Transport, Func<IDeviceChangeListener> Create);