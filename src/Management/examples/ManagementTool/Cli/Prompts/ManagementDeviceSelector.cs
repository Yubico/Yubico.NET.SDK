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

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

/// <summary>
/// Device selector for Management operations.
/// Supports SmartCard (CCID), FIDO HID, and OTP HID transports.
/// Does not auto-select when multiple devices are found.
/// </summary>
public sealed class ManagementDeviceSelector : DeviceSelectorBase
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static ManagementDeviceSelector Instance { get; } = new();

    /// <inheritdoc />
    protected override ConnectionType[] SupportedConnectionTypes { get; } =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidFido,
        ConnectionType.HidOtp
    ];

    /// <inheritdoc />
    protected override string SupportedTransportsDescription => "SmartCard (CCID), FIDO HID, OTP HID";
}
