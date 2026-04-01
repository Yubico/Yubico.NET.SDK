// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Menus;

/// <summary>
/// CLI menu for code calculation operations: calculate single and calculate all.
/// </summary>
public static class CodeMenu
{
    /// <summary>
    /// Calculates a code for a single credential.
    /// </summary>
    public static async Task CalculateAsync(
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Calculate Code");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        if (session.IsLocked)
        {
            if (!await UnlockSessionAsync(session))
            {
                return;
            }
        }

        var credential = await CredentialMenu.SelectCredentialAsync(session, name, cancellationToken);
        if (credential is null)
        {
            return;
        }

        var credName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

        try
        {
            var code = await session.CalculateCodeAsync(credential, cancellationToken: cancellationToken);

            AnsiConsole.WriteLine();
            var panel = new Panel($"[bold green]{Markup.Escape(code.Value)}[/]")
                .Header($"[green]{Markup.Escape(credName)}[/]")
                .Border(BoxBorder.Rounded)
                .Padding(2, 1);
            AnsiConsole.Write(panel);

            if (credential.OathType == OathType.Totp)
            {
                var validFrom = DateTimeOffset.FromUnixTimeSeconds(code.ValidFrom).LocalDateTime;
                var validTo = DateTimeOffset.FromUnixTimeSeconds(code.ValidTo).LocalDateTime;
                OutputHelpers.WriteKeyValue("Valid from", validFrom.ToString("HH:mm:ss"));
                OutputHelpers.WriteKeyValue("Valid to", validTo.ToString("HH:mm:ss"));

                var remaining = code.ValidTo - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (remaining > 0)
                {
                    OutputHelpers.WriteKeyValue("Expires in", $"{remaining}s");
                }
            }
        }
        catch (Exception ex) when (ex.Message.Contains("touch", StringComparison.OrdinalIgnoreCase))
        {
            OutputHelpers.WriteInfo("Touch the YubiKey to generate the code...");
            // Re-throw to let the user know the operation requires physical interaction
            throw;
        }
    }

    /// <summary>
    /// Calculates codes for all credentials on the device.
    /// </summary>
    public static async Task CalculateAllAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Calculate All Codes");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        if (session.IsLocked)
        {
            if (!await UnlockSessionAsync(session))
            {
                return;
            }
        }

        var results = await session.CalculateAllAsync(cancellationToken: cancellationToken);

        if (results.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return;
        }

        var table = OutputHelpers.CreateTable("Account", "Code", "Type", "Valid Until");

        foreach (var (credential, code) in results.OrderBy(r =>
                     OutputHelpers.FormatCredentialName(r.Key.Issuer, r.Key.Name)))
        {
            var credName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

            if (code is not null)
            {
                var validTo = credential.OathType == OathType.Totp
                    ? DateTimeOffset.FromUnixTimeSeconds(code.ValidTo).LocalDateTime.ToString("HH:mm:ss")
                    : "—";

                table.AddRow(
                    Markup.Escape(credName),
                    $"[bold green]{Markup.Escape(code.Value)}[/]",
                    credential.OathType.ToString(),
                    validTo);
            }
            else
            {
                var reason = credential.TouchRequired == true ? "[yellow]Touch required[/]" : "[grey]HOTP[/]";
                table.AddRow(
                    Markup.Escape(credName),
                    reason,
                    credential.OathType.ToString(),
                    "—");
            }
        }

        AnsiConsole.Write(table);

        var codesGenerated = results.Count(r => r.Value is not null);
        OutputHelpers.WriteInfo($"{codesGenerated}/{results.Count} codes calculated.");

        if (results.Any(r => r.Value is null && r.Key.TouchRequired == true))
        {
            OutputHelpers.WriteWarning("Some credentials require touch. Use 'Calculate' for individual codes.");
        }
    }

    /// <summary>
    /// Prompts for password and validates against the locked session.
    /// </summary>
    private static async Task<bool> UnlockSessionAsync(IOathSession session)
    {
        AnsiConsole.MarkupLine("[yellow]This device is password-protected.[/]");

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter OATH password:")
                .Secret());

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(password);
            await session.ValidateAsync(key);
            OutputHelpers.WriteSuccess("Device unlocked.");
            AnsiConsole.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Authentication failed: {ex.Message}");
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
}
