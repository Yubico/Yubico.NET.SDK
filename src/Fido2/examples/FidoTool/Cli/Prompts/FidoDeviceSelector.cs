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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;

/// <summary>
/// Device selector for FIDO2 operations.
/// Supports FIDO HID (USB) and SmartCard (NFC) transports.
/// In non-interactive mode, prefers FIDO HID as the native FIDO2 transport.
/// </summary>
public sealed class FidoDeviceSelector : DeviceSelectorBase
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static FidoDeviceSelector Instance { get; } = new();

    /// <inheritdoc />
    protected override ConnectionType[] SupportedConnectionTypes { get; } =
    [
        ConnectionType.HidFido,
        ConnectionType.SmartCard
    ];

    /// <inheritdoc />
    protected override string SupportedTransportsDescription => "FIDO HID (USB), SmartCard (NFC)";

    /// <inheritdoc />
    /// <remarks>
    /// Prefers FIDO HID in non-interactive mode as it is the native FIDO2 transport.
    /// Falls back to the first available device if no FIDO HID device is found.
    /// </remarks>
    protected override IYubiKey? AutoSelectDevice(IReadOnlyList<IYubiKey> devices) =>
        devices.FirstOrDefault(d => d.ConnectionType == ConnectionType.HidFido)
        ?? devices[0];
}
