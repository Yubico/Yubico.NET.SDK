# 26n Management builder reuse changes an already-built configuration (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only (the Android SDK's builder has the same shape; see Sibling SDKs). Python has no builder. |
| Fix group | A (no-brainer: copy the dictionary in `Build`) |
| Evidence | unit test |
| Since the audit | `DeviceConfig.cs` and `ManagementSession.cs` are unchanged at current yubikit. |

## What the audit says

`DeviceConfig.Builder.Build()` wraps the builder's live capability dictionary instead of copying it. Calling `WithCapabilities` on the same builder afterwards changes a configuration that was already built and validated, and the serialization step does not check again.

> "Later calls to `WithCapabilities` at lines 157–160 consequently change previously built configurations, including setting USB capabilities to zero despite the Build() rejection at lines 204–205."

Abbreviations: USB is universal serial bus. NFC is near-field communication. The management configuration controls which transports and applications are enabled on the key. SDK is software development kit. TLV is tag-length-value, the encoding for the configuration bytes.

## What is right

- **`Build()` wraps the live dictionary.** `new ReadOnlyDictionary<Transport, int>(_enabledCapabilities)` passes the builder's own dictionary to the configuration ([DeviceConfig.cs:209](../../../src/Management/src/DeviceConfig.cs#L209)). The read-only wrapper hides writes through the configuration, not through the builder.
- **`WithCapabilities` writes to that dictionary.** The setter assigns into `_enabledCapabilities` ([DeviceConfig.cs:157-162](../../../src/Management/src/DeviceConfig.cs#L157-L162)), so a later call changes the built configuration.
- **The USB check runs only in `Build()`.** The zero-USB rule is at [DeviceConfig.cs:204-205](../../../src/Management/src/DeviceConfig.cs#L204-L205). Serialization writes the USB value without checking it ([DeviceConfig.cs:84-86](../../../src/Management/src/DeviceConfig.cs#L84-L86)).
- **The unit test reproduces the problem.** [YESDK1634_BuiltConfigurationIsSnapshotOfBuilder](../../../src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs#L8-L19) builds with USB set to OATH, changes the builder to zero, and expects the built configuration to keep its value. **Result today (2026-10-10): fails, "Expected: 32, Actual: 0".**

## What is wrong or imprecise

- **Serialization is not the only unchecked path.** `DeviceConfig` is a public record with a public `EnabledCapabilities` initializer ([DeviceConfig.cs:42](../../../src/Management/src/DeviceConfig.cs#L42)). A caller can construct a `DeviceConfig` directly with a zero USB value, and `GetBytes` serializes it (by reading the code; not run). The audit's suggested fix (copy in `Build`) does not close this. The validation has to happen at serialization too, or at the session boundary.

## Why it matters

- **Who triggers it.** An application that builds a configuration and later changes the same builder, or that keeps a builder in a shared place and reuses it. A typical case is a settings screen that builds a configuration once and writes it later, after the user has changed another value on the same builder.
- **Impact.** The write sends a configuration with USB capabilities set to zero. Depending on the key's other transports, the device may then be reachable only over those transports, or not at all until it is reconfigured. This was not tested on a device.
- **Silent.** The write succeeds, and the built object still passes `Build()` when it is checked before the change.
- **Preconditions.** The builder is changed after `Build()` and before the configuration is written.

## Specification

No device specification in the cache covers this. The governing contract is the SDK's own:

> "At least one USB capability must be enabled." (exception message from `Build()`, [DeviceConfig.cs:204-205](../../../src/Management/src/DeviceConfig.cs#L204-L205))

The type exposes `EnabledCapabilities` as `IReadOnlyDictionary`, which suggests snapshot semantics. The code does not state an immutability guarantee.

## Canonical Python reference

- **Python is not affected by the builder problem.** `DeviceConfig` is a dataclass with a plain dictionary, and there is no builder to reuse ([yubikit/management.py:237-243](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L237-L243)).
- **Python has the second gap.** `get_bytes` writes the USB value as given, with no check for zero ([management.py:258-260](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/management.py#L258-L260)). A direct caller can write zero in Python too. This is the same gap as the second point under "What is wrong or imprecise".

## Sibling SDKs (context only)

- The Android SDK has the same shape. `DeviceConfig(Builder builder)` stores the builder's map without copying it ([DeviceConfig.java:51-52](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/management/src/main/java/com/yubico/yubikit/management/DeviceConfig.java#L51-L52)), and `build()` passes the builder itself ([DeviceConfig.java:222-224](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/management/src/main/java/com/yubico/yubikit/management/DeviceConfig.java#L222-L224)). The Android builder has no USB check at all. The same fix applies there.

## Reproduction

- Unit: [YESDK1634_BuiltConfigurationIsSnapshotOfBuilder](../../../src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs#L8-L19). Asserts that the built configuration still reports USB `32` after the builder is changed to `0`, and that a later `Build()` throws. **Result today: fails, "Expected: 32, Actual: 0".**
- Hardware: none. Writing a configuration with USB disabled is an operational risk on a test key, and the unit test shows the behaviour.

## Proposed fix

- **Recommendation.**
  1. In `Build()`, copy the dictionary before wrapping it: `new ReadOnlyDictionary<Transport, int>(new Dictionary<Transport, int>(_enabledCapabilities))` ([DeviceConfig.cs:207-214](../../../src/Management/src/DeviceConfig.cs#L207-L214)).
  2. Re-check the USB rule at serialization in `GetBytes` ([DeviceConfig.cs:84-86](../../../src/Management/src/DeviceConfig.cs#L84-L86)), so that a directly constructed `DeviceConfig` with USB zero is rejected before it is written. Throw `InvalidOperationException`, the same type `Build()` uses.
  3. Apply the same copy in the Android SDK's `build()`, and add the same check there.
- **Options considered.**
  - Option 1 (recommended): copy in `Build()` and validate in `GetBytes`. Two small changes; both enforce the invariant the SDK already claims.
  - Option 2: copy only. Fixes the reported path and leaves the direct-construction gap.
  - Option 3: make `DeviceConfig` construction private and only allow the builder. Clean, but a larger breaking change to a public record.
- **API impact.** None for builder callers. Direct construction with USB zero starts to throw at the session call, which is a behaviour change for those callers.
- **Proving test.** The existing test, plus one that builds a `DeviceConfig` directly with USB zero and expects `SetDeviceConfigAsync` to throw before any command is sent.
- **Depends on / interacts with.** Independent of the other findings. `DeviceConfig.cs` and `ManagementSession.cs` are not in any other finding's path.
- **Open questions for the maintainer.** Builder-only validation, or validation at the session boundary as well? The recommendation is both. Should the Android SDK's change be tracked as a separate finding?

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~YESDK1634_BuiltConfigurationIsSnapshotOfBuilder"
```

Read in this order:

- [DeviceConfig.cs:148-216](../../../src/Management/src/DeviceConfig.cs#L148-L216) (the builder, `WithCapabilities` and `Build`).
- [DeviceConfig.cs:84-86](../../../src/Management/src/DeviceConfig.cs#L84-L86) (USB serialization without a check).
- [ManagementSession.cs:143-166](../../../src/Management/src/ManagementSession.cs#L143-L166) (`SetDeviceConfigAsync`, where the configuration is serialized).
- `DeviceConfig` in `yubikit/management.py` (lines 237-260) in the Python reference.
