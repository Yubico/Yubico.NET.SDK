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
using SharedOutput = Yubico.YubiKit.Cli.Shared.Output.OutputHelpers;
using SharedConfirm = Yubico.YubiKit.Cli.Shared.Output.ConfirmationPrompts;
using SharedPin = Yubico.YubiKit.Cli.Shared.Output.PinPrompt;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent FIDO2 tool output.
/// Delegates common methods to the shared CLI library.
/// </summary>
public static class OutputHelpers
{
    // ── Delegated to shared library ──────────────────────────────────────────

    /// <inheritdoc cref="SharedOutput.WriteHeader"/>
    public static void WriteHeader(string title) => SharedOutput.WriteHeader(title);

    /// <inheritdoc cref="SharedOutput.WriteSuccess"/>
    public static void WriteSuccess(string message) => SharedOutput.WriteSuccess(message);

    /// <inheritdoc cref="SharedOutput.WriteError"/>
    public static void WriteError(string message) => SharedOutput.WriteError(message);

    /// <inheritdoc cref="SharedOutput.WriteWarning"/>
    public static void WriteWarning(string message) => SharedOutput.WriteWarning(message);

    /// <inheritdoc cref="SharedOutput.WriteInfo"/>
    public static void WriteInfo(string message) => SharedOutput.WriteInfo(message);

    /// <inheritdoc cref="SharedOutput.WriteKeyValue"/>
    public static void WriteKeyValue(string key, string? value) => SharedOutput.WriteKeyValue(key, value);

    /// <inheritdoc cref="SharedOutput.WriteKeyValueMarkup"/>
    public static void WriteKeyValueMarkup(string key, string valueMarkup) =>
        SharedOutput.WriteKeyValueMarkup(key, valueMarkup);

    /// <inheritdoc cref="SharedOutput.PromptForTouch"/>
    public static void PromptForTouch() => SharedOutput.PromptForTouch();

    /// <summary>
    /// Displays bytes as a hex string.
    /// </summary>
    public static void WriteHex(string label, ReadOnlyMemory<byte> data) =>
        SharedOutput.WriteHex(label, data);

    /// <inheritdoc cref="SharedOutput.CreatePanel"/>
    public static Panel CreatePanel(string title, string content) => SharedOutput.CreatePanel(title, content);

    /// <inheritdoc cref="SharedOutput.CreateTable"/>
    public static Table CreateTable(params string[] columns) => SharedOutput.CreateTable(columns);

    /// <inheritdoc cref="SharedOutput.WriteActiveDevice"/>
    public static void WriteActiveDevice(string deviceDisplayName) =>
        SharedOutput.WriteActiveDevice(deviceDisplayName);

    /// <inheritdoc cref="SharedOutput.WriteBoolValue"/>
    public static void WriteBoolValue(string label, bool value, string? trueText = null, string? falseText = null) =>
        SharedOutput.WriteBoolValue(label, value, trueText, falseText);

    /// <inheritdoc cref="SharedConfirm.ConfirmDangerous"/>
    public static bool ConfirmDangerous(string action) => SharedConfirm.ConfirmDangerous(action);

    /// <inheritdoc cref="SharedConfirm.ConfirmDestructive"/>
    public static bool ConfirmDestructive(string action, string confirmationWord = "RESET") =>
        SharedConfirm.ConfirmDestructive(action, confirmationWord);

    /// <inheritdoc cref="SharedPin.PromptForPin"/>
    public static string PromptForPin(string label = "PIN") => SharedPin.PromptForPin(label);
}
