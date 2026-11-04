// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Sets the Reset Code (openpgp access set-reset-code).
/// </summary>
public sealed class AccessSetResetCodeCommand : OpenPgpCommand<AccessSetResetCodeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--admin-pin <PIN>")]
        [Description("Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }

        [CommandOption("--reset-code <CODE>")]
        [Description("New Reset Code (prompted if not provided).")]
        public string? ResetCode { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        using var adminPin = GetAdminPin(settings.AdminPin);
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return 1;
        }

        using var resetCode = GetResetCode(settings.ResetCode);
        if (resetCode is null)
        {
            OutputHelpers.WriteError("Reset Code is required.");
            return 1;
        }

        await session.VerifyAdminAsync(adminPin.Memory);
        await session.SetResetCodeAsync(resetCode.Memory);
        OutputHelpers.WriteSuccess("Reset Code has been set.");
        return 0;
    }
}