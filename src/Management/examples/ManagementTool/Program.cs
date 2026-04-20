// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;
using Yubico.YubiKit.Management.Examples.ManagementTool.Commands;

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

try
{
    if (args.Length > 0)
    {
        // CLI dispatch mode
        return await DispatchCliAsync(args);
    }

    // Interactive mode (existing behavior)
    return await RunInteractiveAsync();
}
finally
{
    await YubiKeyManager.ShutdownAsync();
}

// --- CLI dispatch ---

static async Task<int> DispatchCliAsync(string[] args)
{
    var command = args[0].ToLowerInvariant();

    return command switch
    {
        "info" => await InfoCommand.ExecuteAsync(),
        "config" => await ConfigCommand.ExecuteAsync(),
        "-h" or "--help" or "help" => PrintUsageAndReturn(),
        _ => PrintUnknownCommand(command)
    };
}

// --- Interactive mode ---

static async Task<int> RunInteractiveAsync()
{
    // Application banner
    AnsiConsole.Write(
        new FigletText("Mgmt Tool")
            .LeftJustified()
            .Color(Color.Blue));

    AnsiConsole.MarkupLine("[grey]YubiKey Management Tool - SDK Example Application[/]");
    AnsiConsole.WriteLine();

    using var cts = CommandHelper.CreateConsoleCts();

    return await InteractiveMenuBuilder.Create("What would you like to do?")
        .AddItem("Device Info", ct => DeviceInfoMenu.RunAsync())
        .AddItem("USB Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Usb))
        .AddItem("NFC Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Nfc))
        .AddItem("Timeouts", ct => TimeoutsMenu.RunAsync())
        .AddItem("Device Flags", ct => DeviceFlagsMenu.RunAsync())
        .AddItem("Lock Code", ct => LockCodeMenu.RunAsync())
        .AddItem("Factory Reset", ct => ResetMenu.RunAsync())
        .RunAsync(cts.Token);
}

// --- Usage strings ---

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: managementtool <command> [[options]]");
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
