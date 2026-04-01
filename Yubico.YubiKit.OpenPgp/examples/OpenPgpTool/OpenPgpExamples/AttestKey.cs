// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates getting an attestation certificate for a key slot (firmware 5.2.0+).
/// </summary>
public static class AttestKey
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot to attest:")
                .AddChoices(["Signature", "Decryption", "Authentication"]));

        var keyRef = slot switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            _ => KeyRef.Aut
        };

        try
        {
            var cert = await session.AttestKeyAsync(keyRef, cancellationToken);

            AnsiConsole.MarkupLine("[bold]Attestation Certificate[/]");
            OutputHelpers.WriteKeyValue("Subject", cert.Subject);
            OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
            OutputHelpers.WriteKeyValue("Serial Number", cert.SerialNumber);
            OutputHelpers.WriteKeyValue("Not Before", cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"));
            OutputHelpers.WriteKeyValue("Not After", cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"));
            OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

            if (cert.PublicKey is not null)
            {
                OutputHelpers.WriteKeyValue("Public Key Algorithm", cert.PublicKey.Oid?.FriendlyName ?? "Unknown");
            }

            OutputHelpers.WriteSuccess($"Attestation certificate retrieved for {slot} slot");
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Key attestation requires firmware 5.2.0 or later");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to get attestation: {ex.Message}");
            OutputHelpers.WriteInfo("Make sure a key has been generated in the selected slot");
        }
    }
}
