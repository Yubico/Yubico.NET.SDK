using Spectre.Console;

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
                // await DeviceInfoMenu.RunAsync();
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "🔌 USB Capabilities":
                // await CapabilitiesMenu.RunAsync(Transport.Usb);
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "📶 NFC Capabilities":
                // await CapabilitiesMenu.RunAsync(Transport.Nfc);
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "⏱️  Timeouts":
                // await TimeoutsMenu.RunAsync();
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "🚩 Device Flags":
                // await DeviceFlagsMenu.RunAsync();
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "🔒 Lock Code":
                // await LockCodeMenu.RunAsync();
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
                break;

            case "⚠️  Factory Reset":
                // await ResetMenu.RunAsync();
                AnsiConsole.MarkupLine("[yellow]Not yet implemented[/]");
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
