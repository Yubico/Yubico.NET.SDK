// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
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
        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        var resetCode = GetPin(settings.ResetCode, "Enter new Reset Code");

        if (string.IsNullOrEmpty(settings.ResetCode))
        {
            var confirm = OutputHelpers.PromptPin("Confirm new Reset Code");
            if (!string.Equals(resetCode, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("Reset Codes do not match.");
                return 1;
            }
        }

        await session.VerifyAdminAsync(Encoding.UTF8.GetBytes(adminPin));
        await session.SetResetCodeAsync(Encoding.UTF8.GetBytes(resetCode));
        OutputHelpers.WriteSuccess("Reset Code has been set.");
        return 0;
    }
}