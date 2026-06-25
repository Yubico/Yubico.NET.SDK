// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using SharedOutput = Yubico.YubiKit.Cli.Shared.Output.OutputHelpers;
using SharedConfirm = Yubico.YubiKit.Cli.Shared.Output.ConfirmationPrompts;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent output.
/// Delegates common methods to the shared CLI library.
/// </summary>
public static class OutputHelpers
{
    // -- Delegated to shared library --

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

    /// <inheritdoc cref="SharedOutput.WriteHex(string, ReadOnlySpan{byte})"/>
    public static void WriteHex(string label, ReadOnlySpan<byte> data) => SharedOutput.WriteHex(label, data);

    /// <inheritdoc cref="SharedOutput.CreateTable"/>
    public static Table CreateTable(params string[] columns) => SharedOutput.CreateTable(columns);

    /// <inheritdoc cref="SharedConfirm.ConfirmDestructive"/>
    public static bool ConfirmDestructive(string action, string confirmationWord = "RESET") =>
        SharedConfirm.ConfirmDestructive(action, confirmationWord);

    /// <inheritdoc cref="SharedOutput.WriteActiveDevice"/>
    public static void WriteActiveDevice(string deviceDisplayName) =>
        SharedOutput.WriteActiveDevice(deviceDisplayName);
}
