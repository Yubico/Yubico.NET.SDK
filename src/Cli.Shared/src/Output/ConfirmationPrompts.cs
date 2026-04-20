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
/// Provides confirmation prompts for dangerous and destructive operations.
/// Shared across all CLI tools that need user confirmation before irreversible actions.
/// </summary>
public static class ConfirmationPrompts
{
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
    /// Displays a double-confirmation for extremely dangerous operations (e.g., factory reset).
    /// Requires the user to type a confirmation word to proceed.
    /// </summary>
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
}