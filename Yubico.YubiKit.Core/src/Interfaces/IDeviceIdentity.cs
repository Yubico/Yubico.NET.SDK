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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Interfaces;

/// <summary>
/// Represents the identity and capability information of a YubiKey device.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides read-only access to device information used for:
/// <list type="bullet">
/// <item>Device identification (serial number, firmware version)</item>
/// <item>Form factor and capability detection</item>
/// <item>Device correlation across multiple transports</item>
/// <item>Configuration fingerprinting for devices without serial numbers</item>
/// </list>
/// </para>
/// <para>
/// The interface is designed to be implemented by device info readers from
/// the Management module while being consumable from Core for device correlation.
/// </para>
/// </remarks>
public interface IDeviceIdentity
{
    /// <summary>
    /// Gets the device serial number, or <c>null</c> if unavailable.
    /// </summary>
    /// <remarks>
    /// Serial number may be null for Security Keys or devices with serial number
    /// visibility disabled. When null, device correlation falls back to 
    /// configuration fingerprinting.
    /// </remarks>
    int? SerialNumber { get; }

    /// <summary>
    /// Gets the device firmware version.
    /// </summary>
    FirmwareVersion FirmwareVersion { get; }

    /// <summary>
    /// Gets the physical form factor of the device.
    /// </summary>
    FormFactor FormFactor { get; }

    /// <summary>
    /// Gets the capabilities supported over USB transport.
    /// </summary>
    DeviceCapabilities UsbSupported { get; }

    /// <summary>
    /// Gets the capabilities supported over NFC transport.
    /// </summary>
    DeviceCapabilities NfcSupported { get; }

    /// <summary>
    /// Gets the capabilities currently enabled over USB transport.
    /// </summary>
    DeviceCapabilities UsbEnabled { get; }

    /// <summary>
    /// Gets the capabilities currently enabled over NFC transport.
    /// </summary>
    DeviceCapabilities NfcEnabled { get; }

    /// <summary>
    /// Gets the auto-eject timeout in seconds for CCID touch-eject feature.
    /// </summary>
    ushort AutoEjectTimeout { get; }

    /// <summary>
    /// Gets the challenge-response timeout configuration.
    /// </summary>
    ReadOnlyMemory<byte> ChallengeResponseTimeout { get; }

    /// <summary>
    /// Gets miscellaneous device flags.
    /// </summary>
    DeviceFlags DeviceFlags { get; }

    /// <summary>
    /// Gets a value indicating whether NFC is restricted on this device.
    /// </summary>
    bool IsNfcRestricted { get; }

    /// <summary>
    /// Gets the union of all capabilities supported across any transport.
    /// </summary>
    /// <remarks>
    /// This is a computed property that returns <c>UsbSupported | NfcSupported</c>.
    /// </remarks>
    DeviceCapabilities SupportedCapabilities => UsbSupported | NfcSupported;
}
