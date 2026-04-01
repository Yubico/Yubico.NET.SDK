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

using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>
/// Provides output formatting for both Spectre.Console (interactive) and JSON (CI) modes.
/// </summary>
public static class OutputHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Writes a section header.
    /// </summary>
    public static void WriteHeader(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[green]{Markup.Escape(title)}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes a success message.
    /// </summary>
    public static void WriteSuccess(string message) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public static void WriteError(string message) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(message)}");

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public static void WriteWarning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {Markup.Escape(message)}");

    /// <summary>
    /// Writes a key-value pair.
    /// </summary>
    public static void WriteKeyValue(string key, string? value) =>
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] {Markup.Escape(value ?? "N/A")}");

    /// <summary>
    /// Writes a key-value pair with color-coded boolean.
    /// </summary>
    public static void WriteBool(string key, bool value)
    {
        var color = value ? "green" : "grey";
        var text = value ? "Yes" : "No";
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] [{color}]{text}[/]");
    }

    /// <summary>
    /// Writes a hex-encoded byte array.
    /// </summary>
    public static void WriteHex(string label, ReadOnlySpan<byte> data) =>
        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(label)}:[/] {Convert.ToHexString(data)}");

    /// <summary>
    /// Serializes an object to JSON and writes to stdout.
    /// </summary>
    public static void WriteJson(object value) =>
        Console.WriteLine(JsonSerializer.Serialize(value, JsonOptions));

    /// <summary>
    /// Writes an error as JSON to stderr.
    /// </summary>
    public static void WriteJsonError(string message)
    {
        var error = new { error = message };
        Console.Error.WriteLine(JsonSerializer.Serialize(error, JsonOptions));
    }

    /// <summary>
    /// Prompts user to select a slot interactively.
    /// </summary>
    public static Slot PromptSlot()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select slot:")
                .AddChoices(["Slot 1", "Slot 2"]));

        return choice == "Slot 1" ? Slot.One : Slot.Two;
    }

    /// <summary>
    /// Prompts for hex input interactively.
    /// </summary>
    public static byte[]? PromptHex(string label, bool required = true)
    {
        var prompt = new TextPrompt<string>($"[grey]{Markup.Escape(label)} (hex):[/]");
        if (!required)
        {
            prompt.AllowEmpty();
        }

        var input = AnsiConsole.Prompt(prompt);
        if (string.IsNullOrWhiteSpace(input))
        {
            return required ? null : [];
        }

        try
        {
            return Convert.FromHexString(input);
        }
        catch (FormatException)
        {
            WriteError("Invalid hex input.");
            return null;
        }
    }

    /// <summary>
    /// Parses a hex string to bytes, with error reporting.
    /// </summary>
    public static byte[]? ParseHex(string? hex, string paramName)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Invalid hex value for {paramName}: {hex}");
        }
    }
}
