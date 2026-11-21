// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;
using Yubico.YubiKit.Management.Examples.ManagementTool.Commands;

using var cts = CommandHelper.CreateConsoleCts();
try
{
    return args.Length > 0
        ? await DispatchCliAsync(args, cts.Token)
        : await RunInteractiveAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operation cancelled. Cancellation does not roll back a write already sent.");
    return ExitCode.UserCancelled;
}

// --- CLI dispatch ---

static async Task<int> DispatchCliAsync(string[] args, CancellationToken cancellationToken)
{
    var command = args[0].ToLowerInvariant();
    if (args.Length != 1)
    {
        if (args.Length == 2 && IsHelp(args[1]) && command is "info" or "config")
        {
            Console.WriteLine($"Usage: managementtool {command}");
            Console.WriteLine(command == "info"
                ? "Select a SmartCard YubiKey and display its device information."
                : "Select a SmartCard YubiKey and display its current configuration.");
            return 0;
        }

        Console.Error.WriteLine("Invalid arguments.");
        PrintUsage();
        return 1;
    }

    return command switch
    {
        "info" => await InfoCommand.ExecuteAsync(cancellationToken),
        "config" => await ConfigCommand.ExecuteAsync(cancellationToken),
        "-h" or "--help" or "help" => PrintUsageAndReturn(),
        _ => PrintUnknownCommand(command)
    };
}

static bool IsHelp(string argument) => argument is "-h" or "--help";

// --- Interactive mode ---

static async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
{
    // Application banner
    AnsiConsole.Write(
        new FigletText("Mgmt Tool")
            .LeftJustified()
            .Color(Color.Blue));

    AnsiConsole.MarkupLine("[grey]YubiKey Management Tool - SDK Example Application[/]");
    AnsiConsole.WriteLine();

    var result = await InteractiveMenuBuilder.Create("What would you like to do?")
        .AddItem("Device Info", DeviceInfoMenu.RunAsync)
        .AddItem("USB Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Usb, ct))
        .AddItem("NFC Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Nfc, ct))
        .AddItem("Timeouts", TimeoutsMenu.RunAsync)
        .AddItem("Device Flags", DeviceFlagsMenu.RunAsync)
        .AddItem("Lock Code", LockCodeMenu.RunAsync)
        .AddItem("Factory Reset", ResetMenu.RunAsync)
        .WithErrorHandler(ex =>
        {
            if (ex is OperationCanceledException)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(ex);
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        })
        .RunAsync(cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();
    return result;
}

// --- Usage strings ---

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: managementtool <command> [--help]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  info        Display device information (firmware, serial, capabilities)");
    Console.Error.WriteLine("  config      Display current device configuration");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Run 'managementtool <command> --help' for more information on a command.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("When run with no arguments, starts in interactive mode.");
}

static int PrintUsageAndReturn()
{
    PrintUsage();
    return 0;
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine();
    PrintUsage();
    return 1;
}