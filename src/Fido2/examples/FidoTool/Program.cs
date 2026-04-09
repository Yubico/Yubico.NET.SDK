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
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

// Application banner
AnsiConsole.Write(
    new FigletText("FIDO Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiKey FIDO2 Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

using var cts = CommandHelper.CreateConsoleCts();

int exitCode;

if (args.Length > 0)
{
    // CLI verb mode: parse and execute command directly
    exitCode = await RunVerbAsync(args, cts.Token);
}
else
{
    // Interactive menu mode
    exitCode = await InteractiveMenuBuilder.Create("What would you like to do?")
        .AddItem("Authenticator Info", ct => InfoMenu.RunAsync(ct))
        .AddItem("Access (PIN Management)", ct => AccessMenu.RunAsync(ct))
        .AddItem("Config (Authenticator Settings)", ct => ConfigMenu.RunAsync(ct))
        .AddItem("Credentials (Discoverable Credentials)", ct => CredentialsMenu.RunAsync(ct))
        .AddItem("Fingerprints (Bio Enrollment)", ct => FingerprintsMenu.RunAsync(ct))
        .AddItem("Create Credential (MakeCredential)", ct => CredentialMenu.RunMakeCredentialAsync(ct))
        .AddItem("Get Assertion", ct => CredentialMenu.RunGetAssertionAsync(ct))
        .AddItem("Factory Reset", ct => ResetMenu.RunAsync(ct))
        .RunAsync(cts.Token);
}

await YubiKeyManager.ShutdownAsync();

return exitCode;

// ---------------------------------------------------------------------------
// CLI verb routing — mirrors ykman fido command tree
// ---------------------------------------------------------------------------
static async Task<int> RunVerbAsync(string[] args, CancellationToken cancellationToken)
{
    try
    {
        return args[0].ToLowerInvariant() switch
        {
            "info" => await RunInfoVerbAsync(cancellationToken),
            "reset" => await RunResetVerbAsync(args, cancellationToken),
            "access" => await RunAccessVerbAsync(args, cancellationToken),
            "config" => await RunConfigVerbAsync(args, cancellationToken),
            "credentials" => await RunCredentialsVerbAsync(args, cancellationToken),
            "fingerprints" => await RunFingerprintsVerbAsync(args, cancellationToken),
            "help" or "--help" or "-h" => ShowHelp(),
            _ => ShowUnknownCommand(args[0])
        };
    }
    catch (OperationCanceledException)
    {
        return 1;
    }
    catch (Exception ex)
    {
        OutputHelpers.WriteError(ex.Message);
        return 1;
    }
}

// ---------------------------------------------------------------------------
// fido info
// ---------------------------------------------------------------------------
static async Task<int> RunInfoVerbAsync(CancellationToken cancellationToken)
{
    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteActiveDevice(selection.DisplayName);

    var result = await GetAuthenticatorInfo.QueryAsync(selection.Device, cancellationToken);

    if (!result.Success)
    {
        OutputHelpers.WriteError(result.ErrorMessage!);
        return 1;
    }

    InfoMenu.DisplayAuthenticatorInfo(result.Info!);
    return 0;
}

// ---------------------------------------------------------------------------
// fido reset [-f]
// ---------------------------------------------------------------------------
static async Task<int> RunResetVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var force = HasFlag(args, "-f") || HasFlag(args, "--force");

    if (!force)
    {
        if (!OutputHelpers.ConfirmDestructive("factory reset the FIDO2 application"))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return 0;
        }
    }

    if (!force)
    {
        // Query preflight info from the currently connected device
        var preSelection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (preSelection is null)
        {
            OutputHelpers.WriteError("No YubiKey found.");
            return 1;
        }

        var preflight = await ResetAuthenticator.GetPreflightInfoAsync(
            preSelection.Device, cancellationToken);

        var touchInstruction = preflight?.LongTouchForReset == true
            ? "Press and hold for 10 seconds after re-inserting"
            : "Touch your YubiKey after re-inserting";

        AnsiConsole.MarkupLine("[yellow]To perform the reset:[/]");
        AnsiConsole.MarkupLine("[yellow]  1. Remove your YubiKey[/]");
        AnsiConsole.MarkupLine("[yellow]  2. Re-insert your YubiKey[/]");
        AnsiConsole.MarkupLine($"[yellow]  3. {touchInstruction}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]Remove your YubiKey now...[/]");
        AnsiConsole.MarkupLine("[grey]Press Enter after re-inserting your YubiKey.[/]");
        Console.ReadLine();
    }

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    // Query the (reinserted) device for accurate touch message
    var reinsertedPreflight = await ResetAuthenticator.GetPreflightInfoAsync(
        selection.Device, cancellationToken);

    var touchMsg = reinsertedPreflight?.TouchMessage ?? "Touch your YubiKey to confirm.";
    AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(touchMsg)}[/]");

    var result = await ResetAuthenticator.ResetAsync(selection.Device, cancellationToken);

    if (result.Success)
    {
        OutputHelpers.WriteSuccess("FIDO2 application has been factory reset.");
        return 0;
    }

    OutputHelpers.WriteError(result.ErrorMessage!);
    return 1;
}

// ---------------------------------------------------------------------------
// fido access change-pin [--pin PIN] [--new-pin PIN]
// fido access verify-pin [--pin PIN]
// ---------------------------------------------------------------------------
static async Task<int> RunAccessVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var subVerb = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteActiveDevice(selection.DisplayName);

    switch (subVerb)
    {
        case "set-pin":
        {
            using var newPinOwner = FidoPinHelper.GetNewPin(ParseOption(args, "--new-pin"));
            if (newPinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var result = await PinManagement.SetPinAsync(selection.Device, newPinOwner.Memory, cancellationToken);
            if (result.Success) { OutputHelpers.WriteSuccess("PIN set successfully."); return 0; }
            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "change-pin":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"), "Enter current PIN: ");
            if (pinOwner is null) { OutputHelpers.WriteError("Current PIN required."); return 1; }

            using var newPinOwner = FidoPinHelper.GetNewPin(ParseOption(args, "--new-pin"));
            if (newPinOwner is null) { OutputHelpers.WriteError("New PIN required."); return 1; }

            var result = await PinManagement.ChangePinAsync(
                selection.Device, pinOwner.Memory, newPinOwner.Memory, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("PIN changed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "verify-pin":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var result = await PinManagement.VerifyPinAsync(
                selection.Device, pinOwner.Memory, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("PIN is correct.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool access <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]change-pin[/]    Change the PIN    [--pin PIN] [--new-pin PIN]");
            AnsiConsole.MarkupLine(
                "  [green]verify-pin[/]    Verify the PIN    [--pin PIN]");
            return 0;
    }
}

// ---------------------------------------------------------------------------
// fido config toggle-always-uv [--pin PIN]
// fido config enable-ep-attestation [--pin PIN]
// ---------------------------------------------------------------------------
static async Task<int> RunConfigVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var subVerb = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteActiveDevice(selection.DisplayName);

    switch (subVerb)
    {
        case "toggle-always-uv":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var result = await ConfigManagement.ToggleAlwaysUvAsync(
                selection.Device, pinOwner.Memory, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Always-UV setting toggled.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "enable-ep-attestation":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var result = await ConfigManagement.EnableEnterpriseAttestationAsync(
                selection.Device, pinOwner.Memory, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Enterprise attestation enabled.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool config <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]toggle-always-uv[/]        Toggle always-UV    [--pin PIN]");
            AnsiConsole.MarkupLine(
                "  [green]enable-ep-attestation[/]    Enable enterprise attestation    [--pin PIN]");
            return 0;
    }
}

// ---------------------------------------------------------------------------
// fido credentials list [--pin PIN]
// fido credentials delete CREDENTIAL_ID [--pin PIN] [-f]
// ---------------------------------------------------------------------------
static async Task<int> RunCredentialsVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var subVerb = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteActiveDevice(selection.DisplayName);

    switch (subVerb)
    {
        case "list":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var rpResult = await CredentialManagementExample.EnumerateRelyingPartiesAsync(
                selection.Device, pinOwner.Memory, cancellationToken);

            if (!rpResult.Success)
            {
                OutputHelpers.WriteError(rpResult.ErrorMessage!);
                return 1;
            }

            if (rpResult.RelyingParties.Count == 0)
            {
                OutputHelpers.WriteInfo("No credentials stored on this authenticator.");
                return 0;
            }

            OutputHelpers.WriteSuccess($"Found {rpResult.RelyingParties.Count} relying party(ies).");
            AnsiConsole.WriteLine();

            foreach (var rp in rpResult.RelyingParties)
            {
                AnsiConsole.MarkupLine($"  [green bold]{Markup.Escape(rp.RelyingParty.Id)}[/]");
                OutputHelpers.WriteHex("    RP ID Hash", rp.RpIdHash);

                var credResult = await CredentialManagementExample.EnumerateCredentialsAsync(
                    selection.Device, pinOwner.Memory, rp.RpIdHash, cancellationToken);

                if (credResult.Success)
                {
                    foreach (var cred in credResult.Credentials)
                    {
                        CredentialsMenu.DisplayStoredCredential(cred);
                    }
                }

                AnsiConsole.WriteLine();
            }

            return 0;
        }

        case "delete":
        {
            // Positional argument: CREDENTIAL_ID (hex), expected at args[2]
            if (args.Length < 3 || args[2].StartsWith("--") || args[2].StartsWith("-"))
            {
                OutputHelpers.WriteError(
                    "Missing credential ID. Usage: FidoTool credentials delete CREDENTIAL_ID [--pin PIN] [-f]");
                return 1;
            }

            var idHex = args[2];
            byte[] credentialId;
            try
            {
                credentialId = Convert.FromHexString(idHex);
            }
            catch (FormatException)
            {
                OutputHelpers.WriteError("Invalid hex string for credential ID.");
                return 1;
            }

            var force = HasFlag(args, "-f") || HasFlag(args, "--force");
            if (!force)
            {
                if (!OutputHelpers.ConfirmDangerous("permanently delete this credential"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return 0;
                }
            }

            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null) { OutputHelpers.WriteError("PIN required."); return 1; }

            var deleteResult = await CredentialManagementExample.DeleteCredentialAsync(
                selection.Device, pinOwner.Memory, credentialId, cancellationToken);

            if (deleteResult.Success)
            {
                OutputHelpers.WriteSuccess("Credential deleted successfully.");
                return 0;
            }

            OutputHelpers.WriteError(deleteResult.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool credentials <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]list[/]      List discoverable credentials    [--pin PIN]");
            AnsiConsole.MarkupLine(
                "  [green]delete[/]    Delete a credential              CREDENTIAL_ID [--pin PIN] [-f]");
            return 0;
    }
}

// ---------------------------------------------------------------------------
// fido fingerprints list [--pin PIN]
// fido fingerprints add NAME [--pin PIN]
// fido fingerprints delete FINGERPRINT_ID [--pin PIN] [-f]
// fido fingerprints rename FINGERPRINT_ID NAME [--pin PIN]
// ---------------------------------------------------------------------------
static async Task<int> RunFingerprintsVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var subVerb = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteActiveDevice(selection.DisplayName);

    switch (subVerb)
    {
        case "list":
        {
            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null)
            {
                OutputHelpers.WriteError("PIN is required.");
                return 1;
            }

            var result = await BioEnrollmentExample.EnumerateEnrollmentsAsync(
                selection.Device, pinOwner.Memory, cancellationToken);

            if (!result.Success)
            {
                OutputHelpers.WriteError(result.ErrorMessage!);
                return 1;
            }

            if (result.Templates.Count == 0)
            {
                OutputHelpers.WriteInfo("No fingerprints enrolled.");
                return 0;
            }

            OutputHelpers.WriteSuccess($"Found {result.Templates.Count} enrollment(s).");
            AnsiConsole.WriteLine();

            foreach (var template in result.Templates)
            {
                FingerprintsMenu.DisplayTemplate(template);
            }

            return 0;
        }

        case "add":
        {
            // Positional argument: NAME, expected at args[2]
            string? friendlyName = null;
            if (args.Length > 2 && !args[2].StartsWith("--") && !args[2].StartsWith("-"))
            {
                friendlyName = args[2];
            }

            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null)
            {
                OutputHelpers.WriteError("PIN is required.");
                return 1;
            }

            AnsiConsole.MarkupLine("[yellow]Touch your YubiKey now...[/]");

            var result = await BioEnrollmentExample.EnrollFingerprintAsync(
                selection.Device,
                pinOwner.Memory,
                friendlyName,
                onSampleCaptured: (sample, remaining, status) =>
                {
                    var statusText = BioEnrollmentExample.FormatSampleStatus(status);
                    AnsiConsole.MarkupLine($"  Sample {sample}: {Markup.Escape(statusText)} ({remaining} remaining)");

                    if (remaining > 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]  Touch the sensor again...[/]");
                    }
                },
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Fingerprint enrolled successfully.");
                OutputHelpers.WriteHex("Template ID", result.TemplateId);
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "delete":
        {
            // Positional argument: FINGERPRINT_ID (hex), expected at args[2]
            if (args.Length < 3 || args[2].StartsWith("--") || args[2].StartsWith("-"))
            {
                OutputHelpers.WriteError(
                    "Missing fingerprint ID. Usage: FidoTool fingerprints delete FINGERPRINT_ID [--pin PIN] [-f]");
                return 1;
            }

            var idHex = args[2];
            byte[] templateId;
            try
            {
                templateId = Convert.FromHexString(idHex);
            }
            catch (FormatException)
            {
                OutputHelpers.WriteError("Invalid hex string for fingerprint ID.");
                return 1;
            }

            var force = HasFlag(args, "-f") || HasFlag(args, "--force");
            if (!force)
            {
                if (!OutputHelpers.ConfirmDangerous("permanently delete this fingerprint enrollment"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return 0;
                }
            }

            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null)
            {
                OutputHelpers.WriteError("PIN is required.");
                return 1;
            }

            var result = await BioEnrollmentExample.RemoveEnrollmentAsync(
                selection.Device, pinOwner.Memory, templateId, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Fingerprint enrollment removed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "rename":
        {
            // Positional arguments: FINGERPRINT_ID NAME, expected at args[2] and args[3]
            if (args.Length < 4
                || args[2].StartsWith("--") || args[2].StartsWith("-")
                || args[3].StartsWith("--") || args[3].StartsWith("-"))
            {
                OutputHelpers.WriteError(
                    "Missing arguments. Usage: FidoTool fingerprints rename FINGERPRINT_ID NAME [--pin PIN]");
                return 1;
            }

            var idHex = args[2];
            var newName = args[3];

            byte[] templateId;
            try
            {
                templateId = Convert.FromHexString(idHex);
            }
            catch (FormatException)
            {
                OutputHelpers.WriteError("Invalid hex string for fingerprint ID.");
                return 1;
            }

            using var pinOwner = FidoPinHelper.GetPin(ParseOption(args, "--pin"));
            if (pinOwner is null)
            {
                OutputHelpers.WriteError("PIN is required.");
                return 1;
            }

            var result = await BioEnrollmentExample.RenameEnrollmentAsync(
                selection.Device, pinOwner.Memory, templateId, newName, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Fingerprint enrollment renamed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool fingerprints <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]list[/]      List enrolled fingerprints           [--pin PIN]");
            AnsiConsole.MarkupLine(
                "  [green]add[/]       Enroll new fingerprint               NAME [--pin PIN]");
            AnsiConsole.MarkupLine(
                "  [green]delete[/]    Delete a fingerprint enrollment      FINGERPRINT_ID [--pin PIN] [-f]");
            AnsiConsole.MarkupLine(
                "  [green]rename[/]    Rename a fingerprint enrollment      FINGERPRINT_ID NAME [--pin PIN]");
            return 0;
    }
}

// ---------------------------------------------------------------------------
// Argument parsing helpers
// ---------------------------------------------------------------------------
static string? ParseOption(string[] args, string flag)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasFlag(string[] args, string flag) =>
    args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

// ---------------------------------------------------------------------------
// Help
// ---------------------------------------------------------------------------
static int ShowHelp()
{
    AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool <command> [subcommand] [options]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Commands:[/]");
    AnsiConsole.MarkupLine("  [green]info[/]            Display general status of the FIDO2 application");
    AnsiConsole.MarkupLine("  [green]reset[/]           Reset all FIDO applications [-f]");
    AnsiConsole.MarkupLine("  [green]access[/]          Manage PIN (change-pin, verify-pin)");
    AnsiConsole.MarkupLine("  [green]config[/]          Configure authenticator settings (toggle-always-uv, enable-ep-attestation)");
    AnsiConsole.MarkupLine("  [green]credentials[/]     Manage discoverable credentials (list, delete)");
    AnsiConsole.MarkupLine("  [green]fingerprints[/]    Manage fingerprint enrollments (list, add, delete, rename)");
    AnsiConsole.MarkupLine("  [green]help[/]            Show this help message");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Run without arguments for interactive mode.[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Options:[/]");
    AnsiConsole.MarkupLine("  [green]--pin PIN[/]       Provide PIN (prompted if omitted in interactive mode)");
    AnsiConsole.MarkupLine("  [green]-f, --force[/]     Skip confirmation prompts");
    return 0;
}

static int ShowUnknownCommand(string command)
{
    OutputHelpers.WriteError($"Unknown command: {command}");
    AnsiConsole.MarkupLine("[grey]Run 'FidoTool help' for available commands.[/]");
    return 1;
}
