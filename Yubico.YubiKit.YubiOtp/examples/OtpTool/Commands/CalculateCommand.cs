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

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Performs HMAC-SHA1 challenge-response operations.
/// </summary>
public static class CalculateCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot ?? throw new ArgumentException("--slot is required for calculate command.");
        var challenge = OutputHelper.ParseHex(options.Challenge, "challenge")
                        ?? throw new ArgumentException("--challenge is required for calculate command.");

        await using var session = await DeviceHelper.CreateSessionAsync(options.Json, ct);
        if (session is null)
        {
            if (options.Json) OutputHelper.WriteJsonError("No YubiKey found.");
            else OutputHelper.WriteError("No YubiKey found.");
            return 1;
        }

        try
        {
            var response = await session.CalculateHmacSha1Async(slot, challenge, ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    slot = slot.ToString(),
                    challenge = Convert.ToHexString(challenge),
                    response = Convert.ToHexString(response.Span)
                });
            }
            else
            {
                OutputHelper.WriteHeader("HMAC-SHA1 Challenge-Response");
                OutputHelper.WriteKeyValue("Slot", slot.ToString());
                OutputHelper.WriteHex("Challenge", challenge);
                OutputHelper.WriteHex("Response", response.Span);
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (options.Json) OutputHelper.WriteJsonError(ex.Message);
            else OutputHelper.WriteError(ex.Message);
            return 1;
        }
    }

    public static async Task RunInteractiveAsync(CancellationToken ct)
    {
        var slot = OutputHelper.PromptSlot();
        var challenge = OutputHelper.PromptHex("Challenge data");
        if (challenge is null)
        {
            return;
        }

        await using var session = await DeviceHelper.CreateSessionAsync(jsonMode: false, ct);
        if (session is null)
        {
            return;
        }

        try
        {
            AnsiConsole.MarkupLine("[grey]Touch the YubiKey if touch is required...[/]");
            var response = await session.CalculateHmacSha1Async(slot, challenge, ct);

            OutputHelper.WriteHeader("HMAC-SHA1 Result");
            OutputHelper.WriteKeyValue("Slot", slot.ToString());
            OutputHelper.WriteHex("Challenge", challenge);
            OutputHelper.WriteHex("Response", response.Span);
        }
        catch (Exception ex)
        {
            OutputHelper.WriteError(ex.Message);
        }
    }
}
