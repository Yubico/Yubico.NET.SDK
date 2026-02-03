using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("Mgmt Tool")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[grey]YubiKey Management Tool - SDK Example Application[/]");
AnsiConsole.WriteLine();

// Build host with YubiKey services via DI
using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
    })
    .ConfigureServices(services => services
        .AddYubiKeyManagerCore()
        .AddYubiKeyManager())
    .Build();

// Start background services (required for DeviceChanges observable)
await host.StartAsync();

// ConsoleLifetime (from CreateDefaultBuilder) handles Ctrl+C and triggers this token
var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
var manager = host.Services.GetRequiredService<IYubiKeyManager>();

// Main menu loop
while (!ct.IsCancellationRequested)
{
    string choice;
    try
    {
        choice = await new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .PageSize(15)
            .AddChoices(
            [
                "📋 Device Info",
                "🔌 USB Capabilities",
                "📶 NFC Capabilities",
                "⏳ Timeouts",
                "🚩 Device Flags",
                "🔒 Lock Code",
                "⚠️  Factory Reset",
                "❌ Exit"
            ])
            .ShowAsync(AnsiConsole.Console, ct);
    }
    catch (OperationCanceledException)
    {
        break;
    }

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
                await DeviceInfoMenu.RunAsync(manager);
                break;

            case "🔌 USB Capabilities":
                await CapabilitiesMenu.RunAsync(Transport.Usb, manager);
                break;

            case "📶 NFC Capabilities":
                await CapabilitiesMenu.RunAsync(Transport.Nfc, manager);
                break;

            case "⏱️  Timeouts":
                await TimeoutsMenu.RunAsync(manager);
                break;

            case "🚩 Device Flags":
                await DeviceFlagsMenu.RunAsync(manager);
                break;

            case "🔒 Lock Code":
                await LockCodeMenu.RunAsync(manager);
                break;

            case "⚠️  Factory Reset":
                await ResetMenu.RunAsync(manager);
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

await host.StopAsync();

return 0;
