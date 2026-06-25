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
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;

/// <summary>
/// Interactive menu for authenticator configuration operations.
/// Mirrors ykman's "fido config" command group.
/// </summary>
public static class ConfigMenu
{
    /// <summary>
    /// Runs the authenticator config sub-menu.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Config (Authenticator Settings)");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Check feature support
        var supported = await ConfigManagement.IsSupported(selection.Device, cancellationToken);
        if (!supported)
        {
            OutputHelpers.WriteError(
                "Authenticator config is not supported on this device (requires firmware 5.4+).");
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "Toggle always-UV",
                    "Enable enterprise attestation",
                    "Set minimum PIN length",
                    "Back"
                ]));

        switch (choice)
        {
            case "Toggle always-UV":
                await RunToggleAlwaysUvAsync(selection.Device, cancellationToken);
                break;
            case "Enable enterprise attestation":
                await RunEnableEnterpriseAttestationAsync(selection.Device, cancellationToken);
                break;
            case "Set minimum PIN length":
                await RunSetMinPinLengthAsync(selection.Device, cancellationToken);
                break;
        }
    }

    private static async Task RunToggleAlwaysUvAsync(
        IYubiKey device, CancellationToken cancellationToken)
    {
        OutputHelpers.WriteInfo(
            "This toggles the always-UV setting. When enabled, user verification is required for every operation.");

        using var pinOwner = FidoPinHelper.PromptForPin();
        if (pinOwner is null)
        {
            OutputHelpers.WriteError("PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Toggling always-UV...", async _ =>
                await ConfigManagement.ToggleAlwaysUvAsync(device, pinOwner.Memory, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Always-UV setting toggled.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunEnableEnterpriseAttestationAsync(
        IYubiKey device, CancellationToken cancellationToken)
    {
        OutputHelpers.WriteWarning(
            "Once enabled, enterprise attestation cannot be disabled without a factory reset.");

        if (!AnsiConsole.Confirm("Enable enterprise attestation?", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Operation cancelled.");
            return;
        }

        using var pinOwner = FidoPinHelper.PromptForPin();
        if (pinOwner is null)
        {
            OutputHelpers.WriteError("PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Enabling enterprise attestation...", async _ =>
                await ConfigManagement.EnableEnterpriseAttestationAsync(
                    device, pinOwner.Memory, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Enterprise attestation enabled.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunSetMinPinLengthAsync(
        IYubiKey device, CancellationToken cancellationToken)
    {
        var length = AnsiConsole.Ask("New minimum PIN length (4-63):", 6);

        var forceChange = AnsiConsole.Confirm(
            "Force PIN change before next use?", defaultValue: false);

        using var pinOwner = FidoPinHelper.PromptForPin();
        if (pinOwner is null)
        {
            OutputHelpers.WriteError("PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Setting minimum PIN length...", async _ =>
                await ConfigManagement.SetMinPinLengthAsync(
                    device, pinOwner.Memory, length, forceChangePin: forceChange,
                    cancellationToken: cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess($"Minimum PIN length set to {length}.");
            if (forceChange)
            {
                OutputHelpers.WriteWarning("PIN change will be required before next use.");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }
}
