using Spectre.Console;

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

    // Placeholder for feature dispatch - will be implemented in later tasks
    AnsiConsole.MarkupLine($"[yellow]Selected: {choice} - Not yet implemented[/]");
    AnsiConsole.WriteLine();
}

return 0;
