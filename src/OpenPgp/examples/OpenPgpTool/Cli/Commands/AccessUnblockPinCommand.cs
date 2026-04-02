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
        var resetCode = GetPin(settings.ResetCode, "Enter Reset Code");
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

        await session.ResetPinAsync(resetCode, newPin, useAdmin: false);
        OutputHelpers.WriteSuccess("User PIN has been unblocked.");
        return 0;
    }
}