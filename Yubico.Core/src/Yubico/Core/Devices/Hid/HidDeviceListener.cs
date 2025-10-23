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
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// A class that provides events for HID device arrival and removal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class exposes two events to the caller: <see cref="Arrived"/> and <see cref="Removed"/>. As their names
    /// suggest, subscribing to these events will inform your code whenever a new HID device is discovered on the
    /// system, or one is removed, respectively.
    /// </para>
    /// <para>
    /// You will only receive events for devices that were added or removed after you subscribed to the event. That is,
    /// arrival events will only be raised for devices added after you have subscribed to the Arrived events. All devices
    /// already attached to the system will be ignored.
    /// </para>
    /// </remarks>
    public abstract class HidDeviceListener : IDisposable
    {
        private readonly ILogger _log = Logging.Log.GetLogger<HidDeviceListener>();

        /// <summary>
        /// Subscribe to receive an event whenever a Human Interface Device (HID) is added to the computer.
        /// </summary>
        public event EventHandler<HidDeviceEventArgs>? Arrived;

        /// <summary>
        /// Subscribe to receive an event whenever a Human Interface Device (HID) is removed from the computer.
        /// </summary>
        public event EventHandler<HidDeviceEventArgs>? Removed;

        private bool _disposed;

        /// <summary>
        /// Creates an instance of a <see cref="HidDeviceListener"/>.
        /// </summary>
        /// <returns>
        /// An instance of HidDeviceListener.
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">
        /// This class depends on operating system specific support being present. If this exception is being raised,
        /// the operating system or platform that you are attempting to run this does not have HID device notification
        /// support.
        /// </exception>
        public static HidDeviceListener Create() =>
            SdkPlatformInfo.OperatingSystem switch
            {
                SdkPlatform.Windows => new WindowsHidDeviceListener(),
                SdkPlatform.MacOS => new MacOSHidDeviceListener(),
                SdkPlatform.Linux => new LinuxHidDeviceListener(),
                _ => throw new PlatformNotSupportedException()
            };

        /// <summary>
        /// Implementers should call this method when they have discovered a new HID device on the system.
        /// </summary>
        /// <param name="device">
        /// The device instance that originates this event.
        /// </param>
        protected void OnArrived(IHidDevice device)
        {
            _log.LogInformation("HID {Device} arrived.", device);

            if (Arrived is null)
            {
                return;
            }

            // Invoke each handler individually to ensure one throwing handler doesn't prevent others from executing
            foreach (Delegate? @delegate in Arrived.GetInvocationList())
            {
                var handler = (EventHandler<HidDeviceEventArgs>)@delegate;
                try
                {
                    handler.Invoke(this, new HidDeviceEventArgs(device));
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Exception in user's HID Arrived event handler. The exception has been caught to prevent SDK background thread crash.");
                }
            }
        }

        /// <summary>
        /// Implementers should call this method when they have discovered that a HID device has been removed from
        /// the system.
        /// </summary>
        /// <param name="device">
        /// The device instance that originates this event.
        /// </param>
        protected void OnRemoved(IHidDevice device)
        {
            _log.LogInformation("HID {Device} removed.", device);

            if (Removed is null)
            {
                return;
            }

            // Invoke each handler individually to ensure one throwing handler doesn't prevent others from executing
            foreach (Delegate? @delegate in Removed.GetInvocationList())
            {
                var handler = (EventHandler<HidDeviceEventArgs>)@delegate;
                try
                {
                    handler.Invoke(this, new HidDeviceEventArgs(device));
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Exception in user's HID Removed event handler. The exception has been caught to prevent SDK background thread crash.");
                }
            }
        }

        /// <summary>
        /// Implementers can call this method to reset the event handlers during cleanup.
        /// </summary>
        protected void ClearEventHandlers()
        {
            Arrived = null;
            Removed = null;
        }

        /// <summary>
        /// Release the managed and unmanaged resources used by the
        /// <see cref="HidDeviceListener" />.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release the managed and unmanaged resources used by the
        /// <see cref="HidDeviceListener" />.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                ClearEventHandlers();
            }

            _disposed = true;
        }
    }
}
