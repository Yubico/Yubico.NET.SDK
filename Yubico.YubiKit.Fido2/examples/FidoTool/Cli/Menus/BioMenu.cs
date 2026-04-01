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
using Yubico.YubiKit.Fido2.BioEnrollment;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;

/// <summary>
/// Interactive menu for biometric enrollment operations.
/// </summary>
public static class BioMenu
{
    /// <summary>
    /// Runs the bio enrollment sub-menu.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Bio Enrollment");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Check feature support
        var supported = await BioEnrollmentExample.IsSupported(selection.Device, cancellationToken);
        if (!supported)
        {
            OutputHelpers.WriteError(
                "Bio enrollment is not supported on this device (requires firmware 5.2+ with biometric hardware).");
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "Sensor info",
                    "Enroll fingerprint",
                    "List enrollments",
                    "Rename enrollment",
                    "Remove enrollment",
                    "Back"
                ]));

        switch (choice)
        {
            case "Sensor info":
                await RunSensorInfoAsync(selection.Device, cancellationToken);
                break;
            case "Enroll fingerprint":
                await RunEnrollAsync(selection.Device, cancellationToken);
                break;
            case "List enrollments":
                await RunListAsync(selection.Device, cancellationToken);
                break;
            case "Rename enrollment":
                await RunRenameAsync(selection.Device, cancellationToken);
                break;
            case "Remove enrollment":
                await RunRemoveAsync(selection.Device, cancellationToken);
                break;
        }
    }

    private static async Task RunSensorInfoAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var result = await AnsiConsole.Status()
            .StartAsync("Querying sensor info...", async _ =>
                await BioEnrollmentExample.GetSensorInfoAsync(device, pin, cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        var info = result.SensorInfo!;
        OutputHelpers.WriteSuccess("Fingerprint sensor info retrieved.");
        OutputHelpers.WriteKeyValue("Sensor type",
            info.FingerprintKind == FingerprintKind.Touch ? "Touch" : "Swipe");
        OutputHelpers.WriteKeyValue("Max samples for enrollment",
            info.MaxCaptureSamplesRequiredForEnroll.ToString());

        if (info.MaxTemplateCount.HasValue)
        {
            OutputHelpers.WriteKeyValue("Max templates", info.MaxTemplateCount.ToString());
        }
    }

    private static async Task RunEnrollAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var friendlyName = AnsiConsole.Ask("Friendly name (optional):", string.Empty);
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            friendlyName = null;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Place your finger on the sensor...[/]");
        AnsiConsole.WriteLine();

        var result = await BioEnrollmentExample.EnrollFingerprintAsync(
            device,
            pin,
            friendlyName,
            onSampleCaptured: (sample, remaining, status) =>
            {
                var statusText = BioEnrollmentExample.FormatSampleStatus(status);
                var color = status == FingerprintSampleStatus.Good ? "green" : "yellow";

                AnsiConsole.MarkupLine(
                    $"  [{color}]Sample {sample}:[/] {Markup.Escape(statusText)} " +
                    $"[grey]({remaining} remaining)[/]");

                if (remaining > 0)
                {
                    AnsiConsole.MarkupLine("[yellow]  Touch the sensor again...[/]");
                }
            },
            cancellationToken);

        if (result.Success)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("Fingerprint enrolled successfully.");
            OutputHelpers.WriteHex("Template ID", result.TemplateId);
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunListAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var result = await AnsiConsole.Status()
            .StartAsync("Enumerating enrollments...", async _ =>
                await BioEnrollmentExample.EnumerateEnrollmentsAsync(device, pin, cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        if (result.Templates.Count == 0)
        {
            OutputHelpers.WriteInfo("No fingerprints enrolled.");
            return;
        }

        OutputHelpers.WriteSuccess($"Found {result.Templates.Count} enrollment(s).");
        AnsiConsole.WriteLine();

        foreach (var template in result.Templates)
        {
            DisplayTemplate(template);
        }
    }

    private static async Task RunRenameAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var templateIdHex = AnsiConsole.Ask<string>("Template ID (hex):");
        byte[] templateId;
        try
        {
            templateId = Convert.FromHexString(templateIdHex);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for template ID.");
            return;
        }

        var newName = AnsiConsole.Ask<string>("New friendly name:");

        var result = await AnsiConsole.Status()
            .StartAsync("Renaming enrollment...", async _ =>
                await BioEnrollmentExample.RenameEnrollmentAsync(
                    device, pin, templateId, newName, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Enrollment renamed successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunRemoveAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var templateIdHex = AnsiConsole.Ask<string>("Template ID (hex):");
        byte[] templateId;
        try
        {
            templateId = Convert.FromHexString(templateIdHex);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for template ID.");
            return;
        }

        if (!OutputHelpers.ConfirmDangerous("permanently remove this fingerprint enrollment"))
        {
            OutputHelpers.WriteInfo("Operation cancelled.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Removing enrollment...", async _ =>
                await BioEnrollmentExample.RemoveEnrollmentAsync(
                    device, pin, templateId, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Enrollment removed successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    /// <summary>
    /// Displays a template info entry.
    /// </summary>
    internal static void DisplayTemplate(TemplateInfo template)
    {
        OutputHelpers.WriteHex("  Template ID", template.TemplateId);
        OutputHelpers.WriteKeyValue("    Name", template.FriendlyName ?? "(unnamed)");

        if (template.SampleCount.HasValue)
        {
            OutputHelpers.WriteKeyValue("    Samples", template.SampleCount.ToString());
        }
    }

    private static async Task<string> PromptForPinAsync(CancellationToken cancellationToken) =>
        await new TextPrompt<string>("PIN:")
            .Secret()
            .ShowAsync(AnsiConsole.Console, cancellationToken);
}
