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
/// Interactive menu for credential operations: MakeCredential and GetAssertion.
/// </summary>
public static class CredentialMenu
{
    /// <summary>
    /// Runs the interactive MakeCredential flow.
    /// </summary>
    public static async Task RunMakeCredentialAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Create Credential");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            OutputHelpers.WriteError("No YubiKey found.");
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var rpId = AnsiConsole.Ask("Relying Party ID:", "example.com");
        var userName = AnsiConsole.Ask("User Name:", "test@example.com");
        var displayName = AnsiConsole.Ask("Display Name:", userName);

        var pin = AnsiConsole.Confirm("Provide a PIN for user verification?", defaultValue: true)
            ? new TextPrompt<string>("PIN:").Secret().ShowAsync(AnsiConsole.Console, cancellationToken)
                .GetAwaiter().GetResult()
            : null;

        OutputHelpers.WriteWarning(
            "Using random clientDataHash for testing. In production, this must be the SHA-256 of the WebAuthn clientData JSON.");
        AnsiConsole.WriteLine();

        OutputHelpers.WriteTouchPrompt();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Creating credential...", async _ =>
                await MakeCredential.CreateAsync(
                    selection.Device,
                    rpId,
                    userName,
                    displayName,
                    pin,
                    cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        DisplayCredentialResult(result);
    }

    /// <summary>
    /// Runs the interactive GetAssertion flow.
    /// </summary>
    public static async Task RunGetAssertionAsync(CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Get Assertion");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            OutputHelpers.WriteError("No YubiKey found.");
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var rpId = AnsiConsole.Ask("Relying Party ID:", "example.com");

        var pin = AnsiConsole.Confirm("Provide a PIN for user verification?", defaultValue: true)
            ? new TextPrompt<string>("PIN:").Secret().ShowAsync(AnsiConsole.Console, cancellationToken)
                .GetAwaiter().GetResult()
            : null;

        OutputHelpers.WriteWarning(
            "Using random clientDataHash for testing. In production, this must be the SHA-256 of the WebAuthn clientData JSON.");
        AnsiConsole.WriteLine();

        OutputHelpers.WriteTouchPrompt();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Getting assertion...", async _ =>
                await GetAssertion.AssertAsync(
                    selection.Device,
                    rpId,
                    pin,
                    cancellationToken));

        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage!);
            return;
        }

        DisplayAssertionResult(result);
    }

    /// <summary>
    /// Displays MakeCredential result details.
    /// </summary>
    internal static void DisplayCredentialResult(MakeCredential.MakeCredentialResult result)
    {
        var credential = result.Credential!;

        OutputHelpers.WriteSuccess("Credential created successfully.");
        AnsiConsole.WriteLine();

        OutputHelpers.WriteKeyValue("Attestation Format", credential.Format);
        OutputHelpers.WriteHex("Credential ID", credential.GetCredentialId());
        OutputHelpers.WriteHex("AAGUID", credential.GetAaguid().ToByteArray());

        var authData = credential.AuthenticatorData;
        OutputHelpers.WriteKeyValue("Sign Count", authData.SignCount.ToString());
        OutputHelpers.WriteBoolValue("User Present", authData.UserPresent);
        OutputHelpers.WriteBoolValue("User Verified", authData.UserVerified);

        if (credential.EnterpriseAttestation.HasValue)
        {
            OutputHelpers.WriteBoolValue("Enterprise Attestation", credential.EnterpriseAttestation.Value);
        }

        OutputHelpers.WriteHex("Client Data Hash (random)", result.ClientDataHash);
    }

    /// <summary>
    /// Displays GetAssertion result details.
    /// </summary>
    internal static void DisplayAssertionResult(GetAssertion.GetAssertionResult result)
    {
        var assertion = result.Assertion!;

        OutputHelpers.WriteSuccess("Assertion received successfully.");
        AnsiConsole.WriteLine();

        if (assertion.Credential is not null)
        {
            OutputHelpers.WriteHex("Credential ID", assertion.Credential.Id);
        }

        if (assertion.User is not null)
        {
            OutputHelpers.WriteKeyValue("User Name", assertion.User.Name);
            OutputHelpers.WriteKeyValue("Display Name", assertion.User.DisplayName);
            OutputHelpers.WriteHex("User ID", assertion.User.Id);
        }

        var authData = assertion.AuthenticatorData;
        OutputHelpers.WriteKeyValue("Sign Count", authData.SignCount.ToString());
        OutputHelpers.WriteBoolValue("User Present", authData.UserPresent);
        OutputHelpers.WriteBoolValue("User Verified", authData.UserVerified);

        OutputHelpers.WriteKeyValue("Signature Length", $"{assertion.Signature.Length} bytes");

        if (result.TotalCredentials > 1)
        {
            OutputHelpers.WriteKeyValue("Total Matching Credentials", result.TotalCredentials.ToString());
        }
    }
}
