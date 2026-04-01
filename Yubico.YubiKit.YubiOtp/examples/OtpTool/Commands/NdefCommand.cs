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
/// Configures NDEF for NFC on a slot (ykman otp ndef SLOT).
/// </summary>
public static class NdefCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool ndef <1|2>");

        NdefType ndefType = ParseNdefType(options.NdefTypeValue);

        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm(
                        $"Configure NDEF on slot {FormatSlot(slot)}?",
                        defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            await session.SetNdefConfigurationAsync(
                slot,
                options.Prefix,
                accessCode,
                ndefType,
                ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    status = "ok",
                    slot = FormatSlot(slot),
                    type = "ndef",
                    ndefType = ndefType.ToString().ToLowerInvariant(),
                    prefix = options.Prefix
                });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} NDEF configured.");
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }

    private static NdefType ParseNdefType(string? value) =>
        value?.ToUpperInvariant() switch
        {
            null or "URI" => NdefType.Uri,
            "TEXT" => NdefType.Text,
            _ => throw new ArgumentException($"Invalid --ndef-type: {value}. Must be URI or TEXT.")
        };

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}