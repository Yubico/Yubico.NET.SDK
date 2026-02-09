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
using Yubico.YubiKit.Core.PlatformInterop;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Abstract base class for platform-specific HID device listeners.
/// Monitors for HID device arrival and removal events using OS-level notifications.
/// </summary>
public abstract class HidDeviceListener : IDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidDeviceListener>();
    
    private bool _disposed;

    /// <summary>
    /// Callback invoked when any HID device event (arrival or removal) occurs.
    /// </summary>
    public Action? DeviceEvent { get; set; }

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
    /// Signals that a device event has occurred.
    /// </summary>
    protected void OnDeviceEvent()
    {
        try
        {
            DeviceEvent?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in DeviceEvent callback");
        }
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
            DeviceEvent = null;
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
