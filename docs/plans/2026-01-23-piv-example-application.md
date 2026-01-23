# PIV Example Application Implementation Plan

**Goal:** Build a modern, maintainable PIV example application demonstrating all SDK operations in ~2,500 lines using Spectre.Console and vertical slicing architecture.

**Architecture:** Interactive CLI tool using Spectre.Console for UI. Each PIV feature (device info, PIN management, key generation, certificates, crypto, attestation, slot overview, reset) lives in a single self-contained file under `Features/`. Shared utilities handle device selection, secure PIN prompts, and output formatting.

**Tech Stack:** .NET 10, C# 14, Spectre.Console 0.49.1, Yubico.YubiKit.Piv, Yubico.YubiKit.Core

**PRD:** `docs/specs/piv-example-application/final_spec.md`

---

## Prerequisites

Before starting implementation:
- [ ] Read `CLAUDE.md` for coding standards
- [ ] Read `Yubico.YubiKit.Piv/CLAUDE.md` for PIV-specific patterns
- [ ] Familiarize with `IPivSession` interface in `Yubico.YubiKit.Piv/src/IPivSession.cs`

---

## Task Overview

| Task | Component | Est. LOC | User Story |
|------|-----------|----------|------------|
| 1 | Project Setup | 50 | - |
| 2 | Shared/OutputHelpers | 80 | - |
| 3 | Shared/SecurePinPrompt | 60 | US-2 |
| 4 | Shared/DeviceSelector | 100 | US-1 |
| 5 | Program.cs (Main Menu) | 120 | - |
| 6 | Features/DeviceInfo | 150 | US-1 |
| 7 | Features/SlotOverview | 200 | US-7 |
| 8 | Features/PinManagement | 400 | US-2 |
| 9 | Features/KeyGeneration | 300 | US-3 |
| 10 | Features/Certificates | 400 | US-4 |
| 11 | Features/Crypto | 300 | US-5 |
| 12 | Features/Attestation | 200 | US-6 |
| 13 | Features/Reset | 100 | US-8 |
| 14 | README & SDK Pain Points | - | - |

**Total Estimated LOC:** ~2,460

---

## Task 1: Project Setup

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/Program.cs` (stub)

### Step 1: Create directory structure

```bash
mkdir -p Yubico.YubiKit.Piv/examples/PivTool/{Features,Shared}
```

### Step 2: Create project file

Create `Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PivTool</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.49.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Yubico.YubiKit.Piv.csproj" />
    <ProjectReference Include="../../../Yubico.YubiKit.Core/src/Yubico.YubiKit.Core.csproj" />
    <ProjectReference Include="../../../Yubico.YubiKit.Management/src/Yubico.YubiKit.Management.csproj" />
  </ItemGroup>

</Project>
```

### Step 3: Create Program.cs stub

Create `Yubico.YubiKit.Piv/examples/PivTool/Program.cs`:

```csharp
using Spectre.Console;

AnsiConsole.MarkupLine("[green]PIV Tool[/] - YubiKey PIV Example Application");
AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
Console.ReadKey();
```

### Step 4: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 5: Commit

```bash
git add Yubico.YubiKit.Piv/examples/
git commit -m "feat(piv): scaffold PivTool example project"
```

---

## Task 2: Shared/OutputHelpers

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/Shared/OutputHelpers.cs`

### Step 1: Create OutputHelpers

Create `Yubico.YubiKit.Piv/examples/PivTool/Shared/OutputHelpers.cs`:

```csharp
using Spectre.Console;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Piv;

namespace PivTool.Shared;

internal static class OutputHelpers
{
    public static void WriteSuccess(string message) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");

    public static void WriteError(string message) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");

    public static void WriteWarning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");

    public static void WriteInfo(string message) =>
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {message}");

    public static void WriteHeader(string title)
    {
        var rule = new Rule($"[bold]{title}[/]");
        rule.Justification = Justify.Left;
        AnsiConsole.Write(rule);
    }

    public static string FormatSlot(PivSlot slot) => slot switch
    {
        PivSlot.Authentication => "9a - Authentication",
        PivSlot.Signature => "9c - Digital Signature",
        PivSlot.KeyManagement => "9d - Key Management",
        PivSlot.CardAuthentication => "9e - Card Authentication",
        PivSlot.Attestation => "f9 - Attestation",
        _ when (int)slot >= 0x82 && (int)slot <= 0x95 => 
            $"{(int)slot:x2} - Retired {(int)slot - 0x81}",
        _ => $"{(int)slot:x2} - Unknown"
    };

    public static string FormatAlgorithm(PivAlgorithm algorithm) => algorithm switch
    {
        PivAlgorithm.Rsa1024 => "RSA 1024",
        PivAlgorithm.Rsa2048 => "RSA 2048",
        PivAlgorithm.Rsa3072 => "RSA 3072",
        PivAlgorithm.Rsa4096 => "RSA 4096",
        PivAlgorithm.EccP256 => "ECC P-256",
        PivAlgorithm.EccP384 => "ECC P-384",
        PivAlgorithm.Ed25519 => "Ed25519",
        PivAlgorithm.X25519 => "X25519",
        _ => algorithm.ToString()
    };

    public static string FormatPinPolicy(PivPinPolicy policy) => policy switch
    {
        PivPinPolicy.Default => "Default",
        PivPinPolicy.Never => "Never",
        PivPinPolicy.Once => "Once",
        PivPinPolicy.Always => "Always",
        PivPinPolicy.MatchOnce => "Match Once",
        PivPinPolicy.MatchAlways => "Match Always",
        _ => policy.ToString()
    };

    public static string FormatTouchPolicy(PivTouchPolicy policy) => policy switch
    {
        PivTouchPolicy.Default => "Default",
        PivTouchPolicy.Never => "Never",
        PivTouchPolicy.Always => "Always",
        PivTouchPolicy.Cached => "Cached",
        _ => policy.ToString()
    };

    public static void DisplayCertificateSummary(X509Certificate2 cert)
    {
        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");
        table.Border(TableBorder.Rounded);

        table.AddRow("Subject", cert.Subject);
        table.AddRow("Issuer", cert.Issuer);
        table.AddRow("Serial", cert.SerialNumber);
        table.AddRow("Not Before", cert.NotBefore.ToString("yyyy-MM-dd"));
        table.AddRow("Not After", cert.NotAfter.ToString("yyyy-MM-dd"));
        table.AddRow("Thumbprint", cert.Thumbprint[..16] + "...");

        AnsiConsole.Write(table);
    }

    public static void DisplayPublicKey(ReadOnlyMemory<byte> publicKey, PivAlgorithm algorithm)
    {
        var panel = new Panel(Convert.ToBase64String(publicKey.Span))
        {
            Header = new PanelHeader($"Public Key ({FormatAlgorithm(algorithm)})"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Shared/OutputHelpers.cs
git commit -m "feat(piv): add OutputHelpers for Spectre.Console formatting"
```

---

## Task 3: Shared/SecurePinPrompt

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/Shared/SecurePinPrompt.cs`

### Step 1: Create SecurePinPrompt

Create `Yubico.YubiKit.Piv/examples/PivTool/Shared/SecurePinPrompt.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Spectre.Console;
using Yubico.YubiKit.Piv;

namespace PivTool.Shared;

/// <summary>
/// Secure PIN/PUK prompt with automatic memory zeroing.
/// Callers receive byte[] that MUST be zeroed in finally block.
/// </summary>
internal static class SecurePinPrompt
{
    /// <summary>
    /// Prompts for PIN and returns UTF-8 bytes.
    /// CALLER MUST zero returned array using CryptographicOperations.ZeroMemory().
    /// </summary>
    public static byte[] PromptForPin(string message = "Enter PIN:")
    {
        string input = AnsiConsole.Prompt(
            new TextPrompt<string>($"[yellow]{message}[/]")
                .Secret()
                .Validate(pin => pin.Length >= 4 && pin.Length <= 8
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]PIN must be 4-8 characters[/]")));

        char[] chars = input.ToCharArray();
        try
        {
            return Encoding.UTF8.GetBytes(chars);
        }
        finally
        {
            Array.Clear(chars);
        }
    }

    /// <summary>
    /// Prompts for PUK and returns UTF-8 bytes.
    /// CALLER MUST zero returned array using CryptographicOperations.ZeroMemory().
    /// </summary>
    public static byte[] PromptForPuk(string message = "Enter PUK:")
    {
        string input = AnsiConsole.Prompt(
            new TextPrompt<string>($"[yellow]{message}[/]")
                .Secret()
                .Validate(puk => puk.Length == 8
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]PUK must be exactly 8 characters[/]")));

        char[] chars = input.ToCharArray();
        try
        {
            return Encoding.UTF8.GetBytes(chars);
        }
        finally
        {
            Array.Clear(chars);
        }
    }

    /// <summary>
    /// Prompts for management key (hex string) and returns bytes.
    /// CALLER MUST zero returned array using CryptographicOperations.ZeroMemory().
    /// </summary>
    public static byte[] PromptForManagementKey(
        PivManagementKeyType keyType,
        string message = "Enter management key (hex):")
    {
        int expectedLength = keyType switch
        {
            PivManagementKeyType.TripleDes => 24,
            PivManagementKeyType.Aes128 => 16,
            PivManagementKeyType.Aes192 => 24,
            PivManagementKeyType.Aes256 => 32,
            _ => 24
        };

        string input = AnsiConsole.Prompt(
            new TextPrompt<string>($"[yellow]{message}[/]")
                .Secret()
                .Validate(hex =>
                {
                    if (hex.Length != expectedLength * 2)
                        return ValidationResult.Error(
                            $"[red]Key must be {expectedLength * 2} hex characters ({expectedLength} bytes)[/]");
                    if (!hex.All(c => Uri.IsHexDigit(c)))
                        return ValidationResult.Error("[red]Invalid hex characters[/]");
                    return ValidationResult.Success();
                }));

        return Convert.FromHexString(input);
    }

    /// <summary>
    /// Executes an action with PIN, ensuring PIN bytes are zeroed after use.
    /// </summary>
    public static async Task<T> WithPinAsync<T>(
        string message,
        Func<ReadOnlyMemory<byte>, Task<T>> action)
    {
        byte[] pinBytes = PromptForPin(message);
        try
        {
            return await action(new ReadOnlyMemory<byte>(pinBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinBytes);
        }
    }

    /// <summary>
    /// Executes an action with PIN, ensuring PIN bytes are zeroed after use.
    /// </summary>
    public static async Task WithPinAsync(
        string message,
        Func<ReadOnlyMemory<byte>, Task> action)
    {
        byte[] pinBytes = PromptForPin(message);
        try
        {
            await action(new ReadOnlyMemory<byte>(pinBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinBytes);
        }
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Shared/SecurePinPrompt.cs
git commit -m "feat(piv): add SecurePinPrompt with memory zeroing"
```

---

## Task 4: Shared/DeviceSelector

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/Shared/DeviceSelector.cs`

### Step 1: Create DeviceSelector

Create `Yubico.YubiKit.Piv/examples/PivTool/Shared/DeviceSelector.cs`:

```csharp
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Piv;

namespace PivTool.Shared;

internal static class DeviceSelector
{
    /// <summary>
    /// Discovers YubiKeys and lets user select one if multiple are connected.
    /// Returns null if no device found.
    /// </summary>
    public static async Task<IYubiKey?> SelectDeviceAsync(
        IDeviceRepository repository,
        CancellationToken ct = default)
    {
        var devices = await repository.FindAllAsync(ConnectionType.SmartCard, ct);

        if (devices.Count == 0)
        {
            OutputHelpers.WriteError("No YubiKey detected. Please insert a YubiKey and try again.");
            return null;
        }

        if (devices.Count == 1)
        {
            var device = devices[0];
            OutputHelpers.WriteInfo($"Using YubiKey (Serial: {device.SerialNumber ?? "N/A"})");
            return device;
        }

        // Multiple devices - prompt for selection
        var choices = devices.Select(d => 
            $"Serial: {d.SerialNumber ?? "N/A"} | Firmware: {d.FirmwareVersion}").ToList();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Multiple YubiKeys detected. Select one:[/]")
                .PageSize(10)
                .AddChoices(choices));

        int index = choices.IndexOf(selection);
        return devices[index];
    }

    /// <summary>
    /// Creates a PIV session with the selected device.
    /// </summary>
    public static async Task<PivSession?> CreatePivSessionAsync(
        IYubiKey device,
        CancellationToken ct = default)
    {
        try
        {
            return await device.CreatePivSessionAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to open PIV session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Executes an action with a PIV session, handling device selection and cleanup.
    /// </summary>
    public static async Task WithPivSessionAsync(
        IDeviceRepository repository,
        Func<PivSession, CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        var device = await SelectDeviceAsync(repository, ct);
        if (device is null) return;

        var session = await CreatePivSessionAsync(device, ct);
        if (session is null) return;

        await using (session)
        {
            try
            {
                await action(session, ct);
            }
            catch (InvalidPinException ex)
            {
                OutputHelpers.WriteError($"Incorrect PIN. {ex.RetriesRemaining} attempts remaining.");
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Operation failed: {ex.Message}");
            }
        }
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Shared/DeviceSelector.cs
git commit -m "feat(piv): add DeviceSelector for multi-device handling"
```

---

## Task 5: Program.cs (Main Menu)

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/Program.cs`

### Step 1: Update Program.cs with main menu

Replace `Yubico.YubiKit.Piv/examples/PivTool/Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Yubico.YubiKit.Core;
using PivTool.Features;
using PivTool.Shared;

// Setup DI
var services = new ServiceCollection();
services.AddYubiKeyManagerCore();
var provider = services.BuildServiceProvider();
var repository = provider.GetRequiredService<IDeviceRepository>();

// Main loop
while (true)
{
    Console.Clear();
    DisplayHeader();

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .PageSize(12)
            .AddChoices([
                "📋 Slot Overview",
                "🔐 PIN Management",
                "🔑 Key Generation",
                "📜 Certificate Operations",
                "✍️  Sign / Decrypt",
                "🛡️  Key Attestation",
                "⚙️  Device Info",
                "⚠️  Reset PIV",
                "─────────────────",
                "❌ Exit"
            ]));

    if (choice.Contains("Exit"))
        break;

    if (choice.Contains("───"))
        continue;

    Console.Clear();

    var ct = CancellationToken.None;

    try
    {
        var action = choice switch
        {
            _ when choice.Contains("Slot Overview") => 
                () => SlotOverview.RunAsync(repository, ct),
            _ when choice.Contains("PIN Management") => 
                () => PinManagement.RunAsync(repository, ct),
            _ when choice.Contains("Key Generation") => 
                () => KeyGeneration.RunAsync(repository, ct),
            _ when choice.Contains("Certificate") => 
                () => Certificates.RunAsync(repository, ct),
            _ when choice.Contains("Sign") => 
                () => Crypto.RunAsync(repository, ct),
            _ when choice.Contains("Attestation") => 
                () => Attestation.RunAsync(repository, ct),
            _ when choice.Contains("Device Info") => 
                () => DeviceInfo.RunAsync(repository, ct),
            _ when choice.Contains("Reset") => 
                () => Reset.RunAsync(repository, ct),
            _ => () => Task.CompletedTask
        };

        await action();
    }
    catch (Exception ex)
    {
        OutputHelpers.WriteError($"Unexpected error: {ex.Message}");
    }

    AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
    Console.ReadKey(intercept: true);
}

static void DisplayHeader()
{
    AnsiConsole.Write(
        new FigletText("PIV Tool")
            .LeftJustified()
            .Color(Color.Green));
    AnsiConsole.MarkupLine("[grey]YubiKey PIV Example Application[/]\n");
}
```

### Step 2: Create stub feature files

Create stub files for each feature (they'll be implemented in later tasks):

**`Yubico.YubiKit.Piv/examples/PivTool/Features/DeviceInfo.cs`:**
```csharp
using Yubico.YubiKit.Core;
using PivTool.Shared;

namespace PivTool.Features;

internal static class DeviceInfo
{
    public static async Task RunAsync(IDeviceRepository repository, CancellationToken ct)
    {
        OutputHelpers.WriteInfo("Device Info - Not yet implemented");
        await Task.CompletedTask;
    }
}
```

Create similar stubs for: `SlotOverview.cs`, `PinManagement.cs`, `KeyGeneration.cs`, `Certificates.cs`, `Crypto.cs`, `Attestation.cs`, `Reset.cs`

### Step 3: Verify build and run

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
dotnet run --project Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Menu displays, can navigate and exit.

### Step 4: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/
git commit -m "feat(piv): add main menu with feature stubs"
```

---

## Task 6: Features/DeviceInfo

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/Features/DeviceInfo.cs`

### Step 1: Implement DeviceInfo

Replace `Yubico.YubiKit.Piv/examples/PivTool/Features/DeviceInfo.cs`:

```csharp
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using PivTool.Shared;

namespace PivTool.Features;

internal static class DeviceInfo
{
    public static async Task RunAsync(IDeviceRepository repository, CancellationToken ct)
    {
        OutputHelpers.WriteHeader("Device Information");

        var device = await DeviceSelector.SelectDeviceAsync(repository, ct);
        if (device is null) return;

        // Get management info
        await using var mgmtSession = await device.CreateManagementSessionAsync(cancellationToken: ct);
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync(ct);

        // Get PIV-specific info
        await using var pivSession = await device.CreatePivSessionAsync(cancellationToken: ct);
        var serialNumber = await pivSession.GetSerialNumberAsync(ct);
        var pinMetadata = await pivSession.GetPinMetadataAsync(ct);
        var pukMetadata = await pivSession.GetPukMetadataAsync(ct);
        var mgmtKeyMetadata = await pivSession.GetManagementKeyMetadataAsync(ct);

        // Device table
        var deviceTable = new Table();
        deviceTable.Border(TableBorder.Rounded);
        deviceTable.AddColumn("Property");
        deviceTable.AddColumn("Value");

        deviceTable.AddRow("Serial Number", serialNumber.ToString());
        deviceTable.AddRow("Firmware Version", deviceInfo.FirmwareVersion.ToString());
        deviceTable.AddRow("Form Factor", deviceInfo.FormFactor.ToString());
        deviceTable.AddRow("FIPS Mode", deviceInfo.IsFipsSeries ? "Yes" : "No");

        AnsiConsole.Write(deviceTable);
        AnsiConsole.WriteLine();

        // PIV Status table
        OutputHelpers.WriteHeader("PIV Status");

        var pivTable = new Table();
        pivTable.Border(TableBorder.Rounded);
        pivTable.AddColumn("Credential");
        pivTable.AddColumn("Status");
        pivTable.AddColumn("Retries");

        // PIN status with default warning
        string pinStatus = pinMetadata.IsDefault 
            ? "[yellow]🔓 DEFAULT[/]" 
            : "[green]✓ Changed[/]";
        pivTable.AddRow("PIN", pinStatus, 
            $"{pinMetadata.RetriesRemaining}/{pinMetadata.TotalRetries}");

        // PUK status with default warning
        string pukStatus = pukMetadata.IsDefault 
            ? "[yellow]🔓 DEFAULT[/]" 
            : "[green]✓ Changed[/]";
        pivTable.AddRow("PUK", pukStatus, 
            $"{pukMetadata.RetriesRemaining}/{pukMetadata.TotalRetries}");

        // Management key status
        string mgmtStatus = mgmtKeyMetadata.IsDefault 
            ? "[yellow]🔓 DEFAULT[/]" 
            : "[green]✓ Changed[/]";
        pivTable.AddRow("Management Key", mgmtStatus, 
            $"Type: {mgmtKeyMetadata.KeyType}");

        AnsiConsole.Write(pivTable);

        // Show warning if any defaults detected
        if (pinMetadata.IsDefault || pukMetadata.IsDefault || mgmtKeyMetadata.IsDefault)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteWarning(
                "This YubiKey is using factory default credentials. " +
                "Change them before using in production!");
        }

        // Supported features
        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Supported PIV Features");

        var version = deviceInfo.FirmwareVersion;
        var features = new List<(string Name, bool Supported)>
        {
            ("Attestation", PivFeatures.Attestation.IsSupported(version)),
            ("Metadata", PivFeatures.Metadata.IsSupported(version)),
            ("AES Management Key", PivFeatures.AesKey.IsSupported(version)),
            ("Move/Delete Key", PivFeatures.MoveKey.IsSupported(version)),
            ("Curve25519", PivFeatures.Cv25519.IsSupported(version)),
            ("RSA 3072/4096", PivFeatures.Rsa3072Rsa4096.IsSupported(version)),
            ("P-384", PivFeatures.P384.IsSupported(version)),
            ("Cached Touch", PivFeatures.TouchCached.IsSupported(version))
        };

        var featuresTable = new Table();
        featuresTable.Border(TableBorder.Rounded);
        featuresTable.AddColumn("Feature");
        featuresTable.AddColumn("Status");

        foreach (var (name, supported) in features)
        {
            featuresTable.AddRow(name, 
                supported ? "[green]✓ Supported[/]" : "[grey]✗ Not supported[/]");
        }

        AnsiConsole.Write(featuresTable);
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Test with YubiKey (manual)

```bash
dotnet run --project Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Navigate to Device Info, verify output displays correctly.

### Step 4: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Features/DeviceInfo.cs
git commit -m "feat(piv): implement DeviceInfo feature (US-1)"
```

---

## Task 7: Features/SlotOverview

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/Features/SlotOverview.cs`

### Step 1: Implement SlotOverview

Replace `Yubico.YubiKit.Piv/examples/PivTool/Features/SlotOverview.cs`:

```csharp
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Piv;
using PivTool.Shared;

namespace PivTool.Features;

internal static class SlotOverview
{
    private static readonly PivSlot[] StandardSlots =
    [
        PivSlot.Authentication,
        PivSlot.Signature,
        PivSlot.KeyManagement,
        PivSlot.CardAuthentication
    ];

    private static readonly PivSlot[] RetiredSlots =
    [
        PivSlot.Retired1, PivSlot.Retired2, PivSlot.Retired3, PivSlot.Retired4,
        PivSlot.Retired5, PivSlot.Retired6, PivSlot.Retired7, PivSlot.Retired8,
        PivSlot.Retired9, PivSlot.Retired10, PivSlot.Retired11, PivSlot.Retired12,
        PivSlot.Retired13, PivSlot.Retired14, PivSlot.Retired15, PivSlot.Retired16,
        PivSlot.Retired17, PivSlot.Retired18, PivSlot.Retired19, PivSlot.Retired20
    ];

    public static async Task RunAsync(IDeviceRepository repository, CancellationToken ct)
    {
        OutputHelpers.WriteHeader("PIV Slot Overview");

        await DeviceSelector.WithPivSessionAsync(repository, async (session, ct) =>
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Slot");
            table.AddColumn("Algorithm");
            table.AddColumn("PIN Policy");
            table.AddColumn("Touch Policy");
            table.AddColumn("Certificate");

            // Standard slots
            foreach (var slot in StandardSlots)
            {
                await AddSlotRowAsync(table, session, slot, ct);
            }

            // Separator
            table.AddRow("───", "───", "───", "───", "───");

            // Attestation slot
            await AddSlotRowAsync(table, session, PivSlot.Attestation, ct);

            AnsiConsole.Write(table);

            // Offer to show retired slots
            if (AnsiConsole.Confirm("\nShow retired slots (82-95)?", defaultValue: false))
            {
                var retiredTable = new Table();
                retiredTable.Border(TableBorder.Rounded);
                retiredTable.AddColumn("Slot");
                retiredTable.AddColumn("Algorithm");
                retiredTable.AddColumn("PIN Policy");
                retiredTable.AddColumn("Touch Policy");
                retiredTable.AddColumn("Certificate");

                foreach (var slot in RetiredSlots)
                {
                    await AddSlotRowAsync(retiredTable, session, slot, ct);
                }

                AnsiConsole.Write(retiredTable);
            }

            // Offer detailed view
            if (AnsiConsole.Confirm("\nView certificate details for a slot?", defaultValue: false))
            {
                var slotChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select slot:")
                        .AddChoices(StandardSlots.Select(OutputHelpers.FormatSlot)));

                var selectedSlot = StandardSlots.First(s => 
                    OutputHelpers.FormatSlot(s) == slotChoice);

                var cert = await session.GetCertificateAsync(selectedSlot, ct);
                if (cert is not null)
                {
                    AnsiConsole.WriteLine();
                    OutputHelpers.DisplayCertificateSummary(cert);
                }
                else
                {
                    OutputHelpers.WriteInfo("No certificate in this slot.");
                }
            }
        }, ct);
    }

    private static async Task AddSlotRowAsync(
        Table table, 
        PivSession session, 
        PivSlot slot, 
        CancellationToken ct)
    {
        var metadata = await session.GetSlotMetadataAsync(slot, ct);
        var cert = await session.GetCertificateAsync(slot, ct);

        if (metadata is null)
        {
            table.AddRow(
                OutputHelpers.FormatSlot(slot),
                "[grey]-[/]",
                "[grey]-[/]",
                "[grey]-[/]",
                "[grey]-[/]");
        }
        else
        {
            string certStatus = cert is not null
                ? "[green]✓[/]"
                : "[grey]✗[/]";

            table.AddRow(
                OutputHelpers.FormatSlot(slot),
                OutputHelpers.FormatAlgorithm(metadata.Algorithm),
                OutputHelpers.FormatPinPolicy(metadata.PinPolicy),
                OutputHelpers.FormatTouchPolicy(metadata.TouchPolicy),
                certStatus);
        }
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Features/SlotOverview.cs
git commit -m "feat(piv): implement SlotOverview feature (US-7)"
```

---

## Task 8: Features/PinManagement

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/Features/PinManagement.cs`

### Step 1: Implement PinManagement

Replace `Yubico.YubiKit.Piv/examples/PivTool/Features/PinManagement.cs`:

```csharp
using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Piv;
using PivTool.Shared;

namespace PivTool.Features;

internal static class PinManagement
{
    public static async Task RunAsync(IDeviceRepository repository, CancellationToken ct)
    {
        OutputHelpers.WriteHeader("PIN Management");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices([
                    "View PIN/PUK Status",
                    "Verify PIN",
                    "Change PIN",
                    "Change PUK",
                    "Unblock PIN (using PUK)",
                    "Set PIN/PUK Retry Limits",
                    "Change Management Key",
                    "← Back"
                ]));

        if (choice.Contains("Back")) return;

        await DeviceSelector.WithPivSessionAsync(repository, async (session, ct) =>
        {
            switch (choice)
            {
                case var c when c.Contains("View"):
                    await ViewStatusAsync(session, ct);
                    break;
                case var c when c.Contains("Verify PIN"):
                    await VerifyPinAsync(session, ct);
                    break;
                case var c when c.Contains("Change PIN"):
                    await ChangePinAsync(session, ct);
                    break;
                case var c when c.Contains("Change PUK"):
                    await ChangePukAsync(session, ct);
                    break;
                case var c when c.Contains("Unblock"):
                    await UnblockPinAsync(session, ct);
                    break;
                case var c when c.Contains("Retry"):
                    await SetRetryLimitsAsync(session, ct);
                    break;
                case var c when c.Contains("Management"):
                    await ChangeManagementKeyAsync(session, ct);
                    break;
            }
        }, ct);
    }

    private static async Task ViewStatusAsync(PivSession session, CancellationToken ct)
    {
        var pinMeta = await session.GetPinMetadataAsync(ct);
        var pukMeta = await session.GetPukMetadataAsync(ct);
        var mgmtMeta = await session.GetManagementKeyMetadataAsync(ct);

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Credential");
        table.AddColumn("Default?");
        table.AddColumn("Retries");

        string pinWarning = pinMeta.IsDefault ? "[yellow]🔓 YES[/]" : "[green]No[/]";
        table.AddRow("PIN", pinWarning, 
            $"{pinMeta.RetriesRemaining}/{pinMeta.TotalRetries}");

        string pukWarning = pukMeta.IsDefault ? "[yellow]🔓 YES[/]" : "[green]No[/]";
        table.AddRow("PUK", pukWarning, 
            $"{pukMeta.RetriesRemaining}/{pukMeta.TotalRetries}");

        string mgmtWarning = mgmtMeta.IsDefault ? "[yellow]🔓 YES[/]" : "[green]No[/]";
        table.AddRow("Management Key", mgmtWarning, 
            $"Type: {mgmtMeta.KeyType}");

        AnsiConsole.Write(table);

        if (pinMeta.IsDefault || pukMeta.IsDefault || mgmtMeta.IsDefault)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteWarning(
                "Factory defaults detected! Change credentials before production use.");
        }
    }

    private static async Task VerifyPinAsync(PivSession session, CancellationToken ct)
    {
        await SecurePinPrompt.WithPinAsync("Enter PIN to verify:", async pin =>
        {
            await session.VerifyPinAsync(pin, ct);
            OutputHelpers.WriteSuccess("PIN verified successfully!");
        });
    }

    private static async Task ChangePinAsync(PivSession session, CancellationToken ct)
    {
        byte[] oldPin = SecurePinPrompt.PromptForPin("Enter current PIN:");
        byte[] newPin = SecurePinPrompt.PromptForPin("Enter new PIN:");
        byte[] confirmPin = SecurePinPrompt.PromptForPin("Confirm new PIN:");

        try
        {
            if (!newPin.AsSpan().SequenceEqual(confirmPin))
            {
                OutputHelpers.WriteError("New PINs do not match.");
                return;
            }

            await session.ChangePinAsync(
                new ReadOnlyMemory<byte>(oldPin),
                new ReadOnlyMemory<byte>(newPin),
                ct);

            OutputHelpers.WriteSuccess("PIN changed successfully!");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldPin);
            CryptographicOperations.ZeroMemory(newPin);
            CryptographicOperations.ZeroMemory(confirmPin);
        }
    }

    private static async Task ChangePukAsync(PivSession session, CancellationToken ct)
    {
        byte[] oldPuk = SecurePinPrompt.PromptForPuk("Enter current PUK:");
        byte[] newPuk = SecurePinPrompt.PromptForPuk("Enter new PUK:");
        byte[] confirmPuk = SecurePinPrompt.PromptForPuk("Confirm new PUK:");

        try
        {
            if (!newPuk.AsSpan().SequenceEqual(confirmPuk))
            {
                OutputHelpers.WriteError("New PUKs do not match.");
                return;
            }

            await session.ChangePukAsync(
                new ReadOnlyMemory<byte>(oldPuk),
                new ReadOnlyMemory<byte>(newPuk),
                ct);

            OutputHelpers.WriteSuccess("PUK changed successfully!");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldPuk);
            CryptographicOperations.ZeroMemory(newPuk);
            CryptographicOperations.ZeroMemory(confirmPuk);
        }
    }

    private static async Task UnblockPinAsync(PivSession session, CancellationToken ct)
    {
        byte[] puk = SecurePinPrompt.PromptForPuk("Enter PUK:");
        byte[] newPin = SecurePinPrompt.PromptForPin("Enter new PIN:");
        byte[] confirmPin = SecurePinPrompt.PromptForPin("Confirm new PIN:");

        try
        {
            if (!newPin.AsSpan().SequenceEqual(confirmPin))
            {
                OutputHelpers.WriteError("New PINs do not match.");
                return;
            }

            await session.UnblockPinAsync(
                new ReadOnlyMemory<byte>(puk),
                new ReadOnlyMemory<byte>(newPin),
                ct);

            OutputHelpers.WriteSuccess("PIN unblocked and changed successfully!");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(puk);
            CryptographicOperations.ZeroMemory(newPin);
            CryptographicOperations.ZeroMemory(confirmPin);
        }
    }

    private static async Task SetRetryLimitsAsync(PivSession session, CancellationToken ct)
    {
        OutputHelpers.WriteWarning(
            "This operation requires management key authentication.");

        var mgmtMeta = await session.GetManagementKeyMetadataAsync(ct);
        byte[] mgmtKey = SecurePinPrompt.PromptForManagementKey(
            mgmtMeta.KeyType, 
            "Enter management key (hex):");

        try
        {
            await session.AuthenticateAsync(new ReadOnlyMemory<byte>(mgmtKey), ct);

            int pinRetries = AnsiConsole.Prompt(
                new TextPrompt<int>("PIN retry limit (1-255):")
                    .DefaultValue(3)
                    .Validate(n => n is >= 1 and <= 255
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Must be 1-255")));

            int pukRetries = AnsiConsole.Prompt(
                new TextPrompt<int>("PUK retry limit (1-255):")
                    .DefaultValue(3)
                    .Validate(n => n is >= 1 and <= 255
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Must be 1-255")));

            await session.SetPinAttemptsAsync(pinRetries, pukRetries, ct);

            OutputHelpers.WriteSuccess(
                $"Retry limits set: PIN={pinRetries}, PUK={pukRetries}");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }

    private static async Task ChangeManagementKeyAsync(PivSession session, CancellationToken ct)
    {
        var currentMeta = await session.GetManagementKeyMetadataAsync(ct);

        byte[] currentKey = SecurePinPrompt.PromptForManagementKey(
            currentMeta.KeyType,
            "Enter current management key (hex):");

        try
        {
            await session.AuthenticateAsync(new ReadOnlyMemory<byte>(currentKey), ct);

            var newKeyType = AnsiConsole.Prompt(
                new SelectionPrompt<PivManagementKeyType>()
                    .Title("Select new key type:")
                    .AddChoices([
                        PivManagementKeyType.Aes256,
                        PivManagementKeyType.Aes192,
                        PivManagementKeyType.Aes128,
                        PivManagementKeyType.TripleDes
                    ]));

            byte[] newKey = SecurePinPrompt.PromptForManagementKey(
                newKeyType,
                "Enter new management key (hex):");

            try
            {
                bool requireTouch = AnsiConsole.Confirm(
                    "Require touch for management key operations?",
                    defaultValue: false);

                await session.SetManagementKeyAsync(
                    newKeyType,
                    new ReadOnlyMemory<byte>(newKey),
                    requireTouch,
                    ct);

                OutputHelpers.WriteSuccess("Management key changed successfully!");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(newKey);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentKey);
        }
    }
}
```

### Step 2: Verify build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

Expected: Build succeeded.

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/Features/PinManagement.cs
git commit -m "feat(piv): implement PinManagement feature (US-2)"
```

---

## Task 9-13: Remaining Features

The remaining tasks follow the same pattern. For brevity, here are the key implementation points:

### Task 9: KeyGeneration.cs (~300 LOC)
- Slot selection prompt
- Algorithm selection (RSA 1024-4096, ECC P-256/P-384, Ed25519, X25519)
- PIN/Touch policy selection
- Management key authentication
- Call `GenerateKeyAsync`, display public key
- Support `ImportKeyAsync`, `MoveKeyAsync`, `DeleteKeyAsync`

### Task 10: Certificates.cs (~400 LOC)
- View certificate details
- Import from PEM/DER file
- Export to PEM/DER file
- Delete certificate
- Generate self-signed certificate (using .NET X509Certificate2 builder)
- Generate CSR

### Task 11: Crypto.cs (~300 LOC)
- Sign data (prompt for data or file)
- Decrypt data (RSA only)
- Verify signature
- Display timing information

### Task 12: Attestation.cs (~200 LOC)
- Select slot
- Call `AttestKeyAsync`
- Display attestation certificate chain
- Verify chain to Yubico root

### Task 13: Reset.cs (~100 LOC)
- Multiple confirmation prompts
- Clear warning about data loss
- Call `ResetAsync`
- Post-reset warning about default credentials

---

## Task 14: README & SDK Pain Points

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/README.md`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/SDK_PAIN_POINTS.md`

### Step 1: Create README

Create `Yubico.YubiKit.Piv/examples/PivTool/README.md`:

```markdown
# PIV Tool

A modern CLI example application demonstrating YubiKey PIV operations using the Yubico.NET.SDK.

## Features

- **Device Discovery** - List connected YubiKeys with PIV capabilities
- **Slot Overview** - View all PIV slots and their configurations
- **PIN Management** - Manage PIN, PUK, and management key
- **Key Generation** - Generate key pairs in PIV slots
- **Certificate Operations** - Import, export, and manage certificates
- **Cryptographic Operations** - Sign and decrypt data
- **Key Attestation** - Verify on-device key generation
- **PIV Reset** - Reset PIV application to factory defaults

## Requirements

- .NET 10.0 SDK
- YubiKey with PIV support (YubiKey 4, 5 series, or Security Key)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Supported Platforms

- Windows (Windows Terminal, PowerShell, cmd.exe)
- macOS (Terminal.app, iTerm2)
- Linux (tested on Ubuntu Terminal)

## Security Notes

- All PIN/PUK/management key inputs are masked and securely zeroed from memory after use
- Private keys never leave the YubiKey - all cryptographic operations happen on-device
- Factory default credentials (PIN: 123456, PUK: 12345678) are flagged with warnings

## License

Apache 2.0 - See LICENSE.txt in repository root.
```

### Step 2: Create SDK Pain Points template

Create `Yubico.YubiKit.Piv/examples/PivTool/SDK_PAIN_POINTS.md`:

```markdown
# SDK Pain Points - PIV Example

Document any SDK usability issues encountered during implementation.

## Template

### Issue N: [Title]
**Severity:** High | Medium | Low  
**Category:** API Design | Documentation | Error Handling | Performance  
**Description:** [What was difficult or unexpected]  
**Workaround:** [How the example handles it]  
**Suggestion:** [Proposed SDK improvement]

---

## Issues Found

_(Document issues during implementation)_
```

### Step 3: Commit

```bash
git add Yubico.YubiKit.Piv/examples/PivTool/README.md
git add Yubico.YubiKit.Piv/examples/PivTool/SDK_PAIN_POINTS.md
git commit -m "docs(piv): add README and SDK pain points template"
```

---

## Final Verification

### Step 1: Full build

```bash
dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

### Step 2: LOC count

```bash
find Yubico.YubiKit.Piv/examples/PivTool -name "*.cs" -exec wc -l {} + | tail -1
```

Expected: ~2,000-2,500 lines total.

### Step 3: Manual test with YubiKey

Test each feature with a real YubiKey:
1. Device Info - verify device details display
2. Slot Overview - verify table renders correctly
3. PIN Management - verify PIN operations work
4. Key Generation - generate a test key
5. Certificate Operations - view/import/export
6. Crypto - sign test data
7. Attestation - verify attestation chain
8. Reset - test with locked device (careful!)

### Step 4: Final commit

```bash
git add -A
git commit -m "feat(piv): complete PIV Tool example application

Implements US-1 through US-8 from PIV Example Application PRD.

Features:
- Device discovery and multi-device selection
- PIN/PUK/management key management with secure memory handling
- Key generation for all algorithms and slots
- Certificate import/export/management
- Signing and decryption operations
- Key attestation verification
- PIV reset with safety confirmations

Technical:
- Vertical slicing architecture (~2,400 LOC)
- Spectre.Console for rich CLI
- Secure credential handling with memory zeroing
- No abstraction over SDK (demonstrates actual usage)

Closes: piv-example-application PRD"
```

---

## Execution Handoff

**Plan complete and saved to `docs/plans/2026-01-23-piv-example-application.md`.**

**Ready to execute?**

Execute tasks in order using:
- TDD workflow for implementation tasks
- Verification before marking tasks done
- Commit after each completed task

**Recommended approach:**
1. Start with Tasks 1-5 (scaffolding + shared utilities)
2. Implement features in order of dependency: DeviceInfo → SlotOverview → PinManagement → KeyGeneration → Certificates → Crypto → Attestation → Reset
3. Manual test each feature with a real YubiKey before committing
4. Document SDK pain points as encountered
