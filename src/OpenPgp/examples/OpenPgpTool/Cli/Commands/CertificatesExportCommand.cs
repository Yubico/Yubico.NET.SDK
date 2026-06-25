// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Exports a certificate from a key slot (openpgp certificates export).
/// </summary>
public sealed class CertificatesExportCommand : OpenPgpCommand<CertificatesExportCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut, att).")]
        public string Key { get; init; } = "";

        [CommandOption("--format <FORMAT>")]
        [Description("Output format: PEM or DER (default: PEM).")]
        [DefaultValue("PEM")]
        public string Format { get; init; } = "PEM";
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var keyRef = ParseKeyRef(settings.Key);
        var cert = await session.GetCertificateAsync(keyRef);

        if (cert is null)
        {
            OutputHelpers.WriteError(
                $"No certificate stored in {FormatKeyRef(keyRef)} slot.");
            return 1;
        }

        var format = settings.Format.ToUpperInvariant();
        switch (format)
        {
            case "PEM":
                {
                    var pem = PemEncoding.Write("CERTIFICATE", cert.RawData);
                    Console.WriteLine(new string(pem));
                    break;
                }

            case "DER":
                {
                    using var stdout = Console.OpenStandardOutput();
                    stdout.Write(cert.RawData);
                    break;
                }

            default:
                OutputHelpers.WriteError(
                    $"Unsupported format: {settings.Format}. Use PEM or DER.");
                return 1;
        }

        return 0;
    }
}