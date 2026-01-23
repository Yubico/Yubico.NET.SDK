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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Piv.Examples.PivTool.Shared;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Handles PIN, PUK, and management key operations.
/// </summary>
public static class PinManagementFeature
{
    private static readonly byte[] DefaultManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    /// <summary>
    /// Runs the PIN management feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("PIN Management");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);

        // Show metadata if available
        await DisplayMetadataAsync(session, cancellationToken);

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("PIN Management Options:")
                    .PageSize(12)
                    .AddChoices(
                    [
                        "🔍 Verify PIN",
                        "🔄 Change PIN",
                        "🔄 Change PUK",
                        "🔓 Unblock PIN (using PUK)",
                        "⚙️  Set PIN/PUK Retry Limits",
                        "📊 View PIN/PUK Metadata",
                        "🔑 Change Management Key",
                        "↩️  Back to Main Menu"
                    ]));

            if (choice == "↩️  Back to Main Menu")
            {
                break;
            }

            try
            {
                switch (choice)
                {
                    case "🔍 Verify PIN":
                        await VerifyPinAsync(session, cancellationToken);
                        break;

                    case "🔄 Change PIN":
                        await ChangePinAsync(session, cancellationToken);
                        break;

                    case "🔄 Change PUK":
                        await ChangePukAsync(session, cancellationToken);
                        break;

                    case "🔓 Unblock PIN (using PUK)":
                        await UnblockPinAsync(session, cancellationToken);
                        break;

                    case "⚙️  Set PIN/PUK Retry Limits":
                        await SetRetryLimitsAsync(session, cancellationToken);
                        break;

                    case "📊 View PIN/PUK Metadata":
                        await DisplayMetadataAsync(session, cancellationToken);
                        break;

                    case "🔑 Change Management Key":
                        await ChangeManagementKeyAsync(session, cancellationToken);
                        break;
                }
            }
            catch (InvalidPinException ex)
            {
                HandlePinError(ex);
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError(ex.Message);
            }

            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Verifies the PIN.
    /// </summary>
    private static async Task VerifyPinAsync(IPivSession session, CancellationToken cancellationToken)
    {
        var pin = PinPrompt.GetPin();
        if (pin is null)
        {
            return;
        }

        try
        {
            await session.VerifyPinAsync(pin, cancellationToken);
            OutputHelpers.WriteSuccess("PIN verified successfully.");

            var remaining = await session.GetPinAttemptsAsync(cancellationToken);
            OutputHelpers.WriteRetryCount("PIN", remaining);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    private static async Task ChangePinAsync(IPivSession session, CancellationToken cancellationToken)
    {
        var oldPin = PinPrompt.GetPin("Enter current PIN");
        if (oldPin is null)
        {
            return;
        }

        var newPin = PinPrompt.GetNewPin();
        if (newPin is null)
        {
            CryptographicOperations.ZeroMemory(oldPin);
            return;
        }

        try
        {
            await session.ChangePinAsync(oldPin, newPin, cancellationToken);
            OutputHelpers.WriteSuccess("PIN changed successfully.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldPin);
            CryptographicOperations.ZeroMemory(newPin);
        }
    }

    /// <summary>
    /// Changes the PUK.
    /// </summary>
    private static async Task ChangePukAsync(IPivSession session, CancellationToken cancellationToken)
    {
        var oldPuk = PinPrompt.GetPuk("Enter current PUK");
        if (oldPuk is null)
        {
            return;
        }

        var newPuk = PinPrompt.GetNewPuk();
        if (newPuk is null)
        {
            CryptographicOperations.ZeroMemory(oldPuk);
            return;
        }

        try
        {
            await session.ChangePukAsync(oldPuk, newPuk, cancellationToken);
            OutputHelpers.WriteSuccess("PUK changed successfully.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldPuk);
            CryptographicOperations.ZeroMemory(newPuk);
        }
    }

    /// <summary>
    /// Unblocks PIN using PUK.
    /// </summary>
    private static async Task UnblockPinAsync(IPivSession session, CancellationToken cancellationToken)
    {
        var puk = PinPrompt.GetPuk();
        if (puk is null)
        {
            return;
        }

        var newPin = PinPrompt.GetNewPin();
        if (newPin is null)
        {
            CryptographicOperations.ZeroMemory(puk);
            return;
        }

        try
        {
            await session.UnblockPinAsync(puk, newPin, cancellationToken);
            OutputHelpers.WriteSuccess("PIN unblocked and set to new value.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(puk);
            CryptographicOperations.ZeroMemory(newPin);
        }
    }

    /// <summary>
    /// Sets PIN/PUK retry limits.
    /// </summary>
    private static async Task SetRetryLimitsAsync(IPivSession session, CancellationToken cancellationToken)
    {
        OutputHelpers.WriteWarning("Setting retry limits requires management key authentication.");
        OutputHelpers.WriteWarning("This will also reset PIN and PUK to defaults!");

        if (!AnsiConsole.Confirm("Continue?", defaultValue: false))
        {
            return;
        }

        // Authenticate with management key
        if (!await AuthenticateManagementKeyAsync(session, cancellationToken))
        {
            return;
        }

        var pinAttempts = AnsiConsole.Ask("PIN retry limit (1-255):", 3);
        var pukAttempts = AnsiConsole.Ask("PUK retry limit (1-255):", 3);

        if (pinAttempts < 1 || pinAttempts > 255 || pukAttempts < 1 || pukAttempts > 255)
        {
            OutputHelpers.WriteError("Retry limits must be between 1 and 255.");
            return;
        }

        await session.SetPinAttemptsAsync(pinAttempts, pukAttempts, cancellationToken);
        OutputHelpers.WriteSuccess($"Retry limits set: PIN={pinAttempts}, PUK={pukAttempts}");
        OutputHelpers.WriteDefaultCredentialWarning("PIN");
        OutputHelpers.WriteDefaultCredentialWarning("PUK");
    }

    /// <summary>
    /// Displays PIN/PUK metadata.
    /// </summary>
    private static async Task DisplayMetadataAsync(IPivSession session, CancellationToken cancellationToken)
    {
        try
        {
            // Get PIN metadata
            var pinMeta = await session.GetPinMetadataAsync(cancellationToken);
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("PIN Retries Remaining", pinMeta.RetriesRemaining.ToString());
            OutputHelpers.WriteKeyValue("PIN Total Retries", pinMeta.TotalRetries.ToString());

            if (pinMeta.IsDefault)
            {
                OutputHelpers.WriteDefaultCredentialWarning("PIN");
            }

            // Get PUK metadata
            var pukMeta = await session.GetPukMetadataAsync(cancellationToken);
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("PUK Retries Remaining", pukMeta.RetriesRemaining.ToString());
            OutputHelpers.WriteKeyValue("PUK Total Retries", pukMeta.TotalRetries.ToString());

            if (pukMeta.IsDefault)
            {
                OutputHelpers.WriteDefaultCredentialWarning("PUK");
            }

            // Get management key metadata
            var mgmtMeta = await session.GetManagementKeyMetadataAsync(cancellationToken);
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("Management Key Type", mgmtMeta.KeyType.ToString());
            OutputHelpers.WriteKeyValue("Touch Required", mgmtMeta.TouchPolicy.ToString());

            if (mgmtMeta.IsDefault)
            {
                OutputHelpers.WriteDefaultCredentialWarning("Management Key");
            }
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteInfo("PIN/PUK metadata requires firmware 5.3+");

            // Fall back to basic retry count
            var retries = await session.GetPinAttemptsAsync(cancellationToken);
            OutputHelpers.WriteKeyValue("PIN Retries Remaining", retries.ToString());
        }
    }

    /// <summary>
    /// Changes the management key.
    /// </summary>
    private static async Task ChangeManagementKeyAsync(IPivSession session, CancellationToken cancellationToken)
    {
        // Authenticate with current management key
        if (!await AuthenticateManagementKeyAsync(session, cancellationToken))
        {
            return;
        }

        // Select new key type
        var keyTypeChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select management key algorithm:")
                .AddChoices(
                [
                    "3DES (24 bytes) - Legacy",
                    "AES-128 (16 bytes)",
                    "AES-192 (24 bytes)",
                    "AES-256 (32 bytes)"
                ]));

        var (keyType, keyLength) = keyTypeChoice switch
        {
            "3DES (24 bytes) - Legacy" => (PivManagementKeyType.TripleDes, 24),
            "AES-128 (16 bytes)" => (PivManagementKeyType.Aes128, 16),
            "AES-192 (24 bytes)" => (PivManagementKeyType.Aes192, 24),
            "AES-256 (32 bytes)" => (PivManagementKeyType.Aes256, 32),
            _ => (PivManagementKeyType.TripleDes, 24)
        };

        // Get new management key
        var newKey = PinPrompt.GetManagementKey("Enter new management key (hex)", keyLength);
        if (newKey is null)
        {
            return;
        }

        var requireTouch = AnsiConsole.Confirm("Require touch for management key operations?", defaultValue: false);

        try
        {
            await session.SetManagementKeyAsync(keyType, newKey, requireTouch, cancellationToken);
            OutputHelpers.WriteSuccess($"Management key changed to {keyType}.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(newKey);
        }
    }

    /// <summary>
    /// Authenticates with management key, prompting if needed.
    /// </summary>
    private static async Task<bool> AuthenticateManagementKeyAsync(
        IPivSession session,
        CancellationToken cancellationToken)
    {
        if (session.IsAuthenticated)
        {
            return true;
        }

        var useDefault = AnsiConsole.Confirm("Use default management key?", defaultValue: true);

        byte[]? key = null;
        try
        {
            if (useDefault)
            {
                key = (byte[])DefaultManagementKey.Clone();
            }
            else
            {
                var expectedLength = session.ManagementKeyType switch
                {
                    PivManagementKeyType.TripleDes => 24,
                    PivManagementKeyType.Aes128 => 16,
                    PivManagementKeyType.Aes192 => 24,
                    PivManagementKeyType.Aes256 => 32,
                    _ => 24
                };

                key = PinPrompt.GetManagementKey($"Enter management key ({session.ManagementKeyType})", expectedLength);
                if (key is null)
                {
                    return false;
                }
            }

            await session.AuthenticateAsync(key, cancellationToken);
            OutputHelpers.WriteSuccess("Management key authenticated.");
            return true;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Incorrect management key: {ex.Message}");
            return false;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    /// <summary>
    /// Handles PIN/PUK errors with user-friendly messages.
    /// </summary>
    private static void HandlePinError(InvalidPinException ex)
    {
        if (ex.RetriesRemaining == 0)
        {
            if (ex.Message.Contains("PIN"))
            {
                OutputHelpers.WriteError("PIN is blocked. Use PUK to unblock or reset PIV.");
            }
            else
            {
                OutputHelpers.WriteError("PUK is blocked. PIV reset required.");
            }
        }
        else
        {
            var type = ex.Message.Contains("PUK") ? "PUK" : "PIN";
            OutputHelpers.WriteError($"Incorrect {type}. {ex.RetriesRemaining} attempts remaining.");
        }
    }
}
