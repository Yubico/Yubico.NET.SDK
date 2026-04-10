// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp;
using static Yubico.YubiKit.Cli.YkTool.Commands.OpenPgp.OpenPgpHelpers;

namespace Yubico.YubiKit.Cli.YkTool.Commands.OpenPgp;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class CertificatesExportSettings : GlobalSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Key slot (sig, dec, aut, att).")]
    public string Key { get; init; } = "";

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: PEM or DER (default: PEM).")]
    [DefaultValue("PEM")]
    public string Format { get; init; } = "PEM";
}

public sealed class CertificatesImportSettings : GlobalSettings
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

public sealed class CertificatesDeleteSettings : GlobalSettings
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

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class OpenPgpCertificatesExportCommand : YkCommandBase<CertificatesExportSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, CertificatesExportSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);
        var cert = await session.GetCertificateAsync(keyRef);

        if (cert is null)
        {
            OutputHelpers.WriteError($"No certificate stored in {FormatKeyRef(keyRef)} slot.");
            return ExitCode.GenericError;
        }

        var format = settings.Format.ToUpperInvariant();
        switch (format)
        {
            case "PEM":
            {
                var pem = PemEncoding.Write("CERTIFICATE", cert.RawData);
                AnsiConsole.WriteLine(new string(pem));
                break;
            }
            case "DER":
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(cert.RawData);
                break;
            }
            default:
                OutputHelpers.WriteError($"Unsupported format: {settings.Format}. Use PEM or DER.");
                return ExitCode.GenericError;
        }

        return ExitCode.Success;
    }
}

public sealed class OpenPgpCertificatesImportCommand : YkCommandBase<CertificatesImportSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, CertificatesImportSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);

        if (!File.Exists(settings.CertFile))
        {
            OutputHelpers.WriteError($"File not found: {settings.CertFile}");
            return ExitCode.GenericError;
        }

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadCertificateFromFile(settings.CertFile);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load certificate: {ex.Message}");
            return ExitCode.GenericError;
        }

        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        byte[] adminPinBytes = Encoding.UTF8.GetBytes(adminPin);
        try
        {
            await session.VerifyAdminAsync(adminPinBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(adminPinBytes);
        }

        await session.PutCertificateAsync(keyRef, cert);

        OutputHelpers.WriteSuccess($"Certificate imported to {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpCertificatesDeleteCommand : YkCommandBase<CertificatesDeleteSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, CertificatesDeleteSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);

        if (!ConfirmAction($"delete the certificate from {FormatKeyRef(keyRef)}", settings.Force))
        {
            OutputHelpers.WriteInfo("Certificate deletion cancelled.");
            return ExitCode.UserCancelled;
        }

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        byte[] adminPinBytes = Encoding.UTF8.GetBytes(adminPin);
        try
        {
            await session.VerifyAdminAsync(adminPinBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(adminPinBytes);
        }

        await session.DeleteCertificateAsync(keyRef);

        OutputHelpers.WriteSuccess($"Certificate deleted from {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
    }
}
