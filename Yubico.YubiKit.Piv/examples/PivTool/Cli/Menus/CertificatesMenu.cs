// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for certificate operations.
/// </summary>
public static class CertificatesMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Certificate Operations");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "View Certificate",
                    "Export Certificate (PEM)",
                    "Import Certificate",
                    "Generate Self-Signed Certificate",
                    "Generate CSR",
                    "Delete Certificate",
                    "Back"
                ]));

        if (choice == "Back")
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);

        switch (choice)
        {
            case "View Certificate":
                await ViewCertificateAsync(session, cancellationToken);
                break;
            case "Export Certificate (PEM)":
                await ExportCertificateAsync(session, cancellationToken);
                break;
            case "Import Certificate":
                await ImportCertificateAsync(session, cancellationToken);
                break;
            case "Generate Self-Signed Certificate":
                await GenerateSelfSignedAsync(session, cancellationToken);
                break;
            case "Generate CSR":
                await GenerateCsrAsync(session, cancellationToken);
                break;
            case "Delete Certificate":
                await DeleteCertificateAsync(session, cancellationToken);
                break;
        }
    }

    private static async Task ViewCertificateAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot:");

        var result = await Certificates.GetCertificateAsync(session, slot, ct);
        if (!result.Success)
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to read certificate");
            return;
        }

        if (result.Certificate is not null)
        {
            OutputHelpers.WriteSuccess("Certificate found:");
            OutputHelpers.WriteKeyValue("Subject", result.Certificate.Subject);
            OutputHelpers.WriteKeyValue("Issuer", result.Certificate.Issuer);
            OutputHelpers.WriteKeyValue("Serial", result.Certificate.SerialNumber);
            OutputHelpers.WriteKeyValue("Not Before", result.Certificate.NotBefore.ToString("yyyy-MM-dd"));
            OutputHelpers.WriteKeyValue("Not After", result.Certificate.NotAfter.ToString("yyyy-MM-dd"));
            OutputHelpers.WriteKeyValue("Thumbprint", result.Certificate.Thumbprint);
        }
    }

    private static async Task ExportCertificateAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot:");

        var result = await Certificates.ExportCertificatePemAsync(session, slot, ct);

        if (!result.Success || result.CsrPem is null)
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Export failed");
            return;
        }

        var filename = AnsiConsole.Ask<string>("Enter filename to save:");
        await File.WriteAllTextAsync(filename, result.CsrPem, ct);
        OutputHelpers.WriteSuccess($"Certificate exported to {filename}");
    }

    private static async Task ImportCertificateAsync(IPivSession session, CancellationToken ct)
    {
        // Authenticate first
        using var mgmtKey = PinPrompt.GetManagementKeyWithDefault("Management key");
        if (mgmtKey is null)
        {
            return;
        }

        var authResult = await PinManagement.AuthenticateAsync(session, mgmtKey.Memory.Span.ToArray(), ct);
        if (!authResult.Success)
        {
            OutputHelpers.WriteError(authResult.ErrorMessage ?? "Failed to authenticate");
            return;
        }

        var slot = SlotSelector.SelectSlot("Select slot:");

        var filename = AnsiConsole.Ask<string>("Enter certificate file path:");
        if (!File.Exists(filename))
        {
            OutputHelpers.WriteError("File not found");
            return;
        }

        var certData = await File.ReadAllBytesAsync(filename, ct);
        var result = await Certificates.ImportCertificateAsync(session, slot, certData, false, ct);

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Certificate imported successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Import failed");
        }
    }

    private static async Task GenerateSelfSignedAsync(IPivSession session, CancellationToken ct)
    {
        // Authenticate management key
        using var mgmtKey = PinPrompt.GetManagementKeyWithDefault("Management key");
        if (mgmtKey is null)
        {
            return;
        }

        var authResult = await PinManagement.AuthenticateAsync(session, mgmtKey.Memory.Span.ToArray(), ct);
        if (!authResult.Success)
        {
            OutputHelpers.WriteError(authResult.ErrorMessage ?? "Failed to authenticate");
            return;
        }

        // Verify PIN (required for signing operations)
        using var pin = PinPrompt.GetPinWithDefault("PIN");
        if (pin is null)
        {
            return;
        }

        var pinResult = await PinManagement.VerifyPinAsync(session, pin.Memory.Span.ToArray(), ct);
        if (!pinResult.Success)
        {
            OutputHelpers.WriteError(pinResult.ErrorMessage ?? "PIN verification failed");
            return;
        }

        var slot = SlotSelector.SelectSlot("Select slot (must have existing key):");
        var subject = AnsiConsole.Ask("Enter subject (e.g., CN=Test User):", "CN=Test User");
        var validDays = AnsiConsole.Ask("Enter validity in days:", 365);

        var result = await Certificates.GenerateSelfSignedAsync(session, slot, subject, validDays, ct);

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Self-signed certificate generated and stored");
            if (result.Certificate is not null)
            {
                OutputHelpers.WriteKeyValue("Subject", result.Certificate.Subject);
                OutputHelpers.WriteKeyValue("Valid Until", result.Certificate.NotAfter.ToString("yyyy-MM-dd"));
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Generation failed");
        }
    }

    private static async Task GenerateCsrAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot (must have existing key):");
        var subject = AnsiConsole.Ask("Enter subject (e.g., CN=Test User):", "CN=Test User");

        var result = await Certificates.GenerateCsrAsync(session, slot, subject, ct);

        if (result.Success && result.CsrPem is not null)
        {
            OutputHelpers.WriteSuccess("CSR generated:");
            AnsiConsole.WriteLine(result.CsrPem);

            if (AnsiConsole.Confirm("Save to file?", defaultValue: false))
            {
                var filename = AnsiConsole.Ask<string>("Enter filename:");
                await File.WriteAllTextAsync(filename, result.CsrPem, ct);
                OutputHelpers.WriteSuccess($"CSR saved to {filename}");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "CSR generation failed");
        }
    }

    private static async Task DeleteCertificateAsync(IPivSession session, CancellationToken ct)
    {
        // Authenticate first
        using var mgmtKey = PinPrompt.GetManagementKeyWithDefault("Management key");
        if (mgmtKey is null)
        {
            return;
        }

        var authResult = await PinManagement.AuthenticateAsync(session, mgmtKey.Memory.Span.ToArray(), ct);
        if (!authResult.Success)
        {
            OutputHelpers.WriteError(authResult.ErrorMessage ?? "Failed to authenticate");
            return;
        }

        var slot = SlotSelector.SelectSlot("Select slot:");

        if (!AnsiConsole.Confirm($"Delete certificate from slot {slot}?", defaultValue: false))
        {
            return;
        }

        var result = await Certificates.DeleteCertificateAsync(session, slot, ct);

        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Certificate deleted");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Delete failed");
        }
    }
}
