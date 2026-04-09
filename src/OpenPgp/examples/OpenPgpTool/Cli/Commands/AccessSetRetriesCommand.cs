// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Sets PIN retry counts (openpgp access set-retries).
/// </summary>
public sealed class AccessSetRetriesCommand : OpenPgpCommand<AccessSetRetriesCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<USER>")]
        [Description("User PIN retry count.")]
        public int UserRetries { get; init; }

        [CommandArgument(1, "<RESET>")]
        [Description("Reset Code retry count.")]
        public int ResetRetries { get; init; }

        [CommandArgument(2, "<ADMIN>")]
        [Description("Admin PIN retry count.")]
        public int AdminRetries { get; init; }

        [CommandOption("--admin-pin <PIN>")]
        [Description("Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");

        await session.VerifyAdminAsync(Encoding.UTF8.GetBytes(adminPin));
        await session.SetPinAttemptsAsync(
            settings.UserRetries,
            settings.ResetRetries,
            settings.AdminRetries);

        OutputHelpers.WriteSuccess(
            $"PIN retry counts set to User={settings.UserRetries}, " +
            $"Reset={settings.ResetRetries}, Admin={settings.AdminRetries}.");
        return 0;
    }
}