using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Cli;
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

return await InteractiveMenuBuilder.Create("What would you like to do?")
    .AddItem("Device Info", _ => DeviceInfoMenu.RunAsync())
    .AddItem("PIN Management", _ => PinManagementMenu.RunAsync())
    .AddItem("Key Generation", _ => KeyGenerationMenu.RunAsync())
    .AddItem("Certificate Operations", _ => CertificatesMenu.RunAsync())
    .AddItem("Cryptographic Operations", _ => CryptoMenu.RunAsync())
    .AddItem("Key Attestation", _ => AttestationMenu.RunAsync())
    .AddItem("Slot Overview", _ => SlotOverviewMenu.RunAsync())
    .AddItem("Reset PIV", _ => ResetMenu.RunAsync())
    .RunAsync(CancellationToken.None);