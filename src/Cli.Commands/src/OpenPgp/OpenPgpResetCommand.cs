// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp;

namespace Yubico.YubiKit.Cli.Commands.OpenPgp;

public sealed class OpenPgpResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Confirm the action without prompting.")]
    public bool Force { get; init; }
}

public sealed class OpenPgpResetCommand : YkCommandBase<OpenPgpResetSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OpenPgpResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "factory reset the OpenPGP application, permanently destroying all keys and data"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        await AnsiConsole.Status()
            .StartAsync("Resetting OpenPGP application...", async _ =>
            {
                await session.ResetAsync();
            });

        OutputHelpers.WriteSuccess("OpenPGP application has been reset.");
        OutputHelpers.WriteInfo("Default User PIN: 123456");
        OutputHelpers.WriteInfo("Default Admin PIN: 12345678");
        return ExitCode.Success;
    }
}
