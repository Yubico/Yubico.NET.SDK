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
/// Displays the OTP slot configuration state.
/// </summary>
public static class StatusCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        await using var session = await DeviceHelper.CreateSessionAsync(options.Json, ct);
        if (session is null)
        {
            if (options.Json)
            {
                OutputHelper.WriteJsonError("No YubiKey found.");
            }
            else
            {
                OutputHelper.WriteError("No YubiKey found.");
            }

            return 1;
        }

        var state = session.GetConfigState();

        if (options.Json)
        {
            OutputHelper.WriteJson(BuildStatusResult(state));
            return 0;
        }

        DisplayStatus(state);
        return 0;
    }

    public static async Task RunInteractiveAsync(CancellationToken ct)
    {
        await using var session = await DeviceHelper.CreateSessionAsync(jsonMode: false, ct);
        if (session is null)
        {
            return;
        }

        var state = session.GetConfigState();
        DisplayStatus(state);
    }

    private static void DisplayStatus(ConfigState state)
    {
        OutputHelper.WriteHeader("OTP Slot Configuration");

        OutputHelper.WriteKeyValue("Firmware", state.FirmwareVersion.ToString());
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[green]Property[/]"))
            .AddColumn(new TableColumn("[green]Slot 1[/]"))
            .AddColumn(new TableColumn("[green]Slot 2[/]"));

        try
        {
            table.AddRow(
                "Configured",
                FormatBool(state.IsConfigured(Slot.One)),
                FormatBool(state.IsConfigured(Slot.Two)));
        }
        catch (InvalidOperationException)
        {
            table.AddRow("Configured", "[grey]N/A (fw < 2.1)[/]", "[grey]N/A (fw < 2.1)[/]");
        }

        try
        {
            table.AddRow(
                "Touch Triggered",
                FormatBool(state.IsTouchTriggered(Slot.One)),
                FormatBool(state.IsTouchTriggered(Slot.Two)));
        }
        catch (InvalidOperationException)
        {
            table.AddRow("Touch Triggered", "[grey]N/A (fw < 3.0)[/]", "[grey]N/A (fw < 3.0)[/]");
        }

        table.AddRow("LED Inverted", FormatBool(state.IsLedInverted()), "");

        AnsiConsole.Write(table);
    }

    private static object BuildStatusResult(ConfigState state)
    {
        bool? slot1Configured = null;
        bool? slot2Configured = null;
        bool? slot1Touch = null;
        bool? slot2Touch = null;

        try
        {
            slot1Configured = state.IsConfigured(Slot.One);
            slot2Configured = state.IsConfigured(Slot.Two);
        }
        catch (InvalidOperationException) { }

        try
        {
            slot1Touch = state.IsTouchTriggered(Slot.One);
            slot2Touch = state.IsTouchTriggered(Slot.Two);
        }
        catch (InvalidOperationException) { }

        return new
        {
            firmware = state.FirmwareVersion.ToString(),
            slot1 = new
            {
                configured = slot1Configured,
                touchTriggered = slot1Touch
            },
            slot2 = new
            {
                configured = slot2Configured,
                touchTriggered = slot2Touch
            },
            ledInverted = state.IsLedInverted()
        };
    }

    private static string FormatBool(bool value) =>
        value ? "[green]Yes[/]" : "[grey]No[/]";
}
