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
/// Deletes an OTP slot configuration (ykman otp delete SLOT).
/// </summary>
public static class DeleteCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool delete <1|2>");

        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm($"Delete slot {FormatSlot(slot)} configuration?", defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            await session.DeleteSlotAsync(slot, accessCode, ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new { status = "ok", action = "delete", slot = FormatSlot(slot) });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} configuration deleted.");
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}