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
/// Swaps the configurations of slot 1 and slot 2 (ykman otp swap).
/// </summary>
public static class SwapCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        if (!options.Force && !options.Json)
        {
            if (!AnsiConsole.Confirm("Swap slot 1 and slot 2 configurations?", defaultValue: false))
            {
                OutputHelper.WriteError("Aborted.");
                return 1;
            }
        }

        await using var session = await DeviceHelper.CreateSessionAsync(ct);

        await session.SwapSlotsAsync(ct);

        if (options.Json)
        {
            OutputHelper.WriteJson(new { status = "ok", action = "swap" });
        }
        else
        {
            OutputHelper.WriteSuccess("Slot 1 and slot 2 configurations swapped.");
        }

        return 0;
    }
}
