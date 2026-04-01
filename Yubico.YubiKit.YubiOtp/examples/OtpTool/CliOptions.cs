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

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>
/// Parsed CLI options from command-line arguments.
/// </summary>
public sealed record CliOptions
{
    /// <summary>
    /// The command to execute (status, program, calculate, swap, delete).
    /// Null when running in interactive mode.
    /// </summary>
    public string? Command { get; init; }

    /// <summary>
    /// The sub-command for program operations (otp, hmac, static, hotp).
    /// </summary>
    public string? SubCommand { get; init; }

    /// <summary>
    /// The slot to operate on (1 or 2).
    /// </summary>
    public Slot? Slot { get; init; }

    /// <summary>
    /// Hex-encoded key material for programming operations.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Hex-encoded access code for protected slot operations.
    /// </summary>
    public string? AccessCode { get; init; }

    /// <summary>
    /// Hex-encoded new access code to set on the slot.
    /// </summary>
    public string? NewAccessCode { get; init; }

    /// <summary>
    /// Hex-encoded challenge for HMAC-SHA1 challenge-response.
    /// </summary>
    public string? Challenge { get; init; }

    /// <summary>
    /// Hex-encoded public ID for Yubico OTP programming.
    /// </summary>
    public string? PublicId { get; init; }

    /// <summary>
    /// Hex-encoded private ID for Yubico OTP programming.
    /// </summary>
    public string? PrivateId { get; init; }

    /// <summary>
    /// Password text for static password programming.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Number of HOTP digits (6 or 8).
    /// </summary>
    public int? Digits { get; init; }

    /// <summary>
    /// Initial moving factor for HOTP.
    /// </summary>
    public int? Imf { get; init; }

    /// <summary>
    /// Whether to require touch for HMAC-SHA1 operations.
    /// </summary>
    public bool RequireTouch { get; init; }

    /// <summary>
    /// Whether to generate random key material.
    /// </summary>
    public bool Generate { get; init; }

    /// <summary>
    /// Whether to append carriage return after output.
    /// </summary>
    public bool AppendCr { get; init; }

    /// <summary>
    /// Whether to output in JSON format for CI integration.
    /// </summary>
    public bool Json { get; init; }

    /// <summary>
    /// Whether to skip confirmation prompts.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Whether to use short challenge mode for HMAC-SHA1.
    /// </summary>
    public bool ShortChallenge { get; init; }

    /// <summary>
    /// Parses command-line arguments into a <see cref="CliOptions"/> instance.
    /// Returns null if no command is specified (interactive mode).
    /// </summary>
    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var command = args[0].ToLowerInvariant();
        string? subCommand = null;
        Slot? slot = null;
        string? key = null;
        string? accessCode = null;
        string? newAccessCode = null;
        string? challenge = null;
        string? publicId = null;
        string? privateId = null;
        string? password = null;
        int? digits = null;
        int? imf = null;
        bool requireTouch = false;
        bool generate = false;
        bool appendCr = false;
        bool json = false;
        bool force = false;
        bool shortChallenge = false;

        int startIndex = 1;

        // Check for sub-command (program otp, program hmac, etc.)
        if (command == "program" && args.Length > 1 && !args[1].StartsWith('-'))
        {
            subCommand = args[1].ToLowerInvariant();
            startIndex = 2;
        }

        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--slot" or "-s":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int slotNum))
                    {
                        slot = slotNum switch
                        {
                            1 => YubiOtp.Slot.One,
                            2 => YubiOtp.Slot.Two,
                            _ => throw new ArgumentException($"Invalid slot number: {slotNum}. Must be 1 or 2.")
                        };
                    }
                    break;

                case "--key" or "-k":
                    if (i + 1 < args.Length) key = args[++i];
                    break;

                case "--access-code" or "-a":
                    if (i + 1 < args.Length) accessCode = args[++i];
                    break;

                case "--new-access-code":
                    if (i + 1 < args.Length) newAccessCode = args[++i];
                    break;

                case "--challenge" or "-c":
                    if (i + 1 < args.Length) challenge = args[++i];
                    break;

                case "--public-id":
                    if (i + 1 < args.Length) publicId = args[++i];
                    break;

                case "--private-id":
                    if (i + 1 < args.Length) privateId = args[++i];
                    break;

                case "--password" or "-p":
                    if (i + 1 < args.Length) password = args[++i];
                    break;

                case "--digits":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int d))
                    {
                        digits = d;
                    }
                    break;

                case "--imf":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int imfVal))
                    {
                        imf = imfVal;
                    }
                    break;

                case "--touch":
                    requireTouch = true;
                    break;

                case "--generate" or "-g":
                    generate = true;
                    break;

                case "--append-cr":
                    appendCr = true;
                    break;

                case "--json" or "-j":
                    json = true;
                    break;

                case "--force" or "-f":
                    force = true;
                    break;

                case "--short":
                    shortChallenge = true;
                    break;

                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return new CliOptions
        {
            Command = command,
            SubCommand = subCommand,
            Slot = slot,
            Key = key,
            AccessCode = accessCode,
            NewAccessCode = newAccessCode,
            Challenge = challenge,
            PublicId = publicId,
            PrivateId = privateId,
            Password = password,
            Digits = digits,
            Imf = imf,
            RequireTouch = requireTouch,
            Generate = generate,
            AppendCr = appendCr,
            Json = json,
            Force = force,
            ShortChallenge = shortChallenge
        };
    }

    /// <summary>
    /// Prints CLI usage information.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("""
            OtpTool - YubiKey OTP Slot Configuration Tool

            USAGE:
              OtpTool [command] [options]
              OtpTool                              Interactive mode

            COMMANDS:
              status                               View slot configuration state
              program otp                          Program Yubico OTP slot
              program hmac                         Program HMAC-SHA1 challenge-response
              program static                       Program static password
              program hotp                         Program HOTP (event-based OTP)
              calculate                            Perform HMAC-SHA1 challenge-response
              swap                                 Swap slot 1 and slot 2
              delete                               Delete a slot configuration

            COMMON OPTIONS:
              --slot, -s <1|2>                     Slot number (required for most commands)
              --access-code, -a <hex>              Current access code (hex, 6 bytes)
              --json, -j                           Output in JSON format
              --force, -f                          Skip confirmation prompts
              --help, -h                           Show this help

            PROGRAM OPTIONS:
              --key, -k <hex>                      Key material (hex)
              --generate, -g                       Generate random key material
              --new-access-code <hex>              Set access code on slot (hex, 6 bytes)
              --append-cr                          Append carriage return after output
              --public-id <hex>                    Public ID for Yubico OTP (hex)
              --private-id <hex>                   Private ID for Yubico OTP (hex, 6 bytes)
              --password, -p <text>                Password for static password mode
              --digits <6|8>                       HOTP digit count (default: 6)
              --imf <value>                        HOTP initial moving factor
              --touch                              Require touch for HMAC-SHA1
              --short                              Allow short HMAC-SHA1 challenges

            EXAMPLES:
              OtpTool status --json
              OtpTool program hmac -s 2 -k 0102...1314 --touch
              OtpTool program hmac -s 2 --generate
              OtpTool program otp -s 1 --generate
              OtpTool program static -s 2 --password "MySecretPassword"
              OtpTool program hotp -s 1 --key 0102...1314 --digits 8
              OtpTool calculate -s 2 --challenge 48656C6C6F --json
              OtpTool swap
              OtpTool delete -s 2 --force
            """);
    }
}
