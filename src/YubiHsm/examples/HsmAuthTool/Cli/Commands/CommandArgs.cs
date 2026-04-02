// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Parses ykman-style CLI arguments with support for flags, named options, and positional args.
/// </summary>
internal static class CommandArgs
{
    public static bool HasFlag(string[] args, params string[] flags) =>
        args.Any(a => flags.Any(f => string.Equals(a, f, StringComparison.OrdinalIgnoreCase)));

    public static string? GetOption(string[] args, params string[] names)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (names.Any(n => string.Equals(args[i], n, StringComparison.OrdinalIgnoreCase)))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the remaining positional arguments after consuming known subcommands.
    /// For example, in "credentials add MyLabel --touch", after consuming ["credentials", "add"],
    /// the first positional arg is "MyLabel".
    /// </summary>
    public static string? GetPositional(string[] args, int subcommandCount)
    {
        if (args.Length <= subcommandCount)
        {
            return null;
        }

        var candidate = args[subcommandCount];
        return candidate.StartsWith('-') ? null : candidate;
    }

    /// <summary>
    /// Parses a hex string into a byte array. Returns null if the input is null or invalid.
    /// </summary>
    public static byte[]? ParseHex(string? hex)
    {
        if (hex is null)
        {
            return null;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the default management key (16 bytes of zeros).
    /// </summary>
    public static byte[] DefaultManagementKey => new byte[16];
}
