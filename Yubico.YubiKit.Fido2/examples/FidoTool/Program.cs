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

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

int exitCode;

if (args.Length > 0)
{
    // CLI verb mode: parse and execute command directly
    exitCode = await RunVerbAsync(args, cts.Token);
}
else
{
    // Interactive menu mode
    exitCode = await RunInteractiveAsync(cts.Token);
}

await YubiKeyManager.ShutdownAsync();

return exitCode;

// ---------------------------------------------------------------------------
// CLI verb routing
// ---------------------------------------------------------------------------
static async Task<int> RunVerbAsync(string[] args, CancellationToken cancellationToken)
{
    try
    {
        return args[0].ToLowerInvariant() switch
        {
            "info" => await RunInfoVerbAsync(cancellationToken),
            "pin" => await RunPinVerbAsync(args, cancellationToken),
            "credential" => await RunCredentialVerbAsync(args, cancellationToken),
            "bio" => await RunBioVerbAsync(args, cancellationToken),
            "config" => await RunConfigVerbAsync(args, cancellationToken),
            "reset" => await RunResetVerbAsync(args, cancellationToken),
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
// Interactive menu loop
// ---------------------------------------------------------------------------
static async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        string choice;
        try
        {
            choice = await new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(15)
                .AddChoices(
                [
                    "📋 Authenticator Info",
                    "🔑 PIN Management",
                    "🔐 Create Credential",
                    "✅ Get Assertion",
                    "📂 Credential Management",
                    "🖐️  Bio Enrollment",
                    "⚙️  Authenticator Config",
                    "⚠️  Factory Reset",
                    "❌ Exit"
                ])
                .ShowAsync(AnsiConsole.Console, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (choice == "❌ Exit")
        {
            AnsiConsole.MarkupLine("[grey]Goodbye![/]");
            break;
        }

        try
        {
            switch (choice)
            {
                case "📋 Authenticator Info":
                    await InfoMenu.RunAsync(cancellationToken);
                    break;

                case "🔑 PIN Management":
                    await PinMenu.RunAsync(cancellationToken);
                    break;

                case "🔐 Create Credential":
                    await CredentialMenu.RunMakeCredentialAsync(cancellationToken);
                    break;

                case "✅ Get Assertion":
                    await CredentialMenu.RunGetAssertionAsync(cancellationToken);
                    break;

                case "📂 Credential Management":
                    await CredentialMgmtMenu.RunAsync(cancellationToken);
                    break;

                case "🖐️  Bio Enrollment":
                    await BioMenu.RunAsync(cancellationToken);
                    break;

                case "⚙️  Authenticator Config":
                    await ConfigMenu.RunAsync(cancellationToken);
                    break;

                case "⚠️  Factory Reset":
                    await ResetMenu.RunAsync(cancellationToken);
                    break;

                default:
                    AnsiConsole.MarkupLine($"[yellow]Selected: {choice} - Not yet implemented[/]");
                    break;
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError(ex.Message);
        }

        AnsiConsole.WriteLine();
    }

    return 0;
}

// ---------------------------------------------------------------------------
// Verb stubs - to be implemented by feature files
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

static async Task<int> RunPinVerbAsync(string[] args, CancellationToken cancellationToken)
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
        case "set":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError("Missing --pin flag. Usage: FidoTool pin set --pin <value>");
                return 1;
            }

            var result = await PinManagement.SetPinAsync(selection.Device, pin, cancellationToken);
            if (result.Success)
            {
                OutputHelpers.WriteSuccess("PIN set successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "change":
        {
            var oldPin = ParseFlag(args, "--old");
            var newPin = ParseFlag(args, "--new");

            if (oldPin is null || newPin is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool pin change --old <value> --new <value>");
                return 1;
            }

            var result = await PinManagement.ChangePinAsync(
                selection.Device, oldPin, newPin, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("PIN changed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "retries":
        {
            var pinResult = await PinManagement.GetPinRetriesAsync(
                selection.Device, cancellationToken);

            if (!pinResult.Success)
            {
                OutputHelpers.WriteError(pinResult.ErrorMessage!);
                return 1;
            }

            OutputHelpers.WriteKeyValue("PIN retries remaining", pinResult.Retries.ToString());
            if (pinResult.PowerCycleRequired)
            {
                OutputHelpers.WriteWarning(
                    "Power cycle required. Remove and re-insert the YubiKey before next PIN attempt.");
            }

            var uvResult = await PinManagement.GetUvRetriesAsync(
                selection.Device, cancellationToken);

            if (uvResult.Success)
            {
                OutputHelpers.WriteKeyValue("UV retries remaining", uvResult.Retries.ToString());
                if (uvResult.PowerCycleRequired)
                {
                    OutputHelpers.WriteWarning(
                        "Power cycle required. Remove and re-insert the YubiKey before next UV attempt.");
                }
            }

            return 0;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool pin <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine("  [green]set[/]       Set initial PIN      --pin <value>");
            AnsiConsole.MarkupLine("  [green]change[/]    Change existing PIN   --old <value> --new <value>");
            AnsiConsole.MarkupLine("  [green]retries[/]   Show PIN/UV retries");
            return 0;
    }
}

static string? ParseFlag(string[] args, string flag)
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

static async Task<int> RunCredentialVerbAsync(string[] args, CancellationToken cancellationToken)
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
        case "make":
        {
            var rp = ParseFlag(args, "--rp");
            var user = ParseFlag(args, "--user");

            if (rp is null || user is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool credential make --rp <id> --user <name> [--display <name>] [--pin <value>]");
                return 1;
            }

            var display = ParseFlag(args, "--display");
            var pin = ParseFlag(args, "--pin");

            OutputHelpers.WriteWarning(
                "Using random clientDataHash for testing. In production, this must be the SHA-256 of the WebAuthn clientData JSON.");
            OutputHelpers.WriteTouchPrompt();

            var result = await MakeCredential.CreateAsync(
                selection.Device, rp, user, display, pin, cancellationToken);

            if (!result.Success)
            {
                OutputHelpers.WriteError(result.ErrorMessage!);
                return 1;
            }

            CredentialMenu.DisplayCredentialResult(result);
            return 0;
        }

        case "assert":
        {
            var rp = ParseFlag(args, "--rp");

            if (rp is null)
            {
                OutputHelpers.WriteError(
                    "Missing --rp flag. Usage: FidoTool credential assert --rp <id> [--pin <value>]");
                return 1;
            }

            var pin = ParseFlag(args, "--pin");

            OutputHelpers.WriteWarning(
                "Using random clientDataHash for testing. In production, this must be the SHA-256 of the WebAuthn clientData JSON.");
            OutputHelpers.WriteTouchPrompt();

            var result = await GetAssertion.AssertAsync(
                selection.Device, rp, pin, cancellationToken);

            if (!result.Success)
            {
                OutputHelpers.WriteError(result.ErrorMessage!);
                return 1;
            }

            CredentialMenu.DisplayAssertionResult(result);
            return 0;
        }

        case "list":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError(
                    "Missing --pin flag. Usage: FidoTool credential list --pin <value>");
                return 1;
            }

            var rpResult = await CredentialManagementExample.EnumerateRelyingPartiesAsync(
                selection.Device, pin, cancellationToken);

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
                    selection.Device, pin, rp.RpIdHash, cancellationToken);

                if (credResult.Success)
                {
                    foreach (var cred in credResult.Credentials)
                    {
                        CredentialMgmtMenu.DisplayStoredCredential(cred);
                    }
                }

                AnsiConsole.WriteLine();
            }

            return 0;
        }

        case "delete":
        {
            var pin = ParseFlag(args, "--pin");
            var idHex = ParseFlag(args, "--id");

            if (pin is null || idHex is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool credential delete --id <hex> --pin <value>");
                return 1;
            }

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

            var deleteResult = await CredentialManagementExample.DeleteCredentialAsync(
                selection.Device, pin, credentialId, cancellationToken);

            if (deleteResult.Success)
            {
                OutputHelpers.WriteSuccess("Credential deleted successfully.");
                return 0;
            }

            OutputHelpers.WriteError(deleteResult.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool credential <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]make[/]      Create credential   --rp <id> --user <name> [--display <name>] [--pin <value>]");
            AnsiConsole.MarkupLine(
                "  [green]assert[/]    Get assertion        --rp <id> [--pin <value>]");
            AnsiConsole.MarkupLine(
                "  [green]list[/]      List credentials     --pin <value>");
            AnsiConsole.MarkupLine(
                "  [green]delete[/]    Delete credential    --id <hex> --pin <value>");
            return 0;
    }
}

static async Task<int> RunBioVerbAsync(string[] args, CancellationToken cancellationToken)
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
        case "enroll":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError(
                    "Missing --pin flag. Usage: FidoTool bio enroll --pin <value> [--name <friendly>]");
                return 1;
            }

            var friendlyName = ParseFlag(args, "--name");

            AnsiConsole.MarkupLine("[yellow]Place your finger on the sensor...[/]");

            var result = await BioEnrollmentExample.EnrollFingerprintAsync(
                selection.Device,
                pin,
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

        case "list":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError(
                    "Missing --pin flag. Usage: FidoTool bio list --pin <value>");
                return 1;
            }

            var result = await BioEnrollmentExample.EnumerateEnrollmentsAsync(
                selection.Device, pin, cancellationToken);

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
                BioMenu.DisplayTemplate(template);
            }

            return 0;
        }

        case "rename":
        {
            var pin = ParseFlag(args, "--pin");
            var idHex = ParseFlag(args, "--id");
            var name = ParseFlag(args, "--name");

            if (pin is null || idHex is null || name is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool bio rename --id <hex> --name <friendly> --pin <value>");
                return 1;
            }

            byte[] templateId;
            try
            {
                templateId = Convert.FromHexString(idHex);
            }
            catch (FormatException)
            {
                OutputHelpers.WriteError("Invalid hex string for template ID.");
                return 1;
            }

            var result = await BioEnrollmentExample.RenameEnrollmentAsync(
                selection.Device, pin, templateId, name, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Enrollment renamed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "remove":
        {
            var pin = ParseFlag(args, "--pin");
            var idHex = ParseFlag(args, "--id");

            if (pin is null || idHex is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool bio remove --id <hex> --pin <value>");
                return 1;
            }

            byte[] templateId;
            try
            {
                templateId = Convert.FromHexString(idHex);
            }
            catch (FormatException)
            {
                OutputHelpers.WriteError("Invalid hex string for template ID.");
                return 1;
            }

            var result = await BioEnrollmentExample.RemoveEnrollmentAsync(
                selection.Device, pin, templateId, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Enrollment removed successfully.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        default:
            AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool bio <subcommand> [options]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Subcommands:[/]");
            AnsiConsole.MarkupLine(
                "  [green]enroll[/]    Enroll fingerprint   --pin <value> [--name <friendly>]");
            AnsiConsole.MarkupLine(
                "  [green]list[/]      List enrollments     --pin <value>");
            AnsiConsole.MarkupLine(
                "  [green]rename[/]    Rename enrollment    --id <hex> --name <friendly> --pin <value>");
            AnsiConsole.MarkupLine(
                "  [green]remove[/]    Remove enrollment    --id <hex> --pin <value>");
            return 0;
    }
}

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
        case "enterprise":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError(
                    "Missing --pin flag. Usage: FidoTool config enterprise --pin <value>");
                return 1;
            }

            var result = await ConfigManagement.EnableEnterpriseAttestationAsync(
                selection.Device, pin, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Enterprise attestation enabled.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "always-uv":
        {
            var pin = ParseFlag(args, "--pin");
            if (pin is null)
            {
                OutputHelpers.WriteError(
                    "Missing --pin flag. Usage: FidoTool config always-uv --pin <value>");
                return 1;
            }

            var result = await ConfigManagement.ToggleAlwaysUvAsync(
                selection.Device, pin, cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Always-UV setting toggled.");
                return 0;
            }

            OutputHelpers.WriteError(result.ErrorMessage!);
            return 1;
        }

        case "min-pin":
        {
            var pin = ParseFlag(args, "--pin");
            var lengthStr = ParseFlag(args, "--length");

            if (pin is null || lengthStr is null)
            {
                OutputHelpers.WriteError(
                    "Missing flags. Usage: FidoTool config min-pin --length <n> --pin <value>");
                return 1;
            }

            if (!int.TryParse(lengthStr, out var length))
            {
                OutputHelpers.WriteError("Invalid value for --length. Must be an integer.");
                return 1;
            }

            var result = await ConfigManagement.SetMinPinLengthAsync(
                selection.Device, pin, length, cancellationToken: cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess($"Minimum PIN length set to {length}.");
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
                "  [green]enterprise[/]   Enable enterprise attestation   --pin <value>");
            AnsiConsole.MarkupLine(
                "  [green]always-uv[/]    Toggle always-UV                --pin <value>");
            AnsiConsole.MarkupLine(
                "  [green]min-pin[/]      Set minimum PIN length          --length <n> --pin <value>");
            return 0;
    }
}

static async Task<int> RunResetVerbAsync(string[] args, CancellationToken cancellationToken)
{
    var force = args.Any(a => string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase));

    if (!force)
    {
        if (!OutputHelpers.ConfirmDestructive("factory reset the FIDO2 application"))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return 0;
        }
    }

    AnsiConsole.MarkupLine("[yellow]To perform the reset:[/]");
    AnsiConsole.MarkupLine("[yellow]  1. Remove your YubiKey[/]");
    AnsiConsole.MarkupLine("[yellow]  2. Re-insert your YubiKey[/]");
    AnsiConsole.MarkupLine("[yellow]  3. Touch your YubiKey within 5 seconds[/]");
    AnsiConsole.WriteLine();

    if (!force)
    {
        AnsiConsole.MarkupLine("[grey]Press Enter after re-inserting your YubiKey.[/]");
        Console.ReadLine();
    }

    var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
    if (selection is null)
    {
        OutputHelpers.WriteError("No YubiKey found.");
        return 1;
    }

    OutputHelpers.WriteTouchPrompt();

    var result = await ResetAuthenticator.ResetAsync(selection.Device, cancellationToken);

    if (result.Success)
    {
        OutputHelpers.WriteSuccess("FIDO2 application has been factory reset.");
        return 0;
    }

    OutputHelpers.WriteError(result.ErrorMessage!);
    return 1;
}

static int ShowHelp()
{
    AnsiConsole.MarkupLine("[bold]Usage:[/] FidoTool [command] [subcommand] [options]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Commands:[/]");
    AnsiConsole.MarkupLine("  [green]info[/]          Display authenticator capabilities");
    AnsiConsole.MarkupLine("  [green]pin[/]           PIN management (set, change, retries)");
    AnsiConsole.MarkupLine("  [green]credential[/]    Credential operations (make, assert, list, delete)");
    AnsiConsole.MarkupLine("  [green]bio[/]           Biometric enrollment (enroll, list, rename, remove)");
    AnsiConsole.MarkupLine("  [green]config[/]        Authenticator config (enterprise, always-uv, min-pin)");
    AnsiConsole.MarkupLine("  [green]reset[/]         Factory reset (destructive)");
    AnsiConsole.MarkupLine("  [green]help[/]          Show this help message");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Run without arguments for interactive mode.[/]");
    return 0;
}

static int ShowUnknownCommand(string command)
{
    OutputHelpers.WriteError($"Unknown command: {command}");
    AnsiConsole.MarkupLine("[grey]Run 'FidoTool help' for available commands.[/]");
    return 1;
}