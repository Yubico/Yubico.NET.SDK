# Applet public API

## Product grammar

Each applet exposes a sealed session, a complete SDK-implemented interface for testing and composition, an
`IYubiKey.CreateXSessionAsync` entry point that owns its hidden connection, and a static `XSession.CreateAsync`
entry point that borrows an existing connection. Both factories accept `SessionCreationOptions?` followed by a
defaulted cancellation token. Use `await using` for every returned session.

`SessionCreationOptions` is consumed during creation and is not retained. It groups only cross-cutting creation
concerns: protocol configuration, borrowed secure-channel parameters, preferred connection type, and the
effective firmware-version override. A direct factory treats `PreferredConnectionType` as an
assertion. A device factory uses it for selection. Supplying secure-channel parameters without a preference
continues to force SmartCard where that behavior already existed.

All application sessions expose their effective `FirmwareVersion`, `ConnectionType`, initialization state,
protocol-authentication state, and feature checks through `IApplicationSession`. Applet interfaces mirror all
public concrete-session operations. They are implemented by the SDK and are not applet plug-in contracts.

## Operation rules

- Required domain inputs stay positional. Do not introduce request or command objects merely to shorten a method.
- Use a trailing options object only for a demonstrated growth axis, multiple independent policies, or otherwise
  ambiguous booleans. A single intrinsic boolean may remain when it reads naturally.
- Device input/output methods return `Task` or `Task<T>`, end in `Async`, and take a final defaulted cancellation
  token.
- Borrowed asynchronous bytes use `ReadOnlyMemory<byte>`. Textual secret bytes use an `Utf8` parameter suffix.
  The caller owns and clears borrowed secrets. The SDK clears every sensitive buffer it allocates.
- Public collections use read-only interfaces unless mutation is the explicit contract. OATH `DeriveKey` remains
  the deliberate `byte[]` output exception because the caller owns and must clear that derived secret.
- Preserve protocol vocabulary where semantics differ. Normalize only equivalent concepts.
- Applet dependency-injection registrations and factory delegates are not part of the product surface.

PIV biometric verification is a three-state mode, represented by `PivUserVerification`: `Verify`,
`VerifyAndRequestTemporaryPin`, and `CheckOnly`. This intentionally differs from the original plan's options-object
proposal. The previous two booleans exposed four combinations even though `(true, true)` was not a fourth behavior;
check-only silently won. An enum makes every valid mode explicit.

## Compatibility process

The ten shipping SDK projects use `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Their current 2.0-alpha surfaces are
reviewed in `PublicAPI.Unshipped.txt`; `PublicAPI.Shipped.txt` remains only the nullable header throughout the alpha
series and is populated at the first beta release. While the shipped files are empty, surface changes are ordinary
declaration edits; once populated, every break requires an explicit `*REMOVED*` entry, which is the right ratchet for
beta rather than alpha churn. Nothing from this decision round gates that transition: F8 has no public API deadline,
and expanding the convenience set is future feature work rather than a deferred API decision. Once a stable baseline
package exists, enable package validation against the preceding stable version. Never add an optional parameter to an
already shipped signature; add an overload or an options property.

### Adding an interface member

Applet session interfaces are SDK-implemented contracts, but test fakes may implement them. After the general-
availability baseline, add a new operation with a default interface implementation that returns a failed `Task`
containing `NotSupportedException`; the concrete SDK session supplies the real implementation. Do not put protocol
logic in the interface. A large optional subsystem may instead use a companion type, but capability-interface
proliferation is not the default.

## Taxonomy

| ID | Category | Decision |
|---|---|---|
| CRE | Session creation | Shared options and uniform factories |
| CON | Common contract | Shared connection type and documented effective version |
| PAR | Interface parity | Concrete public members appear on session interfaces |
| EXT | Extensibility | Options only for proven growth axes |
| MEM | Memory ownership | Borrowed bytes use read-only memory |
| ASY | Asynchronous shape | Task, Async suffix, final cancellation token |
| COL | Collections | Read-only public collection contracts |
| NAM | Naming | Normalize equivalent names while preserving protocol terms |
| SUR | Surface minimization | No applet dependency-injection APIs |
| CMP | Compatibility | Public API declarations and later package validation |
| CNV | Convenience layer | Resolved: normalized, not expanded; all ten one-shot conveniences accept `SessionCreationOptions`, while adding new conveniences is future feature work rather than a deferred API decision |
| VER | Firmware override | Resolved: retain `FirmwareVersionOverride`; all eight sessions consume it, and Security Domain depends on it as its only exact version source |

CNV is normalized, not expanded. Expanding the set beyond the existing ten conveniences remains future feature work
and must not be reopened as a deferred public API question. `FirmwareVersionOverride` remains public: all eight
sessions consume it, and Security Domain cannot detect firmware at all, making it that applet's only exact version
source rather than an override. Its XML documentation records this exception; without a supplied value, Security
Domain conservatively assumes firmware 5.3.0.

## Resolved decisions

| Finding | Resolution |
|---|---|
| F2 | **RESOLVED:** The Core `CreateAppletSessionAsync<TSession>` template remains correctly refused; the narrower defect was eight factories rebuilding options field by field, fixed by `SessionCreationOptions.WithPreferredConnectionType`. |
| F7 / CNV | **RESOLVED:** The ten one-shot conveniences now have a uniform `SessionCreationOptions?`-then-cancellation shape, pinned by `OneShotDeviceExtensions_UseUniformOptionsAndCancellationShape` in `src/PublicApi/`. |
| VER | **RESOLVED — RETAIN:** `FirmwareVersionOverride` stays public and is consumed by all eight sessions; its XML documentation records that Security Domain cannot detect firmware and uses it as its only exact version source. |
| WebAuthn conventions | **RESOLVED:** The existing `WebAuthnDeviceFactory_UsesSessionOptionsAndCancellationShape` facade convention in `src/PublicApi/` is sufficient; no further facade-specific conventions are warranted at this time. |

## Deferred repository-layout decision

F8 remains deferred with no deadline: do not relocate `src/PublicApi/`. Its local README and CLAUDE guidance and the
root module table already identify it as a test-only cross-module convention project with no shipping assembly.
Revisit it only as part of an authorized repository-layout change.

## Resolved pre-existing surface defects

- **RESOLVED:** `Yubico.YubiKit.YubiOtp.SlotConfiguration` no longer exposes protected mutable `_fixed`, `_uid`,
  `_key`, or `_fixedSize` storage. Derived classes use protected copy-in setters that validate length and never
  surrender the array, so key material cannot be aliased out of a derived type.
- **RESOLVED:** `Yubico.YubiKit.Core.Sessions.ApplicationIds` is a static class exposing `ReadOnlyMemory<byte>`
  properties rather than mutable process-wide `byte[]` fields, and each read returns a fresh copy.
  `ReadOnlyMemory<byte>` alone would not have been sufficient, because `MemoryMarshal.TryGetArray` recovers the
  backing array; copying is what removes the process-wide write.
