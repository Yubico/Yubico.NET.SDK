// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Attempts the OpenPGP application reset sequence (openpgp reset).
/// </summary>
/// <remarks>
///     Successful YubiKey reset preserves ATT key/certificate and fixed ATT policy. Verify persistent
///     state independently before treating a normal return as proof of erasure.
/// </remarks>
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
            AnsiConsole.MarkupLine("[red]  - Delete ordinary keys and certificates (SIG, DEC, AUT)[/]");
            AnsiConsole.MarkupLine("[red]  - Preserve the ATT key, certificate, and fixed touch policy[/]");
            AnsiConsole.MarkupLine("[red]  - Reset PINs to defaults (User: 123456, Admin: 12345678)[/]");
            AnsiConsole.MarkupLine("[red]  - Clear ordinary key fingerprints and metadata[/]");
            AnsiConsole.WriteLine();

            if (!OutputHelpers.ConfirmDestructive(
                    "factory reset the OpenPGP application, permanently destroying ordinary key material"))
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

        OutputHelpers.WriteSuccess("OpenPGP reset completed and the applet was reopened. Inspect persistent state to verify erasure.");
        OutputHelpers.WriteInfo("Default User PIN: 123456");
        OutputHelpers.WriteInfo("Default Admin PIN: 12345678");
        return 0;
    }
}
