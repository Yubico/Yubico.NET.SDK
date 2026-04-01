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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var options = CliOptions.Parse(args);

    // CLI mode: execute command and exit
    if (options is not null)
    {
        YubiKeyManager.StartMonitoring();
        int exitCode = await RunCommandAsync(options, cts.Token);
        await YubiKeyManager.ShutdownAsync();
        return exitCode;
    }

    // Interactive mode: show menu loop
    return await RunInteractiveAsync(cts.Token);
}
catch (ArgumentException ex)
{
    AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
    AnsiConsole.WriteLine();
    CliOptions.PrintHelp();
    return 1;
}
catch (OperationCanceledException)
{
    return 0;
}

static async Task<int> RunCommandAsync(CliOptions options, CancellationToken ct) =>
    options.Command switch
    {
        "status" => await StatusCommand.RunAsync(options, ct),
        "program" => await ProgramCommand.RunAsync(options, ct),
        "calculate" => await CalculateCommand.RunAsync(options, ct),
        "swap" => await SwapCommand.RunAsync(options, ct),
        "delete" => await DeleteCommand.RunAsync(options, ct),
        "help" => ShowHelp(),
        _ => throw new ArgumentException($"Unknown command: {options.Command}. Run 'OtpTool --help' for usage.")
    };

static int ShowHelp()
{
    CliOptions.PrintHelp();
    return 0;
}

static async Task<int> RunInteractiveAsync(CancellationToken ct)
{
    AnsiConsole.Write(
        new FigletText("OTP Tool")
            .LeftJustified()
            .Color(Color.Blue));

    AnsiConsole.MarkupLine("[grey]YubiKey OTP Slot Configuration Tool - SDK Example Application[/]");
    AnsiConsole.WriteLine();

    YubiKeyManager.StartMonitoring();

    while (!ct.IsCancellationRequested)
    {
        string choice;
        try
        {
            choice = await new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(12)
                .AddChoices(
                [
                    "View Slot Status",
                    "Program Slot",
                    "HMAC-SHA1 Challenge-Response",
                    "Swap Slots",
                    "Delete Slot",
                    "Exit"
                ])
                .ShowAsync(AnsiConsole.Console, ct);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (choice == "Exit")
        {
            AnsiConsole.MarkupLine("[grey]Goodbye![/]");
            break;
        }

        try
        {
            switch (choice)
            {
                case "View Slot Status":
                    await StatusCommand.RunInteractiveAsync(ct);
                    break;
                case "Program Slot":
                    await ProgramCommand.RunInteractiveAsync(ct);
                    break;
                case "HMAC-SHA1 Challenge-Response":
                    await CalculateCommand.RunInteractiveAsync(ct);
                    break;
                case "Swap Slots":
                    await SwapCommand.RunInteractiveAsync(ct);
                    break;
                case "Delete Slot":
                    await DeleteCommand.RunInteractiveAsync(ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.WriteLine();
    }

    await YubiKeyManager.ShutdownAsync();
    return 0;
}
