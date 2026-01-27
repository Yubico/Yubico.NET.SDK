using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("Mgmt Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiKey Management Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Main menu loop
while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .PageSize(15)
            .AddChoices(
            [
                "📋 Device Info",
                "🔌 USB Capabilities",
                "📶 NFC Capabilities",
                "⏱️  Timeouts",
                "🚩 Device Flags",
                "🔒 Lock Code",
                "⚠️  Factory Reset",
                "❌ Exit"
            ]));

    if (choice == "❌ Exit")
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    try
    {
        switch (choice)
        {
            case "📋 Device Info":
                await DeviceInfoMenu.RunAsync();
                break;

            case "🔌 USB Capabilities":
                await CapabilitiesMenu.RunAsync(Transport.Usb);
                break;

            case "📶 NFC Capabilities":
                await CapabilitiesMenu.RunAsync(Transport.Nfc);
                break;

            case "⏱️  Timeouts":
                await TimeoutsMenu.RunAsync();
                break;

            case "🚩 Device Flags":
                await DeviceFlagsMenu.RunAsync();
                break;

            case "🔒 Lock Code":
                await LockCodeMenu.RunAsync();
                break;

            case "⚠️  Factory Reset":
                await ResetMenu.RunAsync();
                break;

            default:
                AnsiConsole.MarkupLine($"[yellow]Selected: {choice} - Not yet implemented[/]");
                break;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    }

    AnsiConsole.WriteLine();
}

return 0;
