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

namespace Yubico.Core.Devices
{
    /// <summary>
    /// Defines the contract for device-related event arguments in a generic context.
    /// This interface allows for type-safe access to device information in event handling
    /// scenarios, supporting device types that implement the <see cref="IDevice"/> interface.
    /// It enables specific device type information to be preserved and accessed in event handlers.
    /// </summary>
    /// <remarks>
    /// While this interface does not inherit from <see cref="System.EventArgs"/>, it retains the "Args" suffix
    /// in its name. This naming convention is deliberately chosen to maintain consistency with standard
    /// event argument naming patterns in C#, particularly for improved readability when used in delegate
    /// and event handler signatures. The familiar "Args" suffix clearly indicates the interface's role
    /// in event-related contexts, despite not directly extending <see cref="System.EventArgs"/>.
    /// </remarks>
    /// <typeparam name="TDevice">The specific type of <see cref="IDevice"/> this event argument represents.
    /// This type parameter is covariant, allowing for more specific device types to be used
    /// where a more general device type is expected.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public interface IDeviceEventArgs<out TDevice> where TDevice : IDevice
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        /// <summary>
        /// Gets the specific type of <see cref="IDevice"/> that originated the event.
        /// This property will always be populated, regardless of whether this is an arrival event or a removal event.
        /// If the device was removed, not all members will be available on the object. An exception will be thrown if
        /// you try to use the device in a way that requires it to be present.
        /// </summary>
        /// <remarks>
        /// This property provides access to the specific <c>TDevice</c> instance associated with the current event.
        /// </remarks>
        /// <value>
        /// An instance of <c>TDevice</c> that triggered this event.
        /// </value>
        TDevice Device { get; }
    }

    /// <summary>
    /// Event arguments given whenever a device is added or removed from the system, providing strongly-typed access to the device that triggered the event.
    /// </summary>
    /// <typeparam name="TDevice">The type of device associated with this event, which must implement <see cref="IDevice"/>.</typeparam>
    public abstract class DeviceEventArgs<TDevice> : EventArgs, IDeviceEventArgs<TDevice>
        where TDevice : IDevice
    {
        /// <inheritdoc />
        public TDevice Device { get; }

        /// <summary>
        /// Constructs a new instance of the <see cref="DeviceEventArgs{TDevice}"/> class.
        /// </summary>
        protected DeviceEventArgs(TDevice device)
        {
            Device = device;
        }
    }
}
