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

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Performs HMAC-SHA1 challenge-response calculation (ykman otp calculate SLOT [CHALLENGE]).
/// </summary>
public static class CalculateCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool calculate <1|2> [CHALLENGE]");

        byte[] challenge = OutputHelper.RequireHex(
            options.Challenge, "CHALLENGE (positional argument after slot)");

        try
        {
            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            var response = await session.CalculateHmacSha1Async(slot, challenge, ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    slot = FormatSlot(slot),
                    challenge = Convert.ToHexString(challenge),
                    response = Convert.ToHexString(response.Span)
                });
            }
            else
            {
                OutputHelper.WriteHeader("HMAC-SHA1 Challenge-Response");
                OutputHelper.WriteKeyValue("Slot", FormatSlot(slot));
                OutputHelper.WriteHex("Challenge", challenge);
                OutputHelper.WriteHex("Response", response.Span);
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(challenge);
        }
    }

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}
