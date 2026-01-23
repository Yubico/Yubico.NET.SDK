using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Features;

// Application banner
AnsiConsole.Write(
    new FigletText("PIV Tool")
        .LeftJustified()
        .Color(Color.Green));

AnsiConsole.MarkupLine("[grey]YubiKey PIV Management Tool - SDK Example Application[/]");
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
                "🔐 PIN Management",
                "🔑 Key Generation",
                "📜 Certificate Operations",
                "✍️  Cryptographic Operations",
                "🛡️  Key Attestation",
                "📊 Slot Overview",
                "⚠️  Reset PIV",
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
                await DeviceInfoFeature.RunAsync();
                break;

            case "🔐 PIN Management":
                await PinManagementFeature.RunAsync();
                break;

            case "🔑 Key Generation":
                await KeyGenerationFeature.RunAsync();
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
