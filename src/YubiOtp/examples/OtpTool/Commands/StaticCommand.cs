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
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Programs a static password on a slot (ykman otp static SLOT).
/// </summary>
/// <remarks>
/// The layout-aware configuration translates text to HID scan codes. It cannot erase its immutable
/// source string; raw translated mutable scan codes
/// permit source-buffer cleanup. Generated passwords are intentionally displayed only in human mode.
/// </remarks>
public static class StaticCommand
{
    // Layout-friendly characters still need translation; UTF-8/ASCII values are not HID scan codes.
    private const string ModHexChars = "cbdefghijklnrtuv";

    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool static <1|2>");

        string layoutName = options.KeyboardLayout ?? nameof(KeyboardLayout.ModHex);
        if (!Enum.TryParse<KeyboardLayout>(layoutName, ignoreCase: true, out var layout) || !Enum.IsDefined(layout))
        {
            throw new ArgumentException($"Unsupported keyboard layout: {layoutName}.");
        }

        string password;

        if (options.Generate)
        {
            int length = options.Length ?? 38;
            password = GenerateStaticPassword(length);

            if (!options.Json)
            {
                OutputHelper.WriteKeyValue("Generated password", password);
            }
        }
        else
        {
            password = options.Password
                              ?? throw new ArgumentException(
                                  "--password or --generate is required for static command.");
        }

        using var config = new StaticPasswordSlotConfiguration(password, layout);
        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm(
                        $"Program slot {FormatSlot(slot)} with static password?",
                        defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            await session.PutConfigurationAsync(slot, config, options.DeleteAccessCode ? default : accessCode,
                accessCode, ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    status = "ok",
                    slot = FormatSlot(slot),
                    type = "static"
                });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} programmed with static password.");
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }

    private static string GenerateStaticPassword(int length)
    {
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        var sb = new StringBuilder(length);
        foreach (byte b in randomBytes)
        {
            sb.Append(ModHexChars[b % ModHexChars.Length]);
        }

        CryptographicOperations.ZeroMemory(randomBytes);
        return sb.ToString();
    }

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}