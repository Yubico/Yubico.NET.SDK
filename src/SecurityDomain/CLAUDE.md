# CLAUDE.md - Security Domain Module

This file provides Claude-specific guidance for working with the Security Domain module. **Read [README.md](README.md) first** for general module documentation.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new SCP protocols, key operations) should include usage examples in README.md and implementation guidance in CLAUDE.md
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** (especially automatic reset mechanism) should be reflected in the test pattern sections below

## Module Context

The shared catalog is Core's `CryptographicDefinitions.Keys/Curves/Algorithms`; key objects expose `KeyInfo`.
Keep SCP11's explicit P-256/profile validation separate from catalog applicability and optional COSE mappings.
See [Core catalog](../Core/README.md#cryptographic-metadata); SCP wire IDs remain protocol-owned.

The Security Domain module manages YubiKey's root security application, which controls secure channel establishment using SCP (Secure Channel Protocol). This is a **low-level security module** that requires careful handling of cryptographic keys and secure sessions.

**Key Files:**
- [`SecurityDomainSession.cs`](src/SecurityDomainSession.cs) - Public facade, lifecycle/state owner, and visible GlobalPlatform APDU flows
- [`SecurityDomainBackend.cs`](src/Backend/SecurityDomainBackend.cs) - Applet-owned selected SmartCard backend for normal APDU sends
- [`SecurityDomainKeyMaterial.cs`](src/SecurityDomainKeyMaterial.cs) - Pure SCP key component, KCV, and checksum helpers
- [`SecurityDomainTlvEncoding.cs`](src/SecurityDomainTlvEncoding.cs) - Pure TLV encoding helpers for delete filters and related payloads
- Test infrastructure in `tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/`

## Critical Security Requirements

### Memory and Crypto Safety

This module handles **highly sensitive cryptographic material**. Apply these rules strictly:

Follow the root memory rules: use `stackalloc` for synchronous buffers up to 512 bytes, pooled arrays for
larger synchronous buffers, and `Memory<byte>`/owned memory across awaits. Clear sensitive caller-owned
buffers in `finally`, dispose cryptographic objects, and use fixed-time comparisons for secret-derived
values. Log references and operation names, never key bytes, KCVs, or challenge/response payloads.

### Key Component Handling

The `SecurityDomainKeyMaterial.EncodeKeyComponent()` pattern demonstrates correct key handling:
1. Rent buffer from `ArrayPool`
2. Copy sensitive data to rented buffer
3. Perform encryption
4. Zero the buffer in `finally` block
5. Return the array to the pool after clearing the used plaintext range

This describes that helper's plaintext copy, not a guarantee that every temporary cryptographic value
in the module or its fixtures is cleared. Supplied keys/parameters remain the caller's responsibility;
do not infer deeper clone/borrow rules from this helper.

## Test Infrastructure

### Automatic SD Reset System

Integration tests can request a destructive reset with `resetBeforeUse: true`. It is not unconditional:
related successive sessions use `false` to preserve the keys created by the previous session. Use only
authorized dedicated test devices; custom Security Domain keys and associated data can be lost.

#### How It Works

Tests use the `WithSecurityDomainSessionAsync` extension method in [`TestStateExtensions.cs`](tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs):

The helper takes `bool resetBeforeUse`, `Func<SecurityDomainSession, Task> action`, optional
`ProtocolConfiguration? configuration`, `ScpKeyParameters? scpKeyParams`, and `CancellationToken` in that
order. It passes the test state's firmware version and supplied configuration to each session factory.

**Two-Session Reset Pattern:**
1. Creates `resetSession` without SCP authentication
2. Calls `ResetAsync()` and disposes that session before creating another
3. Creates the actual test `session` with the supplied SCP parameters (or none)
4. Runs the test action

#### The ResetAsync Method

Located in [`SecurityDomainSession.cs`](src/SecurityDomainSession.cs), the reset process:

1. **Enumerates keys** via `ListKeyInformationAsync()`; an empty list skips reset
2. **For each processed reference**, determines the blocking instruction:
   - SCP03 (KID=0x01): `INITIALIZE UPDATE` with reference 00/00; component KIDs 02/03 are skipped
   - SCP11a/c (KID=0x11/0x15): `EXTERNAL AUTHENTICATE`
   - SCP11b (KID=0x13): `INTERNAL AUTHENTICATE`
   - Others: `PERFORM SECURITY OPERATION`
3. **Sends at most 65 raw attempts** per processed reference with `ResetAttemptPayload`, bypassing SCP
4. **Stops that reference** on `AuthenticationMethodBlocked` or `SecurityStatusNotSatisfied`; ordinary
   transmission errors are retried, cancellation is rethrown, and exhaustion itself does not throw
5. **Reselects/reinitializes**, without verifying factory contents afterward

Return alone is not proof of restoration, and cancellation/failure can leave partial persistent changes.
Yubico documents factory SCP03 restoration after last-set deletion; do not extend that to a verified
mixed-key/firmware guarantee. Retained SCP parameters can fail reauthentication. Reinitialization currently
omits the original configuration/version and clears the explicit-version indicator; a fresh session with
the required options is the workaround, not a claim that losing them is intended policy.

#### Writing Tests

Use the typed xUnit-v2 patterns in [tests/CLAUDE.md](tests/CLAUDE.md) for reset, owned parameter scopes,
and successive sessions. When testing reset itself, disable the helper's preliminary reset. Assert the
specific state/readiness you need; a successful callback does not prove complete device restoration.

## Common Patterns

### Session Facade With Shallow Pure Helpers

SecurityDomain does **not** use `SecurityDomainSession` partial classes. Keep `SecurityDomainSession` as the single public facade that owns lifecycle, protocol state, reset orchestration, and public `ISecurityDomainSession` methods.

Allowed helper extraction is intentionally narrow:

```text
SecurityDomainSession public method
  -> validate/session-state check
  -> build visible GlobalPlatform APDU/TLV payload
  -> transmit through SecurityDomainBackend or raw reset connection
  -> call pure encode/parse/KCV helper only where it clarifies local code
```

Do not introduce operation-specific command classes such as `PutKeyCommand`, `GetDataCommand`, `DeleteKeyCommand`, or `ResetCommand`. Do not hide the reset raw connection bypass behind a helper.

Use `CaIdentifierType` flags to select KLOC, KLCC, or both; preserve KLOC-before-KLCC result ordering.

### SCP Parameter Types

`Scp03KeyParameters` takes a key reference and static ENC/MAC/DEK set. `Scp11KeyParameters` takes a device
key reference and trusted device public key; SCP11a/c also take the OCE private key, CA-KLOC reference and
host certificate chain. SCP11b authenticates the device, not the host, so `IsAuthenticated` is not a
management-authorization check. See the [README role table](README.md#understand-scp11ac-provisioning-roles):
the device certificate store is not OCE-chain provisioning. Keep caller-owned parameters alive through
session disposal, and do not infer unverified deep ownership rules from the retained `_scpKeyParams` reference.

### Session Initialization Pattern

`CreateAsync(ISmartCardConnection, SessionCreationOptions?, CancellationToken)` combines construction and
asynchronous initialization. The constructor retains the connection and SCP parameters. `InitializeAsync`
takes configuration, firmware version, then cancellation token. It prepares the protocol, selects the SD
through `SecurityDomainBackend.SelectApplicationAsync`, and configures it with the supplied version or
5.3.0 fallback. If configuration returns a different protocol, the backend is rebuilt for that protocol.

`ProtocolFactory`, `IProtocol`, and `ISmartCardProtocol` are not consumer extension points. External low-level
code should use `RawSmartCardSession`; SecurityDomain uses these types only as friend-assembly implementation
machinery for its applet session.

### Error Handling

Initialization wraps `ApduException`, `BadResponseException`, or `NotSupportedException` from
`ConfigureProtocolAsync` only when SCP parameters were supplied. Preparation and app selection precede
that catch; cancellation is rethrown unchanged. Later operation failures retain their original types.
`SecureChannelException.StatusWord` copies only an immediate `ApduException` status, not a nested one.

## GlobalPlatform Command Structure

The session builds these command instructions and TLV tags locally. This is a source-navigation aid,
not evidence of complete GlobalPlatform conformance:

```csharp
const byte ClaGlobalPlatform = 0x80;

// Common instructions
const byte InsGetData = 0xCA;
const byte InsPutKey = 0xD8;
const byte InsInitializeUpdate = 0x50;
const byte InsExternalAuthenticate = 0x82;
const byte InsInternalAuthenticate = 0x88;
const byte InsDelete = 0xE4;
const byte InsGenerateKey = 0xF1;
const byte InsPerformSecurityOperation = 0x2A;

// TLV tags for data encoding
const int TagKeyInformationTemplate = 0xE0;
const byte TagControlReference = 0xA6;
const byte TagKidKvn = 0x83;
```

## Debugging Tips

### Logging

Session creation does not take a logger factory. The module uses the inherited SDK logger; do not add a
session-specific logging parameter or assume that verbose payload logging is safe for SCP traffic.

### Key Information Inspection

This composable fragment requires `ISecurityDomainSession session`, `ILogger logger`, and
`CancellationToken cancellationToken`, with the SecurityDomain and Microsoft.Extensions.Logging namespaces:

```csharp
var keyInfo = await session.ListKeyInformationAsync(cancellationToken);
foreach (var entry in keyInfo)
{
    logger.LogDebug(
        "Key: KID={Kid:X2}, KVN={Kvn:X2}",
        entry.KeyReference.Kid,
        entry.KeyReference.Kvn);
}
```

## Firmware Version Considerations

- **5.3.0**: Initial Security Domain support, SCP03 only
- **5.4.3**: Minimum selected by the module's SCP03 integration tests, not the SCP03 introduction version
- **5.7.2**: SCP11 support; existing factory-state tests expect four entries rather than three

These count assertions describe the tested factory-state expectation, not a universal count after key
management. `GenerateKeyAsync` locally gates SCP11 only when an exact version was supplied at creation.

## Known Gotchas

1. **Reset is destructive**: Failed or cancelled reset may already have removed custom state; return is not factory-state verification.
2. **Factory SCP03 reference**: KVN=0xFF is reserved; the first custom import replaces the factory set rather than retaining it for later deletion.
3. **SCP11 requires 5.7.2+**: Don't test SCP11 features on older firmware.
4. **Certificate chain order matters**: For SCP11a/c, leaf cert goes last in the chain.
5. **ArrayPool buffer sizes**: Always rent exact or larger size, never assume exact size returned.
6. **Certificate bundle ownership**: `GetCertificatesAsync` accumulates locally and disposes every loaded
   certificate before rethrowing a later load failure. Never skip corrupt entries. Successful results belong
   to the caller, not the session. `SecurityDomainCertificateBundleTests` observes real loaded certificates
   through the internal per-session `CertificateLoadedObserver` and checks native handles on failure/success.

## Related Modules

- **Core.SmartCard.Scp**: Base SCP protocol implementations
- **Core.Cryptography**: ECPrivateKey, ECPublicKey, StaticKeys
- **Tests.Shared**: YubiKeyTestState, test infrastructure
