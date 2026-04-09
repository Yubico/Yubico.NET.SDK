// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Sets the touch policy for a key slot (openpgp keys set-touch).
/// </summary>
public sealed class KeysSetTouchCommand : OpenPgpCommand<KeysSetTouchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut, att).")]
        public string Key { get; init; } = "";

        [CommandArgument(1, "<POLICY>")]
        [Description("Touch policy (on, off, fixed, cached, cached-fixed).")]
        public string Policy { get; init; } = "";

        [CommandOption("--admin-pin <PIN>")]
        [Description("Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }

        [CommandOption("-f|--force")]
        [Description("Confirm the action without prompting.")]
        public bool Force { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var keyRef = ParseKeyRef(settings.Key);

        var uif = settings.Policy.ToLowerInvariant() switch
        {
            "on" => Uif.On,
            "off" => Uif.Off,
            "fixed" => Uif.Fixed,
            "cached" => Uif.Cached,
            "cached-fixed" => Uif.CachedFixed,
            _ => throw new ArgumentException(
                $"Invalid policy: {settings.Policy}. Must be on, off, fixed, cached, or cached-fixed.")
        };

        // Check if current policy is fixed
        var current = await session.GetUifAsync(keyRef);
        if (current.IsFixed())
        {
            OutputHelpers.WriteError("Current touch policy is fixed and cannot be changed without factory reset.");
            return 1;
        }

        // Warn about fixed policies
        if (uif.IsFixed() && !ConfirmAction(
                "set a PERMANENT touch policy that cannot be changed without factory reset", settings.Force))
        {
            OutputHelpers.WriteInfo("Touch policy change cancelled.");
            return 1;
        }

        using var adminPin = GetAdminPin(settings.AdminPin);
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return 1;
        }

        await session.VerifyAdminAsync(adminPin.Memory);
        await session.SetUifAsync(keyRef, uif);

        OutputHelpers.WriteSuccess(
            $"Touch policy for {FormatKeyRef(keyRef)} set to {settings.Policy}.");
        return 0;
    }
}