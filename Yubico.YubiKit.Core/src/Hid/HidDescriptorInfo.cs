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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Raw HID descriptor information as reported by the operating system.
/// Contains the actual Usage Page and Usage values from the HID report descriptor.
/// </summary>
/// <remarks>
/// These values follow the HID Usage Tables specification (USB-IF).
/// Do not use these values directly for business logic - use <see cref="HidInterfaceType"/> instead.
/// </remarks>
public sealed record HidDescriptorInfo
{
    /// <summary>
    /// HID Usage Page value from descriptor (e.g., 0x01 for Generic Desktop, 0xF1D0 for FIDO).
    /// </summary>
    public ushort UsagePage { get; init; }
    
    /// <summary>
    /// HID Usage value from descriptor (e.g., 0x06 for Keyboard, 0x01 for U2F Device).
    /// </summary>
    public ushort Usage { get; init; }
    
    /// <summary>
    /// Device path or identifier.
    /// </summary>
    public string DevicePath { get; init; } = string.Empty;
    
    /// <summary>
    /// Vendor ID (e.g., 0x1050 for Yubico).
    /// </summary>
    public short VendorId { get; init; }
    
    /// <summary>
    /// Product ID.
    /// </summary>
    public short ProductId { get; init; }

    public override string ToString() => $"UsagePage=0x{UsagePage:X4}, Usage=0x{Usage:X4}, DevicePath={DevicePath}, VendorId=0x{VendorId:X4}, ProductId=0x{ProductId:X4}";
}
