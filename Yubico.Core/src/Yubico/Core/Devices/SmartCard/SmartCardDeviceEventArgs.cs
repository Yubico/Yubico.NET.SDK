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

namespace Yubico.Core.Devices.SmartCard
{
    /// <summary>
    ///     Event arguments given whenever a smart card device is added or removed from the system.
    /// </summary>
    public class SmartCardDeviceEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructs a new instance of the <see cref="SmartCardDeviceEventArgs" /> class.
        /// </summary>
        /// <param name="device">
        ///     The smart card device that is originating this event.
        /// </param>
        public SmartCardDeviceEventArgs(ISmartCardDevice device)
        {
            Device = device;
        }

        /// <summary>
        ///     The smart card device that originated the event.
        /// </summary>
        /// <remarks>
        ///     This property will always be populated, regardless of whether this is an arrival event or a removal event.
        ///     If the device was removed, not all members will be available on the object. An exception will be thrown if
        ///     you try to use the device in a way that requires it to be present.
        /// </remarks>
        public ISmartCardDevice Device { get; set; }
    }
}
