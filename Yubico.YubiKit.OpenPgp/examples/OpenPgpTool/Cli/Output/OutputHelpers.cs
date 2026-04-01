// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent output.
/// </summary>
public static class OutputHelpers
{
    /// <summary>
    /// Displays a section header with a rule.
    /// </summary>
    public static void WriteHeader(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[green]{Markup.Escape(title)}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a success message.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]v[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]x[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]![/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an info message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]i[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays a key-value pair.
    /// </summary>
    public static void WriteKeyValue(string key, string? value)
    {
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] {Markup.Escape(value ?? "N/A")}");
    }

    /// <summary>
    /// Displays a key-value pair with markup in the value.
    /// </summary>
    public static void WriteKeyValueMarkup(string key, string valueMarkup)
    {
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] {valueMarkup}");
    }

    /// <summary>
    /// Displays bytes as a hex string.
    /// </summary>
    public static void WriteHex(string label, ReadOnlySpan<byte> data)
    {
        var hex = data.Length > 0 ? Convert.ToHexString(data) : "(empty)";
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(label)}:[/] {Markup.Escape(hex)}");
    }

    /// <summary>
    /// Creates a table with standard styling.
    /// </summary>
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

    /// <summary>
    /// Displays a confirmation prompt with clear warning styling.
    /// </summary>
    public static bool ConfirmDangerous(string action)
    {
        AnsiConsole.MarkupLine($"[red bold]WARNING:[/] This will {Markup.Escape(action)}.");
        AnsiConsole.MarkupLine("[red]This action cannot be undone.[/]");
        AnsiConsole.WriteLine();

        return AnsiConsole.Confirm("[red]Are you sure you want to proceed?[/]", defaultValue: false);
    }

    /// <summary>
    /// Displays a double-confirmation for extremely dangerous operations.
    /// </summary>
    public static bool ConfirmDestructive(string action, string confirmationWord = "RESET")
    {
        AnsiConsole.MarkupLine("[red bold]DANGER[/]");
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

    /// <summary>
    /// Displays the currently active device.
    /// </summary>
    public static void WriteActiveDevice(string deviceDisplayName)
    {
        AnsiConsole.MarkupLine($"[blue]Using:[/] [cyan]{Markup.Escape(deviceDisplayName)}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a boolean value with color coding.
    /// </summary>
    public static void WriteBoolValue(string label, bool value, string? trueText = null, string? falseText = null)
    {
        var text = value ? (trueText ?? "Yes") : (falseText ?? "No");
        var color = value ? "green" : "grey";
        WriteKeyValueMarkup(label, $"[{color}]{Markup.Escape(text)}[/]");
    }

    /// <summary>
    /// Prompts for a PIN with masked input.
    /// </summary>
    public static string PromptPin(string label)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>($"[green]{Markup.Escape(label)}:[/]")
                .Secret());
    }
}
