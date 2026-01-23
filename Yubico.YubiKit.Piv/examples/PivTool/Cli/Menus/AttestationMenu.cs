// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for key attestation operations.
/// </summary>
public static class AttestationMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Key Attestation");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        var slot = SlotSelector.SelectSlot("Select slot to attest:");

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);

        await AnsiConsole.Status()
            .StartAsync("Getting attestation...", async ctx =>
            {
                var result = await Attestation.GetAttestationAsync(session, slot, cancellationToken);

                if (!result.Success)
                {
                    OutputHelpers.WriteError(result.ErrorMessage ?? "Attestation failed");
                    return;
                }

                OutputHelpers.WriteSuccess("Attestation certificate retrieved");

                if (result.AttestationCertificate is not null)
                {
                    AnsiConsole.WriteLine();
                    OutputHelpers.WriteInfo("Attestation Certificate:");
                    OutputHelpers.WriteKeyValue("Subject", result.AttestationCertificate.Subject);
                    OutputHelpers.WriteKeyValue("Issuer", result.AttestationCertificate.Issuer);
                    OutputHelpers.WriteKeyValue("Serial", result.AttestationCertificate.SerialNumber);
                    OutputHelpers.WriteKeyValue("Thumbprint", result.AttestationCertificate.Thumbprint);
                }

                if (result.IntermediateCertificate is not null)
                {
                    AnsiConsole.WriteLine();
                    OutputHelpers.WriteInfo("Intermediate Certificate:");
                    OutputHelpers.WriteKeyValue("Subject", result.IntermediateCertificate.Subject);
                    OutputHelpers.WriteKeyValue("Issuer", result.IntermediateCertificate.Issuer);
                }
            });

        // Offer to export
        if (AnsiConsole.Confirm("Export attestation certificate?", defaultValue: false))
        {
            var result = await Attestation.GetAttestationAsync(session, slot, cancellationToken);
            if (result.Success && result.AttestationCertificate is not null)
            {
                var filename = AnsiConsole.Ask<string>("Enter filename:");
                var pem = result.AttestationCertificate.ExportCertificatePem();
                await File.WriteAllTextAsync(filename, pem, cancellationToken);
                OutputHelpers.WriteSuccess($"Certificate exported to {filename}");
            }
        }
    }
}
