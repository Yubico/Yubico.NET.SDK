// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Generates an attestation certificate for a key slot (openpgp keys attest).
/// </summary>
/// <remarks>
///     Attestation replaces the target certificate; reading output alone establishes no trusted chain or key binding.
/// </remarks>
public sealed class KeysAttestCommand : AsyncCommand<KeysAttestCommand.Settings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Key.ToLowerInvariant() is not ("sig" or "dec" or "aut"))
        {
            OutputHelpers.WriteError("Attestation requires the sig, dec, or aut slot.");
            return ExitCode.GenericError;
        }

        try
        {
            using var pin = OpenPgpCommand<Settings>.GetPin(settings.Pin);
            if (pin is null)
                return ExitCode.UserCancelled;

            var keyRef = OpenPgpCommand<Settings>.ParseKeyRef(settings.Key);
            return await OpenPgpCommand<Settings>.WithSessionAsync(async session =>
            {
                await session.VerifyPinAsync(pin.Memory);
                using var certificate = await session.AttestKeyAsync(keyRef);
                WriteCertificate(certificate);
                return ExitCode.Success;
            });
        }
        catch (OperationCanceledException)
        {
            return ExitCode.UserCancelled;
        }
    }

    private static void WriteCertificate(X509Certificate2 cert)
    {
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
    }
}