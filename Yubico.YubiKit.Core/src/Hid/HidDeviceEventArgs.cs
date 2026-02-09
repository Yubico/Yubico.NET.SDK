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

using Yubico.YubiKit.Core.Hid.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Event arguments for HID device arrival and removal events.
/// </summary>
/// <param name="device">The HID device that was added or removed.</param>
public sealed class HidDeviceEventArgs(IHidDevice device) : EventArgs
{
    /// <summary>
    /// Gets the HID device associated with this event.
    /// </summary>
    /// <remarks>
    /// For removal events on some platforms, this may be a <see cref="NullDevice"/> 
    /// when full device information is unavailable.
    /// </remarks>
    public IHidDevice Device { get; } = device ?? throw new ArgumentNullException(nameof(device));
}
