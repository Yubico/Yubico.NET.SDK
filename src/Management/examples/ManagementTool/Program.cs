using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("Mgmt Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiKey Management Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Start monitoring for device events
YubiKeyManager.StartMonitoring();

using var cts = CommandHelper.CreateConsoleCts();

var exitCode = await InteractiveMenuBuilder.Create("What would you like to do?")
    .AddItem("Device Info", ct => DeviceInfoMenu.RunAsync())
    .AddItem("USB Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Usb))
    .AddItem("NFC Capabilities", ct => CapabilitiesMenu.RunAsync(Transport.Nfc))
    .AddItem("Timeouts", ct => TimeoutsMenu.RunAsync())
    .AddItem("Device Flags", ct => DeviceFlagsMenu.RunAsync())
    .AddItem("Lock Code", ct => LockCodeMenu.RunAsync())
    .AddItem("Factory Reset", ct => ResetMenu.RunAsync())
    .RunAsync(cts.Token);

await YubiKeyManager.ShutdownAsync();

return exitCode;
