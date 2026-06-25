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
/// Provides PIN/password prompts with masked input for CLI tools.
/// </summary>
public static class PinPrompt
{
    /// <summary>
    /// Prompts the user for a PIN interactively with masked input.
    /// Used when --pin is not provided on the command line.
    /// </summary>
    public static string PromptForPin(string label = "PIN") =>
        AnsiConsole.Prompt(
            new TextPrompt<string>($"{label}:")
                .Secret());
}