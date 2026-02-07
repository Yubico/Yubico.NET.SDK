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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// A correlation key used to identify and group YubiKey transport references
/// that belong to the same physical device.
/// </summary>
/// <remarks>
/// <para>
/// The correlation key combines multiple device attributes to create a unique
/// identifier for a physical YubiKey across different transport connections:
/// </para>
/// <list type="bullet">
/// <item>Serial number (when available)</item>
/// <item>Firmware version</item>
/// <item>Form factor</item>
/// <item>Supported capabilities</item>
/// <item>Configuration fingerprint (enabled capabilities, timeouts, flags)</item>
/// </list>
/// <para>
/// Two transport references (e.g., SmartCard and HID) with matching correlation
/// keys are assumed to represent the same physical YubiKey device.
/// </para>
/// </remarks>
internal readonly record struct DeviceCorrelationKey(
    int? SerialNumber,
    FirmwareVersion FirmwareVersion,
    FormFactor FormFactor,
    DeviceCapabilities SupportedCapabilities,
    string ConfigFingerprint)
{
    /// <summary>
    /// Creates a correlation key from device identity information.
    /// </summary>
    /// <param name="identity">The device identity to create a key from.</param>
    /// <returns>A new <see cref="DeviceCorrelationKey"/> for the device.</returns>
    public static DeviceCorrelationKey From(IDeviceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return new DeviceCorrelationKey(
            SerialNumber: identity.SerialNumber,
            FirmwareVersion: identity.FirmwareVersion,
            FormFactor: identity.FormFactor,
            SupportedCapabilities: identity.SupportedCapabilities,
            ConfigFingerprint: identity.ComputeConfigFingerprint());
    }
}
