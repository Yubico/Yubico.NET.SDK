// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Menus;

/// <summary>
/// CLI menu for credential management operations: list, add, delete, rename.
/// </summary>
public static class CredentialMenu
{
    /// <summary>
    /// Lists all OATH credentials on the device.
    /// </summary>
    public static async Task ListAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("List Credentials");

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

        var credentials = await session.ListCredentialsAsync(cancellationToken);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return;
        }

        var table = OutputHelpers.CreateTable("Account", "Type", "Algorithm", "Period", "Touch");

        foreach (var cred in credentials.OrderBy(c => OutputHelpers.FormatCredentialName(c.Issuer, c.Name)))
        {
            table.AddRow(
                Markup.Escape(OutputHelpers.FormatCredentialName(cred.Issuer, cred.Name)),
                cred.OathType.ToString(),
                "—",
                cred.OathType == OathType.Totp ? $"{cred.Period}s" : "—",
                cred.TouchRequired == true ? "[yellow]Yes[/]" : "[grey]No[/]");
        }

        AnsiConsole.Write(table);
        OutputHelpers.WriteInfo($"{credentials.Count} credential(s) found.");
    }

    /// <summary>
    /// Adds a credential from an otpauth:// URI.
    /// </summary>
    public static async Task AddAsync(
        string? uri = null,
        CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Add Credential");

        uri ??= AnsiConsole.Ask<string>("Enter otpauth:// URI:");

        CredentialData credentialData;
        try
        {
            credentialData = CredentialData.ParseUri(uri);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Invalid URI: {ex.Message}");
            return;
        }

        // Display what will be added
        AnsiConsole.MarkupLine("[bold]Credential Details[/]");
        OutputHelpers.WriteKeyValue("Account",
            OutputHelpers.FormatCredentialName(credentialData.Issuer, credentialData.Name));
        OutputHelpers.WriteKeyValue("Type", credentialData.OathType.ToString());
        OutputHelpers.WriteKeyValue("Algorithm", credentialData.HashAlgorithm.ToString());
        OutputHelpers.WriteKeyValue("Digits", credentialData.Digits.ToString());
        if (credentialData.OathType == OathType.Totp)
        {
            OutputHelpers.WriteKeyValue("Period", $"{credentialData.Period}s");
        }
        else
        {
            OutputHelpers.WriteKeyValue("Counter", credentialData.Counter.ToString());
        }

        AnsiConsole.WriteLine();

        var requireTouch = AnsiConsole.Confirm("Require touch to generate codes?", defaultValue: false);

        AnsiConsole.WriteLine();

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

        await session.PutCredentialAsync(credentialData, requireTouch, cancellationToken);

        OutputHelpers.WriteSuccess(
            $"Credential added: {OutputHelpers.FormatCredentialName(credentialData.Issuer, credentialData.Name)}");
    }

    /// <summary>
    /// Deletes a credential from the device.
    /// </summary>
    public static async Task DeleteAsync(
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Delete Credential");

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

        var credential = await SelectCredentialAsync(session, name, cancellationToken);
        if (credential is null)
        {
            return;
        }

        var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

        if (!OutputHelpers.ConfirmDangerous($"delete credential '{displayName}'"))
        {
            OutputHelpers.WriteInfo("Delete cancelled.");
            return;
        }

        await session.DeleteCredentialAsync(credential, cancellationToken);
        OutputHelpers.WriteSuccess($"Credential deleted: {displayName}");
    }

    /// <summary>
    /// Renames a credential on the device. Requires firmware 5.3.1+.
    /// </summary>
    public static async Task RenameAsync(
        string? oldName = null,
        string? newName = null,
        CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Rename Credential");

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

        var credential = await SelectCredentialAsync(session, oldName, cancellationToken);
        if (credential is null)
        {
            return;
        }

        var currentName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);
        AnsiConsole.MarkupLine($"[bold]Current name:[/] {Markup.Escape(currentName)}");
        AnsiConsole.WriteLine();

        var newIssuer = AnsiConsole.Prompt(
            new TextPrompt<string>("New issuer (leave empty for none):")
                .AllowEmpty());
        var newAccountName = newName ?? AnsiConsole.Ask<string>("New account name:");

        try
        {
            var renamed = await session.RenameCredentialAsync(
                credential,
                string.IsNullOrEmpty(newIssuer) ? null : newIssuer,
                newAccountName,
                cancellationToken);

            var renamedName = OutputHelpers.FormatCredentialName(renamed.Issuer, renamed.Name);
            OutputHelpers.WriteSuccess($"Credential renamed: {currentName} -> {renamedName}");
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Rename requires firmware 5.3.1 or later.");
        }
    }

    /// <summary>
    /// Prompts the user to select a credential from the device, or finds one by name.
    /// </summary>
    internal static async Task<Credential?> SelectCredentialAsync(
        IOathSession session,
        string? filterName = null,
        CancellationToken cancellationToken = default)
    {
        var credentials = await session.ListCredentialsAsync(cancellationToken);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return null;
        }

        // If a name filter was provided, try to find a match
        if (filterName is not null)
        {
            var match = credentials.FirstOrDefault(c =>
                OutputHelpers.FormatCredentialName(c.Issuer, c.Name)
                    .Contains(filterName, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }

            OutputHelpers.WriteWarning($"No credential matching '{filterName}' found.");
        }

        // Interactive selection
        var choices = credentials
            .OrderBy(c => OutputHelpers.FormatCredentialName(c.Issuer, c.Name))
            .Select(c => OutputHelpers.FormatCredentialName(c.Issuer, c.Name))
            .ToList();
        choices.Add("Cancel");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a credential:")
                .PageSize(15)
                .AddChoices(choices));

        if (selected == "Cancel")
        {
            return null;
        }

        return credentials.First(c =>
            OutputHelpers.FormatCredentialName(c.Issuer, c.Name) == selected);
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
