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

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

/// <summary>
/// Device selector for OATH operations.
/// OATH only supports SmartCard (CCID) transport.
/// In non-interactive mode, auto-selects the first available device.
/// </summary>
public sealed class OathDeviceSelector : DeviceSelectorBase
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static OathDeviceSelector Instance { get; } = new();

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
}
