// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

/// <summary>
/// Provides plain-text formatting utilities for consistent CLI output.
/// No Spectre.Console dependency - works in pipes and non-interactive terminals.
/// </summary>
public static class OutputHelpers
{
    /// <summary>
    /// Writes a success message to stdout.
    /// </summary>
    public static void WriteSuccess(string message) =>
        Console.WriteLine($"  {message}");

    /// <summary>
    /// Writes an error message to stderr.
    /// </summary>
    public static void WriteError(string message) =>
        Console.Error.WriteLine($"Error: {message}");

    /// <summary>
    /// Writes a warning message to stderr.
    /// </summary>
    public static void WriteWarning(string message) =>
        Console.Error.WriteLine($"WARNING: {message}");

    /// <summary>
    /// Writes an informational message to stderr (so stdout remains machine-parseable).
    /// </summary>
    public static void WriteInfo(string message) =>
        Console.Error.WriteLine(message);

    /// <summary>
    /// Writes a key-value pair.
    /// </summary>
    public static void WriteKeyValue(string key, string? value) =>
        Console.WriteLine($"{key}: {value ?? "N/A"}");

    /// <summary>
    /// Formats an issuer:name credential display name.
    /// </summary>
    public static string FormatCredentialName(string? issuer, string name) =>
        issuer is not null ? $"{issuer}:{name}" : name;
}