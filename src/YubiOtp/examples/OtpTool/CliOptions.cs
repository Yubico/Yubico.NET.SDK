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
/// Parsed CLI options matching ykman's OTP command structure.
/// Slot is always a positional argument (1 or 2).
/// </summary>
public sealed record CliOptions
{
    /// <summary>
    /// The command to execute (info, swap, delete, chalresp, hotp, static, yubiotp, calculate, ndef, settings).
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// The slot to operate on (1 or 2). Positional argument for commands that require it.
    /// </summary>
    public Slot? Slot { get; init; }

    /// <summary>
    /// Hex-encoded 6-byte access code for protected slot operations.
    /// Global option: --access-code HEX.
    /// </summary>
    public string? AccessCode { get; init; }

    /// <summary>
    /// Whether to skip confirmation prompts (-f / --force).
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Whether to output in JSON format (--json / -j).
    /// </summary>
    public bool Json { get; init; }

    // -- chalresp options --

    /// <summary>
    /// Generate a random HMAC key (--generate).
    /// </summary>
    public bool Generate { get; init; }

    /// <summary>
    /// Require physical touch for challenge-response (--require-touch).
    /// </summary>
    public bool RequireTouch { get; init; }

    /// <summary>
    /// Program for TOTP use (implies --require-touch, enables short challenge) (--totp).
    /// </summary>
    public bool Totp { get; init; }

    /// <summary>
    /// Hex-encoded key material for programming operations (--key HEX).
    /// </summary>
    public string? Key { get; init; }

    // -- hotp options --

    /// <summary>
    /// Number of HOTP digits: 6 or 8 (--digits 6|8).
    /// </summary>
    public int? Digits { get; init; }

    /// <summary>
    /// Initial moving factor for HOTP (--imf VALUE).
    /// </summary>
    public int? Imf { get; init; }

    // -- static options --

    /// <summary>
    /// Password text for static password programming (--password TEXT).
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Length of generated static password (--length N).
    /// </summary>
    public int? Length { get; init; }

    /// <summary>
    /// Keyboard layout for static password (--keyboard-layout LAYOUT).
    /// </summary>
    public string? KeyboardLayout { get; init; }

    // -- yubiotp options --

    /// <summary>
    /// Use device serial number as public ID prefix (--serial-public-id).
    /// </summary>
    public bool SerialPublicId { get; init; }

    /// <summary>
    /// Generate random private ID (--generate-private-id).
    /// </summary>
    public bool GeneratePrivateId { get; init; }

    /// <summary>
    /// Generate random AES key (--generate-key).
    /// </summary>
    public bool GenerateKey { get; init; }

    /// <summary>
    /// Hex-encoded public ID for Yubico OTP (--public-id HEX).
    /// </summary>
    public string? PublicId { get; init; }

    /// <summary>
    /// Hex-encoded private ID for Yubico OTP (--private-id HEX).
    /// </summary>
    public string? PrivateId { get; init; }

    // -- calculate options --

    /// <summary>
    /// Hex-encoded challenge for challenge-response (positional after SLOT).
    /// </summary>
    public string? Challenge { get; init; }

    // -- ndef options --

    /// <summary>
    /// URI prefix for NDEF configuration (--prefix URI_PREFIX).
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// NDEF record type: TEXT or URI (--ndef-type TEXT|URI).
    /// </summary>
    public string? NdefTypeValue { get; init; }

    // -- settings options --

    /// <summary>
    /// Pacing delay between keystrokes: 10 or 20 ms (--pacing 10|20).
    /// </summary>
    public int? Pacing { get; init; }

    /// <summary>
    /// Disable carriage return after OTP output (--no-enter).
    /// </summary>
    public bool NoEnter { get; init; }

    /// <summary>
    /// Parses command-line arguments into a <see cref="CliOptions"/> instance.
    /// Matches ykman's OTP command structure with positional SLOT argument.
    /// </summary>
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            Environment.Exit(0);
        }

        string command = args[0].ToLowerInvariant();

        if (command is "--help" or "-h" or "help")
        {
            PrintHelp();
            Environment.Exit(0);
        }

        Slot? slot = null;
        string? accessCode = null;
        bool force = false;
        bool json = false;
        bool generate = false;
        bool requireTouch = false;
        bool totp = false;
        string? key = null;
        int? digits = null;
        int? imf = null;
        string? password = null;
        int? length = null;
        string? keyboardLayout = null;
        bool serialPublicId = false;
        bool generatePrivateId = false;
        bool generateKey = false;
        string? publicId = null;
        string? privateId = null;
        string? challenge = null;
        string? prefix = null;
        string? ndefTypeValue = null;
        int? pacing = null;
        bool noEnter = false;

        // Parse positional SLOT argument (first non-flag arg after command)
        int startIndex = 1;
        if (CommandRequiresSlot(command) && args.Length > 1 && !args[1].StartsWith('-'))
        {
            if (int.TryParse(args[1], out int slotNum))
            {
                slot = slotNum switch
                {
                    1 => YubiOtp.Slot.One,
                    2 => YubiOtp.Slot.Two,
                    _ => throw new ArgumentException($"Invalid slot: {slotNum}. Must be 1 or 2.")
                };
                startIndex = 2;

                // For calculate, the next positional arg is the challenge
                if (command == "calculate" && args.Length > 2 && !args[2].StartsWith('-'))
                {
                    challenge = args[2];
                    startIndex = 3;
                }
            }
            else
            {
                throw new ArgumentException($"Invalid slot: {args[1]}. Must be 1 or 2.");
            }
        }

        for (int i = startIndex; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--access-code":
                    accessCode = ConsumeValue(args, ref i, "access-code");
                    break;

                case "--force" or "-f":
                    force = true;
                    break;

                case "--json" or "-j":
                    json = true;
                    break;

                case "--generate":
                    generate = true;
                    break;

                case "--require-touch":
                    requireTouch = true;
                    break;

                case "--totp":
                    totp = true;
                    break;

                case "--key":
                    key = ConsumeValue(args, ref i, "key");
                    break;

                case "--digits":
                    string digitsStr = ConsumeValue(args, ref i, "digits");
                    if (!int.TryParse(digitsStr, out int d) || d is not (6 or 8))
                    {
                        throw new ArgumentException("--digits must be 6 or 8.");
                    }

                    digits = d;
                    break;

                case "--imf":
                    string imfStr = ConsumeValue(args, ref i, "imf");
                    if (!int.TryParse(imfStr, out int imfVal))
                    {
                        throw new ArgumentException("--imf must be an integer.");
                    }

                    imf = imfVal;
                    break;

                case "--password":
                    password = ConsumeValue(args, ref i, "password");
                    break;

                case "--length":
                    string lenStr = ConsumeValue(args, ref i, "length");
                    if (!int.TryParse(lenStr, out int lenVal) || lenVal < 1 || lenVal > 38)
                    {
                        throw new ArgumentException("--length must be between 1 and 38.");
                    }

                    length = lenVal;
                    break;

                case "--keyboard-layout":
                    keyboardLayout = ConsumeValue(args, ref i, "keyboard-layout");
                    break;

                case "--serial-public-id":
                    serialPublicId = true;
                    break;

                case "--generate-private-id":
                    generatePrivateId = true;
                    break;

                case "--generate-key":
                    generateKey = true;
                    break;

                case "--public-id":
                    publicId = ConsumeValue(args, ref i, "public-id");
                    break;

                case "--private-id":
                    privateId = ConsumeValue(args, ref i, "private-id");
                    break;

                case "--prefix":
                    prefix = ConsumeValue(args, ref i, "prefix");
                    break;

                case "--ndef-type":
                    ndefTypeValue = ConsumeValue(args, ref i, "ndef-type");
                    break;

                case "--pacing":
                    string pacingStr = ConsumeValue(args, ref i, "pacing");
                    if (!int.TryParse(pacingStr, out int pacingVal) || pacingVal is not (10 or 20))
                    {
                        throw new ArgumentException("--pacing must be 10 or 20.");
                    }

                    pacing = pacingVal;
                    break;

                case "--no-enter":
                    noEnter = true;
                    break;

                case "--help" or "-h":
                    PrintCommandHelp(command);
                    Environment.Exit(0);
                    break;

                default:
                    throw new ArgumentException($"Unknown option: {arg}");
            }
        }

        return new CliOptions
        {
            Command = command,
            Slot = slot,
            AccessCode = accessCode,
            Force = force,
            Json = json,
            Generate = generate,
            RequireTouch = requireTouch,
            Totp = totp,
            Key = key,
            Digits = digits,
            Imf = imf,
            Password = password,
            Length = length,
            KeyboardLayout = keyboardLayout,
            SerialPublicId = serialPublicId,
            GeneratePrivateId = generatePrivateId,
            GenerateKey = generateKey,
            PublicId = publicId,
            PrivateId = privateId,
            Challenge = challenge,
            Prefix = prefix,
            NdefTypeValue = ndefTypeValue,
            Pacing = pacing,
            NoEnter = noEnter
        };
    }

    /// <summary>
    /// Prints top-level usage information matching ykman's format.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("""
            OtpTool - YubiKey OTP Configuration (ykman-compatible)

            USAGE:
              OtpTool <command> [SLOT] [options]

            COMMANDS:
              info                        Display OTP slot status
              swap                        Swap slot 1 and slot 2 configurations
              delete SLOT [-f]            Delete a slot configuration
              chalresp SLOT [options]     Program HMAC-SHA1 challenge-response
              hotp SLOT [options]         Program HOTP (event-based OTP)
              static SLOT [options]       Program static password
              yubiotp SLOT [options]      Program Yubico OTP
              calculate SLOT [CHALLENGE]  Perform challenge-response calculation
              ndef SLOT [options]         Configure NDEF for NFC
              settings SLOT [options]     Update slot settings (flags only)

            GLOBAL OPTIONS:
              --access-code HEX           6-byte access code for protected slots
              --force, -f                 Skip confirmation prompts
              --json, -j                  Output in JSON format
              --help, -h                  Show help

            EXAMPLES:
              OtpTool info
              OtpTool info --json
              OtpTool chalresp 2 --generate --require-touch
              OtpTool chalresp 2 --key 0102...1314 --totp
              OtpTool hotp 1 --key 0102...1314 --digits 8
              OtpTool static 2 --generate --length 16
              OtpTool yubiotp 1 --generate-key --generate-private-id --serial-public-id
              OtpTool calculate 2 48656C6C6F
              OtpTool delete 2 -f
              OtpTool swap
              OtpTool ndef 1 --prefix https://example.com/
              OtpTool settings 2 --pacing 20 --no-enter
            """);
    }

    /// <summary>
    /// Prints help for a specific command.
    /// </summary>
    public static void PrintCommandHelp(string command)
    {
        string help = command switch
        {
            "info" => """
                OtpTool info - Display OTP slot status

                USAGE: OtpTool info [--json]
                """,

            "swap" => """
                OtpTool swap - Swap slot 1 and slot 2 configurations

                USAGE: OtpTool swap [-f] [--access-code HEX]
                """,

            "delete" => """
                OtpTool delete - Delete a slot configuration

                USAGE: OtpTool delete SLOT [-f] [--access-code HEX]
                """,

            "chalresp" => """
                OtpTool chalresp - Program HMAC-SHA1 challenge-response

                USAGE: OtpTool chalresp SLOT [options]

                OPTIONS:
                  --key HEX                 20-byte HMAC-SHA1 key (hex)
                  --generate                Generate random key
                  --require-touch           Require physical touch
                  --totp                    TOTP mode (touch + short challenge)
                  --access-code HEX         Current access code
                """,

            "hotp" => """
                OtpTool hotp - Program HOTP (event-based OTP)

                USAGE: OtpTool hotp SLOT [options]

                OPTIONS:
                  --key HEX                 20-byte HMAC key (hex)
                  --generate                Generate random key
                  --digits 6|8              OTP digit count (default: 6)
                  --imf VALUE               Initial moving factor
                  --access-code HEX         Current access code
                """,

            "static" => """
                OtpTool static - Program static password

                USAGE: OtpTool static SLOT [options]

                OPTIONS:
                  --password TEXT            Password text
                  --generate                Generate random password
                  --length N                Length of generated password (1-38)
                  --keyboard-layout LAYOUT  Keyboard layout (default: US)
                  --access-code HEX         Current access code
                """,

            "yubiotp" => """
                OtpTool yubiotp - Program Yubico OTP

                USAGE: OtpTool yubiotp SLOT [options]

                OPTIONS:
                  --public-id HEX           Public ID (hex, up to 16 bytes)
                  --serial-public-id        Use device serial as public ID
                  --private-id HEX          Private ID (hex, 6 bytes)
                  --generate-private-id     Generate random private ID
                  --key HEX                 AES key (hex, 16 bytes)
                  --generate-key            Generate random AES key
                  --access-code HEX         Current access code
                """,

            "calculate" => """
                OtpTool calculate - Perform HMAC-SHA1 challenge-response

                USAGE: OtpTool calculate SLOT [CHALLENGE]

                CHALLENGE is a hex-encoded value. If omitted, reads from stdin.
                """,

            "ndef" => """
                OtpTool ndef - Configure NDEF for NFC

                USAGE: OtpTool ndef SLOT [options]

                OPTIONS:
                  --prefix URI_PREFIX       URI prefix for NDEF
                  --ndef-type TEXT|URI       NDEF record type (default: URI)
                  --access-code HEX         Current access code
                """,

            "settings" => """
                OtpTool settings - Update slot settings (flags only)

                USAGE: OtpTool settings SLOT [options]

                OPTIONS:
                  --pacing 10|20            Keystroke pacing delay (ms)
                  --no-enter                Disable carriage return after output
                  --access-code HEX         Current access code
                """,

            _ => $"Unknown command: {command}. Run 'OtpTool --help' for usage."
        };

        Console.WriteLine(help);
    }

    private static bool CommandRequiresSlot(string command) =>
        command is "delete" or "chalresp" or "hotp" or "static" or "yubiotp"
            or "calculate" or "ndef" or "settings";

    private static string ConsumeValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"--{optionName} requires a value.");
        }

        return args[++index];
    }
}