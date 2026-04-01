// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates reading key information, algorithm attributes, and fingerprints.
/// </summary>
public static class GetKeyInfo
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var appData = await session.GetApplicationRelatedDataAsync(cancellationToken);
        var disc = appData.Discretionary;

        var table = OutputHelpers.CreateTable("Slot", "Algorithm", "Status", "Fingerprint");

        KeyRef[] slots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];

        foreach (var slot in slots)
        {
            var attrs = GetAlgorithmAttributes(disc, slot);
            var algString = FormatAlgorithm(attrs);

            var fingerprint = GetFingerprint(disc, slot);
            var fpString = fingerprint.Length > 0 ? Convert.ToHexString(fingerprint.Span) : "(none)";

            // Truncate long fingerprints for table display
            if (fpString.Length > 40)
            {
                fpString = fpString[..40] + "...";
            }

            var status = fingerprint.Length > 0 ? "[green]Present[/]" : "[grey]Empty[/]";

            table.AddRow(
                Markup.Escape(FormatSlotName(slot)),
                Markup.Escape(algString),
                status,
                Markup.Escape(fpString));
        }

        AnsiConsole.Write(table);

        // Try to get key information (status per slot)
        try
        {
            var keyInfo = await session.GetKeyInformationAsync(cancellationToken);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Key Status[/]");

            foreach (var slot in slots)
            {
                var keyStatus = GetKeyStatus(keyInfo, slot);
                var statusColor = keyStatus switch
                {
                    KeyStatus.Generated => "green",
                    KeyStatus.Imported => "blue",
                    _ => "grey"
                };

                OutputHelpers.WriteKeyValueMarkup(
                    FormatSlotName(slot),
                    $"[{statusColor}]{keyStatus}[/]");
            }
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteInfo("Key information requires firmware 5.2.0+");
        }
    }

    private static AlgorithmAttributes? GetAlgorithmAttributes(DiscretionaryDataObjects disc, KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => disc.AlgorithmAttributesSig,
            KeyRef.Dec => disc.AlgorithmAttributesDec,
            KeyRef.Aut => disc.AlgorithmAttributesAut,
            KeyRef.Att => disc.AlgorithmAttributesAtt,
            _ => null
        };

    private static ReadOnlyMemory<byte> GetFingerprint(DiscretionaryDataObjects disc, KeyRef slot) =>
        disc.Fingerprints.TryGetValue(slot, out var fp) ? fp : ReadOnlyMemory<byte>.Empty;

    private static KeyStatus GetKeyStatus(KeyInformation keyInfo, KeyRef slot) =>
        keyInfo.TryGetValue(slot, out var status) ? status : KeyStatus.None;

    private static string FormatSlotName(KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => "Signature",
            KeyRef.Dec => "Decryption",
            KeyRef.Aut => "Authentication",
            KeyRef.Att => "Attestation",
            _ => slot.ToString()
        };

    private static string FormatAlgorithm(AlgorithmAttributes? attrs) =>
        attrs switch
        {
            RsaAttributes rsa => $"RSA {rsa.NLen}",
            EcAttributes ec => $"{FormatEcAlgorithm(ec.AlgorithmId)} {ec.Oid}",
            null => "Unknown",
            _ => $"Algorithm 0x{attrs.AlgorithmId:X2}"
        };

    private static string FormatEcAlgorithm(int algorithmId) =>
        algorithmId switch
        {
            0x12 => "ECDH",
            0x13 => "ECDSA",
            0x16 => "EdDSA",
            _ => "EC"
        };
}
