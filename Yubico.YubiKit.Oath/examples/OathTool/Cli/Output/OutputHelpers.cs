// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Spectre.Console;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

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
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an info message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(message)}");
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
    /// Creates a panel with a title.
    /// </summary>
    public static Panel CreatePanel(string title, string content)
    {
        return new Panel(content)
            .Header($"[green]{Markup.Escape(title)}[/]")
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);
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
        AnsiConsole.MarkupLine("[red bold]⚠️  DANGER ⚠️[/]");
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
        AnsiConsole.MarkupLine($"[blue]🔑 Using:[/] [cyan]{Markup.Escape(deviceDisplayName)}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a credential in a formatted row.
    /// </summary>
    public static string FormatCredentialName(string? issuer, string name)
    {
        return issuer is not null ? $"{issuer}:{name}" : name;
    }
}
