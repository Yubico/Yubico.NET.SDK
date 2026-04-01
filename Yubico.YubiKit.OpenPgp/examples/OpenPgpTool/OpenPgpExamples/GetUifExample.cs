// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates reading the User Interaction Flag (touch policy) for all key slots.
/// </summary>
public static class GetUifExample
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var table = OutputHelpers.CreateTable("Slot", "Touch Policy", "Fixed", "Cached");

        KeyRef[] slots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];

        foreach (var slot in slots)
        {
            try
            {
                var uif = await session.GetUifAsync(slot, cancellationToken);
                table.AddRow(
                    Markup.Escape(FormatSlot(slot)),
                    Markup.Escape(uif.ToString()),
                    uif.IsFixed() ? "[yellow]Yes[/]" : "[grey]No[/]",
                    uif.IsCached() ? "[blue]Yes[/]" : "[grey]No[/]");
            }
            catch (NotSupportedException)
            {
                table.AddRow(
                    Markup.Escape(FormatSlot(slot)),
                    "[grey]Not supported[/]",
                    "[grey]-[/]",
                    "[grey]-[/]");
            }
        }

        AnsiConsole.Write(table);
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
}
