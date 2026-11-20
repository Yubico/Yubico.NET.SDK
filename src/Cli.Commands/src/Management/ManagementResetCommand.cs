// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.Management;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class ManagementResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompts.")]
    public bool Force { get; init; }
}

/// <summary>
///     Requests Bio Multi-protocol Edition combined FIDO/PIV reset, retaining the F9 attestation slot.
///     Uses SmartCard only. The session checks firmware 5.6.0; the device determines product eligibility.
///     Does not establish Management configuration-lock recovery.
/// </summary>
public sealed class ManagementResetCommand : YkCommandBase<ManagementResetSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, ManagementResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset Bio MPE FIDO and PIV data and the shared PIN, retaining the F9 attestation slot"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateManagementSessionAsync(
            deviceContext.SessionOptions);
        await session.ResetDeviceAsync();

        OutputHelpers.WriteSuccess("Combined FIDO/PIV reset confirmed.");
        return ExitCode.Success;
    }
}