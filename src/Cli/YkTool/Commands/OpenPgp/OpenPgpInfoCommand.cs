// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp;

namespace Yubico.YubiKit.Cli.YkTool.Commands.OpenPgp;

public sealed class OpenPgpInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var appData = await session.GetApplicationRelatedDataAsync();

        OutputHelpers.WriteHeader("OpenPGP Application");
        OutputHelpers.WriteKeyValue("AID Version",
            $"{appData.Aid.Version.Major}.{appData.Aid.Version.Minor}");
        OutputHelpers.WriteKeyValue("Manufacturer", $"0x{appData.Aid.Manufacturer:X4}");
        OutputHelpers.WriteKeyValue("Serial Number", appData.Aid.Serial.ToString());

        if (appData.HistoricalBytes.Length > 0)
        {
            OutputHelpers.WriteHex("Historical Bytes", appData.HistoricalBytes.Span);
        }

        var pinStatus = await session.GetPinStatusAsync();

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("PIN Status");
        OutputHelpers.WriteKeyValue("Signature PIN Policy",
            pinStatus.SignaturePinPolicy == PinPolicy.Always
                ? "Verify every signature"
                : "Verify once per session");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Remaining Attempts[/]");
        WriteAttempts("User PIN", pinStatus.AttemptsUser);
        WriteAttempts("Reset Code", pinStatus.AttemptsReset);
        WriteAttempts("Admin PIN", pinStatus.AttemptsAdmin);

        var sigCount = await session.GetSignatureCounterAsync();
        AnsiConsole.WriteLine();
        OutputHelpers.WriteKeyValue("Signature Counter", sigCount.ToString());

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Keys");

        var disc = appData.Discretionary;
        var table = OutputHelpers.CreateTable("Slot", "Algorithm", "Status", "Fingerprint");

        KeyRef[] slots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];
        foreach (var slot in slots)
        {
            var attrs = GetAlgorithmAttributes(disc, slot);
            var algString = FormatAlgorithm(attrs);
            var fingerprint = GetFingerprint(disc, slot);
            var fpString = fingerprint.Length > 0 ? Convert.ToHexString(fingerprint.Span) : "(none)";

            if (fpString.Length > 40)
            {
                fpString = fpString[..40] + "...";
            }

            var status = fingerprint.Length > 0 ? "[green]Present[/]" : "[grey]Empty[/]";
            table.AddRow(
                Markup.Escape(FormatSlotShort(slot)),
                Markup.Escape(algString),
                status,
                Markup.Escape(fpString));
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Touch Policies");
        foreach (var slot in slots)
        {
            try
            {
                var uif = await session.GetUifAsync(slot);
                var uifStr = uif switch
                {
                    Uif.Off => "[grey]Off[/]",
                    Uif.On => "[green]On[/]",
                    Uif.Fixed => "[yellow]On (fixed)[/]",
                    Uif.Cached => "[blue]Cached[/]",
                    Uif.CachedFixed => "[yellow]Cached (fixed)[/]",
                    _ => "[grey]Unknown[/]"
                };
                OutputHelpers.WriteKeyValueMarkup(FormatSlotShort(slot), uifStr);
            }
            catch (NotSupportedException)
            {
                OutputHelpers.WriteKeyValueMarkup(FormatSlotShort(slot), "[grey]N/A[/]");
            }
        }

        return ExitCode.Success;
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

    private static string FormatSlotShort(KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => "SIG",
            KeyRef.Dec => "DEC",
            KeyRef.Aut => "AUT",
            KeyRef.Att => "ATT",
            _ => slot.ToString()
        };

    private static AlgorithmAttributes? GetAlgorithmAttributes(
        DiscretionaryDataObjects disc, KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => disc.AlgorithmAttributesSig,
            KeyRef.Dec => disc.AlgorithmAttributesDec,
            KeyRef.Aut => disc.AlgorithmAttributesAut,
            KeyRef.Att => disc.AlgorithmAttributesAtt,
            _ => null
        };

    private static ReadOnlyMemory<byte> GetFingerprint(
        DiscretionaryDataObjects disc, KeyRef slot) =>
        disc.Fingerprints.TryGetValue(slot, out var fp) ? fp : ReadOnlyMemory<byte>.Empty;

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
