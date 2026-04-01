// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Generates an attestation certificate for a key slot (openpgp keys attest).
/// </summary>
public sealed class KeysAttestCommand : OpenPgpCommand<KeysAttestCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut).")]
        public string Key { get; init; } = "";

        [CommandOption("--pin <PIN>")]
        [Description("User PIN (prompted if not provided).")]
        public string? Pin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var keyRef = ParseKeyRef(settings.Key);

        var cert = await session.AttestKeyAsync(keyRef);

        AnsiConsole.MarkupLine("[bold]Attestation Certificate[/]");
        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Serial Number", cert.SerialNumber);
        OutputHelpers.WriteKeyValue("Not Before",
            cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"));
        OutputHelpers.WriteKeyValue("Not After",
            cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"));
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

        if (cert.PublicKey is not null)
        {
            OutputHelpers.WriteKeyValue("Public Key Algorithm",
                cert.PublicKey.Oid?.FriendlyName ?? "Unknown");
        }

        // Output PEM to stdout
        AnsiConsole.WriteLine();
        var pem = PemEncoding.Write("CERTIFICATE", cert.RawData);
        AnsiConsole.WriteLine(new string(pem));

        return 0;
    }
}