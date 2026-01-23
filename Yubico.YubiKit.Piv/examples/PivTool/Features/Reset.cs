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
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Features;

/// <summary>
/// Handles PIV application reset to factory defaults.
/// </summary>
public static class ResetFeature
{
    /// <summary>
    /// Runs the reset feature.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("PIV Reset");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        // Display warnings
        DisplayResetWarnings();

        // First confirmation
        if (!AnsiConsole.Confirm("[red]Do you understand that ALL PIV data will be permanently lost?[/]", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return;
        }

        // Second confirmation - type to confirm
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]Type 'RESET' to confirm:[/]");
        var confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "RESET", StringComparison.Ordinal))
        {
            OutputHelpers.WriteInfo("Reset cancelled - confirmation did not match.");
            return;
        }

        // Check reset requirements
        OutputHelpers.WriteInfo("Checking reset requirements...");

        // Note: PIV reset typically requires either:
        // 1. Both PIN and PUK to be blocked, OR
        // 2. Management key authentication

        try
        {
            // Try management key authentication first
            var useManagementKey = AnsiConsole.Confirm("Authenticate with management key?", defaultValue: true);

            if (useManagementKey)
            {
                if (!await AuthenticateManagementKeyAsync(session, cancellationToken))
                {
                    OutputHelpers.WriteInfo("Reset requires blocked PIN and PUK, or management key authentication.");
                    return;
                }
            }

            // Perform reset
            await AnsiConsole.Status()
                .StartAsync("Resetting PIV application...", async ctx =>
                {
                    await session.ResetAsync(cancellationToken);
                });

            OutputHelpers.WriteSuccess("PIV application has been reset to factory defaults.");
            AnsiConsole.WriteLine();

            // Display post-reset warnings
            DisplayPostResetWarnings();

            // Prompt to change credentials
            if (AnsiConsole.Confirm("Change default credentials now?", defaultValue: true))
            {
                OutputHelpers.WriteInfo("Please use PIN Management to change default PIN, PUK, and management key.");
            }
        }
        catch (InvalidOperationException ex)
        {
            HandleResetError(ex);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays reset warnings.
    /// </summary>
    private static void DisplayResetWarnings()
    {
        var panel = new Panel(
            "[red bold]⚠️  WARNING: PIV RESET ⚠️[/]\n\n" +
            "This operation will:\n" +
            "  • [red]Delete ALL private keys[/]\n" +
            "  • [red]Delete ALL certificates[/]\n" +
            "  • [red]Reset PIN to default (123456)[/]\n" +
            "  • [red]Reset PUK to default (12345678)[/]\n" +
            "  • [red]Reset management key to default[/]\n" +
            "  • [red]Clear all PIV data objects[/]\n\n" +
            "[yellow]This action CANNOT be undone![/]")
            .Header("[red]DANGER[/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.Red)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays post-reset warnings about default credentials.
    /// </summary>
    private static void DisplayPostResetWarnings()
    {
        var panel = new Panel(
            "[yellow]🔓 DEFAULT CREDENTIALS ARE NOW IN USE[/]\n\n" +
            "Your PIV credentials have been reset to:\n" +
            "  • PIN: [yellow]123456[/]\n" +
            "  • PUK: [yellow]12345678[/]\n" +
            "  • Management Key: [yellow]Default 3DES key[/]\n\n" +
            "[yellow bold]Change these immediately for security![/]")
            .Header("[yellow]Security Notice[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Authenticates with management key.
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
                key =
                [
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                ];
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
            return true;
        }
        catch
        {
            OutputHelpers.WriteError("Incorrect management key.");
            return false;
        }
        finally
        {
            if (key is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    /// <summary>
    /// Handles reset errors.
    /// </summary>
    private static void HandleResetError(InvalidOperationException ex)
    {
        if (ex.Message.Contains("biometric", StringComparison.OrdinalIgnoreCase))
        {
            OutputHelpers.WriteError("Cannot reset: Biometrics are configured on this device.");
            OutputHelpers.WriteInfo("Disable biometrics before resetting PIV.");
        }
        else if (ex.Message.Contains("blocked", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            OutputHelpers.WriteError("Reset requires blocked PIN and PUK, or management key authentication.");
        }
        else
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}");
        }
    }
}
