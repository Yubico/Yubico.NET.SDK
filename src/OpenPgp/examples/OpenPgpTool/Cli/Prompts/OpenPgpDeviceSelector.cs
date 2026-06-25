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
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;

/// <summary>
/// Device selector for OpenPGP operations.
/// OpenPGP uses SmartCard (CCID) transport only.
/// In non-interactive mode, auto-selects the first available device.
/// Emits error to stderr when no devices found in non-interactive mode.
/// </summary>
public sealed class OpenPgpDeviceSelector : DeviceSelectorBase
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static OpenPgpDeviceSelector Instance { get; } = new();

    /// <inheritdoc />
    protected override ConnectionType[] SupportedConnectionTypes { get; } =
    [
        ConnectionType.SmartCard
    ];

    /// <inheritdoc />
    protected override string SupportedTransportsDescription => "SmartCard (CCID)";

    /// <inheritdoc />
    protected override ConnectionType FindAllConnectionTypeFilter => ConnectionType.SmartCard;

    /// <inheritdoc />
    protected override IYubiKey? AutoSelectDevice(IReadOnlyList<IYubiKey> devices) =>
        devices[0];

    /// <inheritdoc />
    protected override bool HandleNonInteractiveNoDevices()
    {
        Console.Error.WriteLine("Error: No YubiKey detected. OpenPGP requires SmartCard (CCID) transport.");
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Silently swallows device info errors (no debug output).
    /// </remarks>
    protected override async Task<DeviceInfo?> GetDeviceInfoAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            return await device.GetDeviceInfoAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
