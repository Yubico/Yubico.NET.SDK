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

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.CredentialManagement;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;
using Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Prompts;
using Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Menus;

/// <summary>
/// Interactive menu for credential management: list RPs, list credentials, delete, update user.
/// </summary>
public static class CredentialMgmtMenu
{
    /// <summary>
    /// Runs the credential management sub-menu.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Credential Management");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Check feature support
        var supported = await CredentialManagementExample.IsSupported(selection.Device, cancellationToken);
        if (!supported)
        {
            OutputHelpers.WriteError(
                "Credential management is not supported on this device (requires firmware 5.2+).");
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "View credential metadata",
                    "List all credentials",
                    "Delete a credential",
                    "Update user info",
                    "Back"
                ]));

        switch (choice)
        {
            case "View credential metadata":
                await RunMetadataAsync(selection.Device, cancellationToken);
                break;
            case "List all credentials":
                await RunListAllAsync(selection.Device, cancellationToken);
                break;
            case "Delete a credential":
                await RunDeleteAsync(selection.Device, cancellationToken);
                break;
            case "Update user info":
                await RunUpdateUserAsync(selection.Device, cancellationToken);
                break;
        }
    }

    private static async Task RunMetadataAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var result = await AnsiConsole.Status()
            .StartAsync("Querying credential metadata...", async _ =>
                await CredentialManagementExample.GetMetadataAsync(device, pin, cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        var metadata = result.Metadata!;
        OutputHelpers.WriteSuccess("Credential metadata retrieved.");
        OutputHelpers.WriteKeyValue("Existing credentials",
            metadata.ExistingResidentCredentialsCount.ToString());
        OutputHelpers.WriteKeyValue("Remaining slots",
            metadata.MaxPossibleRemainingResidentCredentialsCount.ToString());
    }

    private static async Task RunListAllAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var rpResult = await AnsiConsole.Status()
            .StartAsync("Enumerating relying parties...", async _ =>
                await CredentialManagementExample.EnumerateRelyingPartiesAsync(
                    device, pin, cancellationToken));

        if (!rpResult.Success)
        {
            OutputHelpers.WriteError(rpResult.ErrorMessage!);
            return;
        }

        if (rpResult.RelyingParties.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this authenticator.");
            return;
        }

        OutputHelpers.WriteSuccess($"Found {rpResult.RelyingParties.Count} relying party(ies).");
        AnsiConsole.WriteLine();

        foreach (var rp in rpResult.RelyingParties)
        {
            AnsiConsole.MarkupLine($"  [green bold]{Markup.Escape(rp.RelyingParty.Id)}[/]");

            if (!string.IsNullOrEmpty(rp.RelyingParty.Name))
            {
                OutputHelpers.WriteKeyValue("    Name", rp.RelyingParty.Name);
            }

            OutputHelpers.WriteHex("    RP ID Hash", rp.RpIdHash);

            // Enumerate credentials for this RP
            var credResult = await CredentialManagementExample.EnumerateCredentialsAsync(
                device, pin, rp.RpIdHash, cancellationToken);

            if (!credResult.Success)
            {
                OutputHelpers.WriteError($"    Failed to list credentials: {credResult.ErrorMessage}");
                continue;
            }

            foreach (var cred in credResult.Credentials)
            {
                DisplayStoredCredential(cred);
            }

            AnsiConsole.WriteLine();
        }
    }

    private static async Task RunDeleteAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var credIdHex = AnsiConsole.Ask<string>("Credential ID (hex):");
        byte[] credentialId;
        try
        {
            credentialId = Convert.FromHexString(credIdHex);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for credential ID.");
            return;
        }

        if (!OutputHelpers.ConfirmDangerous("permanently delete this credential"))
        {
            OutputHelpers.WriteInfo("Operation cancelled.");
            return;
        }

        var result = await AnsiConsole.Status()
            .StartAsync("Deleting credential...", async _ =>
                await CredentialManagementExample.DeleteCredentialAsync(
                    device, pin, credentialId, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Credential deleted successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    private static async Task RunUpdateUserAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var pin = await PromptForPinAsync(cancellationToken);

        var credIdHex = AnsiConsole.Ask<string>("Credential ID (hex):");
        byte[] credentialId;
        try
        {
            credentialId = Convert.FromHexString(credIdHex);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for credential ID.");
            return;
        }

        var userIdHex = AnsiConsole.Ask<string>("User ID (hex):");
        byte[] userId;
        try
        {
            userId = Convert.FromHexString(userIdHex);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for user ID.");
            return;
        }

        var userName = AnsiConsole.Ask<string>("New user name:");
        var displayName = AnsiConsole.Ask("New display name:", userName);

        var result = await AnsiConsole.Status()
            .StartAsync("Updating user information...", async _ =>
                await CredentialManagementExample.UpdateUserInfoAsync(
                    device, pin, credentialId, userName, displayName, userId, cancellationToken));

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("User information updated successfully.");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
        }
    }

    /// <summary>
    /// Displays details of a stored credential (used by both interactive and CLI modes).
    /// </summary>
    internal static void DisplayStoredCredential(StoredCredentialInfo cred)
    {
        OutputHelpers.WriteHex("    Credential ID", cred.CredentialId.Id);
        OutputHelpers.WriteKeyValue("      User Name", cred.User.Name);
        OutputHelpers.WriteKeyValue("      Display Name", cred.User.DisplayName);
        OutputHelpers.WriteHex("      User ID", cred.User.Id);

        if (cred.CredProtectPolicy.HasValue)
        {
            var protectLevel = cred.CredProtectPolicy.Value switch
            {
                1 => "userVerificationOptional",
                2 => "userVerificationOptionalWithCredentialIDList",
                3 => "userVerificationRequired",
                _ => $"unknown ({cred.CredProtectPolicy.Value})"
            };
            OutputHelpers.WriteKeyValue("      Cred Protect", protectLevel);
        }
    }

    private static async Task<string> PromptForPinAsync(CancellationToken cancellationToken) =>
        await new TextPrompt<string>("PIN:")
            .Secret()
            .ShowAsync(AnsiConsole.Console, cancellationToken);
}
