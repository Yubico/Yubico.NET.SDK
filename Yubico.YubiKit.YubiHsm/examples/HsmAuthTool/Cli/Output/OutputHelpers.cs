// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent output.
/// </summary>
public static class OutputHelpers
{
    public static void WriteHeader(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[green]{Markup.Escape(title)}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    public static void WriteSuccess(string message) =>
        AnsiConsole.MarkupLine($"[green]\u2713[/] {Markup.Escape(message)}");

    public static void WriteError(string message) =>
        AnsiConsole.MarkupLine($"[red]\u2717[/] {Markup.Escape(message)}");

    public static void WriteWarning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]\u26a0[/] {Markup.Escape(message)}");

    public static void WriteInfo(string message) =>
        AnsiConsole.MarkupLine($"[blue]\u2139[/] {Markup.Escape(message)}");

    public static void WriteKeyValue(string key, string? value) =>
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] {Markup.Escape(value ?? "N/A")}");

    public static void WriteHex(string label, ReadOnlySpan<byte> data) =>
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(label)}:[/] {Convert.ToHexString(data)}");

    public static Table CreateTable(params string[] columns)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        foreach (var column in columns)
        {
            table.AddColumn(new TableColumn($"[green]{Markup.Escape(column)}[/]"));
        }

        return table;
    }

    public static bool ConfirmDestructive(string action, string confirmationWord = "RESET")
    {
        AnsiConsole.MarkupLine("[red bold]\u26a0\ufe0f  DANGER \u26a0\ufe0f[/]");
        AnsiConsole.MarkupLine($"[red]This will {Markup.Escape(action)}.[/]");
        AnsiConsole.MarkupLine("[red]ALL DATA WILL BE PERMANENTLY LOST.[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("[red]Are you absolutely sure?[/]", defaultValue: false))
        {
            return false;
        }

        AnsiConsole.WriteLine();
        var input = AnsiConsole.Ask<string>($"Type '[yellow]{confirmationWord}[/]' to confirm:");

        return string.Equals(input, confirmationWord, StringComparison.Ordinal);
    }

    public static void WriteActiveDevice(string deviceDisplayName)
    {
        AnsiConsole.MarkupLine($"[blue]\ud83d\udd11 Using:[/] [cyan]{Markup.Escape(deviceDisplayName)}[/]");
        AnsiConsole.WriteLine();
    }
}
