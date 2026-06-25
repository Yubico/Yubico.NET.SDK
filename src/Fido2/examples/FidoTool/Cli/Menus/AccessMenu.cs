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
/// Interactive menu for PIN/access management operations.
/// Mirrors ykman's "fido access" command group.
/// </summary>
public static class AccessMenu
{
    /// <summary>
    /// Runs the access management sub-menu.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Access (PIN Management)");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select access operation:")
                .AddChoices(
                [
                    "Change PIN",
                    "Verify PIN",
                    "Set PIN (first time)",
                    "View PIN retries",
                    "View UV retries",
                    "Back"
                ]));

        switch (choice)
        {
            case "Change PIN":
                await RunChangePinAsync(selection.Device, cancellationToken);
                break;
            case "Verify PIN":
                await RunVerifyPinAsync(selection.Device, cancellationToken);
                break;
            case "Set PIN (first time)":
                await RunSetPinAsync(selection.Device, cancellationToken);
                break;
            case "View PIN retries":
                await RunPinRetriesAsync(selection.Device, cancellationToken);
                break;
            case "View UV retries":
                await RunUvRetriesAsync(selection.Device, cancellationToken);
                break;
        }
    }

    private static async Task RunChangePinAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        using var currentPinOwner = FidoPinHelper.PromptForPin("Enter current PIN: ");
        if (currentPinOwner is null)
        {
            OutputHelpers.WriteError("Current PIN is required.");
            return;
        }

        using var newPinOwner = FidoPinHelper.PromptForNewPin("Enter new PIN: ");
        if (newPinOwner is null)
        {
            OutputHelpers.WriteError("New PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Changing PIN...", async _ =>
                await PinManagement.ChangePinAsync(device, currentPinOwner.Memory, newPinOwner.Memory, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN changed successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunVerifyPinAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        using var pinOwner = FidoPinHelper.PromptForPin();
        if (pinOwner is null)
        {
            OutputHelpers.WriteError("PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Verifying PIN...", async _ =>
                await PinManagement.VerifyPinAsync(device, pinOwner.Memory, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN is correct.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunSetPinAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        using var newPinOwner = FidoPinHelper.PromptForNewPin("Enter new PIN (4-63 characters): ");
        if (newPinOwner is null)
        {
            OutputHelpers.WriteError("New PIN is required.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Setting PIN...", async _ =>
                await PinManagement.SetPinAsync(device, newPinOwner.Memory, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN set successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunPinRetriesAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var result = await AnsiConsole.Status()
            .StartAsync("Querying PIN retries...", async _ =>
                await PinManagement.GetPinRetriesAsync(device, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteKeyValue("PIN retries remaining", result.Retries.ToString());

            if (result.PowerCycleRequired)
            {
                OutputHelpers.WriteWarning(
                    "Power cycle required. Remove and re-insert the YubiKey before next PIN attempt.");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunUvRetriesAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var result = await AnsiConsole.Status()
            .StartAsync("Querying UV retries...", async _ =>
                await PinManagement.GetUvRetriesAsync(device, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteKeyValue("UV retries remaining", result.Retries.ToString());

            if (result.PowerCycleRequired)
            {
                OutputHelpers.WriteWarning(
                    "Power cycle required. Remove and re-insert the YubiKey before next UV attempt.");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }
}
