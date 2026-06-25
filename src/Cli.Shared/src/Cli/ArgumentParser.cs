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

namespace Yubico.YubiKit.Cli.Shared.Cli;

/// <summary>
/// Parses ykman-style CLI arguments with support for flags, named options, and positional args.
/// Shared across all CLI tools that use manual argument parsing.
/// </summary>
public static class ArgumentParser
{
    /// <summary>
    /// Checks if any of the specified flags are present in the argument list.
    /// </summary>
    public static bool HasFlag(string[] args, params string[] flags) =>
        args.Any(a => flags.Any(f => string.Equals(a, f, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Gets the value following a named option flag.
    /// Supports multiple flag names (e.g., "--pin" and "-p").
    /// </summary>
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
    /// Gets all positional arguments (non-flag arguments that don't follow a flag).
    /// </summary>
    public static List<string> GetPositionalArgs(string[] args)
    {
        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith('-'))
            {
                // Skip flag and its value (if it has one)
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    i++; // skip the value
                }

                continue;
            }

            positional.Add(args[i]);
        }

        return positional;
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
}