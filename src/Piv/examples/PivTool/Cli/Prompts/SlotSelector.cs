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

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;

/// <summary>
/// Provides PIV slot selection prompts.
/// </summary>
public static class SlotSelector
{
    private static readonly string[] StandardSlotChoices =
    [
        "9A - Authentication",
        "9C - Digital Signature",
        "9D - Key Management",
        "9E - Card Authentication"
    ];

    /// <summary>
    /// Prompts user to select a PIV slot.
    /// </summary>
    /// <param name="prompt">Prompt text to display.</param>
    /// <returns>Selected PIV slot.</returns>
    public static PivSlot SelectSlot(string prompt = "Select slot")
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .AddChoices(StandardSlotChoices));

        return selection[..2] switch
        {
            "9A" => PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            _ => PivSlot.Authentication
        };
    }

    /// <summary>
    /// Gets the display name for a PIV slot.
    /// </summary>
    public static string GetSlotName(PivSlot slot)
    {
        return slot switch
        {
            PivSlot.Authentication => "Authentication (9A)",
            PivSlot.Signature => "Digital Signature (9C)",
            PivSlot.KeyManagement => "Key Management (9D)",
            PivSlot.CardAuthentication => "Card Authentication (9E)",
            PivSlot.Attestation => "Attestation (F9)",
            _ => $"Slot {slot}"
        };
    }
}
