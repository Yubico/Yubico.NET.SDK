// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates exporting a certificate from a key slot (firmware 5.2.0+).
/// </summary>
public static class ExportCertificate
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot:")
                .AddChoices(["Signature", "Decryption", "Authentication", "Attestation"]));

        var keyRef = slot switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            "Authentication" => KeyRef.Aut,
            _ => KeyRef.Att
        };

        try
        {
            var cert = await session.GetCertificateAsync(keyRef, cancellationToken);

            if (cert is null)
            {
                OutputHelpers.WriteInfo($"No certificate stored in {slot} slot");
                return;
            }

            AnsiConsole.MarkupLine("[bold]Certificate[/]");
            OutputHelpers.WriteKeyValue("Subject", cert.Subject);
            OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
            OutputHelpers.WriteKeyValue("Serial Number", cert.SerialNumber);
            OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);
            OutputHelpers.WriteKeyValue("Valid", $"{cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");

            if (cert.PublicKey is not null)
            {
                OutputHelpers.WriteKeyValue("Public Key Algorithm", cert.PublicKey.Oid?.FriendlyName ?? "Unknown");
            }

            AnsiConsole.WriteLine();

            if (AnsiConsole.Confirm("Save certificate to file?", defaultValue: false))
            {
                var outPath = AnsiConsole.Ask<string>("Output file path (.cer):");
                var certBytes = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
                await File.WriteAllBytesAsync(outPath, certBytes, cancellationToken);
                OutputHelpers.WriteSuccess($"Certificate saved to {outPath}");
            }
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Certificate storage requires firmware 5.2.0 or later");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to read certificate: {ex.Message}");
        }
    }
}
