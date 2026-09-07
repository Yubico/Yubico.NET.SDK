# Applet public API

## Product grammar

Each applet exposes a sealed session, a complete SDK-implemented interface for testing and composition, an
`IYubiKey.CreateXSessionAsync` entry point that owns its hidden connection, and a static `XSession.CreateAsync`
entry point that borrows an existing connection. Both factories accept `SessionCreationOptions?` followed by a
defaulted cancellation token. Use `await using` for every returned session.

`SessionCreationOptions` is consumed during creation and is not retained. It groups only cross-cutting creation
concerns: protocol configuration, borrowed secure-channel parameters, preferred connection type, and the
provisional effective firmware-version override. A direct factory treats `PreferredConnectionType` as an
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
reviewed in `PublicAPI.Unshipped.txt`; `PublicAPI.Shipped.txt` remains only the nullable header until the stable
baseline is accepted. A public change therefore requires a declaration diff during review. Before 2.0 stable,
resolve the firmware override decision and move the accepted declarations to the shipped files. Once a stable
baseline package exists, enable package validation against the preceding stable version. Never add an optional
parameter to an already shipped signature; add an overload or an options property.

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
| CNV | Convenience layer | Deferred: existing one-shot methods remain normalized but are not expanded |
| VER | Firmware override | Provisional: retain for the alpha and reassess before the stable baseline; Security Domain cannot detect firmware and depends on the override as its only exact version input |

The general one-shot convenience-operation taxonomy and the final disposition of `FirmwareVersionOverride` are
deliberately deferred. This consolidation does not change wire encoding, transport fallback, connection ownership,
or security lifecycle behavior.

The VER decision must account for Security Domain: unlike the other applets, it does not detect firmware during
initialization. `FirmwareVersionOverride` is therefore its only exact version input and directly controls feature
gates; when omitted, Security Domain conservatively assumes firmware 5.3.0. This is recorded context, not a
resolution of VER.

## Deferred decisions

| Finding | Deferred decision | Deadline |
|---|---|---|
| F2 | Do not add a Core `CreateAppletSessionAsync<TSession>` template or rewrite the eight device extensions to forward the options object. Reconsider only with evidence that the repeated snapshots cause defects. | Before `PublicAPI.Shipped.txt` is populated. |
| F7 / CNV | Do not change which one-shot convenience extensions accept `SessionCreationOptions`. Decide the convenience taxonomy and whether those signatures should converge in the dedicated CNV round. | Before `PublicAPI.Shipped.txt` is populated if any signature will change. |
| F8 | Do not relocate `src/PublicApi/`. It is documented in its local README and CLAUDE guidance and in the root module table as a test-only cross-module convention project with no shipping assembly. | No public API deadline; revisit only with an authorized repository-layout change. |
| WebAuthn conventions | WebAuthn is a client facade, not an `ApplicationSession`; do not add it to applet session/interface lists. It is referenced by the convention project, and its device factory has a dedicated options-and-cancellation shape convention. Reassess whether other facade-specific conventions are warranted. | Before `PublicAPI.Shipped.txt` is populated. |

## Known pre-existing surface defects

Resolve these before populating `PublicAPI.Shipped.txt`; they are intentionally not changed by this consolidation:

- `Yubico.YubiKit.YubiOtp.SlotConfiguration` exposes protected mutable `byte[]` fields `_key`, `_uid`, and `_fixed`.
  `_key` can contain AES or HMAC key material, so declaring the current shape shipped would bless mutable secret
  storage on a public abstract type.
- `Yubico.YubiKit.Core.Sessions.ApplicationIds` exposes application identifiers as mutable process-wide `byte[]`
  values. A consumer can corrupt later application selection globally by mutating an element.
