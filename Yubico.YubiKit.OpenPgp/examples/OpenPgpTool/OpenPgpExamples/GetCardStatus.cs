// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates reading the OpenPGP card status including AID, version, and PIN status.
/// </summary>
public static class GetCardStatus
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var appData = await session.GetApplicationRelatedDataAsync(cancellationToken);

        // AID information
        AnsiConsole.MarkupLine("[bold]Application ID[/]");
        OutputHelpers.WriteKeyValue("AID Version", $"{appData.Aid.Version.Major}.{appData.Aid.Version.Minor}");
        OutputHelpers.WriteKeyValue("Manufacturer", $"0x{appData.Aid.Manufacturer:X4}");
        OutputHelpers.WriteKeyValue("Serial Number", appData.Aid.Serial.ToString());
        AnsiConsole.WriteLine();

        // Historical bytes
        if (appData.HistoricalBytes.Length > 0)
        {
            OutputHelpers.WriteHex("Historical Bytes", appData.HistoricalBytes.Span);
            AnsiConsole.WriteLine();
        }

        // PIN status
        var pinStatus = await session.GetPinStatusAsync(cancellationToken);

        AnsiConsole.MarkupLine("[bold]PIN Status[/]");
        OutputHelpers.WriteKeyValue("Signature PIN Policy",
            pinStatus.SignaturePinPolicy == PinPolicy.Always ? "Verify every signature" : "Verify once per session");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Maximum PIN Lengths[/]");
        OutputHelpers.WriteKeyValue("User PIN", pinStatus.MaxLenUser.ToString());
        OutputHelpers.WriteKeyValue("Reset Code", pinStatus.MaxLenReset.ToString());
        OutputHelpers.WriteKeyValue("Admin PIN", pinStatus.MaxLenAdmin.ToString());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Remaining Attempts[/]");
        WriteAttempts("User PIN", pinStatus.AttemptsUser);
        WriteAttempts("Reset Code", pinStatus.AttemptsReset);
        WriteAttempts("Admin PIN", pinStatus.AttemptsAdmin);
        AnsiConsole.WriteLine();

        // Signature counter
        var sigCount = await session.GetSignatureCounterAsync(cancellationToken);
        OutputHelpers.WriteKeyValue("Signature Counter", sigCount.ToString());

        // Extended capabilities
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Extended Capabilities[/]");
        var extCap = appData.Discretionary.ExtendedCapabilities;
        OutputHelpers.WriteBoolValue("Secure Messaging",
            (extCap.Flags & ExtendedCapabilityFlags.SecureMessaging) != 0);
        OutputHelpers.WriteBoolValue("Key Import",
            (extCap.Flags & ExtendedCapabilityFlags.KeyImport) != 0);
        OutputHelpers.WriteBoolValue("Algorithm Attrs Changeable",
            (extCap.Flags & ExtendedCapabilityFlags.AlgorithmAttributesChangeable) != 0);
        OutputHelpers.WriteBoolValue("KDF Supported",
            (extCap.Flags & ExtendedCapabilityFlags.Kdf) != 0);
        OutputHelpers.WriteKeyValue("Max Certificate Length", extCap.CertificateMaxLength.ToString());
    }

    private static void WriteAttempts(string label, int attempts)
    {
        var color = attempts switch
        {
            0 => "red",
            1 => "yellow",
            _ => "green"
        };

        OutputHelpers.WriteKeyValueMarkup(label, $"[{color}]{attempts}[/]");
    }
}
