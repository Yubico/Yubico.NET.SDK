---
name: scaffold-cli-tool
description: Use when creating new CLI tools for YubiKey applications - generates PivTool-style project structure with Spectre.Console
---

# CLI Tool Scaffold

## Overview

Generates a complete CLI tool project following the PivTool architecture pattern used throughout this SDK.

**Core principle:** CLI tools should have consistent structure: project scaffold, CLI infrastructure (output helpers, device selector, prompts), SDK examples (pure business logic), and menu handlers (UI).

## Use when

**Use this skill when:**
- Creating a new CLI tool for a YubiKey application (ManagementTool, OathTool, FidoTool, etc.)
- Porting an existing CLI tool to follow PivTool patterns
- Need consistent Spectre.Console menu loop with device selection

**Don't use when:**
- Building a library (no CLI entry point)
- Creating a test project
- Modifying an existing CLI tool (just edit directly)

## Project Structure

Generate the following directory structure under `Yubico.YubiKit.{Module}/examples/{ToolName}/`:

```
{ToolName}/
├── {ToolName}.csproj
├── Program.cs
├── README.md
├── Cli/
│   ├── Output/
│   │   └── OutputHelpers.cs
│   ├── Prompts/
│   │   └── DeviceSelector.cs
│   └── Menus/
│       └── {Feature}Menu.cs (one per feature)
└── {Module}Examples/
    └── Results/
        └── {Operation}Result.cs
```

## File Templates

### 1. {ToolName}.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>Yubico.YubiKit.{Module}.Examples.{ToolName}</RootNamespace>
    <AssemblyName>{ToolName}</AssemblyName>
    <IsPackable>false</IsPackable>
    
    <!-- Suppress analyzer warnings that are noisy for example/CLI apps -->
    <NoWarn>$(NoWarn);CA1852</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Yubico.YubiKit.{Module}.csproj" />
    <ProjectReference Include="..\..\..\Yubico.YubiKit.Core\src\Yubico.YubiKit.Core.csproj" />
    <ProjectReference Include="..\..\..\Yubico.YubiKit.Management\src\Yubico.YubiKit.Management.csproj" />
  </ItemGroup>

</Project>
```

**Notes:**
- `CA1852` suppresses "sealed class" warnings for Program.cs top-level statements
- Always reference Core and Management (device discovery)
- Reference the module's src project

### 2. Program.cs

```csharp
using Spectre.Console;
using Yubico.YubiKit.{Module}.Examples.{ToolName}.Cli.Menus;

// Application banner
AnsiConsole.Write(
    new FigletText("{Tool Name}")
        .LeftJustified()
        .Color(Color.Green));

AnsiConsole.MarkupLine("[grey]YubiKey {Module} Tool - SDK Example Application[/]");
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
                // Add feature-specific menu items with emojis
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
            // Add cases for each menu item
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
```

### 3. Cli/Output/OutputHelpers.cs

Copy from `Yubico.YubiKit.Piv/examples/PivTool/Cli/Output/OutputHelpers.cs` and:
- Change namespace to `Yubico.YubiKit.{Module}.Examples.{ToolName}.Cli.Output`
- Remove module-specific helpers (e.g., `SetupTouchNotification` for PIV)
- Keep common helpers: `WriteHeader`, `WriteSuccess`, `WriteError`, `WriteWarning`, `WriteInfo`, `WriteKeyValue`, `WriteHex`, `CreatePanel`, `CreateTable`, `ConfirmDangerous`, `ConfirmDestructive`, `WaitForKey`

### 4. Cli/Prompts/DeviceSelector.cs

Copy from PivTool and adapt:
- Change namespace
- Modify `ConnectionType` filter based on module requirements:
  - PIV: `ConnectionType.SmartCard` only
  - Management: All transports (`SmartCard`, `Fido`, `OtpHid`)
  - FIDO2: `ConnectionType.Fido`

### 5. Result Types

```csharp
namespace Yubico.YubiKit.{Module}.Examples.{ToolName}.{Module}Examples.Results;

/// <summary>
/// Result of {operation} operation.
/// </summary>
public record {Operation}Result(
    bool Success,
    // Add operation-specific properties
    string? ErrorMessage = null);
```

### 6. Menu Handlers

```csharp
namespace Yubico.YubiKit.{Module}.Examples.{ToolName}.Cli.Menus;

public static class {Feature}Menu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // 1. Select device
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            OutputHelpers.WriteWarning("No device selected.");
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // 2. Open connection and session
        await using var connection = await selection.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await {Module}Session.CreateAsync(connection, cancellationToken: cancellationToken);

        // 3. Perform operation
        // 4. Display results
    }
}
```

## Dependency Order

Create files in this order to avoid build errors:

1. **Project scaffold:** csproj, README.md
2. **Result types:** No dependencies
3. **SDK examples:** Depend on result types
4. **CLI infrastructure:** OutputHelpers, DeviceSelector (depend on SDK types)
5. **Menu handlers:** Depend on all above
6. **Program.cs wiring:** Final integration (add `using` statements last)

## Common Mistakes

**❌ Adding menu `using` statements before menu files exist**
Program.cs will fail to build. Add menu imports only after creating menu classes.

**❌ Using wrong type names**
Search codebase first: `grep "enum DeviceCapabilities"` not `Capabilities`.

**❌ Forgetting CA1852 suppression**
Top-level Program.cs triggers "seal this class" warnings without suppression.

**❌ Hardcoding ConnectionType**
Different modules require different transports. Check module requirements.

## Verification

- [ ] `dotnet build {ToolName}.csproj` succeeds
- [ ] Project structure matches PivTool layout
- [ ] OutputHelpers has no module-specific code
- [ ] DeviceSelector filters correct ConnectionType(s)
- [ ] Program.cs has FigletText banner and menu loop
- [ ] README.md documents all features

## Related Skills

- `build-project` - Build after creating project
- `secure-credential-prompt` - For PIN/password prompts in menu handlers
- `tdd` - Write tests for SDK example classes
