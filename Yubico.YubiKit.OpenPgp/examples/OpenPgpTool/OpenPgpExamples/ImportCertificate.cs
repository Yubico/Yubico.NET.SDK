// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography.X509Certificates;
using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates importing a certificate to a key slot (firmware 5.2.0+).
/// </summary>
public static class ImportCertificate
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

        var path = AnsiConsole.Ask<string>("Path to certificate file (.cer, .pem, .der):");
        if (!File.Exists(path))
        {
            OutputHelpers.WriteError($"File not found: {path}");
            return;
        }

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadCertificateFromFile(path);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load certificate: {ex.Message}");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Certificate to Import[/]");
        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);
        OutputHelpers.WriteKeyValue("Valid", $"{cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");
        AnsiConsole.WriteLine();

        // Admin PIN required for certificate operations
        var adminPin = OutputHelpers.PromptPin("Admin PIN");

        try
        {
            await session.VerifyAdminAsync(adminPin, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Admin PIN verification failed: {ex.Message}");
            return;
        }

        try
        {
            await session.PutCertificateAsync(keyRef, cert, cancellationToken);
            OutputHelpers.WriteSuccess($"Certificate imported to {slot} slot");
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Certificate storage requires firmware 5.2.0 or later");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to import certificate: {ex.Message}");
        }
    }
}
