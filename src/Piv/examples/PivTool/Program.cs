using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += Cancel;
try
{
    return await CliRunner.RunWithCancellationExitAsync(() => RunAsync(args, cancellation.Token), Console.Error);
}
finally
{
    Console.CancelKeyPress -= Cancel;
}

void Cancel(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cancellation.Cancel();
}

static async Task<int> RunAsync(string[] args, CancellationToken token)
{
    // Non-interactive mode: any argument triggers CLI mode
    // Interactive mode: no arguments; this branch does not inspect terminal capabilities.
    if (args.Length > 0)
    {
        return await CliRunner.RunAsync(args, token);
    }

    // Application banner (interactive only)
    AnsiConsole.Write(
        new FigletText("PIV Tool")
            .LeftJustified()
            .Color(Color.Green));

    AnsiConsole.MarkupLine("[grey]YubiKey PIV Management Tool - SDK Example Application[/]");
    AnsiConsole.MarkupLine("[yellow]Demo only, not for production.[/]");
    AnsiConsole.MarkupLine("[grey]Tip: run with --help for non-interactive CLI usage.[/]");
    AnsiConsole.WriteLine();

    int result = await InteractiveMenuBuilder.Create("What would you like to do?")
        .AddItem("Device Info", DeviceInfoMenu.RunAsync)
        .AddItem("PIN Management", PinManagementMenu.RunAsync)
        .AddItem("Key Generation", KeyGenerationMenu.RunAsync)
        .AddItem("Certificate Operations", CertificatesMenu.RunAsync)
        .AddItem("Cryptographic Operations", CryptoMenu.RunAsync)
        .AddItem("Key Attestation", AttestationMenu.RunAsync)
        .AddItem("Slot Overview", SlotOverviewMenu.RunAsync)
        .AddItem("Reset PIV", ResetMenu.RunAsync)
        .WithErrorHandler(error =>
        {
            if (error is OperationCanceledException)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(error);
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(error.Message)}[/]");
        })
        .RunAsync(token);
    token.ThrowIfCancellationRequested();
    return result;
}
