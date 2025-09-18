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

using System;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard;

/// <summary>
///     A class that provides events for a smart card device arrival and removal.
/// </summary>
/// <remarks>
///     <para>
///         This class exposes two events to the caller: <see cref="Arrived" /> and <see cref="Removed" />. As their names
///         suggest, subscribing to these events will inform your code whenever a new smart card device is discovered on
///         the
///         system, or one is removed, respectively.
///     </para>
///     <para>
///         You will only receive events for devices that were added or removed after you subscribed to the event. That is,
///         arrival events will only be raised for devices added after you have subscribed to the Arrived events. All
///         devices
///         already attached to the system will be ignored.
///     </para>
/// </remarks>
public abstract class SmartCardDeviceListener
{
    private readonly ILogger _log = Log.GetLogger<SmartCardDeviceListener>();

    /// <summary>
    ///     A status that indicates the state of the device listener.
    /// </summary>
    public DeviceListenerStatus Status { get; set; }

    /// <summary>
    ///     Subscribe to receive an event whenever a smart card device is added to the computer.
    /// </summary>
    public event EventHandler<SmartCardDeviceEventArgs>? Arrived;

    /// <summary>
    ///     Subscribe to receive an event whenever a smart card device is removed from the computer.
    /// </summary>
    public event EventHandler<SmartCardDeviceEventArgs>? Removed;

    /// <summary>
    ///     Creates an instance of a <see cref="SmartCardDeviceListener" />.
    /// </summary>
    /// <returns>
    ///     An instance of DesktopSmartCardDeviceListener.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     This class depends on operating system specific support being present. If this exception is being raised,
    ///     the operating system or platform that you are attempting to run this does not have a smart card device notification
    ///     support.
    /// </exception>
    public static SmartCardDeviceListener Create() =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new DesktopSmartCardDeviceListener(),
            SdkPlatform.MacOS => new DesktopSmartCardDeviceListener(),
            SdkPlatform.Linux => new DesktopSmartCardDeviceListener(),
            _ => throw new PlatformNotSupportedException()
        };

    /// <summary>
    ///     Implementers should call this method when they have discovered a new smart card device on the system.
    /// </summary>
    /// <param name="device">
    ///     The device instance that originates this event.
    /// </param>
    protected void OnArrived(ISmartCardDevice device)
    {
        _log.LogInformation("ISmartCardDevice {Device} arrived.", device);
        Arrived?.Invoke(this, new SmartCardDeviceEventArgs(device));
    }

    /// <summary>
    ///     Implementers should call this method when they have discovered that a smart card device has been removed from
    ///     the system.
    /// </summary>
    /// <param name="device">
    ///     The device instance that originates this event.
    /// </param>
    protected void OnRemoved(ISmartCardDevice device)
    {
        _log.LogInformation("ISmartCardDevice {Device} removed.", device);
        Removed?.Invoke(this, new SmartCardDeviceEventArgs(device));
    }

    /// <summary>
    ///     Implementers can call this method to reset the event handlers during cleanup.
    /// </summary>
    protected void ClearEventHandlers()
    {
        Arrived = null;
        Removed = null;
    }
}
