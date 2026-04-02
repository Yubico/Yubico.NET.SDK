// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Shared.Device;

/// <summary>
/// Represents a selected YubiKey device with its identifying information.
/// Shared across all CLI tools.
/// </summary>
/// <param name="Device">The selected YubiKey device.</param>
/// <param name="SerialNumber">The device serial number, if available.</param>
/// <param name="FormFactor">The device form factor.</param>
/// <param name="FirmwareVersion">The firmware version string.</param>
/// <param name="ConnectionType">The connection type used to connect to this device.</param>
public record DeviceSelection(
    IYubiKey Device,
    int? SerialNumber,
    FormFactor FormFactor,
    string FirmwareVersion,
    ConnectionType ConnectionType)
{
    /// <summary>
    /// Gets a display string for this device (e.g., "YubiKey 5C - S/N: 12345678 (SmartCard)").
    /// </summary>
    public string DisplayName =>
        SerialNumber.HasValue
            ? $"YubiKey {FormFactorFormatter.Format(FormFactor)} - S/N: {SerialNumber} ({ConnectionTypeFormatter.Format(ConnectionType)})"
            : $"YubiKey {FormFactorFormatter.Format(FormFactor)} ({ConnectionTypeFormatter.Format(ConnectionType)})";
}