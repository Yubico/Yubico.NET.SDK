// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Resets all OpenPGP data (openpgp reset).
/// </summary>
public sealed class ResetCommand : OpenPgpCommand<ResetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-f|--force")]
        [Description("Confirm the action without prompting.")]
        public bool Force { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        if (!settings.Force)
        {
            AnsiConsole.MarkupLine("[red bold]WARNING: This will reset the OpenPGP application.[/]");
            AnsiConsole.MarkupLine("[red]  - Delete ALL private keys[/]");
            AnsiConsole.MarkupLine("[red]  - Delete ALL certificates[/]");
            AnsiConsole.MarkupLine("[red]  - Reset PINs to defaults (User: 123456, Admin: 12345678)[/]");
            AnsiConsole.MarkupLine("[red]  - Clear all fingerprints and metadata[/]");
            AnsiConsole.WriteLine();

            if (!OutputHelpers.ConfirmDestructive(
                    "factory reset the OpenPGP application, permanently destroying all keys and data"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return 1;
            }
        }

        await AnsiConsole.Status()
            .StartAsync("Resetting OpenPGP application...", async _ =>
            {
                await session.ResetAsync();
            });

        OutputHelpers.WriteSuccess("OpenPGP application has been reset.");
        OutputHelpers.WriteInfo("Default User PIN: 123456");
        OutputHelpers.WriteInfo("Default Admin PIN: 12345678");
        return 0;
    }
}