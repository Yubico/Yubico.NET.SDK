// Copyright 2024 Yubico AB
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
using System.Collections.Generic;
using System.Text;

namespace Yubico.Core.Devices
{
    /// <summary>
    /// Event arguments given whenever a device is added or removed from the system.
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        /// <summary>
        /// The device that originated the event.
        /// </summary>
        public IDevice? BaseDevice { get; set; }

        /// <summary>
        /// Constructs a new instance of the <see cref="DeviceEventArgs"/> class.
        /// </summary>
        /// <param name="device">
        /// The device that is originating this event.
        /// </param>
        public DeviceEventArgs(IDevice? device)
        {
            BaseDevice = device;
        }
    }
}
