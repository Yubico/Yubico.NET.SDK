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
///     Checks firmware 5.6.0, not product eligibility; reset itself requires SmartCard. An advertised HID
///     preference reaches SDK rejection rather than a reset. Prompts twice unless --force is used.
///     Does not establish Management configuration-lock recovery.
/// </summary>
public sealed class ManagementResetCommand : YkCommandBase<ManagementResetSettings>
{
    private static readonly FirmwareVersion MinimumResetVersion = new(5, 6, 0);

    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, ManagementResetSettings settings, YkDeviceContext deviceContext)
    {
        var info = await deviceContext.ReadLiveInfoAsync();
        if (info is null)
        {
            OutputHelpers.WriteError("Could not retrieve device information.");
            return ExitCode.GenericError;
        }

        var deviceInfo = info.Value;

        if (deviceInfo.FirmwareVersion < MinimumResetVersion)
        {
            OutputHelpers.WriteError(
                $"Factory reset requires firmware 5.6.0 or later. Device firmware is {deviceInfo.VersionName}.");
            return ExitCode.FeatureUnsupported;
        }

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "factory reset this YubiKey, erasing ALL credentials, keys, and configuration"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateManagementSessionAsync(
            deviceContext.SessionOptions);
        await session.ResetDeviceAsync();

        OutputHelpers.WriteSuccess("Device has been factory reset.");
        return ExitCode.Success;
    }
}