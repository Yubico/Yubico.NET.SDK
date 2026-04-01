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
/// Updates slot settings/flags without reprogramming key material (ykman otp settings SLOT).
/// Requires the slot to have been programmed with AllowUpdate enabled.
/// </summary>
public static class SettingsCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool settings <1|2>");

        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm(
                        $"Update settings on slot {FormatSlot(slot)}?",
                        defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            using var config = new UpdateConfiguration();

            if (options.Pacing == 10)
            {
                config.PacingChar10();
            }
            else if (options.Pacing == 20)
            {
                config.PacingChar20();
            }

            if (options.NoEnter)
            {
                config.AppendCr(false);
            }

            await session.UpdateConfigurationAsync(slot, config, currentAccessCode: accessCode, cancellationToken: ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    status = "ok",
                    slot = FormatSlot(slot),
                    type = "settings",
                    pacing = options.Pacing,
                    noEnter = options.NoEnter
                });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} settings updated.");
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