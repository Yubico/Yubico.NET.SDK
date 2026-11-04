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
        using var currentPin = GetAdminPin(settings.AdminPin);
        if (currentPin is null)
        {
            OutputHelpers.WriteError("Current Admin PIN is required.");
            return 1;
        }

        using var newPin = GetAdminPin(settings.NewAdminPin);
        if (newPin is null)
        {
            OutputHelpers.WriteError("New Admin PIN is required.");
            return 1;
        }

        await session.ChangeAdminAsync(currentPin.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("Admin PIN has been changed.");
        return 0;
    }
}