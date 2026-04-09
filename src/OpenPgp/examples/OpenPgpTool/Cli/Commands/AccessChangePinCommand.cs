// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Changes the User PIN (openpgp access change-pin).
/// </summary>
public sealed class AccessChangePinCommand : OpenPgpCommand<AccessChangePinCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--pin <PIN>")]
        [Description("Current User PIN (prompted if not provided).")]
        public string? Pin { get; init; }

        [CommandOption("--new-pin <PIN>")]
        [Description("New User PIN (prompted if not provided).")]
        public string? NewPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        using var currentPin = GetPin(settings.Pin);
        if (currentPin is null)
        {
            OutputHelpers.WriteError("Current User PIN is required.");
            return 1;
        }

        using var newPin = GetPin(settings.NewPin);
        if (newPin is null)
        {
            OutputHelpers.WriteError("New User PIN is required.");
            return 1;
        }

        await session.ChangePinAsync(currentPin.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("User PIN has been changed.");
        return 0;
    }
}