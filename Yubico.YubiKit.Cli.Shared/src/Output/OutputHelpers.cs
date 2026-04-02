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

namespace Yubico.YubiKit.Cli.Shared.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent CLI output.
/// Shared across all CLI tools that use Spectre.Console for rich terminal output.
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
        AnsiConsole.MarkupLine($"[green]\u2713[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]\u2717[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]\u26a0[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Displays an info message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]\u2139[/] {Markup.Escape(message)}");
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
    /// Displays bytes as a hex string from a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public static void WriteHex(string label, ReadOnlyMemory<byte> data)
    {
        WriteHex(label, data.Span);
    }

    /// <summary>
    /// Creates a panel with a title.
    /// </summary>
    public static Panel CreatePanel(string title, string content) =>
        new Panel(content)
            .Header($"[green]{Markup.Escape(title)}[/]")
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

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
    /// Displays the currently active device with serial number.
    /// </summary>
    public static void WriteActiveDevice(string deviceDisplayName)
    {
        AnsiConsole.MarkupLine($"[blue]\ud83d\udd11 Using:[/] [cyan]{Markup.Escape(deviceDisplayName)}[/]");
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
    /// Displays a touch prompt for operations requiring user presence.
    /// </summary>
    public static void PromptForTouch()
    {
        AnsiConsole.MarkupLine("[yellow]Touch your YubiKey now...[/]");
    }

    /// <summary>
    /// Waits for the user to press any key.
    /// </summary>
    public static void WaitForKey()
    {
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}