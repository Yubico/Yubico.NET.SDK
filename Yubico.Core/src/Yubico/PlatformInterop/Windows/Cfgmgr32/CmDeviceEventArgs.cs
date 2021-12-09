// Copyright 2021 Yubico AB
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

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Event arguments for Windows HID events.
    /// </summary>
    public class CmDeviceEventArgs : EventArgs
    {
        /// <summary>
        /// The path to the HID device.
        /// </summary>
        /// <value>
        /// A platform defined path to the device.
        /// </value>
        public string DeviceInterfacePath { get; }

        /// <summary>
        /// Constructs an instance of the <see cref="CmDeviceEventArgs"/> class. 
        /// </summary>
        /// <param name="deviceInterfacePath">The path to the HID device.</param>
        public CmDeviceEventArgs(string deviceInterfacePath)
        {
            DeviceInterfacePath = deviceInterfacePath;
        }

        /// <summary>
        /// Gets an instance of the <see cref="CmDevice"/> class. 
        /// </summary>
        /// <returns>A <see cref="CmDevice"/> instance.</returns>
        public CmDevice GetDevice() => new CmDevice(DeviceInterfacePath);
    }
}
