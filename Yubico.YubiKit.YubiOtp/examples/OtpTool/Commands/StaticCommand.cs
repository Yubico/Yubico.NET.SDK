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

using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Programs a static password on a slot (ykman otp static SLOT).
/// </summary>
public static class StaticCommand
{
    // ModHex characters that map to US keyboard scan codes the YubiKey can type.
    private const string ModHexChars = "cbdefghijklnrtuv";

    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool static <1|2>");

        byte[] scanCodes;

        if (options.Generate)
        {
            int length = options.Length ?? 38;
            string generated = GenerateStaticPassword(length);

            if (!options.Json)
            {
                OutputHelper.WriteKeyValue("Generated password", generated);
            }

            scanCodes = Encoding.UTF8.GetBytes(generated);
        }
        else
        {
            string password = options.Password
                              ?? throw new ArgumentException(
                                  "--password or --generate is required for static command.");
            scanCodes = Encoding.UTF8.GetBytes(password);
        }

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

            using var config = new StaticPasswordSlotConfiguration(scanCodes);

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode, cancellationToken: ct);

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
            CryptographicOperations.ZeroMemory(scanCodes);
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
