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

namespace Yubico.YubiKit.Core;

/// <summary>
///     Represents properties and methods common to all devices.
/// </summary>
public interface IDevice
{
    string ReaderName { get; }

    // /// <summary>
    // /// A unique identifier corresponding to the parent USB composite device, if available.
    // /// </summary>
    // /// <remarks>
    // /// <para>
    // /// Certain devices that this SDK works with, like the YubiKey, are presented to the operating system as a
    // /// composite device. This means that, to the operating system, multiple devices may be detected even though
    // /// you have only plugged in one physical device. OSes that do this still expose a single "composite" device
    // /// in their device tree. This is the common parent across all of the sub-devices.
    // /// </para>
    // /// <para>
    // /// Keeping track of this information, if available, can greatly increase the SDK's performance when it attempts
    // /// to reconstruct the single, composite device out of all of the sub-devices. This isn't always possible,
    // /// however, as the OS services that we use to enumerate the sub-devices don't always provide much information
    // /// about its parent device. macOS and Windows 7, for example, cannot provide parent information for smart card
    // /// devices. In those cases, we'll have to make do and attempt to match these devices some other way.
    // /// </para>
    // /// </remarks>
    // string? ParentDeviceId { get; }
    //
    // /// <summary>
    // /// The time when this device was last accessed.
    // /// </summary>
    // /// <remarks>
    // /// Certain Yubico devices, such as the YubiKey, require a short delay before switching to and communicating with
    // /// a different USB interface. This is called the reclaim timeout. The YubiKey SDK must keep track of the last
    // /// interface to have been accessed and when in order to properly implement this delay.
    // /// </remarks>
    // DateTime LastAccessed { get; }
}