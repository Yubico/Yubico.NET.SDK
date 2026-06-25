// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
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

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        byte[]? adminPinBytes = null;

        try
        {
            adminPinBytes = Encoding.UTF8.GetBytes(adminPin);
            await session.VerifyAdminAsync(adminPinBytes);
            await session.SetPinAttemptsAsync(settings.UserRetries, settings.ResetRetries, settings.AdminRetries);

            OutputHelpers.WriteSuccess(
                $"PIN retry counts set to User={settings.UserRetries}, " +
                $"Reset={settings.ResetRetries}, Admin={settings.AdminRetries}.");
            return ExitCode.Success;
        }
        finally
        {
            if (adminPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(adminPinBytes);
            }
        }
    }
}

public sealed class OpenPgpAccessChangePinCommand : YkCommandBase<AccessChangePinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessChangePinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var currentPin = GetPin(settings.Pin, "Enter current User PIN");
        var newPin = GetPin(settings.NewPin, "Enter new User PIN");

        if (string.IsNullOrEmpty(settings.NewPin))
        {
            var confirm = PinPrompt.PromptForPin("Confirm new User PIN");
            if (!string.Equals(newPin, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("New PINs do not match.");
                return ExitCode.GenericError;
            }
        }

        byte[]? currentPinBytes = null;
        byte[]? newPinBytes = null;

        try
        {
            currentPinBytes = Encoding.UTF8.GetBytes(currentPin);
            newPinBytes = Encoding.UTF8.GetBytes(newPin);
            await session.ChangePinAsync(currentPinBytes, newPinBytes);
            OutputHelpers.WriteSuccess("User PIN has been changed.");
            return ExitCode.Success;
        }
        finally
        {
            if (currentPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(currentPinBytes);
            }

            if (newPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(newPinBytes);
            }
        }
    }
}

public sealed class OpenPgpAccessChangeAdminPinCommand : YkCommandBase<AccessChangeAdminPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessChangeAdminPinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var currentPin = GetPin(settings.AdminPin, "Enter current Admin PIN");
        var newPin = GetPin(settings.NewAdminPin, "Enter new Admin PIN");

        if (string.IsNullOrEmpty(settings.NewAdminPin))
        {
            var confirm = PinPrompt.PromptForPin("Confirm new Admin PIN");
            if (!string.Equals(newPin, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("New Admin PINs do not match.");
                return ExitCode.GenericError;
            }
        }

        byte[]? currentPinBytes = null;
        byte[]? newPinBytes = null;

        try
        {
            currentPinBytes = Encoding.UTF8.GetBytes(currentPin);
            newPinBytes = Encoding.UTF8.GetBytes(newPin);
            await session.ChangeAdminAsync(currentPinBytes, newPinBytes);
            OutputHelpers.WriteSuccess("Admin PIN has been changed.");
            return ExitCode.Success;
        }
        finally
        {
            if (currentPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(currentPinBytes);
            }

            if (newPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(newPinBytes);
            }
        }
    }
}

public sealed class OpenPgpAccessSetResetCodeCommand : YkCommandBase<AccessSetResetCodeSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessSetResetCodeSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        var resetCode = GetPin(settings.ResetCode, "Enter new Reset Code");

        if (string.IsNullOrEmpty(settings.ResetCode))
        {
            var confirm = PinPrompt.PromptForPin("Confirm new Reset Code");
            if (!string.Equals(resetCode, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("Reset Codes do not match.");
                return ExitCode.GenericError;
            }
        }

        byte[]? adminPinBytes = null;
        byte[]? resetCodeBytes = null;

        try
        {
            adminPinBytes = Encoding.UTF8.GetBytes(adminPin);
            resetCodeBytes = Encoding.UTF8.GetBytes(resetCode);
            await session.VerifyAdminAsync(adminPinBytes);
            await session.SetResetCodeAsync(resetCodeBytes);
            OutputHelpers.WriteSuccess("Reset Code has been set.");
            return ExitCode.Success;
        }
        finally
        {
            if (adminPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(adminPinBytes);
            }

            if (resetCodeBytes is not null)
            {
                CryptographicOperations.ZeroMemory(resetCodeBytes);
            }
        }
    }
}

public sealed class OpenPgpAccessUnblockPinCommand : YkCommandBase<AccessUnblockPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, AccessUnblockPinSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var resetCode = GetPin(settings.ResetCode, "Enter Reset Code");
        var newPin = GetPin(settings.NewPin, "Enter new User PIN");

        if (string.IsNullOrEmpty(settings.NewPin))
        {
            var confirm = PinPrompt.PromptForPin("Confirm new User PIN");
            if (!string.Equals(newPin, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("New PINs do not match.");
                return ExitCode.GenericError;
            }
        }

        byte[]? resetCodeBytes = null;
        byte[]? newPinBytes = null;

        try
        {
            resetCodeBytes = Encoding.UTF8.GetBytes(resetCode);
            newPinBytes = Encoding.UTF8.GetBytes(newPin);
            await session.ResetPinAsync(resetCodeBytes, newPinBytes, useAdmin: false);
            OutputHelpers.WriteSuccess("User PIN has been unblocked.");
            return ExitCode.Success;
        }
        finally
        {
            if (resetCodeBytes is not null)
            {
                CryptographicOperations.ZeroMemory(resetCodeBytes);
            }

            if (newPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(newPinBytes);
            }
        }
    }
}
