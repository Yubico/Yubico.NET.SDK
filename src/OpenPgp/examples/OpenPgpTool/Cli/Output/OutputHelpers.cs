// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using SharedOutput = Yubico.YubiKit.Cli.Shared.Output.OutputHelpers;
using SharedConfirm = Yubico.YubiKit.Cli.Shared.Output.ConfirmationPrompts;
using SharedPin = Yubico.YubiKit.Cli.Shared.Output.PinPrompt;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent output.
/// Delegates common methods to the shared CLI library.
/// </summary>
public static class OutputHelpers
{
    // ── Delegated to shared library ──────────────────────────────────────────

    /// <inheritdoc cref="SharedOutput.WriteHeader"/>
    public static void WriteHeader(string title) => SharedOutput.WriteHeader(title);

    /// <inheritdoc cref="SharedOutput.WriteSuccess"/>
    public static void WriteSuccess(string message) => SharedOutput.WriteSuccess(message);

    /// <summary>
    /// Displays an error message to stderr.
    /// </summary>
    public static void WriteError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
    }

    /// <inheritdoc cref="SharedOutput.WriteWarning"/>
    public static void WriteWarning(string message) => SharedOutput.WriteWarning(message);

    /// <inheritdoc cref="SharedOutput.WriteInfo"/>
    public static void WriteInfo(string message) => SharedOutput.WriteInfo(message);

    /// <inheritdoc cref="SharedOutput.WriteKeyValue"/>
    public static void WriteKeyValue(string key, string? value) => SharedOutput.WriteKeyValue(key, value);

    /// <inheritdoc cref="SharedOutput.WriteKeyValueMarkup"/>
    public static void WriteKeyValueMarkup(string key, string valueMarkup) =>
        SharedOutput.WriteKeyValueMarkup(key, valueMarkup);

    /// <inheritdoc cref="SharedOutput.WriteHex(string, ReadOnlySpan{byte})"/>
    public static void WriteHex(string label, ReadOnlySpan<byte> data) => SharedOutput.WriteHex(label, data);

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

    /// <summary>
    /// Prompts for a PIN with masked input.
    /// </summary>
    public static string PromptPin(string label) => SharedPin.PromptForPin(label);
}
