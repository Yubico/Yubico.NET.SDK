# SDK Service Layer Integration Skill

Pattern for integrating new device/connection types into the SDK's service-oriented architecture.

## When to Use

Triggers:
- "Add {feature} to SDK"
- "Integrate {component} into FindYubiKeys"
- "Create service for {device type}"
- Adding new transport (HID, NFC, Bluetooth, etc.)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    SDK SERVICE ARCHITECTURE                  │
│                                                              │
│  IFindXxxDevices ──┐                                        │
│  IFindYyyDevices ──┼──► FindYubiKeys ──► YubiKeyFactory     │
│  IFindZzzDevices ──┘         │                │             │
│                              ▼                ▼             │
│                    IYubiKey instances    DI Container       │
│                              │                              │
│                              ▼                              │
│                    DeviceRepositoryCached                   │
└─────────────────────────────────────────────────────────────┘
```

## Integration Steps

### Step 1: Create Finder Interface

Location: `Yubico.YubiKit.Core/src/{Feature}/IFind{Feature}Devices.cs`

```csharp
namespace Yubico.YubiKit.Core.{Feature};

/// <summary>
/// Service for discovering {Feature} devices.
/// </summary>
public interface IFind{Feature}Devices
{
    /// <summary>
    /// Finds all {Feature} devices.
    /// </summary>
    Task<IReadOnlyList<I{Feature}Device>> FindAllAsync(
        CancellationToken cancellationToken = default);
}
```

**Reference:** `IFindPcscDevices` for pattern

### Step 2: Implement Finder Service

Location: `Yubico.YubiKit.Core/src/{Feature}/Find{Feature}Devices.cs`

```csharp
namespace Yubico.YubiKit.Core.{Feature};

public sealed class Find{Feature}Devices : IFind{Feature}Devices
{
    public Task<IReadOnlyList<I{Feature}Device>> FindAllAsync(
        CancellationToken cancellationToken = default)
    {
        // Platform-specific implementation
        if (OperatingSystem.IsMacOS())
            return FindAllMacOSAsync(cancellationToken);
        if (OperatingSystem.IsWindows())
            return FindAllWindowsAsync(cancellationToken);
        if (OperatingSystem.IsLinux())
            return FindAllLinuxAsync(cancellationToken);
        
        return Task.FromResult<IReadOnlyList<I{Feature}Device>>([]);
    }

    [SupportedOSPlatform("macos")]
    private Task<IReadOnlyList<I{Feature}Device>> FindAllMacOSAsync(
        CancellationToken cancellationToken)
    {
        // Call platform-specific enumeration
        var devices = MacOS{Feature}Device.GetList();
        return Task.FromResult(devices);
    }
    
    // Similar for Windows/Linux...
}
```

### Step 3: Create IYubiKey Wrapper

Location: `Yubico.YubiKit.Core/src/YubiKey/{Feature}YubiKey.cs`

```csharp
namespace Yubico.YubiKit.Core.YubiKey;

public sealed class {Feature}YubiKey : IYubiKey
{
    private readonly I{Feature}Device _device;

    public {Feature}YubiKey(I{Feature}Device device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
    }

    /// <summary>
    /// Device ID format: {feature}:{VendorId:X4}:{ProductId:X4}:{UniqueId}
    /// </summary>
    public string DeviceId => $"{feature}:{_device.VendorId:X4}:{_device.ProductId:X4}:{_device.UniqueId}";

    public async Task<TConnection> ConnectAsync<TConnection>(
        CancellationToken cancellationToken = default)
        where TConnection : IConnection
    {
        // Connection type routing
        if (typeof(TConnection) == typeof(IAsync{Feature}Connection))
        {
            var connection = await _device.ConnectAsync(cancellationToken);
            return (TConnection)(object)connection;
        }

        throw new NotSupportedException(
            $"Connection type {typeof(TConnection).Name} not supported for {Feature} devices");
    }
}
```

### Step 4: Update YubiKeyFactory

Location: `Yubico.YubiKit.Core/src/YubiKey/YubiKeyFactory.cs`

```csharp
public IYubiKey Create(IDevice device) =>
    device switch
    {
        IPcscDevice pcscDevice => CreatePcscYubiKey(pcscDevice),
        IHidDevice hidDevice => CreateHidYubiKey(hidDevice),
        I{Feature}Device {feature}Device => Create{Feature}YubiKey({feature}Device),
        _ => throw new NotSupportedException(
            $"Device type {device.GetType().Name} is not supported")
    };

private {Feature}YubiKey Create{Feature}YubiKey(I{Feature}Device device) =>
    new(device);
```

### Step 5: Update FindYubiKeys

Location: `Yubico.YubiKit.Core/src/YubiKey/FindYubiKeys.cs`

```csharp
public sealed class FindYubiKeys : IFindYubiKeys
{
    private readonly IFindPcscDevices _pcscFinder;
    private readonly IFindHidDevices _hidFinder;
    private readonly IFind{Feature}Devices _{feature}Finder;  // Add
    private readonly IYubiKeyFactory _factory;

    public FindYubiKeys(
        IFindPcscDevices pcscFinder,
        IFindHidDevices hidFinder,
        IFind{Feature}Devices {feature}Finder,  // Add
        IYubiKeyFactory factory)
    {
        _pcscFinder = pcscFinder;
        _hidFinder = hidFinder;
        _{feature}Finder = {feature}Finder;  // Add
        _factory = factory;
    }

    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        CancellationToken cancellationToken = default)
    {
        // Run all finders in parallel
        var pcscTask = _pcscFinder.FindAllAsync(cancellationToken);
        var hidTask = _hidFinder.FindAllAsync(cancellationToken);
        var {feature}Task = _{feature}Finder.FindAllAsync(cancellationToken);  // Add

        await Task.WhenAll(pcscTask, hidTask, {feature}Task);

        // Aggregate results
        var yubiKeys = new List<IYubiKey>();
        
        foreach (var device in pcscTask.Result)
            yubiKeys.Add(_factory.Create(device));
        foreach (var device in hidTask.Result)
            yubiKeys.Add(_factory.Create(device));
        foreach (var device in {feature}Task.Result)  // Add
            yubiKeys.Add(_factory.Create(device));

        return yubiKeys;
    }
}
```

### Step 6: Register in DI

Location: `Yubico.YubiKit.Core/src/DependencyInjection.cs`

```csharp
public static IServiceCollection AddYubiKitCore(this IServiceCollection services)
{
    // Existing registrations...
    services.AddTransient<IFindPcscDevices, FindPcscDevices>();
    services.AddTransient<IFindHidDevices, FindHidDevices>();
    
    // Add new finder
    services.AddTransient<IFind{Feature}Devices, Find{Feature}Devices>();
    
    // Factory and aggregator
    services.AddTransient<IYubiKeyFactory, YubiKeyFactory>();
    services.AddTransient<IFindYubiKeys, FindYubiKeys>();
    
    return services;
}
```

### Step 7: Verify Integration

```bash
# Build to verify compilation
dotnet build.cs build

# Run tests to verify DI resolution
dotnet build.cs test --project Core
```

## DeviceId Format Convention

Consistent format across all device types:

| Device Type | Format | Example |
|-------------|--------|---------|
| PCSC | `pcsc:{ReaderName}` | `pcsc:Yubico YubiKey OTP+FIDO+CCID` |
| HID | `hid:{VID:X4}:{PID:X4}:{Usage:X4}` | `hid:1050:0407:F1D0` |
| NFC | `nfc:{UID}` | `nfc:04A2B3C4D5E6F7` |
| BLE | `ble:{MacAddress}` | `ble:AA:BB:CC:DD:EE:FF` |

## Checklist

Before completing service integration:

- [ ] `IFind{Feature}Devices` interface created
- [ ] `Find{Feature}Devices` implementation created
- [ ] Platform-specific finders call correct enumeration
- [ ] `{Feature}YubiKey` wrapper implements `IYubiKey`
- [ ] DeviceId format documented and consistent
- [ ] `YubiKeyFactory` switch expression updated
- [ ] `FindYubiKeys` runs new finder in parallel
- [ ] DI registration added
- [ ] Build passes
- [ ] DI resolution works (test or manual verification)

## Reference Files

When integrating a new service, reference these existing implementations:

- **Interface pattern:** `IFindPcscDevices.cs`
- **Service pattern:** `FindPcscDevices.cs`
- **Wrapper pattern:** `PcscYubiKey.cs`
- **Factory pattern:** `YubiKeyFactory.cs`
- **Aggregator pattern:** `FindYubiKeys.cs`
- **DI pattern:** `DependencyInjection.cs`
