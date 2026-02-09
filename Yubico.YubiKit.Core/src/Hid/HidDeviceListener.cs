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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.PlatformInterop;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Abstract base class for platform-specific HID device listeners.
/// Monitors for HID device arrival and removal events using OS-level notifications.
/// </summary>
public abstract class HidDeviceListener : IDisposable
{
    private static readonly ILogger Logger = NullLoggerFactory.Instance.CreateLogger<HidDeviceListener>();
    
    private event EventHandler<HidDeviceEventArgs>? _arrived;
    private event EventHandler<HidDeviceEventArgs>? _removed;
    private bool _disposed;

    /// <summary>
    /// Raised when a HID device is connected to the system.
    /// </summary>
    public event EventHandler<HidDeviceEventArgs>? Arrived
    {
        add => _arrived += value;
        remove => _arrived -= value;
    }

    /// <summary>
    /// Raised when a HID device is disconnected from the system.
    /// </summary>
    public event EventHandler<HidDeviceEventArgs>? Removed
    {
        add => _removed += value;
        remove => _removed -= value;
    }

    /// <summary>
    /// Gets the current status of the listener.
    /// </summary>
    public DeviceListenerStatus Status { get; protected set; } = DeviceListenerStatus.Stopped;

    /// <summary>
    /// Creates a platform-specific HID device listener.
    /// </summary>
    /// <returns>A <see cref="HidDeviceListener"/> appropriate for the current operating system.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current platform is not supported.
    /// </exception>
    public static HidDeviceListener Create() =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new Windows.WindowsHidDeviceListener(),
            SdkPlatform.MacOS => new MacOS.MacOSHidDeviceListener(),
            SdkPlatform.Linux => new Linux.LinuxHidDeviceListener(),
            _ => throw new PlatformNotSupportedException(
                $"HID device listening is not supported on platform: {SdkPlatformInfo.OperatingSystem}")
        };

    /// <summary>
    /// Raises the <see cref="Arrived"/> event safely.
    /// </summary>
    /// <param name="device">The device that arrived.</param>
    protected void OnArrived(IHidDevice device)
    {
        var args = new HidDeviceEventArgs(device);
        InvokeEventSafely(_arrived, args, "Arrived");
    }

    /// <summary>
    /// Raises the <see cref="Removed"/> event safely.
    /// </summary>
    /// <param name="device">The device that was removed.</param>
    protected void OnRemoved(IHidDevice device)
    {
        var args = new HidDeviceEventArgs(device);
        InvokeEventSafely(_removed, args, "Removed");
    }

    /// <summary>
    /// Invokes an event handler safely, catching exceptions from individual handlers.
    /// </summary>
    private void InvokeEventSafely(EventHandler<HidDeviceEventArgs>? handler, HidDeviceEventArgs args, string eventName)
    {
        if (handler is null)
        {
            return;
        }

        foreach (var invoker in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<HidDeviceEventArgs>)invoker).Invoke(this, args);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception in {EventName} event handler", eventName);
            }
        }
    }

    /// <summary>
    /// Clears all event handlers to prevent leaks during disposal.
    /// </summary>
    protected void ClearEventHandlers()
    {
        _arrived = null;
        _removed = null;
    }

    /// <summary>
    /// Disposes of the listener and releases all resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ClearEventHandlers();
            Status = DeviceListenerStatus.Stopped;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
