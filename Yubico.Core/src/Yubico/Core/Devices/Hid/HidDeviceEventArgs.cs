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

namespace Yubico.Core.Devices.Hid;

/// <summary>
///     Event arguments given whenever a HID device is added or removed from the system.
/// </summary>
public class HidDeviceEventArgs : DeviceEventArgs<IHidDevice>
{
    /// <summary>
    ///     Constructs a new instance of the <see cref="HidDeviceEventArgs" /> class.
    /// </summary>
    /// <param name="device">
    ///     The HID device that is originating this event.
    /// </param>
    public HidDeviceEventArgs(IHidDevice device) : base(device)
    {
    }
}
