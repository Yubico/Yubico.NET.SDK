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

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Programs HOTP (event-based OTP) on a slot (ykman otp hotp SLOT).
/// </summary>
public static class HotpCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool hotp <1|2>");

        byte[] hmacKey;

        if (options.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);

            if (!options.Json)
            {
                OutputHelper.WriteHex("Generated HMAC key", hmacKey);
            }
        }
        else
        {
            hmacKey = OutputHelper.RequireHex(options.Key, "key");
        }

        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm(
                        $"Program slot {FormatSlot(slot)} with HOTP?",
                        defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            using var config = new HotpSlotConfiguration(hmacKey, options.Imf ?? 0);

            if (options.Digits == 8)
            {
                config.Use8Digits();
            }

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode, cancellationToken: ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    status = "ok",
                    slot = FormatSlot(slot),
                    type = "hotp",
                    digits = options.Digits ?? 6,
                    imf = options.Imf ?? 0
                });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} programmed for HOTP.");
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hmacKey);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}