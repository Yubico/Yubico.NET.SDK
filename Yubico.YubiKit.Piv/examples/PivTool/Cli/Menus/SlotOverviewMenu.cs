// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for slot overview display.
/// </summary>
public static class SlotOverviewMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Slot Overview");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);

        await AnsiConsole.Status()
            .StartAsync("Reading slot information...", async ctx =>
            {
                var result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, cancellationToken);

                if (!result.Success)
                {
                    OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to read slots");
                    return;
                }

                DisplaySlotTable(result.Slots);
            });
    }

    private static void DisplaySlotTable(IReadOnlyList<SlotInfo> slots)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Slot")
            .AddColumn("Name")
            .AddColumn("Key")
            .AddColumn("Algorithm")
            .AddColumn("Certificate")
            .AddColumn("Subject");

        foreach (var slot in slots)
        {
            var keyStatus = slot.HasKey ? "[green]Yes[/]" : "[grey]No[/]";
            var certStatus = slot.HasCertificate ? "[green]Yes[/]" : "[grey]No[/]";
            var algorithm = slot.Metadata?.Algorithm.ToString() ?? "-";
            var subject = slot.Certificate?.Subject ?? "-";
            
            // Truncate long subjects
            if (subject.Length > 30)
            {
                subject = subject[..27] + "...";
            }

            table.AddRow(
                $"0x{(byte)slot.Slot:X2}",
                slot.Name,
                keyStatus,
                algorithm,
                certStatus,
                subject);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        OutputHelpers.WriteInfo($"Total slots displayed: {slots.Count}");
    }
}
