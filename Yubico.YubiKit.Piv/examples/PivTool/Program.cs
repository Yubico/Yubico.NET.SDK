using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

// Non-interactive mode: any argument triggers CLI mode
// Interactive mode: no args, or stdin is not a TTY
if (args.Length > 0)
{
    return await CliRunner.RunAsync(args);
}

// Application banner (interactive only)
AnsiConsole.Write(
    new FigletText("PIV Tool")
        .LeftJustified()
        .Color(Color.Green));

AnsiConsole.MarkupLine("[grey]YubiKey PIV Management Tool - SDK Example Application[/]");
AnsiConsole.MarkupLine("[grey]Tip: run with --help for non-interactive CLI usage.[/]");
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
                await DeviceInfoMenu.RunAsync();
                break;

            case "🔐 PIN Management":
                await PinManagementMenu.RunAsync();
                break;

            case "🔑 Key Generation":
                await KeyGenerationMenu.RunAsync();
                break;

            case "📜 Certificate Operations":
                await CertificatesMenu.RunAsync();
                break;

            case "✍️  Cryptographic Operations":
                await CryptoMenu.RunAsync();
                break;

            case "🛡️  Key Attestation":
                await AttestationMenu.RunAsync();
                break;

            case "📊 Slot Overview":
                await SlotOverviewMenu.RunAsync();
                break;

            case "⚠️  Reset PIV":
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