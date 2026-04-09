// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
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
        var currentPin = GetPin(settings.Pin, "Enter current User PIN");
        var newPin = GetPin(settings.NewPin, "Enter new User PIN");

        if (string.IsNullOrEmpty(settings.NewPin))
        {
            var confirm = OutputHelpers.PromptPin("Confirm new User PIN");
            if (!string.Equals(newPin, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("New PINs do not match.");
                return 1;
            }
        }

        await session.ChangePinAsync(Encoding.UTF8.GetBytes(currentPin), Encoding.UTF8.GetBytes(newPin));
        OutputHelpers.WriteSuccess("User PIN has been changed.");
        return 0;
    }
}