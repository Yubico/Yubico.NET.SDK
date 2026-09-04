// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp;
using static Yubico.YubiKit.Cli.Commands.OpenPgp.OpenPgpHelpers;

namespace Yubico.YubiKit.Cli.Commands.OpenPgp;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class AccessSetRetriesSettings : GlobalSettings
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

public sealed class AccessChangePinSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("Current User PIN (prompted if not provided).")]
    public string? Pin { get; init; }

    [CommandOption("--new-pin <PIN>")]
    [Description("New User PIN (prompted if not provided).")]
    public string? NewPin { get; init; }
}

public sealed class AccessChangeAdminPinSettings : GlobalSettings
{
    [CommandOption("--admin-pin <PIN>")]
    [Description("Current Admin PIN (prompted if not provided).")]
    public string? AdminPin { get; init; }

    [CommandOption("--new-admin-pin <PIN>")]
    [Description("New Admin PIN (prompted if not provided).")]
    public string? NewAdminPin { get; init; }
}

public sealed class AccessSetResetCodeSettings : GlobalSettings
{
    [CommandOption("--admin-pin <PIN>")]
    [Description("Admin PIN (prompted if not provided).")]
    public string? AdminPin { get; init; }

    [CommandOption("--reset-code <CODE>")]
    [Description("New Reset Code (prompted if not provided).")]
    public string? ResetCode { get; init; }
}

public sealed class AccessUnblockPinSettings : GlobalSettings
{
    [CommandOption("--reset-code <CODE>")]
    [Description("Reset Code (prompted if not provided).")]
    public string? ResetCode { get; init; }

    [CommandOption("--new-pin <PIN>")]
    [Description("New User PIN (prompted if not provided).")]
    public string? NewPin { get; init; }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class OpenPgpAccessSetRetriesCommand : YkCommandBase<AccessSetRetriesSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessSetRetriesSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);
        await session.SetPinAttemptsAsync(settings.UserRetries, settings.ResetRetries, settings.AdminRetries);

        OutputHelpers.WriteSuccess(
            $"PIN retry counts set to User={settings.UserRetries}, " +
            $"Reset={settings.ResetRetries}, Admin={settings.AdminRetries}.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpAccessChangePinCommand : YkCommandBase<AccessChangePinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessChangePinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var currentPin = GetPin(settings.Pin, "Enter current User PIN");
        using var newPin = GetPin(settings.NewPin, "Enter new User PIN");
        if (currentPin is null || newPin is null)
        {
            OutputHelpers.WriteError("Current and new User PINs are required.");
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.NewPin) &&
            !PinPrompt.ConfirmMatches(newPin, "Confirm new User PIN"))
        {
            OutputHelpers.WriteError("New PINs do not match.");
            return ExitCode.GenericError;
        }

        await session.ChangePinAsync(currentPin.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("User PIN has been changed.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpAccessChangeAdminPinCommand : YkCommandBase<AccessChangeAdminPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessChangeAdminPinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var currentPin = GetPin(settings.AdminPin, "Enter current Admin PIN");
        using var newPin = GetPin(settings.NewAdminPin, "Enter new Admin PIN");
        if (currentPin is null || newPin is null)
        {
            OutputHelpers.WriteError("Current and new Admin PINs are required.");
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.NewAdminPin) &&
            !PinPrompt.ConfirmMatches(newPin, "Confirm new Admin PIN"))
        {
            OutputHelpers.WriteError("New Admin PINs do not match.");
            return ExitCode.GenericError;
        }

        await session.ChangeAdminAsync(currentPin.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("Admin PIN has been changed.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpAccessSetResetCodeCommand : YkCommandBase<AccessSetResetCodeSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessSetResetCodeSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        using var resetCode = GetPin(settings.ResetCode, "Enter new Reset Code");
        if (adminPin is null || resetCode is null)
        {
            OutputHelpers.WriteError("Admin PIN and Reset Code are required.");
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.ResetCode) &&
            !PinPrompt.ConfirmMatches(resetCode, "Confirm new Reset Code"))
        {
            OutputHelpers.WriteError("Reset Codes do not match.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);
        await session.SetResetCodeAsync(resetCode.Memory);
        OutputHelpers.WriteSuccess("Reset Code has been set.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpAccessUnblockPinCommand : YkCommandBase<AccessUnblockPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessUnblockPinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        using var resetCode = GetPin(settings.ResetCode, "Enter Reset Code");
        using var newPin = GetPin(settings.NewPin, "Enter new User PIN");
        if (resetCode is null || newPin is null)
        {
            OutputHelpers.WriteError("Reset Code and new User PIN are required.");
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.NewPin) &&
            !PinPrompt.ConfirmMatches(newPin, "Confirm new User PIN"))
        {
            OutputHelpers.WriteError("New PINs do not match.");
            return ExitCode.GenericError;
        }

        await session.ResetPinUsingResetCodeAsync(resetCode.Memory, newPin.Memory);
        OutputHelpers.WriteSuccess("User PIN has been unblocked.");
        return ExitCode.Success;
    }
}