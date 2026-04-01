// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.ComponentModel;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Deletes a certificate from a key slot (openpgp certificates delete).
/// </summary>
public sealed class CertificatesDeleteCommand : OpenPgpCommand<CertificatesDeleteCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut, att).")]
        public string Key { get; init; } = "";

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

        if (!ConfirmAction(
                $"delete the certificate from {FormatKeyRef(keyRef)}", settings.Force))
        {
            OutputHelpers.WriteInfo("Certificate deletion cancelled.");
            return 1;
        }

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        await session.VerifyAdminAsync(adminPin);
        await session.DeleteCertificateAsync(keyRef);

        OutputHelpers.WriteSuccess(
            $"Certificate deleted from {FormatKeyRef(keyRef)} slot.");
        return 0;
    }
}