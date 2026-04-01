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
/// Deletes an OTP slot configuration.
/// </summary>
public static class DeleteCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot ?? throw new ArgumentException("--slot is required for delete command.");
        var accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        if (!options.Force && !options.Json)
        {
            if (!AnsiConsole.Confirm($"Delete slot {slot} configuration?", defaultValue: false))
            {
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                return 0;
            }
        }

        await using var session = await DeviceHelper.CreateSessionAsync(options.Json, ct);
        if (session is null)
        {
            if (options.Json) OutputHelper.WriteJsonError("No YubiKey found.");
            else OutputHelper.WriteError("No YubiKey found.");
            return 1;
        }

        try
        {
            await session.DeleteSlotAsync(slot, accessCode, ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new { status = "ok", action = "delete", slot = slot.ToString() });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {slot} configuration deleted.");
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

        byte[] accessCode = [];
        if (AnsiConsole.Confirm("Provide current access code?", defaultValue: false))
        {
            accessCode = OutputHelper.PromptHex("Access code") ?? [];
        }

        if (!AnsiConsole.Confirm($"Delete slot {slot} configuration? This cannot be undone.", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        await using var session = await DeviceHelper.CreateSessionAsync(jsonMode: false, ct);
        if (session is null)
        {
            return;
        }

        try
        {
            await session.DeleteSlotAsync(slot, accessCode, ct);
            OutputHelper.WriteSuccess($"Slot {slot} configuration deleted.");
        }
        catch (Exception ex)
        {
            OutputHelper.WriteError(ex.Message);
        }
    }
}
