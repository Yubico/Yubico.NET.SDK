# Session API Improvements Implementation Plan

## Overview

This plan consolidates action items from three review documents:
- `docs/session-api-review.md` - Session patterns and persona analysis
- `docs/session-api-review-part2.md` - Package design and API surface
- `docs/logging-pattern-proposal.md` - Static LoggerFactory pattern

**Goal:** Establish a consistent, developer-friendly session pattern that will serve as the template for future sessions (Piv, Otp, Fido, Oath, YubiHsm, OpenPgp).

---

## Transport Requirements Reference

Understanding transport support is critical for session design decisions:

| Transport | Applications | Connection Type |
|-----------|--------------|-----------------|
| **OTP (HID)** | OTP only | `IOtpConnection` |
| **FIDO (HID)** | U2F, FIDO2 | `IFidoConnection` |
| **SmartCard (CCID/PCSC)** | OATH, PIV, OpenPGP, HSMAUTH, FIDO2 | `ISmartCardConnection` |

**Multi-Transport Sessions (require Backend pattern):**
- `ManagementSession`: SmartCard + FIDO ✅ (already uses Backend)
- `FidoSession` (future): FIDO HID + SmartCard - **must use Backend pattern**

**Single-Transport Sessions (Backend pattern optional):**
- `SecurityDomainSession`: SmartCard only
- `OtpSession` (future): OTP HID only
- `PivSession` (future): SmartCard only
- `OathSession` (future): SmartCard only
- `OpenPgpSession` (future): SmartCard only
- `YubiHsmSession` (future): SmartCard only

---

## Phase 1: Core Infrastructure (Foundation)

These changes must be completed first as other phases depend on them.

### 1.1 Static Logging Infrastructure

**Files:**
- Create: `Yubico.YubiKit.Core/src/YubiKitLogging.cs`
- Modify: `Yubico.YubiKit.Core/src/DependencyInjection.cs`

**Implementation:**
```csharp
// YubiKitLogging.cs
namespace Yubico.YubiKit.Core;

public static class YubiKitLogging
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private static readonly object _lock = new();

    public static ILoggerFactory LoggerFactory
    {
        get { lock (_lock) return _loggerFactory; }
        set { lock (_lock) _loggerFactory = value ?? NullLoggerFactory.Instance; }
    }

    internal static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

    /// <summary>
    /// Temporarily replaces the LoggerFactory. Dispose to restore. Useful for testing.
    /// </summary>
    public static IDisposable UseTemporary(ILoggerFactory factory)
    {
        var original = LoggerFactory;
        LoggerFactory = factory;
        return new Disposable(() => LoggerFactory = original);
    }
}
```

**DI Integration:**
- Update `AddYubiKeyManagerCore()` to auto-wire `ILoggerFactory` from container to `YubiKitLogging.LoggerFactory`

### 1.2 Expand ApplicationSession

**File:** `Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs`

**Current state:** Minimal (~43 lines), mostly commented-out interface members

**Add:**
- `FirmwareVersion` property
- `IsInitialized` property
- `IsAuthenticated` property
- `IsSupported(Feature)` method
- `EnsureSupports(Feature)` method
- `Logger` property (using `YubiKitLogging.CreateLogger<T>()`)
- Proper disposal pattern for protocol

**Proposed structure:**
```csharp
public abstract class ApplicationSession : IApplicationSession
{
    protected ILogger Logger { get; }
    protected IProtocol? Protocol { get; set; }

    public FirmwareVersion FirmwareVersion { get; protected set; } = new();
    public bool IsInitialized { get; protected set; }
    public bool IsAuthenticated { get; protected set; }

    protected ApplicationSession()
    {
        Logger = YubiKitLogging.CreateLogger(GetType().FullName ?? GetType().Name);
    }

    public bool IsSupported(Feature feature) => FirmwareVersion >= feature.Version;

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

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

### 1.3 Update IApplicationSession Interface

**File:** `Yubico.YubiKit.Core/src/YubiKey/IApplicationSession.cs`

**Add:**
```csharp
public interface IApplicationSession : IDisposable
{
    FirmwareVersion FirmwareVersion { get; }
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    bool IsSupported(Feature feature);
    void EnsureSupports(Feature feature);
}
```

---

## Phase 2: ManagementSession Updates

### 2.1 Remove Explicit Logger Parameter

**File:** `Yubico.YubiKit.Management/src/ManagementSession.cs`

**Changes:**
- Remove `ILoggerFactory? loggerFactory` parameter from `CreateAsync()`
- Remove `loggerFactory` parameter from constructor
- Use `YubiKitLogging.CreateLogger<ManagementSession>()` in constructor
- Optionally keep parameter as override (hybrid approach from proposal)

**Before:**
```csharp
public static async Task<ManagementSession> CreateAsync(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ILoggerFactory? loggerFactory = null,
    ScpKeyParameters? scpKeyParams = null,
    CancellationToken cancellationToken = default)
```

**After (hybrid):**
```csharp
public static async Task<ManagementSession> CreateAsync(
    IConnection connection,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParams = null,
    ILoggerFactory? loggerFactory = null,  // Moved to end as optional override
    CancellationToken cancellationToken = default)
```

### 2.2 Update Extensions

**File:** `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs`

**Changes:**
- Update `CreateManagementSessionAsync()` signature to match new parameter order
- Remove explicit loggerFactory threading

### 2.3 Update DependencyInjection

**File:** `Yubico.YubiKit.Management/src/DependencyInjection.cs`

**Changes:**
- Simplify factory registration (no longer needs to capture loggerFactory)
- Add auto-wiring of `YubiKitLogging.LoggerFactory` from DI container

---

## Phase 3: SecurityDomainSession Improvements

### 3.1 Add IYubiKeyExtensions

**Create:** `Yubico.YubiKit.SecurityDomain/src/IYubiKeyExtensions.cs`

```csharp
namespace Yubico.YubiKit.SecurityDomain;

public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            return await SecurityDomainSession.CreateAsync(
                connection, configuration, scpKeyParams, cancellationToken);
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
```

### 3.2 Add DependencyInjection

**Create:** `Yubico.YubiKit.SecurityDomain/src/DependencyInjection.cs`

```csharp
namespace Yubico.YubiKit.SecurityDomain;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeySecurityDomain()
        {
            services.AddSingleton<SecurityDomainSessionFactory>(
                (conn, scp, ct) => SecurityDomainSession.CreateAsync(conn, null, scp, ct));
            return services;
        }
    }
}

public delegate Task<SecurityDomainSession> SecurityDomainSessionFactory(
    ISmartCardConnection connection,
    ScpKeyParameters? scpKeyParams,
    CancellationToken cancellationToken);
```

### 3.3 Fix Version Detection

**File:** `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs`

**Current issue:** Line ~81 hardcodes `FirmwareVersion.V5_3_0`

**Solution options:**
1. Query Management interface for version before SD session
2. Parse SELECT response for version data
3. Accept version as optional parameter

**Recommended:** Option 3 (simplest, allows caller to provide version if known)

```csharp
private async Task InitializeAsync(
    ProtocolConfiguration? configuration,
    FirmwareVersion? firmwareVersion,  // New parameter
    CancellationToken cancellationToken)
{
    // ...
    var version = firmwareVersion ?? await DetectVersionAsync(cancellationToken);
    protocol.Configure(version, configuration);
    // ...
}
```

### 3.4 Remove Logger Parameter

**File:** `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs`

**Changes:**
- Same as ManagementSession: use `YubiKitLogging.CreateLogger<SecurityDomainSession>()`
- Update constructor and `CreateAsync()` signatures

### 3.5 Simplify Protocol Lifecycle

**File:** `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs`

**Current issue:** Two protocol fields (`_baseProtocol` and `_protocol`) causing confusion

**Change:**
- Keep single `_protocol` field
- After SCP wrapping, dispose base protocol reference (it's wrapped, not needed separately)
- Simplify `Dispose()` to only dispose `_protocol`

---

## Phase 4: Add Session Interfaces (Testability)

### 4.1 IManagementSession

**Create:** `Yubico.YubiKit.Management/src/IManagementSession.cs`

```csharp
public interface IManagementSession : IApplicationSession
{
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken ct = default);
    Task SetDeviceConfigAsync(DeviceConfig config, bool reboot,
        byte[]? currentLockCode = null, byte[]? newLockCode = null,
        CancellationToken ct = default);
    Task ResetDeviceAsync(CancellationToken ct = default);
}
```

### 4.2 ISecurityDomainSession

**Create:** `Yubico.YubiKit.SecurityDomain/src/ISecurityDomainSession.cs`

```csharp
public interface ISecurityDomainSession : IApplicationSession
{
    Task<IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>>
        GetKeyInformationAsync(CancellationToken ct = default);
    Task PutKeyAsync(KeyReference keyRef, StaticKeys keys, int replaceKvn = 0,
        CancellationToken ct = default);
    Task DeleteKeyAsync(KeyReference keyRef, bool deleteLastKey = false,
        CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
    // ... other public methods
}
```

### 4.3 Update Session Classes

- `ManagementSession : ApplicationSession, IManagementSession`
- `SecurityDomainSession : ApplicationSession, ISecurityDomainSession`

---

## Phase 5: Secondary Improvements

### 5.1 Standardize Logging Levels

**Guideline:**
- `Trace`: Wire-level data (APDUs) - only for deep debugging
- `Debug`: Method entry/exit, internal state changes
- `Information`: Significant operations completed
- `Warning`: Recoverable errors, fallback paths
- `Error`: Unrecoverable errors before throw

**Files to audit:**
- `ManagementSession.cs` - mostly Debug (good)
- `SecurityDomainSession.cs` - inconsistent (needs review)

### 5.2 Make CapabilityMapper Internal

**File:** `Yubico.YubiKit.Management/src/CapabilityMapper.cs`

**Current:** Marked `// TODO internal` but is public

**Change:** Make `internal` as intended

### 5.3 Add Feature Detection to SecurityDomain

**File:** `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs`

**Add feature constants:**
```csharp
private static readonly Feature FeatureSecurityDomain = new("Security Domain", 5, 3, 0);
private static readonly Feature FeatureScp11 = new("SCP11", 5, 7, 2);
```

**Use in methods:**
```csharp
public async Task GenerateKeyAsync(...)
{
    EnsureSupports(FeatureScp11);  // SCP11 key generation requires 5.7.2+
    // ...
}
```

---

## Phase 6: Documentation Updates

### 6.1 Update CLAUDE.md Files

- `Yubico.YubiKit.Core/CLAUDE.md` - Document YubiKitLogging pattern
- `Yubico.YubiKit.Management/CLAUDE.md` - Create if doesn't exist
- `Yubico.YubiKit.SecurityDomain/CLAUDE.md` - Update with new patterns

### 6.2 Update README.md Files

- Add logging configuration examples
- Update session creation examples (new parameter order)

---

## Added After-the-Fact Improvements (2026-01-14)

These items were surfaced by re-reviewing `docs/research/session-api-review-part2.md` after the refactor work and are recommended follow-on improvements for additional DX/value unlock.

### A. Add SecurityDomain Domain Models (Reduce Opaque Return Types)

SecurityDomain currently exposes “raw”/opaque structures (e.g., nested dictionaries and raw byte payloads). Add typed models and new overloads returning them (keep existing return types for compatibility).

- Introduce types like `KeyInfo`, `CaIdentifier`, `CaIdentifierType` in `Yubico.YubiKit.SecurityDomain`
- Add convenience overloads (or new methods) that return `IReadOnlyList<KeyInfo>` / `IReadOnlyList<CaIdentifier>`

### B. Improve Type Discoverability Across Packages

SecurityDomain relies heavily on Core types (`KeyReference`, `StaticKeys`, `Scp03KeyParameters`, etc.) which live in non-obvious namespaces.

- Add explicit documentation (“Where is X?”) and cross-links in SecurityDomain docs
- Consider re-exports/aliases only if we can do so without creating confusing “duplicate types”

### C. Codify Model Type Guidelines for Future Sessions

Establish a consistent rule-of-thumb for public model types to improve predictability across modules.

- Small immutable data (≈ ≤16 bytes): `readonly record struct`
- Larger immutable data: `sealed record`
- Resource-backed/disposable: `sealed class : IDisposable`

### D. Strengthen “Developer Journey” Docs (1-liner → Session → Manual)

Make common flows explicit for different personas (CLI/PowerShell/API/service devs):

- Provide 3-tier examples for common tasks in README(s): one-liner convenience, session usage, manual connection usage
- Ensure SecurityDomain has parity with Management in examples and entry points

### E. Promote the “Package Checklist” into an Enforced Template

Turn the future-session checklist into a first-class template for new session packages (PIV/OATH/OTP/OpenPGP/YubiHsm/FIDO):

- Checklist should include: DI factory, `IYubiKeyExtensions`, tests (`WithXxxSessionAsync`), domain models, transport notes
- Consider a session template document (or generator later) to reduce repeated design churn

### F. Make Transport Requirements More Obvious at API Boundaries

Beyond the transport table, improve “in-the-moment” clarity:

- Add XML docs on session factory parameters describing transport constraints (e.g., SecurityDomain is SmartCard-only)
- Ensure extension method names and signatures reinforce the expected transport

---

## Implementation Order

```
Phase 1 (Foundation) - Must complete first
├── 1.1 YubiKitLogging.cs
├── 1.2 ApplicationSession expansion
└── 1.3 IApplicationSession updates

Phase 2 (ManagementSession) - Depends on Phase 1
├── 2.1 Remove explicit logger parameter
├── 2.2 Update IYubiKeyExtensions
└── 2.3 Update DependencyInjection

Phase 3 (SecurityDomainSession) - Depends on Phase 1
├── 3.1 Add IYubiKeyExtensions (NEW)
├── 3.2 Add DependencyInjection (NEW)
├── 3.3 Fix version detection
├── 3.4 Remove logger parameter
└── 3.5 Simplify protocol lifecycle

Phase 4 (Interfaces) - Depends on Phase 2 & 3
├── 4.1 IManagementSession
├── 4.2 ISecurityDomainSession
└── 4.3 Update session classes

Phase 5 (Secondary) - Can be done anytime after Phase 1
├── 5.1 Standardize logging levels
├── 5.2 Make CapabilityMapper internal
└── 5.3 Add feature detection to SD

Phase 6 (Documentation) - After all code changes
├── 6.1 CLAUDE.md updates
└── 6.2 README.md updates
```

---

## Verification

### Build Verification
```bash
dotnet build.cs build
```

### Test Verification
```bash
# Unit tests
dotnet build.cs test

# Integration tests (requires YubiKey)
dotnet test Yubico.YubiKit.Management/tests/Yubico.YubiKit.Management.IntegrationTests/
dotnet test Yubico.YubiKit.SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/
```

### Manual Verification

1. **Static logging works:**
```csharp
YubiKitLogging.LoggerFactory = LoggerFactory.Create(b => b.AddConsole());
using var session = await ManagementSession.CreateAsync(connection);
// Should see log output
```

2. **DI auto-wiring works:**
```csharp
services.AddYubiKeyManagerCore();
services.AddYubiKeyManager();
// ILoggerFactory from DI should be automatically used
```

3. **SecurityDomain extensions work:**
```csharp
using var session = await yubiKey.CreateSecurityDomainSessionAsync();
var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync();
```

4. **Existing tests still pass** (no regressions)

---

## Breaking Changes

### API Changes (Minor)

1. **Parameter reordering in CreateAsync:**
   - `loggerFactory` moves from position 3 to position 4 (after `scpKeyParams`)
   - Named parameters will continue to work
   - Positional callers need update

2. **New interfaces:**
   - `IManagementSession` and `ISecurityDomainSession` are additive
   - Existing code unaffected

### Behavioral Changes

1. **Logging by default:**
   - Previously: No logging unless `loggerFactory` explicitly passed
   - Now: No logging by default (same), but can be configured globally via `YubiKitLogging.LoggerFactory`

---

## Files Summary

### New Files
- `Yubico.YubiKit.Core/src/YubiKitLogging.cs`
- `Yubico.YubiKit.SecurityDomain/src/IYubiKeyExtensions.cs`
- `Yubico.YubiKit.SecurityDomain/src/DependencyInjection.cs`
- `Yubico.YubiKit.Management/src/IManagementSession.cs`
- `Yubico.YubiKit.SecurityDomain/src/ISecurityDomainSession.cs`

### Modified Files
- `Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs`
- `Yubico.YubiKit.Core/src/YubiKey/IApplicationSession.cs`
- `Yubico.YubiKit.Core/src/DependencyInjection.cs`
- `Yubico.YubiKit.Management/src/ManagementSession.cs`
- `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs`
- `Yubico.YubiKit.Management/src/DependencyInjection.cs`
- `Yubico.YubiKit.Management/src/CapabilityMapper.cs`
- `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs`

---

*Plan created: 2026-01-12*
*Based on: session-api-review.md, session-api-review-part2.md, logging-pattern-proposal.md*
