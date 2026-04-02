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
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;

/// <summary>
/// Interactive menu for factory reset of the FIDO2 application.
/// </summary>
public static class ResetMenu
{
    /// <summary>
    /// Runs the factory reset flow with multiple confirmation prompts.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Factory Reset");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Double confirmation
        if (!OutputHelpers.ConfirmDestructive("factory reset the FIDO2 application"))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]To perform the reset:[/]");
        AnsiConsole.MarkupLine("[yellow]  1. Remove your YubiKey[/]");
        AnsiConsole.MarkupLine("[yellow]  2. Re-insert your YubiKey[/]");
        AnsiConsole.MarkupLine("[yellow]  3. Touch your YubiKey within 5 seconds[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Ready to proceed?", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return;
        }

        AnsiConsole.MarkupLine("[yellow]Waiting for device re-insertion...[/]");
        AnsiConsole.MarkupLine("[grey]Press Enter after re-inserting your YubiKey.[/]");
        Console.ReadLine();

        // Re-select device after reinsertion
        var newSelection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (newSelection is null)
        {
            OutputHelpers.WriteError("No YubiKey found after re-insertion.");
            return;
        }

        OutputHelpers.PromptForTouch();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Resetting FIDO2 application...", async _ =>
                await ResetAuthenticator.ResetAsync(newSelection.Device, cancellationToken));

        if (result.Success)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("FIDO2 application has been factory reset.");
            OutputHelpers.WriteInfo("All credentials, PINs, and settings have been erased.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }
}
