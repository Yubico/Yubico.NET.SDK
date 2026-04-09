// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Unblocks the User PIN using a Reset Code (openpgp access unblock-pin).
/// </summary>
public sealed class AccessUnblockPinCommand : OpenPgpCommand<AccessUnblockPinCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--reset-code <CODE>")]
        [Description("Reset Code (prompted if not provided).")]
        public string? ResetCode { get; init; }

        [CommandOption("--new-pin <PIN>")]
        [Description("New User PIN (prompted if not provided).")]
        public string? NewPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        using var resetCode = GetResetCode(settings.ResetCode);
        if (resetCode is null)
        {
            OutputHelpers.WriteError("Reset Code is required.");
            return 1;
        }

        using var newPin = GetPin(settings.NewPin);
        if (newPin is null)
        {
            OutputHelpers.WriteError("New User PIN is required.");
            return 1;
        }

        await session.ResetPinAsync(resetCode.Memory, newPin.Memory, useAdmin: false);
        OutputHelpers.WriteSuccess("User PIN has been unblocked.");
        return 0;
    }
}