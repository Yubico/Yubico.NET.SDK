# IYubiKey Composite Device Refactor Plan

**Goal:** Align device abstraction with yubikey-manager pattern where users interact with a composite representing the physical YubiKey (all transports), not individual transport endpoints.

## Agreed Naming

| Type | Purpose |
|------|---------|
| `IYubiKey` (new) | Composite representing the physical YubiKey (aggregates all transports) |
| `IYubiKeyReference` (renamed) | Transport-specific device reference (what current `IYubiKey` becomes) |
| `IConnection` | Open communication channel (unchanged) |

## Decisions Made

1. **Separate PRs** - Step 1 must be completed before Step 2
2. **Composite factory** - New `CompositeYubiKeyFactory` will handle correlation logic
3. **Extension methods on composite** - `CreateManagementSessionAsync()` etc. will be on `IYubiKey`, not `IYubiKeyReference`

## DeviceId Semantics

Both `IYubiKeyReference` and `IYubiKey` have a `DeviceId` property, but they serve different purposes:

| Type | `DeviceId` Purpose | Uniqueness Scope | Format |
|------|-------------------|------------------|--------|
| `IYubiKeyReference` | Cache key for transport endpoints, internal tracking, debugging | Unique per transport interface | `pcsc:{ReaderName}` or `hid:{VID}:{PID}:{Usage}` |
| `IYubiKey` | Cache key for physical devices, user display, `DeviceEvent` tracking | Unique per physical YubiKey | `{SerialNumber}` or `fp:{hash}` (fallback) |

**Key insight:** A single physical YubiKey has multiple `IYubiKeyReference.DeviceId` values (one per transport), but only one `IYubiKey.DeviceId`.

### IYubiKeyReference.DeviceId (existing)

```csharp
// Transport-level identifier - unique per transport interface
// Examples:
//   "pcsc:Yubico YubiKey OTP+FIDO+CCID 0"
//   "hid:1050:0407:0001"
//   "hid:1050:0407:F1D0"
```

### IYubiKey.DeviceId (new)

```csharp
// Physical device identifier - unique per physical YubiKey
// Computed as:
DeviceId = Identity?.SerialNumber?.ToString() 
    ?? $"fp:{ComputeCorrelationFingerprint()}";

// Examples:
//   "12345678"          (serial available)
//   "fp:a1b2c3d4"       (serial unavailable, e.g., Security Key)
```

---

## Step 1: Rename `IYubiKey` → `IYubiKeyReference` (PR #1) ✅ COMPLETE

**Committed.**

---

## Step 2: Create Composite `IYubiKey` (PR #2)

**Prerequisites:** Step 1 PR merged. ✅

### Architecture Decisions

1. **`IDeviceIdentity` in Core** - Core defines the interface and fingerprint computation
2. **Move enums to Core** - `FormFactor`, `DeviceCapabilities`, `DeviceFlags` move from Management to Core ✅ COMPLETE
3. **`CompositeYubiKeyFactory` in Core** - Takes identity reader delegate from Management
4. **Extension methods on `IYubiKey`** - Cleaner top-level API
5. **`IYubiKeyReference` stays public** - May change to internal later

### Types to Move from Management → Core ✅ COMPLETE

- [x] `FormFactor` enum
- [x] `DeviceCapabilities` flags enum  
- [x] `DeviceFlags` flags enum

### New Types to Create in Core

#### `IDeviceIdentity` Interface (with default implementation)
```csharp
// Yubico.YubiKit.Core/src/Interfaces/IDeviceIdentity.cs
public interface IDeviceIdentity
{
    int? SerialNumber { get; }
    FirmwareVersion FirmwareVersion { get; }
    FormFactor FormFactor { get; }
    bool IsFips { get; }
    bool IsSky { get; }
    bool IsLocked { get; }
    
    // Config fields for fingerprinting
    DeviceCapabilities UsbSupported { get; }
    DeviceCapabilities NfcSupported { get; }
    DeviceCapabilities UsbEnabled { get; }
    DeviceCapabilities NfcEnabled { get; }
    ushort AutoEjectTimeout { get; }
    byte ChallengeResponseTimeout { get; }
    DeviceFlags DeviceFlags { get; }
    bool IsNfcRestricted { get; }
    
    /// <summary>
    /// Computes a fingerprint of the device configuration.
    /// Default implementation using C# 14 default interface methods.
    /// </summary>
    byte[] ComputeConfigFingerprint()
    {
        Span<byte> buffer = stackalloc byte[16];
        BinaryPrimitives.WriteUInt16BigEndian(buffer[0..], (ushort)UsbEnabled);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)NfcEnabled);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], AutoEjectTimeout);
        buffer[6] = ChallengeResponseTimeout;
        buffer[7] = (byte)DeviceFlags;
        buffer[8] = (byte)(IsNfcRestricted ? 1 : 0);
        return buffer[..9].ToArray();
    }
}
```

#### `DeviceCorrelationKey` (internal)
```csharp
// Yubico.YubiKit.Core/src/YubiKey/DeviceCorrelationKey.cs
internal readonly record struct DeviceCorrelationKey(
    int? SerialNumber,
    FirmwareVersion FirmwareVersion,
    FormFactor FormFactor,
    DeviceCapabilities UsbSupported,
    DeviceCapabilities NfcSupported,
    bool IsFips,
    bool IsSky,
    bool IsLocked,
    byte[] ConfigFingerprint)
{
    public static DeviceCorrelationKey From(IDeviceIdentity identity) => new(
        identity.SerialNumber,
        identity.FirmwareVersion,
        identity.FormFactor,
        identity.UsbSupported,
        identity.NfcSupported,
        identity.IsFips,
        identity.IsSky,
        identity.IsLocked,
        identity.ComputeConfigFingerprint());
}
```

#### `IYubiKey` Interface (composite)
```csharp
// Yubico.YubiKit.Core/src/Interfaces/IYubiKey.cs
public interface IYubiKey
{
    /// <summary>
    /// Physical device identifier. Unique per physical YubiKey.
    /// Prefers serial number; falls back to correlation fingerprint.
    /// Format: "{SerialNumber}" or "fp:{hash}"
    /// </summary>
    string DeviceId { get; }
    
    /// <summary>
    /// Device identity information (serial, version, form factor, capabilities).
    /// May be null if identity could not be read.
    /// </summary>
    IDeviceIdentity? Identity { get; }
    
    /// <summary>
    /// Available connection types for this physical YubiKey.
    /// </summary>
    IReadOnlyCollection<ConnectionType> AvailableConnections { get; }
    
    /// <summary>
    /// Checks if the specified connection type is available.
    /// </summary>
    bool SupportsConnection<TConnection>() where TConnection : class, IConnection;
    
    /// <summary>
    /// Opens a connection of the specified type.
    /// </summary>
    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;
}
```

#### `CompositeYubiKey` Implementation
```csharp
// Yubico.YubiKit.Core/src/YubiKey/CompositeYubiKey.cs
internal class CompositeYubiKey : IYubiKey
{
    private readonly Dictionary<ConnectionType, IYubiKeyReference> _references;
    private readonly IDeviceIdentity? _identity;
    
    public string DeviceId { get; }
    public IDeviceIdentity? Identity => _identity;
    public IReadOnlyCollection<ConnectionType> AvailableConnections => _references.Keys;
    
    public bool SupportsConnection<TConnection>() where TConnection : class, IConnection
    {
        var connType = ConnectionTypeFor<TConnection>();
        return _references.ContainsKey(connType);
    }
    
    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken ct = default)
        where TConnection : class, IConnection
    {
        var connType = ConnectionTypeFor<TConnection>();
        if (!_references.TryGetValue(connType, out var reference))
            throw new NotSupportedException($"Connection type {typeof(TConnection).Name} not available");
        return reference.ConnectAsync<TConnection>(ct);
    }
}
```

#### `ICompositeYubiKeyFactory` Interface
```csharp
// Yubico.YubiKit.Core/src/YubiKey/ICompositeYubiKeyFactory.cs
public interface ICompositeYubiKeyFactory
{
    /// <summary>
    /// Correlates transport-specific references into composite YubiKeys.
    /// </summary>
    /// <param name="references">Transport-specific device references.</param>
    /// <param name="identityReader">Delegate to read device identity from a reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<IYubiKey>> CreateCompositesAsync(
        IEnumerable<IYubiKeyReference> references,
        Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity?>> identityReader,
        CancellationToken cancellationToken = default);
}
```

### Changes to Existing Types

#### Management: `DeviceInfo` implements `IDeviceIdentity`
```csharp
// Update existing DeviceInfo
public readonly record struct DeviceInfo : IDeviceIdentity
{
    // Existing fields already match interface
    // Just add : IDeviceIdentity to declaration
}
```

#### Management: Provide identity reader for DI
```csharp
// Yubico.YubiKit.Management/src/DependencyInjection.cs
public static IServiceCollection AddYubiKeyManagement(this IServiceCollection services)
{
    services.AddSingleton<Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity?>>>(
        async (reference, ct) =>
        {
            await using var session = await reference.CreateManagementSessionAsync(cancellationToken: ct);
            return await session.GetDeviceInfoAsync(ct);
        });
    return services;
}
```

#### Core: `DeviceRepositoryCached` uses composites
```csharp
// Cache changes from IYubiKeyReference to IYubiKey
private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();

public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(...)
```

#### Core: `DeviceEvent` references `IYubiKey`
```csharp
public class DeviceEvent(DeviceAction action, IYubiKey? device)
```

### Migration of Extension Methods

Extension methods move from `IYubiKeyReference` to `IYubiKey`:

```csharp
// Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)  // Changed from IYubiKeyReference
    {
        public async Task<ManagementSession> CreateManagementSessionAsync(...)
        {
            // IYubiKey routes to appropriate transport
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(ct);
            return await ManagementSession.CreateAsync(connection, ...);
        }
    }
}
```

### Implementation Order

1. [x] Move enums (`FormFactor`, `DeviceCapabilities`, `DeviceFlags`) to Core ✅
2. [ ] Update Management to use Core enums (namespace change)
3. [ ] Create `IDeviceIdentity` interface in Core (with default `ComputeConfigFingerprint()`)
4. [ ] Create `DeviceCorrelationKey` record
5. [ ] Create `IYubiKey` interface
6. [ ] Create `CompositeYubiKey` implementation
7. [ ] Create `ICompositeYubiKeyFactory` and implementation
8. [ ] Update `DeviceInfo` to implement `IDeviceIdentity`
9. [ ] Add identity reader registration in Management DI
10. [ ] Update `DeviceRepositoryCached` to use composites
11. [ ] Update `DeviceEvent` to use `IYubiKey`
12. [ ] Migrate extension methods to `IYubiKey`
13. [ ] Update tests
14. [ ] Update documentation

### Verification

```bash
# Build
dotnet build.cs build

# Test  
dotnet build.cs test

# Verify IYubiKey composite is used
grep -rn "IYubiKey[^R]" --include="*.cs" Yubico.YubiKit.*/src/
```

---

## Open Questions for Step 2

1. **What if DeviceInfo can't be read?** (e.g., FIDO-only Security Key)
   - Create singleton composites (one reference = one composite)
   - Use USB PID as fallback grouping

2. **Caching DeviceInfo on composite creation?**
   - Yes: Avoid repeated queries, but may become stale
   - Recommendation: Cache on creation, provide `RefreshIdentityAsync()` if needed later

---

## Reference: yubikey-manager Pattern

```python
# From yubikey-manager/ykman/device.py

class _PidGroup:
    # Correlates devices by DeviceInfo key
    def _key(self, info):
        return (
            info.serial,
            info.version,
            info.form_factor,
            str(info.supported_capabilities),
            info.config.get_bytes(False),
            info.is_locked,
            info.is_fips,
            info.is_sky,
        )

class _UsbCompositeDevice(YkmanDevice):
    # Aggregates multiple transport handles
    def open_connection(self, connection_type):
        return self._group.connect(self._key, connection_type)
```
