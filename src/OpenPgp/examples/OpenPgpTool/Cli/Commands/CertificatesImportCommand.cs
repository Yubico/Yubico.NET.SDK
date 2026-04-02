// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Imports a certificate into a key slot (openpgp certificates import).
/// </summary>
public sealed class CertificatesImportCommand : OpenPgpCommand<CertificatesImportCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut, att).")]
        public string Key { get; init; } = "";

        [CommandArgument(1, "<CERT_FILE>")]
        [Description("Path to certificate file (.pem, .cer, .der).")]
        public string CertFile { get; init; } = "";

        [CommandOption("--admin-pin <PIN>")]
        [Description("Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var keyRef = ParseKeyRef(settings.Key);

        if (!File.Exists(settings.CertFile))
        {
            OutputHelpers.WriteError($"File not found: {settings.CertFile}");
            return 1;
        }

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadCertificateFromFile(settings.CertFile);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load certificate: {ex.Message}");
            return 1;
        }

        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        await session.VerifyAdminAsync(adminPin);
        await session.PutCertificateAsync(keyRef, cert);

        OutputHelpers.WriteSuccess(
            $"Certificate imported to {FormatKeyRef(keyRef)} slot.");
        return 0;
    }
}