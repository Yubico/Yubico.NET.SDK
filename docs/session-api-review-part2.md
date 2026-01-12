# Session API Code Review - Part 2: Package Design and Public API Surface

## Context

This is the second pass of the session API review, focusing on:
1. **NuGet package boundaries** - "pay for what you use" design
2. **ApplicationSession as base class** - intended design vs. current state
3. **Full public API surface** - all types developers interact with
4. **Cross-package consistency** - how well modules align
5. **Discoverability** - can developers find what they need?

---

## Part 1: Package Architecture

### 1.1 Package Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│                     Developer's Application                      │
└─────────────────────────────────────────────────────────────────┘
        │                    │                     │
        ▼                    ▼                     ▼
┌───────────────┐   ┌────────────────┐   ┌──────────────────────┐
│  Management   │   │ SecurityDomain │   │  Piv (future)        │
│  NuGet Pkg    │   │  NuGet Pkg     │   │  NuGet Pkg           │
└───────────────┘   └────────────────┘   └──────────────────────┘
        │                    │                     │
        ▼                    ▼                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Yubico.YubiKit.Core                          │
│  - Device discovery (IYubiKey, YubiKeyManager)                   │
│  - Connection abstractions (ISmartCardConnection, IFidoConnection)│
│  - Protocol layer (ISmartCardProtocol, APDU handling)            │
│  - SCP support (ScpKeyParameters, StaticKeys, KeyReference)      │
│  - Cryptography (ECPublicKey, ECPrivateKey)                      │
│  - Base types (FirmwareVersion, ApplicationSession)              │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 "Pay for What You Use" Assessment

**Current state:** Well-designed package separation.

| Package | Dependencies | Additional Size |
|---------|--------------|-----------------|
| Core | Microsoft.Extensions.*, System.Reactive, NativeShims | Base requirement |
| Management | Core only | +DeviceInfo, +DeviceConfig, +ManagementSession |
| SecurityDomain | Core only | +SecurityDomainSession |

**Observation:** Both Management and SecurityDomain depend ONLY on Core. No cross-dependencies between application modules. This is correct.

**Future packages (Piv, Oath, Fido, etc.)** should follow the same pattern:
- Depend only on Core
- No dependencies on other application modules
- Self-contained with own types

### 1.3 Package API Surface Summary

#### Yubico.YubiKit.Core (Foundation)

| Category | Types | Notes |
|----------|-------|-------|
| Device Discovery | `IYubiKey`, `YubiKeyManager` | Entry points |
| Connections | `IConnection`, `ISmartCardConnection`, `IFidoConnection` | Abstractions |
| Protocol | `ISmartCardProtocol`, `ApduCommand`, `ApduResponse` | Wire format |
| SCP | `ScpKeyParameters`, `Scp03KeyParameters`, `StaticKeys`, `KeyReference` | Secure channel |
| Crypto | `ECPublicKey`, `ECPrivateKey`, `IPublicKey`, `IPrivateKey` | Key types |
| Base | `ApplicationSession`, `IApplicationSession`, `FirmwareVersion`, `Feature` | Shared types |

#### Yubico.YubiKit.Management

| Type | Category | Public API |
|------|----------|------------|
| `ManagementSession` | Session | `CreateAsync()`, `GetDeviceInfoAsync()`, `SetDeviceConfigAsync()`, `ResetDeviceAsync()` |
| `DeviceInfo` | Model | 20+ properties (SerialNumber, FirmwareVersion, Capabilities, etc.) |
| `DeviceConfig` | Model | Builder pattern, `GetBytes()` for serialization |
| `DeviceCapabilities` | Enum | Flags enum for Otp, U2f, Piv, Oath, etc. |
| `FormFactor` | Enum | USB-A, USB-C, Nano, Biometric variants |
| `DeviceFlags` | Enum | RemoteWakeup, TouchEject |
| `VersionQualifier` | Model | Alpha/Beta/Final version qualifiers |
| `IYubiKeyExtensions` | Extensions | `GetDeviceInfoAsync()`, `SetDeviceConfigAsync()`, `CreateManagementSessionAsync()` |
| `DependencyInjection` | DI | `AddYubiKeyManager()` extension |

#### Yubico.YubiKit.SecurityDomain

| Type | Category | Public API |
|------|----------|------------|
| `SecurityDomainSession` | Session | `CreateAsync()`, 18 public methods |
| (No other public types) | - | All data types come from Core |

**Critical observation:** SecurityDomainSession is the ONLY public type in the package (besides the session class). This is very lean but lacks:
- `IYubiKeyExtensions` equivalent
- DependencyInjection extensions
- Any domain-specific models

---

## Part 2: ApplicationSession Analysis

### 2.1 Current Implementation

```csharp
// Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs
public abstract class ApplicationSession : IApplicationSession
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO release managed resources here
        }
    }
}

public interface IApplicationSession : IDisposable
{
    // bool IsSupported(Feature feature);
    // void EnsureSupports(Feature feature);
    // FirmwareVersion GetVersionAsync { get; }
}
```

### 2.2 Gap Analysis

| Feature | ApplicationSession (Current) | ManagementSession (Actual) | SecurityDomainSession (Actual) |
|---------|------------------------------|----------------------------|--------------------------------|
| Dispose pattern | Basic template | Disposes protocol | Disposes protocol + base protocol |
| Feature detection | Commented out | `EnsureSupports()` + `IsSupported()` | None |
| Version property | Commented out | `_version` field | None (hardcoded) |
| Initialization | None | Two-phase async | Two-phase async |
| Protocol field | None | `_protocol` | `_protocol` + `_baseProtocol` |
| Logger field | None | `_logger` | `_logger` |
| IsInitialized | None | `_isInitialized` | `_isInitialized` |
| IsAuthenticated | None | None | `IsAuthenticated` property |

### 2.3 Recommended ApplicationSession Expansion

Based on the analysis, `ApplicationSession` should be expanded to include:

```csharp
public abstract class ApplicationSession : IApplicationSession
{
    // Core state every session needs
    protected readonly ILogger Logger;
    protected IProtocol Protocol { get; private set; }

    // Firmware version (detected or provided)
    public FirmwareVersion FirmwareVersion { get; protected set; }

    // State tracking
    public bool IsInitialized { get; protected set; }
    public bool IsAuthenticated { get; protected set; }

    // Feature detection (currently commented out but implemented in ManagementSession)
    public bool IsSupported(Feature feature) => FirmwareVersion >= feature.Version;

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

    // Proper disposal
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Protocol?.Dispose();
            Protocol = null;
        }
    }
}
```

### 2.4 Why ApplicationSession Matters

The current "minimal" ApplicationSession means:
1. **Each session reimplements** version detection, feature gating, disposal
2. **No contract enforcement** - sessions can diverge in behavior
3. **Testing is harder** - no common interface to mock
4. **Documentation is inconsistent** - each session documents its own patterns

---

## Part 3: Public API Surface Comparison

### 3.1 Session Method Patterns

| Aspect | ManagementSession | SecurityDomainSession |
|--------|-------------------|----------------------|
| **Factory** | `CreateAsync(connection, config, loggerFactory, scpKeyParams, ct)` | Same signature |
| **Async suffix** | Always used | Always used |
| **CancellationToken** | Last parameter, optional | Last parameter, optional |
| **Return types** | `Task<T>` / `Task` | `Task<T>` / `Task` |
| **Exception pattern** | Throws on error | Throws on error |

**Consistency:** Method signatures are well-aligned.

### 3.2 Model Type Patterns

| Aspect | Management Types | Core Types (used by SD) |
|--------|-----------------|------------------------|
| **DeviceInfo** | `readonly record struct` | - |
| **DeviceConfig** | `sealed record` + Builder | - |
| **FirmwareVersion** | - | `class` with operators |
| **KeyReference** | - | `readonly record struct` |
| **StaticKeys** | - | `sealed class : IDisposable` |

**Issue:** Inconsistent type patterns:
- `DeviceInfo` is a `readonly record struct` (immutable, value semantics)
- `DeviceConfig` is a `sealed record` (immutable reference type)
- `FirmwareVersion` is a `class` (not a record, not a struct)
- `KeyReference` is a `readonly record struct`
- `StaticKeys` is a `sealed class` implementing `IDisposable`

**Recommendation:** Establish guidelines:
- Small, immutable data (≤16 bytes): `readonly record struct`
- Larger data or with managed resources: `sealed record` or `sealed class`
- Disposable resources: `sealed class : IDisposable`

### 3.3 Extension Method Availability

| Package | Has `IYubiKeyExtensions`? | Has DI Extensions? |
|---------|--------------------------|-------------------|
| Management | **Yes** | **Yes** (`AddYubiKeyManager()`) |
| SecurityDomain | **No** | **No** |

**Critical gap:** SecurityDomainSession lacks convenience extensions.

**Recommendation:** Add to SecurityDomain:

```csharp
// IYubiKeyExtensions.cs
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            ILoggerFactory? loggerFactory = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            return await SecurityDomainSession.CreateAsync(
                connection, configuration, loggerFactory, scpKeyParams, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>>
            GetSecurityDomainKeyInfoAsync(
                ScpKeyParameters? scpKeyParams = null,
                CancellationToken cancellationToken = default)
        {
            using var session = await CreateSecurityDomainSessionAsync(
                scpKeyParams, cancellationToken: cancellationToken);
            return await session.GetKeyInformationAsync(cancellationToken);
        }
    }
}

// DependencyInjection.cs
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeySecurityDomain()
        {
            services.AddSingleton<Func<ISmartCardConnection, ScpKeyParameters?, CancellationToken, Task<SecurityDomainSession>>>(
                sp => (conn, scp, ct) => SecurityDomainSession.CreateAsync(
                    conn,
                    loggerFactory: sp.GetService<ILoggerFactory>(),
                    scpKeyParams: scp,
                    cancellationToken: ct));
            return services;
        }
    }
}
```

---

## Part 4: Discoverability Analysis

### 4.1 Developer Journey: "I want device info"

**With Management package:**
```csharp
// Option 1: One-liner (excellent)
var info = await device.GetDeviceInfoAsync();

// Option 2: Session (good)
using var session = await device.CreateManagementSessionAsync();
var info = await session.GetDeviceInfoAsync();

// Option 3: Manual (verbose but explicit)
using var connection = await device.ConnectAsync<ISmartCardConnection>();
using var session = await ManagementSession.CreateAsync(connection);
var info = await session.GetDeviceInfoAsync();
```

**Rating: Excellent** - Multiple options from simple to explicit.

### 4.2 Developer Journey: "I want to manage SCP keys"

**With SecurityDomain package:**
```csharp
// Only option: Manual (verbose)
using var connection = await device.ConnectAsync<ISmartCardConnection>();
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    scpKeyParams: Scp03KeyParameters.Default);
var keyInfo = await session.GetKeyInformationAsync();
```

**Rating: Poor** - No convenience methods, must know connection type.

### 4.3 Type Discoverability

**Management namespace:**
```
Yubico.YubiKit.Management
├── ManagementSession          // Main entry point
├── DeviceInfo                 // Device information
├── DeviceConfig               // Configuration
├── DeviceCapabilities         // Capability flags
├── FormFactor                 // Physical form
├── DeviceFlags                // Misc flags
├── VersionQualifier           // Version metadata
├── VersionQualifierType       // Enum
├── IYubiKeyExtensions         // Convenience
└── DependencyInjection        // DI support
```

**SecurityDomain namespace:**
```
Yubico.YubiKit.SecurityDomain
└── SecurityDomainSession      // That's it!
```

**Issue:** SecurityDomain relies entirely on Core types (`KeyReference`, `StaticKeys`, etc.) which are in different namespaces (`Yubico.YubiKit.Core.SmartCard.Scp`).

**Developer confusion:**
- "Where is `KeyReference`?" → Not in SecurityDomain, it's in Core.SmartCard.Scp
- "Where is `StaticKeys`?" → Same, Core.SmartCard.Scp
- "How do I create SCP03 parameters?" → `Scp03KeyParameters.Default` from Core.SmartCard.Scp

**Recommendation:** Consider type aliases or re-exports:

```csharp
// In SecurityDomain/Types.cs
namespace Yubico.YubiKit.SecurityDomain;

// Re-export for discoverability
public record struct KeyReference : Yubico.YubiKit.Core.SmartCard.Scp.KeyReference;

// Or just add good XML docs pointing to the right types
```

---

## Part 5: API Ergonomics

### 5.1 Parameter Ordering

**ManagementSession.CreateAsync:**
```csharp
public static async Task<ManagementSession> CreateAsync(
    IConnection connection,           // 1. Required: what to connect to
    ProtocolConfiguration? config,    // 2. Protocol tuning
    ILoggerFactory? loggerFactory,    // 3. Logging
    ScpKeyParameters? scpKeyParams,   // 4. Security
    CancellationToken ct)             // 5. Cancellation
```

**SecurityDomainSession.CreateAsync:**
```csharp
public static async Task<SecurityDomainSession> CreateAsync(
    ISmartCardConnection connection,  // 1. Required (more specific!)
    ProtocolConfiguration? config,    // 2. Protocol tuning
    ILoggerFactory? loggerFactory,    // 3. Logging
    ScpKeyParameters? scpKeyParams,   // 4. Security
    CancellationToken ct)             // 5. Cancellation
```

**Issue:** Parameter order is identical, but connection type differs:
- ManagementSession: `IConnection` (generic)
- SecurityDomainSession: `ISmartCardConnection` (specific)

**Impact:** SecurityDomainSession cannot accept `IFidoConnection`. This is intentional (SD is SmartCard-only), but the asymmetry may confuse developers.

**Recommendation:** Add XML doc explaining transport requirements:

```csharp
/// <param name="connection">
/// A SmartCard connection to the YubiKey. SecurityDomain only supports
/// SmartCard transport (CCID/PCSC). For FIDO operations, use the FIDO2 module.
/// </param>
```

### 5.2 Default Values

| Parameter | ManagementSession | SecurityDomainSession |
|-----------|-------------------|----------------------|
| `configuration` | `null` (auto-detect) | `null` (auto-detect) |
| `loggerFactory` | `null` → `NullLoggerFactory` | `null` → `NullLoggerFactory` |
| `scpKeyParams` | `null` (no SCP) | `null` (no SCP) |
| `cancellationToken` | `default` | `default` |

**Consistency:** Defaults are aligned.

### 5.3 Error Messages

**ManagementSession unsupported connection:**
```csharp
throw new NotSupportedException(
    $"The connection type {connection.GetType().Name} is not supported by ManagementSession. " +
    $"Supported types: ISmartCardConnection, IFidoConnection.");
```

**Rating: Good** - Clear, actionable.

**SecurityDomainSession:** Compile-time enforcement via parameter type - no runtime error possible.

**Rating: Good** - Compile-time safety is better.

---

## Part 6: Missing Public Types in SecurityDomain

SecurityDomainSession exposes complex return types that should arguably be first-class public types:

### 6.1 Current Return Types

| Method | Return Type | Issue |
|--------|-------------|-------|
| `GetKeyInformationAsync()` | `IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>` | Opaque nested dictionaries |
| `GetCertificatesAsync()` | `IReadOnlyList<X509Certificate2>` | OK (uses BCL type) |
| `GetSupportedCaIdentifiersAsync()` | `IReadOnlyDictionary<KeyReference, ReadOnlyMemory<byte>>` | Raw bytes, no structure |
| `GenerateKeyAsync()` | `ECPublicKey` | OK (Core type) |

### 6.2 Recommended Domain Types

```csharp
namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
/// Information about a key stored in the Security Domain.
/// </summary>
public readonly record struct KeyInfo
{
    public required KeyReference Reference { get; init; }
    public required IReadOnlyDictionary<byte, byte> Components { get; init; }

    // Convenience properties
    public byte KeyId => Reference.Kid;
    public byte KeyVersionNumber => Reference.Kvn;
    public bool IsDefaultKey => Reference.Kvn == 0xFF;
}

/// <summary>
/// CA identifier information for SCP11 provisioning.
/// </summary>
public readonly record struct CaIdentifier
{
    public required KeyReference KeyReference { get; init; }
    public required ReadOnlyMemory<byte> Identifier { get; init; }
    public required CaIdentifierType Type { get; init; }
}

public enum CaIdentifierType
{
    Kloc,  // Key Loading OCE Certificate
    Klcc   // Key Loading Card Certificate
}
```

**Benefit:** Developers get typed models instead of raw dictionaries/bytes.

---

## Part 7: Comprehensive Findings

### 7.1 What's Working Well

| Aspect | Rating | Notes |
|--------|--------|-------|
| Package separation | | Each module is self-contained |
| Core dependencies | | Management + SD only depend on Core |
| Factory method pattern | | Consistent `CreateAsync()` signatures |
| Async naming | | All async methods have `Async` suffix |
| CancellationToken support | | Present on all async methods |
| Logging infrastructure | | Both use `ILogger<T>` correctly |
| Basic disposal | | Both dispose protocols properly |

### 7.2 Needs Improvement

| Aspect | Priority | Issue |
|--------|----------|-------|
| SecurityDomain extensions | **Critical** | No `IYubiKeyExtensions` |
| SecurityDomain DI | **High** | No `AddYubiKeySecurityDomain()` |
| ApplicationSession | **High** | Too minimal, no shared logic |
| Feature detection | **High** | SD hardcodes version, no feature gates |
| Type discoverability | **Medium** | SD types scattered in Core namespaces |
| Return type models | **Medium** | SD returns raw dictionaries/bytes |
| Logging levels | **Low** | Inconsistent between modules |
| CapabilityMapper visibility | **Low** | Marked `// TODO internal` but is public |

### 7.3 Transport Requirements Reference

| Transport | Applications | Connection Type |
|-----------|--------------|-----------------|
| **OTP (HID)** | OTP only | `IOtpConnection` |
| **FIDO (HID)** | U2F, FIDO2 | `IFidoConnection` |
| **SmartCard (CCID/PCSC)** | OATH, PIV, OpenPGP, HSMAUTH, FIDO2 | `ISmartCardConnection` |

**Multi-Transport Sessions (require Backend pattern):**
- `ManagementSession`: SmartCard + FIDO
- `FidoSession`: FIDO HID + SmartCard (FIDO2 over CCID)

**Single-Transport Sessions:**
- `SecurityDomainSession`: SmartCard only
- `OtpSession`: OTP HID only
- `PivSession`: SmartCard only
- `OathSession`: SmartCard only
- `OpenPgpSession`: SmartCard only
- `YubiHsmSession`: SmartCard only

### 7.4 Package Checklist for Future Sessions

When creating new session packages (Piv, Oath, Fido, etc.):

**Structure:**
- [ ] Single NuGet package depending only on Core
- [ ] Own namespace (`Yubico.YubiKit.Xxx`)
- [ ] Own CLAUDE.md documentation

**Session Class:**
- [ ] Sealed class inheriting `ApplicationSession`
- [ ] Private constructor
- [ ] Public `CreateAsync()` factory with standard parameter order
- [ ] Feature detection using `Feature` class
- [ ] Version detection (not hardcoded)
- [ ] Proper disposal of protocol
- [ ] **Backend pattern** if multi-transport (ManagementSession, FidoSession)

**Extensions:**
- [ ] `IYubiKeyExtensions.cs` with `extension(IYubiKey)` syntax
- [ ] `CreateXxxSessionAsync()` factory extension
- [ ] High-level convenience methods (one-liners for common ops)

**DI Support:**
- [ ] `DependencyInjection.cs` with `AddYubiKeyXxx()` extension
- [ ] Factory delegate registration
- [ ] Integration with Core's `AddYubiKeyManagerCore()`

**Domain Types:**
- [ ] Strongly-typed models for complex return values
- [ ] Proper enums for flags/options
- [ ] Builder pattern for complex configuration

**Tests:**
- [ ] `XxxTestState` class if reset/state management needed
- [ ] `WithXxxSessionAsync()` test extension
- [ ] Integration tests using `[WithYubiKey]` attribute

---

## Part 8: Action Items

### Immediate (Before More Sessions)

1. **Expand ApplicationSession** with:
   - `FirmwareVersion` property
   - `IsInitialized`, `IsAuthenticated` properties
   - `IsSupported()`, `EnsureSupports()` methods
   - Proper disposal pattern

2. **Add SecurityDomain extensions**:
   - `IYubiKeyExtensions.cs`
   - `DependencyInjection.cs`

3. **Fix SecurityDomain version detection**:
   - Remove hardcoded `FirmwareVersion.V5_3_0`
   - Detect from device (query Management or parse SELECT response)

### Short-Term (Next Sprint)

4. **Add domain types to SecurityDomain**:
   - `KeyInfo` record
   - `CaIdentifier` record
   - Better return types for complex queries

5. **Standardize logging levels** across modules

6. **Make CapabilityMapper internal** (as marked)

### Documentation

7. **Add migration guide** for existing patterns
8. **Create session implementation template** (cookiecutter or similar)
9. **Document namespace conventions** for type location

---

## Appendix: Type Location Reference

For developers wondering "where is X?":

| Type | Namespace | Package |
|------|-----------|---------|
| `IYubiKey` | `Yubico.YubiKit.Core.YubiKey` | Core |
| `YubiKeyManager` | `Yubico.YubiKit.Core.YubiKey` | Core |
| `ISmartCardConnection` | `Yubico.YubiKit.Core.SmartCard` | Core |
| `IFidoConnection` | `Yubico.YubiKit.Core.Hid` | Core |
| `FirmwareVersion` | `Yubico.YubiKit.Core.YubiKey` | Core |
| `Feature` | `Yubico.YubiKit.Core.YubiKey` | Core |
| `ApplicationSession` | `Yubico.YubiKit.Core.YubiKey` | Core |
| `ScpKeyParameters` | `Yubico.YubiKit.Core.SmartCard.Scp` | Core |
| `Scp03KeyParameters` | `Yubico.YubiKit.Core.SmartCard.Scp` | Core |
| `StaticKeys` | `Yubico.YubiKit.Core.SmartCard.Scp` | Core |
| `KeyReference` | `Yubico.YubiKit.Core.SmartCard.Scp` | Core |
| `ECPublicKey` | `Yubico.YubiKit.Core.Cryptography` | Core |
| `ECPrivateKey` | `Yubico.YubiKit.Core.Cryptography` | Core |
| `ManagementSession` | `Yubico.YubiKit.Management` | Management |
| `DeviceInfo` | `Yubico.YubiKit.Management` | Management |
| `DeviceConfig` | `Yubico.YubiKit.Management` | Management |
| `DeviceCapabilities` | `Yubico.YubiKit.Management` | Management |
| `SecurityDomainSession` | `Yubico.YubiKit.SecurityDomain` | SecurityDomain |

---

*Review Part 2 completed: 2026-01-12*
*Reviewer: Claude Code*
