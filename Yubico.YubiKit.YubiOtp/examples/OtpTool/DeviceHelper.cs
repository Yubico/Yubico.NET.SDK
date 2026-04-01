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

using Spectre.Console;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>
/// Handles YubiKey device discovery and selection for OTP operations.
/// Supports SmartCard and OTP HID transports.
/// </summary>
public static class DeviceHelper
{
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidOtp
    ];

    /// <summary>
    /// Finds a YubiKey and creates a YubiOTP session.
    /// In automation mode (JSON), fails immediately if no device found.
    /// In interactive mode, prompts for retry.
    /// </summary>
    public static async Task<YubiOtpSession?> CreateSessionAsync(
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        var device = await SelectDeviceAsync(jsonMode, cancellationToken);
        if (device is null)
        {
            return null;
        }

        return await device.CreateYubiOtpSessionAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finds and selects a YubiKey device.
    /// </summary>
    public static async Task<IYubiKey?> SelectDeviceAsync(
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allDevices = await YubiKeyManager.FindAllAsync(
                ConnectionType.All,
                cancellationToken: cancellationToken);

            var devices = allDevices
                .Where(d => SupportedConnectionTypes.Contains(d.ConnectionType))
                .ToList();

            if (devices.Count == 1)
            {
                return devices[0];
            }

            if (devices.Count > 1)
            {
                if (jsonMode)
                {
                    // In JSON mode, use the first device
                    return devices[0];
                }

                return await PromptDeviceSelectionAsync(devices, cancellationToken);
            }

            if (jsonMode)
            {
                return null;
            }

            AnsiConsole.MarkupLine("[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            AnsiConsole.MarkupLine("[grey]Supported transports: SmartCard (CCID), OTP HID[/]");

            if (!AnsiConsole.Confirm("Retry?", defaultValue: true))
            {
                return null;
            }
        }

        return null;
    }

    private static async Task<IYubiKey?> PromptDeviceSelectionAsync(
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        var choices = devices
            .Select((d, i) => $"{i + 1}. YubiKey ({FormatConnectionType(d.ConnectionType)})")
            .ToList();

        choices.Add("Cancel");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Multiple YubiKeys detected. Select one:")
                .PageSize(10)
                .AddChoices(choices));

        if (selection == "Cancel")
        {
            return null;
        }

        int index = choices.IndexOf(selection);
        return devices[index];
    }

    /// <summary>
    /// Formats a connection type for display.
    /// </summary>
    public static string FormatConnectionType(ConnectionType connectionType) =>
        connectionType switch
        {
            ConnectionType.SmartCard => "SmartCard",
            ConnectionType.HidOtp => "OTP HID",
            ConnectionType.HidFido => "FIDO HID",
            _ => "Unknown"
        };
}
