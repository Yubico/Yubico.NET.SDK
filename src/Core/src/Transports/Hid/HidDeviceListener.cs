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
using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid;

/// <summary>
/// Abstract base class for platform-specific HID device listeners.
/// Monitors for HID device arrival and removal events using OS-level notifications.
/// </summary>
/// <remarks>
/// Listeners do not auto-start. Call <see cref="Start"/> after setting up <see cref="Changed"/>
/// callback. The listener establishes baseline state during <see cref="Start"/> to avoid
/// duplicate events for devices already present.
/// </remarks>
public abstract class HidDeviceListener : IDeviceChangeListener
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidDeviceListener>();

    private bool _disposed;
    private volatile DeviceListenerStatus _status = DeviceListenerStatus.Stopped;

    /// <summary>
    /// Callback invoked when a HID listener observes a topology change.
    /// </summary>
    /// <remarks>
    /// The callback is a rescan hint only, never authoritative YubiKey arrival or removal state.
    /// Do not call <see cref="Stop"/> or <see cref="Dispose()"/> from inside the callback.
    /// </remarks>
    public Action? Changed { get; set; }

    /// <summary>
    /// Gets the current status of the listener.
    /// </summary>
    /// <remarks>
    /// The status is written by platform listener threads and may be read from any thread;
    /// reads and writes use volatile semantics. It is diagnostic state, not a synchronization
    /// primitive.
    /// </remarks>
    public DeviceListenerStatus Status
    {
        get => _status;
        protected set => _status = value;
    }

    /// <summary>
    /// Starts the listener. Establishes baseline of currently connected devices,
    /// then begins monitoring for changes. Only fires events for subsequent changes.
    /// </summary>
    /// <remarks>
    /// This method should be called after setting <see cref="Changed"/> callback.
    /// Calling Start() on an already started listener has no effect.
    /// </remarks>
    public abstract void Start();

    /// <summary>
    /// Requests listener shutdown and platform monitoring-resource cleanup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After successful shutdown, a listener may be restarted with <see cref="Start"/> if its platform
    /// implementation has completed cleanup. Retained or uncertain native state can prevent restart.
    /// Calling Stop() on an already stopped listener has no effect.
    /// </para>
    /// <para>
    /// Do not call <see cref="Stop"/> or <see cref="Dispose()"/> from within the
    /// <see cref="Changed"/> callback. Native unregister and thread joins can wait for that callback,
    /// causing a deadlock or consuming the shutdown wait budget. A timeout or failed unregister
    /// does not prove that native callbacks have drained.
    /// </para>
    /// </remarks>
    public abstract void Stop();

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
    /// <param name="hint">The diagnostic rescan hint to log before notifying subscribers.</param>
    private protected void OnDeviceEvent(HidDeviceRescanHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);

        try
        {
            Logger.LogTrace(
                "Received HID rescan hint: {Kind}, {PlatformDeviceId}, {DevicePath}",
                hint.ChangeKind,
                hint.PlatformDeviceId,
                hint.DevicePath);
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Exception in Changed callback");
        }
    }

    /// <summary>
    /// Updates managed disposal state as part of the platform listener's cleanup attempt.
    /// </summary>
    /// <remarks>
    /// Native cleanup depends on the platform outcome. Failed unregister or retained native state can leave
    /// resources unresolved; a returned call or diagnostic status alone does not prove native release.
    /// </remarks>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Changed = null;
            Status = DeviceListenerStatus.Stopped;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Do not call from within the <see cref="Changed"/> callback; see <see cref="Stop"/>.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}