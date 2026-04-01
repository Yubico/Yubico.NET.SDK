// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Changes the Admin PIN (openpgp access change-admin-pin).
/// </summary>
public sealed class AccessChangeAdminPinCommand : OpenPgpCommand<AccessChangeAdminPinCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--admin-pin <PIN>")]
        [Description("Current Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }

        [CommandOption("--new-admin-pin <PIN>")]
        [Description("New Admin PIN (prompted if not provided).")]
        public string? NewAdminPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var currentPin = GetPin(settings.AdminPin, "Enter current Admin PIN");
        var newPin = GetPin(settings.NewAdminPin, "Enter new Admin PIN");

        if (string.IsNullOrEmpty(settings.NewAdminPin))
        {
            var confirm = OutputHelpers.PromptPin("Confirm new Admin PIN");
            if (!string.Equals(newPin, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("New Admin PINs do not match.");
                return 1;
            }
        }

        await session.ChangeAdminAsync(currentPin, newPin);
        OutputHelpers.WriteSuccess("Admin PIN has been changed.");
        return 0;
    }
}