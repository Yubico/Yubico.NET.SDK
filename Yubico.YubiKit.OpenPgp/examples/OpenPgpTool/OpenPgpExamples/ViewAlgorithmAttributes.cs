// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates viewing algorithm attributes and supported algorithms.
/// </summary>
public static class ViewAlgorithmAttributes
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        // Current algorithm attributes per slot
        AnsiConsole.MarkupLine("[bold]Current Algorithm Attributes[/]");
        AnsiConsole.WriteLine();

        KeyRef[] slots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];

        foreach (var slot in slots)
        {
            try
            {
                var attrs = await session.GetAlgorithmAttributesAsync(slot, cancellationToken);
                OutputHelpers.WriteKeyValue(FormatSlot(slot), FormatAttributes(attrs));
            }
            catch
            {
                OutputHelpers.WriteKeyValue(FormatSlot(slot), "Unable to read");
            }
        }

        // Supported algorithms (firmware 5.2.0+)
        AnsiConsole.WriteLine();
        try
        {
            var supported = await session.GetAlgorithmInformationAsync(cancellationToken);

            AnsiConsole.MarkupLine("[bold]Supported Algorithms[/]");
            AnsiConsole.WriteLine();

            var table = OutputHelpers.CreateTable("Slot", "Algorithm", "Details");

            foreach (var (keyRef, attrs) in supported)
            {
                table.AddRow(
                    Markup.Escape(FormatSlot(keyRef)),
                    Markup.Escape(FormatAlgorithmType(attrs)),
                    Markup.Escape(FormatAttributes(attrs)));
            }

            AnsiConsole.Write(table);
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteInfo("Supported algorithm list requires firmware 5.2.0+");
        }
    }

    private static string FormatSlot(KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => "Signature",
            KeyRef.Dec => "Decryption",
            KeyRef.Aut => "Authentication",
            KeyRef.Att => "Attestation",
            _ => slot.ToString()
        };

    private static string FormatAlgorithmType(AlgorithmAttributes attrs) =>
        attrs switch
        {
            RsaAttributes => "RSA",
            EcAttributes ec => ec.AlgorithmId switch
            {
                0x12 => "ECDH",
                0x13 => "ECDSA",
                0x16 => "EdDSA",
                _ => "EC"
            },
            _ => "Unknown"
        };

    private static string FormatAttributes(AlgorithmAttributes attrs) =>
        attrs switch
        {
            RsaAttributes rsa => $"RSA {rsa.NLen}-bit, e={rsa.ELen}-bit, import={rsa.ImportFormat}",
            EcAttributes ec => $"{ec.Oid} ({ec.Oid.ToDottedString()}), import={ec.ImportFormat}",
            _ => $"Algorithm 0x{attrs.AlgorithmId:X2}"
        };
}
