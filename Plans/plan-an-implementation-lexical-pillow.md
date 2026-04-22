# Plan — WebAuthn Client port + previewSign (CTAP v4 draft) extension

## Context

`yubikit-swift` (branch `release/1.3.0`) ships a high-level **WebAuthn Client** layer that sits on top of CTAP2 and a recently added **`previewSign`** extension implementing the CTAP v4 draft *Web Authentication sign extension*. The C# `Yubico.NET.SDK` has a solid CTAP-focused FIDO2 module (`src/Fido2/`) but no WebAuthn-level wrapper: no `clientDataJSON` construction, no attestation-object assembly, no extension-output parsing, no status streaming, no `previewSign`.

This plan ports the Swift WebAuthn Client faithfully (semantics, type names, dispatch model) into idiomatic C# / .NET 10 (file-scoped namespaces, `init`-only properties, `ReadOnlyMemory<byte>`, switch expressions, `is null`, `ZeroMemory` for sensitive bytes, `IAsyncEnumerable<>` instead of Swift `AsyncSequence`, no LINQ on byte spans, no exceptions for control flow). After the WebAuthn Client is complete and unit-tested, `previewSign` is layered on top.

Work is broken into 8 phases. Each phase ships via `/DevTeam Ship`. Two `/CodeAudit` + `/DevTeam Ship` fix gates are placed: one after Phase 6 (WebAuthn Client complete), one after Phase 8 (`previewSign` complete).

**Constraints:**
- Only unit tests and integration tests that do **not** require user presence will be run. UP-required tests must compile and be `[Trait("RequiresUserPresence","true")]`-gated.
- Existing FIDO2 integrations are assumed to pass; do not regress them.
- No public-API changes to `Yubico.YubiKit.Fido2` except an internal fix to capture raw CBOR for `AttestationStatement` (Phase 2).

## Reference docs (read these first)

- `docs/research/DRAFT Web Authentication sign extension Signing arbitrary data using the Web Authentication API. Version 4.md` — original spec.
- `Plans/previewSign_Implementation_Requirements.md` — actionable spec extract (CBOR keys, validation rules, flag semantics).
- `SWIFT_WEBAUTHN_CLIENT_EXPLORATION.md` (repo root) — full Swift API map with file:line refs.
- `CLAUDE.md` — project conventions.
- `src/Fido2/CLAUDE.md` — FIDO2 module patterns and test harness rules.

## Module placement

**New module: `src/WebAuthn/` → `Yubico.YubiKit.WebAuthn` (separate assembly).**

- Mirrors Swift's layered separation; CTAP2 stays in `Fido2`.
- One-way dependency: `WebAuthn → Fido2` (compiler-enforced).
- Matches existing per-module packaging (`Piv`, `Oath`, `OpenPgp`, `YubiHsm`).
- Allows a clean public-API surface using WebAuthn names without colliding with CTAP-level types.

Layout: `src/WebAuthn/{src,tests}/`, with `src/Yubico.YubiKit.WebAuthn.csproj` referencing `src/Fido2/src/Yubico.YubiKit.Fido2.csproj`. Test projects mirror `Fido2` test-project conventions.

## Cross-cutting decisions (lock in now)

- **Naming:** `WebAuthnClient` (sealed, `IAsyncDisposable`) under `Yubico.YubiKit.WebAuthn`. One file per public type.
- **Async model:** `Task<T>` for terminal results; `IAsyncEnumerable<WebAuthnStatus>` for streaming overloads (suffix `StreamAsync`). Both shapes available on every flow that involves UV/PIN.
- **Origin:** caller passes a parsed `WebAuthnOrigin` at `WebAuthnClient` construction, plus a `Func<string,bool> isPublicSuffix` predicate (no built-in PSL dependency). `topOrigin` is opt-in per call.
- **Bytes:** `ReadOnlyMemory<byte>` everywhere on the public surface. Caller owns and zeroes.
- **PIN:** internally held as `IMemoryOwner<byte>` (UTF-8) and `CryptographicOperations.ZeroMemory`'d in `finally`. Convenience overloads accept `string?` but copy + clear immediately.
- **Errors:** single `WebAuthnClientError : Exception` with `Code` enum (`InvalidRequest`, `InvalidState`, `NotAllowed`, `Constraint`, `NotSupported`, `Security`, `Unknown` + previewSign-specific codes). Validation up-front; never use exceptions for control flow.
- **Logging:** `ILoggerFactory?` injected via constructor (default `NullLoggerFactory`); each component uses static `LoggingFactory.CreateLogger<T>()` per `CLAUDE.md`. Never log: PINs, COSE private material, full assertion CBOR, `tbs`, `signByCredential` keys.
- **Transport:** `WebAuthnClient` is transport-agnostic — accepts an `IFidoSession`, which already abstracts HID-FIDO and SmartCard-NFC. Existing FIDO2 transport rules remain enforced inside `FidoSession`.
- **Cancellation:** every async path takes `CancellationToken`; streams use `[EnumeratorCancellation]`; cancellation closes the producer channel.

## Phased breakdown

### Phase 1 — Core WebAuthn data model + COSE primitives

**Goal.** Type vocabulary the WebAuthn client speaks in.

**Deliverables.**
- `src/WebAuthn/src/WebAuthnRelyingParty.cs`, `WebAuthnUser.cs`, `WebAuthnCredentialDescriptor.cs`, `WebAuthnTransport.cs` (enum + `Unknown(string)`).
- `src/WebAuthn/src/Preferences/{ResidentKeyPreference,UserVerificationPreference,AttestationPreference}.cs`.
- `src/WebAuthn/src/Cose/CoseAlgorithm.cs` — `readonly struct` carrier (NOT enum-restricted), constants ES256(-7), EdDSA(-8), ESP256(-9), ES384(-35), RS256(-257), Esp256SplitArkgPlaceholder(-65539); `bool IsKnown`, `int Value`, `Other(int)` factory.
- `src/WebAuthn/src/Cose/CoseKey.cs` — `abstract record CoseKey` with `Ec2`, `Okp`, `Rsa`, `Other` variants; `static CoseKey Decode(ReadOnlyMemory<byte>)`; `byte[] Encode()`.
- `src/WebAuthn/src/Cose/Aaguid.cs` — `readonly struct` wrapping 16 bytes + `Guid Value`.

**Reuses.** `src/Fido2/src/Cbor/CoseKeyWriter.cs` patterns; `System.Formats.Cbor`.

**Tests.** Round-trip three pinned COSE-key vectors (ES256, EdDSA, RSA) byte-identical; `CoseAlgorithm` carries unknown ints; `Aaguid` ↔ `Guid`. **No validation-only tests.**

**Phase 1 verification checklist** (executor MUST tick every box before declaring complete):
- [ ] `src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj` exists and references `src/Fido2/src/Yubico.YubiKit.Fido2.csproj`.
- [ ] `dotnet toolchain.cs build` exits 0 with zero warnings in the new project.
- [ ] `CoseAlgorithm` is a `readonly struct` (not an enum) and carries unknown integer values via `Other(int)`.
- [ ] `CoseAlgorithm.Esp256SplitArkgPlaceholder.Value == -65539`.
- [ ] `CoseKey.Decode → Encode` produces byte-identical output for ES256, EdDSA, and RSA fixture (3 vectors committed under `tests/.../Vectors/`).
- [ ] `Aaguid ↔ Guid` round-trip test passes.
- [ ] All new files use file-scoped namespaces.
- [ ] `grep -rn "ToArray()" src/WebAuthn/src/` returns zero hits except inside `CoseKey.Encode` (where it is the documented exit point).
- [ ] No production code path outside `src/WebAuthn/` references any new type.
- [ ] `dotnet toolchain.cs test` passes for the new unit-test project.

### Phase 2 — `ClientData`, `AttestationObject`, WebAuthn `AuthenticatorData` reader

**Goal.** Build/parse the binary/JSON wrappers around CTAP2 payloads.

**Deliverables.**
- `src/WebAuthn/src/Client/WebAuthnOrigin.cs` — `Scheme/Host/Port`, `TryParse`, injectable PSL predicate.
- `src/WebAuthn/src/Client/ClientData.cs` — hand-rolled JSON construction with key order `type, challenge, origin, crossOrigin` (NOT `JsonSerializer` — guarantees byte parity with Swift); `Hash` returns 32 bytes via `SHA256.HashData`.
- `src/WebAuthn/src/Attestation/{AttestationFormat,AttestationStatement,AttestationObject}.cs` — discriminated `AttestationStatement` (`Packed`, `FidoU2F`, `Apple`, `None`, `Unknown(format,rawCbor)`); `AttestationObject.Decode`/`Encode` with byte-identical round-trip.
- `src/WebAuthn/src/WebAuthnAuthenticatorData.cs` — wraps `Yubico.YubiKit.Fido2.Credentials.AuthenticatorData` and adds `ParsedExtensions: IReadOnlyDictionary<string, ReadOnlyMemory<byte>>`.
- **Internal fix (only public-API edge into `Fido2`):** `src/Fido2/src/Credentials/MakeCredentialResponse.cs` — capture the raw CBOR slice for `AttestationStatement.RawData` (currently empty). No public-API change.
- `src/WebAuthn/src/Util/Base64Url.cs` if not already in Core.

**Reuses.** `Fido2.Credentials.AuthenticatorData.Parse`, `Fido2.Credentials.AttestationStatement.Decode`, `SHA256.HashData`.

**Tests.** Exact JSON byte assertion (key order, `crossOrigin` rendering); `ClientDataHash == SHA256(json)`; `AttestationObject` round-trip for `packed` and `none`; `AuthenticatorData` extension map decoded by identifier from a fixture with `{"credProtect":3}`.

**Phase 2 verification checklist:**
- [ ] `WebAuthnClientData.Hash` returns exactly 32 bytes for any input.
- [ ] `clientDataJSON` byte-equality test passes for fixed inputs (key order: `type, challenge, origin, crossOrigin`).
- [ ] `AttestationObject.Decode → Encode` byte-identical for ≥3 fixtures (`packed`, `fido-u2f`, `none`).
- [ ] `WebAuthnAuthenticatorData.ParsedExtensions["credProtect"]` is non-empty for the test fixture.
- [ ] `WebAuthnOrigin.TryParse("https://example.com:8443")` returns true; `TryParse("data:text/html,foo")` returns false.
- [ ] `MakeCredentialResponse.AttestationStatement.RawData` is populated (no longer empty) — verified by a Fido2 unit test.
- [ ] No public-API surface of `Yubico.YubiKit.Fido2` changed (only the internal `RawData` capture).
- [ ] `dotnet toolchain.cs build` zero warnings; `dotnet toolchain.cs test` passes.
- [ ] No use of `JsonSerializer` for `clientDataJSON` (must be hand-built concatenation).

### Phase 3 — `IWebAuthnBackend` + `WebAuthnClient.MakeCredentialAsync` (terminal)

**Goal.** Stand up the Backend abstraction; ship a working terminal `MakeCredential` (no streaming yet).

**Deliverables.**
- `src/WebAuthn/src/Client/IWebAuthnBackend.cs` — mirrors Swift `Backend` actor: `GetCachedInfoAsync`, `GetUvRetriesAsync`, `GetPinRetriesAsync`, `GetPinUvTokenAsync(method, permissions, rpId?, pinBytes?, ct)`, `MakeCredentialAsync`, `GetAssertionAsync`, `GetNextAssertionAsync`. Surfaces `IProgress<CtapStatus>?` for progress hooks (used by Phase 5).
- `src/WebAuthn/src/Client/FidoSessionWebAuthnBackend.cs` — concrete adapter wrapping `IFidoSession`. Owns `PinUvAuthProtocolV2` (disposed with backend).
- `src/WebAuthn/src/Client/Registration/{RegistrationOptions,RegistrationResponse}.cs`.
- `src/WebAuthn/src/Client/WebAuthnClient.cs` — `public sealed class WebAuthnClient : IAsyncDisposable`. Constructor takes `IWebAuthnBackend, WebAuthnOrigin, IReadOnlySet<string> enterpriseRpIds, Func<string,bool> isPublicSuffix, ILoggerFactory? = null`. First public method: `Task<RegistrationResponse> MakeCredentialAsync(RegistrationOptions, CancellationToken)`.
- `src/WebAuthn/src/Client/Validation/RpIdValidator.cs` — effective domain + PSL + enterprise allow-list.
- `src/WebAuthn/src/Client/UserVerification/UvDecision.cs` — `(useToken, useUv, useUvOption)` from info + preference + PIN presence.

**Reuses.** `Fido2.FidoSession`, `Fido2.Pin.{ClientPin, PinUvAuthProtocolV2, PinUvAuthTokenPermissions}`, `Fido2.Credentials.MakeCredentialResponse`, `Fido2.AuthenticatorInfo`.

**Tests** (mocked `IWebAuthnBackend`).
- Captures `MakeCredentialParameters` and asserts `clientDataHash == SHA256(constructed JSON)`.
- RP-ID validation rejects cross-origin mismatch with typed error.
- Retry on `Ctap2ErrPinAuthInvalid` acquires a fresh token (assert two token-acquisitions).
- AAGUID + public key flow through from attested credential data.
- `ResidentKey: Required` sets `rk` option.

**Phase 3 verification checklist:**
- [ ] `IWebAuthnBackend` interface defined with all 7 methods listed above.
- [ ] `FidoSessionWebAuthnBackend` implements `IWebAuthnBackend` and `IAsyncDisposable`; `DisposeAsync` disposes `PinUvAuthProtocolV2`.
- [ ] `WebAuthnClient.MakeCredentialAsync` returns a populated `RegistrationResponse` against a mocked backend.
- [ ] Unit test: captured `MakeCredentialParameters.ClientDataHash == SHA256(constructed JSON)`.
- [ ] Unit test: cross-origin RP ID throws `WebAuthnClientError` with `Code == InvalidRequest`.
- [ ] Unit test: `Ctap2ErrPinAuthInvalid` once → success; assert exactly 2 token-acquisition calls.
- [ ] Unit test: `ResidentKey: Required` sets the `rk` option in the captured CTAP request.
- [ ] PIN bytes are zeroed in a `finally` block — verified by `grep -rn "ZeroMemory" src/WebAuthn/src/Client/` returning ≥1 hit per PIN-handling method.
- [ ] No `string` PIN escapes the convenience overload boundary — `grep -rn "string pin" src/WebAuthn/src/` only present in the public convenience overload signatures.
- [ ] `dotnet toolchain.cs test` passes.

### Phase 4 — `WebAuthnClient.GetAssertionAsync` + matched-credential model

**Goal.** Authentication with deferred selection (`MatchedCredential.SelectAsync`).

**Deliverables.**
- `src/WebAuthn/src/Client/Authentication/{AuthenticationOptions,AuthenticationResponse,MatchedCredential}.cs`. `MatchedCredential` carries `Id`, optional `User`, and `Func<CancellationToken, Task<AuthenticationResponse>> SelectAsync`. Construction is `internal`.
- `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` — allow-list probing, multi-credential `GetNextAssertion` enumeration, discoverable preselection.
- `WebAuthnClient.GetAssertionAsync(AuthenticationOptions, CancellationToken) → Task<IReadOnlyList<MatchedCredential>>`.

**Reuses.** `FidoSession.GetAssertionAsync`, `GetNextAssertionAsync`; UV/PIN plumbing from Phase 3.

**Tests.** Allow-list probing returns matched set; discoverable enumeration via `GetNextAssertion`; `SelectAsync` is idempotent and returns a complete `AuthenticationResponse`; empty allow-list permitted only for discoverable.

**Phase 4 verification checklist:**
- [ ] `WebAuthnClient.GetAssertionAsync` returns `Task<IReadOnlyList<MatchedCredential>>`.
- [ ] `MatchedCredential` constructor is `internal`.
- [ ] Unit test: discoverable case enumerates via `GetNextAssertion` and returns `numberOfCredentials` matches.
- [ ] Unit test: allow-list probing returns matched set in declared order.
- [ ] Unit test: calling `MatchedCredential.SelectAsync` twice produces equivalent results (idempotent).
- [ ] Unit test: empty allow-list permitted only when authenticator has discoverable credentials.
- [ ] `AuthenticationResponse` exposes `CredentialId`, `RawAuthenticatorData`, `Signature`, `User?`, `SignCount`.
- [ ] No PIN held after method returns — verified by reviewing the call path's `finally` blocks.
- [ ] `dotnet toolchain.cs test` passes.

### Phase 5 — Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) + interactive PIN/UV

**Goal.** Replace terminal `Task<>` with Swift-style `StatusStream` so callers can render UI.

**Deliverables.**
- `src/WebAuthn/src/Client/Status/WebAuthnStatus.cs` — discriminated `abstract record`: `Processing`, `WaitingForUser(Action Cancel)`, `RequestingUv(Action<bool> SetUseUv)`, `RequestingPin(Func<string?, ValueTask> SubmitPin)`, `Finished<T>(T Result)`.
- `WebAuthnClient.MakeCredentialStreamAsync(... , [EnumeratorCancellation] CancellationToken) → IAsyncEnumerable<WebAuthnStatus>`.
- `WebAuthnClient.GetAssertionStreamAsync(...) → IAsyncEnumerable<WebAuthnStatus>`.
- Convenience overloads (parity with Swift `value(pin:useUV:)`):
  - `MakeCredentialAsync(RegistrationOptions, string? pin, bool useUv, CancellationToken)` — drains the stream and auto-responds.
  - `GetAssertionAsync(AuthenticationOptions, string? pin, bool useUv, CancellationToken)`.
- `src/WebAuthn/src/Client/Status/StatusChannel.cs` — internal unbounded `Channel<WebAuthnStatus>` with single-reader; producer uses `try/finally` and `Writer.Complete(exception?)` to guarantee no hangs on cancel.
- Refactor Phase 3/4 flows to publish status events into the channel.

**Reuses.** `System.Threading.Channels.Channel`.

**Tests.** Happy path emits `Processing` then `Finished`; `RequestingPin` emitted when no PIN supplied and consumer's `SubmitPin` completes the flow; cancel during `WaitingForUser` propagates and the iterator terminates within 100 ms; convenience drain helper auto-responds; consecutive identical statuses deduplicated.

**Phase 5 verification checklist:**
- [ ] `WebAuthnStatus` is an `abstract record` with all 5 cases (`Processing`, `WaitingForUser`, `RequestingUv`, `RequestingPin`, `Finished<T>`).
- [ ] `MakeCredentialStreamAsync` and `GetAssertionStreamAsync` use `[EnumeratorCancellation]` on the cancellation token parameter.
- [ ] Unit test: happy path emits `Processing` then `Finished` (in order).
- [ ] Unit test: when no PIN supplied, stream emits `RequestingPin` and resumes after `SubmitPin` completes.
- [ ] Unit test: cancel during `WaitingForUser` causes the iterator to terminate within 100 ms (assert via `CancellationTokenSource(TimeSpan.FromMilliseconds(100))`).
- [ ] Unit test: convenience drain helper auto-responds with provided PIN and returns terminal result.
- [ ] Unit test: consecutive identical `Processing` statuses are deduplicated.
- [ ] Producer code path uses `try/finally` calling `Channel.Writer.Complete(exception?)` — verified by code inspection.
- [ ] Phase 3 and Phase 4 unit tests still pass after refactor.
- [ ] `dotnet toolchain.cs test` passes.

### Phase 6 — Extension input/output framework + first-class wrappers

**Goal.** Mirror Swift's `RegistrationInputs/Outputs`/`AuthenticationInputs/Outputs`; dispatch through existing CTAP2 `ExtensionBuilder`.

**Deliverables.**
- `src/WebAuthn/src/Extensions/{WebAuthnExtensionInputs,WebAuthnExtensionOutputs}.cs` — record class with optional fields per extension (`Prf`, `CredProtect`, `CredBlob`, `MinPinLength`, `LargeBlob`, `CredProps`). previewSign added in Phase 7.
- `src/WebAuthn/src/Extensions/Adapters/{CredProtectAdapter,CredBlobAdapter,PrfAdapter,LargeBlobAdapter,MinPinLengthAdapter,CredPropsAdapter}.cs` — each adapter exposes `BuildCtapInput(IExtensionBuilderContext)` and `ParseOutput(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> rawExtMap, ...)`.
- `src/WebAuthn/src/Extensions/ExtensionPipeline.cs` — orchestrates input adapters → CBOR map for `FidoSession`, then output parsers from `WebAuthnAuthenticatorData.ParsedExtensions`.
- Wire pipeline into `MakeCredentialAsync` and `GetAssertionAsync`; populate `ClientExtensionResults` on responses.
- `credProps.rk` derived per Swift logic.

**Reuses.** `src/Fido2/src/Extensions/{ExtensionBuilder, CredBlobExtension, CredProtectPolicy, LargeBlobExtension, MinPinLengthExtension, PrfExtension, HmacSecretInput}.cs`.

**Tests.** For each adapter, assert the CTAP input bytes match a pinned vector lifted from yubikit-swift unit tests; parse outputs to typed records; `prf.evalByCredential` filters entries not in allow list; `largeBlob.supported = false` when authenticator lacks the key; pipeline returns empty outputs and omits the extensions field when no extensions requested.

**Phase 6 verification checklist:**
- [ ] All 6 adapters present (`CredProtect`, `CredBlob`, `Prf`, `LargeBlob`, `MinPinLength`, `CredProps`).
- [ ] Each adapter has a pinned-vector unit test asserting CTAP input bytes match yubikit-swift output.
- [ ] Unit test: `prf.evalByCredential` filters out entries not in the allow list.
- [ ] Unit test: `largeBlob.supported` is `false` when authenticator lacks the LargeBlob key.
- [ ] Unit test: `credProps.rk` reflects the residentKey option chosen at registration.
- [ ] Unit test: pipeline omits the `extensions` field entirely when `RegistrationInputs`/`AuthenticationInputs` are empty.
- [ ] `WebAuthnClient.MakeCredentialAsync` populates `RegistrationResponse.ClientExtensionResults` non-null when any extension was requested.
- [ ] `WebAuthnClient.GetAssertionAsync` populates `AuthenticationResponse.ClientExtensionResults` analogously.
- [ ] No new public API in `Yubico.YubiKit.Fido2` — verified by `git diff src/Fido2/src/` showing only internal/private modifiers in changed lines.
- [ ] `dotnet toolchain.cs test` passes.

---

### `[GATE 1]` `/CodeAudit` series + `/DevTeam Ship` to fix (after Phase 6)

**Audit categories:** security (PIN/UV handling, `ZeroMemory` coverage, log scrubbing), memory (ArrayPool returns, channel disposal, no leaked `byte[]`), modern C# (`is null`, switch expressions, file-scoped namespaces, init-only), perf (no LINQ on byte spans, no `Encoding.UTF8.GetBytes` of PIN), API style.

**Scope:** files added/modified in Phases 1–6 (`src/WebAuthn/**` plus the `MakeCredentialResponse.cs` `RawData` fix).

**Severity threshold:**
- **Block ship:** any Critical/High in security, memory, correctness; any UP-required test accidentally enabled in CI.
- **Defer:** Low/Info style findings that don't affect public API.

**Round-trip:** findings → `/DevTeam Ship` PRD ("Fix the following findings; before/after diff per finding; re-run unit tests"); each fix is a separate commit; second-pass audit on the diff must show zero new High+.

**Gate 1 verification checklist:**
- [ ] `/CodeAudit` ran with the categories listed above and produced a structured findings file under `Plans/audit-gate-1.md`.
- [ ] Every Critical/High finding has a corresponding fix commit referenced by SHA.
- [ ] Re-run `/CodeAudit` on the diff produced by the fix-up `/DevTeam Ship` reports zero new High+ findings.
- [ ] No tests with `[Trait("RequiresUserPresence","true")]` ran in the CI test invocation.
- [ ] `dotnet toolchain.cs test` passes after fixes.
- [ ] `git status` is clean (no orphaned working-tree changes from the audit).

---

### Phase 7 — `previewSign` types, CBOR I/O, attestation parsing

**Goal.** Add the previewSign extension type model and CBOR encoders/decoders per the v4 draft.

**Deliverables.**
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignFlags.cs` — `[Flags] enum : byte { Unattended=0b000, RequireUserPresence=0b001, RequireUserVerification=0b101 }`.
- `PreviewSignRegistrationInput.cs` — `IReadOnlyList<CoseAlgorithm> Algorithms`, `PreviewSignFlags Flags = RequireUserPresence`. Static factory `GenerateKey(...)`.
- `PreviewSignAuthenticationInput.cs` — `IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> SignByCredential` (use `ByteArrayKeyComparer`).
- `PreviewSignSigningParams.cs` — `KeyHandle, Tbs, AdditionalArgs?` (all `ReadOnlyMemory<byte>`).
- `PreviewSignRegistrationOutput.cs` — `GeneratedSigningKey GeneratedKey`.
- `GeneratedSigningKey.cs` — `KeyHandle, PublicKey: CoseKey, Algorithm: CoseAlgorithm, AttestationObject: WebAuthnAttestationObject, Flags: PreviewSignFlags`.
- `PreviewSignAuthenticationOutput.cs` — `Signature: ReadOnlyMemory<byte>`.
- `PreviewSignCbor.cs` — pure encode/decode helpers using **integer keys**: `kh=2, alg=3, flags=4, tbs=6, args=7, sig=6, attobj=7`. `args` MUST be wrapped in a byte string per the v4 draft.
- `PreviewSignErrors.cs` — typed mapping for `Ctap2ErrUnsupportedAlgorithm`, `Ctap2ErrInvalidOption`, `Ctap2ErrUpRequired`, `Ctap2ErrPuatRequired`, `Ctap2ErrInvalidCredential`, `Ctap2ErrMissingParameter`.

**Reuses.** Phase 1 `CoseAlgorithm/CoseKey`; Phase 2 `WebAuthnAttestationObject`; `System.Formats.Cbor` with `Ctap2Canonical`.

**Tests.** Encoder produces canonical bytes verified against v4 spec CDDL examples (assert exact hex); registration input encodes alg array + flags as integer-keyed map; authentication input omits `args` when null and wraps as bstr otherwise; registration output decodes nested `att-obj` (including its own embedded `flags`); authentication output extracts signature; flags validation throws on unknown values like `0b011`.

**Phase 7 verification checklist:**
- [ ] `PreviewSignFlags` enum has `Unattended=0b000, RequireUserPresence=0b001, RequireUserVerification=0b101`.
- [ ] CBOR encoder uses integer keys exactly: `kh=2, alg=3, flags=4, tbs=6, args=7, sig=6, attobj=7`.
- [ ] Unit test: registration input encodes `{3:[-7,-9],4:1}` byte-exact for `algorithms=[ES256, ESP256], flags=RequireUserPresence`.
- [ ] Unit test: authentication input omits the `args` key when `AdditionalArgs == null`.
- [ ] Unit test: when `AdditionalArgs` is set, it is wrapped as a CBOR byte string (bstr) per spec — verified with a hex assertion.
- [ ] Unit test: registration output decodes the nested `att-obj` including the embedded `flags` field.
- [ ] Unit test: authentication output extracts `Signature` bytes correctly.
- [ ] Unit test: invalid flag value (e.g., `0b011`) throws.
- [ ] CTAP error mapping covers all 6 codes listed in deliverables (`UnsupportedAlgorithm`, `InvalidOption`, `UpRequired`, `PuatRequired`, `InvalidCredential`, `MissingParameter`).
- [ ] No production path outside `src/WebAuthn/src/Extensions/PreviewSign/` calls into these types.
- [ ] `dotnet toolchain.cs test` passes.

### Phase 8 — Wire `previewSign` into `WebAuthnClient`

**Goal.** Integrate the extension into the pipeline; enforce all client-side validation per spec; surface outputs.

**Deliverables.**
- Add `PreviewSign` to `RegistrationInputs/Outputs` and `AuthenticationInputs/Outputs`.
- `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs`:
  - **Registration:** validate algorithms non-empty; choose `Flags` from `UserVerification` preference (UV → `0b101`, else `0b001`); append CBOR map under `previewSign` extension key.
  - **Authentication:** assert `allowCredentials` non-empty (else throws `WebAuthnClientError(InvalidRequest)`); assert `signByCredential.Keys` covers every `allowCredentials` id; route the selected credential's `SigningParams` to the backend.
- Output parsing prefers values extracted from the **verified** attestation object over loose top-level `keyHandle`/`publicKey` (per spec §4).
- `MatchedCredential.SelectAsync` propagates the chosen credentialId so the authentication adapter can pick the correct `SigningParams`.
- Update `src/Fido2/CLAUDE.md` extensions section to point at the WebAuthn-level `previewSign` location.

**Out of scope.** ARKG split-signing implementation. `additionalArgs` is accepted as opaque CBOR bytes only (parity with Swift).

**Reuses.** Phase 7 CBOR helpers; Phase 6 pipeline.

**Tests** (mocked backend; no UP).
- Captures CTAP CBOR sent and asserts byte-exact match for a registration request.
- Mocked attestation-object response populates `RegistrationOutput.GeneratedKey` correctly.
- `Authentication` throws when `allowCredentials` empty.
- `Authentication` throws when `signByCredential` misses an allowed id.
- Multiple allowed credentials: only the selected one's `tbs`/`keyHandle` reach the backend.
- `Authentication.Output.Signature` is populated.
- When loose `keyHandle`/`publicKey` differ from attestation-extracted values, attestation values win after verify.

**Phase 8 verification checklist:**
- [ ] `RegistrationInputs.PreviewSign` and `AuthenticationInputs.PreviewSign` exist; mirrored on the `*Outputs` records.
- [ ] Unit test: end-to-end `MakeCredentialAsync` with `PreviewSign = generateKey(...)` returns a `RegistrationResponse` with populated `ClientExtensionResults.PreviewSign.GeneratedKey`.
- [ ] Unit test: end-to-end `GetAssertionAsync` with `PreviewSign = signByCredential(...)` and non-empty allow list produces non-empty `ClientExtensionResults.PreviewSign.Signature`.
- [ ] Unit test: empty `allowCredentials` throws `WebAuthnClientError(InvalidRequest)` BEFORE any backend call.
- [ ] Unit test: `signByCredential` missing an allowed-list id throws `WebAuthnClientError(InvalidRequest)` BEFORE any backend call.
- [ ] Unit test: with multiple allowed credentials, only the selected credential's `tbs`/`keyHandle` reach the backend.
- [ ] Unit test: when loose `keyHandle`/`publicKey` differ from values inside the verified attestation object, the attestation values win.
- [ ] Flag selection rule: `UserVerification == Required → 0b101`; otherwise `0b001`. Verified by 2 unit tests.
- [ ] `src/Fido2/CLAUDE.md` updated to reference WebAuthn-level previewSign.
- [ ] `dotnet toolchain.cs test` passes.

---

### `[GATE 2]` `/CodeAudit` series + `/DevTeam Ship` to fix (after Phase 8)

**Audit categories:** Gate 1 set, plus CBOR canonical-encoding parity (vectors lifted from Swift unit tests), CTAP→`WebAuthnClientError` mapping completeness, attestation-trust posture (verified values supersede loose ones).

**Scope:** files added/modified in Phases 7–8.

**Severity threshold:** Gate 1 thresholds, plus block on any spec-conformance finding (CBOR key mismatch, missing flag enforcement, missing validation rule).

**Round-trip:** identical pattern to Gate 1.

**Gate 2 verification checklist:**
- [ ] `/CodeAudit` ran with categories above plus CBOR parity check; findings under `Plans/audit-gate-2.md`.
- [ ] Every Critical/High and every spec-conformance finding has a corresponding fix commit referenced by SHA.
- [ ] CBOR parity audit asserts byte-equality between C# and yubikit-swift outputs for all previewSign encoders (vector list pinned in audit doc).
- [ ] Re-run `/CodeAudit` on the diff produced by the fix-up `/DevTeam Ship` reports zero new High+ and zero new spec-conformance findings.
- [ ] No tests with `[Trait("RequiresUserPresence","true")]` ran in CI.
- [ ] `dotnet toolchain.cs test` passes after fixes.
- [ ] `git status` is clean.

---

## Final cumulative checklist (verified after Gate 2)

After all phases and gates complete, the orchestrator MUST verify every box below — these are the project-wide acceptance criteria. Any unchecked box blocks declaring the project complete.

**Build & test gates:**
- [ ] All eight phase verification checklists are 100% checked.
- [ ] Both gate verification checklists are 100% checked.
- [ ] `dotnet toolchain.cs build` exits 0 with zero warnings across the entire solution.
- [ ] `dotnet toolchain.cs test` exits 0; no tests skipped except those tagged `RequiresUserPresence` or `Slow`.
- [ ] All existing FIDO2 unit and non-UP integration tests still pass (no regression vs `develop` baseline).

**Module shape:**
- [ ] `src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj` is included in `Yubico.YubiKit.sln`.
- [ ] `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/` exists and is in the solution.
- [ ] `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/` exists and is in the solution; UP-required tests are trait-gated.
- [ ] One-way dependency confirmed: `grep -rn "Yubico.YubiKit.WebAuthn" src/Fido2/` returns zero hits.

**Public API surface (lock-in for downstream consumers):**
- [ ] `WebAuthnClient` is `sealed` and implements `IAsyncDisposable`.
- [ ] `WebAuthnClient` exposes both terminal (`Task<>`) and streaming (`IAsyncEnumerable<WebAuthnStatus>`) overloads for both `MakeCredential*` and `GetAssertion*`.
- [ ] All public byte-payload parameters/properties are `ReadOnlyMemory<byte>` (no `byte[]`).
- [ ] Single error type `WebAuthnClientError : Exception` with `Code` enum used throughout.

**Spec conformance (previewSign):**
- [ ] Extension identifier string is exactly `"previewSign"`.
- [ ] CBOR keys: `kh=2, alg=3, flags=4, tbs=6, args=7, sig=6, attobj=7` (verified by encoder tests).
- [ ] Flag enforcement: registration sets flags from UV preference; authentication path validates client-side before any CTAP round-trip.
- [ ] Verified attestation object values supersede loose top-level fields per spec §4.

**Security & memory:**
- [ ] `grep -rn "ZeroMemory" src/WebAuthn/src/` shows ≥1 hit per PIN-bearing or key-bearing call path.
- [ ] `grep -rn "ArrayPool" src/WebAuthn/src/` shows every `Rent` paired with a `Return` in a `finally` block.
- [ ] No log statement contains a PIN, key, COSE private material, `tbs`, or `signByCredential` key (audited via `grep -rni "log.*\(pin\|tbs\|signByCredential\|key\)" src/WebAuthn/src/`).
- [ ] No `string PIN` field is stored on any class — only passed transiently into a method scope.

**Documentation:**
- [ ] `src/WebAuthn/CLAUDE.md` exists summarizing module conventions and test harness.
- [ ] `src/Fido2/CLAUDE.md` updated to reference the WebAuthn-level previewSign location.
- [ ] `Plans/audit-gate-1.md` and `Plans/audit-gate-2.md` exist as the audit history.

---

## Critical files to reference

**Swift source (yubikit-swift, `release/1.3.0`):**
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Client.swift`
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/ClientData.swift`
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Origin.swift`
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/ClientError.swift`
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Registration/` (full dir)
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Authentication/` (full dir)
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Backends/` (full dir — `Backend` protocol)
- `YubiKit/YubiKit/FIDO/WebAuthn/Client/Shared/` (extensions dispatch incl. previewSign)
- `YubiKit/YubiKit/FIDO/WebAuthn/Extensions/` (all extension types incl. previewSign)
- `YubiKit/YubiKit/FIDO/WebAuthn/Attestation/` (attestation models)
- `YubiKit/YubiKit/FIDO/WebAuthn/AuthenticatorData.swift`
- `YubiKit/YubiKit/FIDO/WebAuthn/WebAuthn+CBOR.swift`, `WebAuthn+JSON.swift`

**C# target (Yubico.NET.SDK):**
- `src/Fido2/src/FidoSession.cs`
- `src/Fido2/src/Credentials/{AuthenticatorData,MakeCredentialResponse,GetAssertionResponse,AttestedCredentialData,PublicKeyCredentialTypes}.cs`
- `src/Fido2/src/Cbor/{CtapRequestBuilder,CtapResponseParser,CoseKeyWriter}.cs`
- `src/Fido2/src/Extensions/{ExtensionBuilder,ExtensionIdentifiers,ExtensionOutput,CredProtectPolicy,CredBlobExtension,LargeBlobExtension,MinPinLengthExtension,PrfExtension,HmacSecretInput}.cs`
- `src/Fido2/src/Pin/{ClientPin,PinUvAuthProtocolV2,IPinUvAuthProtocol}.cs`
- `src/Fido2/CLAUDE.md`, `src/Fido2/tests/CLAUDE.md`

## Verification strategy

**Unit (CI, no hardware):**
- For every CBOR encoder added (extensions, previewSign, attestation object), pin a hex byte vector lifted from yubikit-swift unit tests; assert byte-identical encoding from C#. Vectors live under `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Vectors/`.
- Round-trip (decode → encode) for `WebAuthnAttestationObject`, `CoseKey`, `AuthenticatorData`, previewSign output maps must produce byte-identical output for ≥3 fixtures each.
- A `FakeWebAuthnBackend` (test-only) drives `WebAuthnClient` through happy-path, retry-on-pin-invalid, multi-credential `GetNextAssertion`, and previewSign success/failure scenarios.

**Integration without UP** (`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/`):
- `WebAuthnClient` construction warm-up via `GetCachedInfoAsync`.
- `Reset` on a freshly-initialized YubiKey.
- RP-ID validation against a real origin.
- Public-suffix-checker invocation paths.

**Integration WITH UP (defined but skipped in automated runs):**
- All `MakeCredential` / `GetAssertion` happy-path tests with a real key are `[Trait("RequiresUserPresence","true")]` and excluded by the standard CI filter `--filter "RequiresUserPresence!=true"`. They must compile and be runnable on a developer machine with a YubiKey.

**Run commands:**
- `dotnet toolchain.cs build`
- `dotnet toolchain.cs test` (unit + non-UP integration)
- Module-targeted: `dotnet toolchain.cs -- test --integration --project WebAuthn --smoke` (after WebAuthn tests project added to the build script's project list)

## Risk register

1. **CBOR canonical-key parity drift.** *Mitigation:* central CBOR encoder helpers + pinned hex vectors per encoder; Gate 2 includes byte-level CBOR parity checks against Swift vectors.
2. **COSE key edge cases for new algorithms** (`esp256SplitARKGPlaceholder = -65539`). *Mitigation:* `CoseAlgorithm` is a `readonly struct` carrier (not a closed enum); decoder routes unknown algs into `CoseKey.Other` rather than throwing.
3. **Attestation-object byte parity with Java/Python SDKs for previewSign unsigned `att-obj`.** *Mitigation:* round-trip tests against ≥3 vectors; Phase 2 fix to capture raw CBOR for `AttestationStatement`.
4. **PIN handling regression in streaming path.** *Mitigation:* PIN traverses as `IMemoryOwner<byte>` UTF-8; zeroed in `finally`; Gate 1 includes a grep for `string` in PIN-bearing call paths.
5. **PinUvAuthProtocolV2 disposal lifetime.** *Mitigation:* `WebAuthnClient` owns + disposes backend; backend owns + disposes protocol; documented in module CLAUDE.md.
6. **Status stream cancellation hangs.** *Mitigation:* producer always calls `Channel.Writer.Complete(exception?)` in `finally`; cancellation unit test asserts iterator terminates within 100 ms.

## Execution sequencing (handoff to `/DevTeam Ship`)

Each phase is dispatched as a separate `/DevTeam Ship` PRD that includes:
1. Phase scope (deliverables list above).
2. "Done means" binary criteria (above).
3. Pointer to this plan + `Plans/previewSign_Implementation_Requirements.md` + `SWIFT_WEBAUTHN_CLIENT_EXPLORATION.md`.
4. Test list with pinned vectors where applicable.
5. Reminder: no UP-required tests in CI; trait gate `RequiresUserPresence`.

After Phase 6: dispatch `/CodeAudit` with the Gate 1 scope/categories above, then dispatch a `/DevTeam Ship` to fix findings. Then proceed to Phase 7.

After Phase 8: dispatch `/CodeAudit` with the Gate 2 scope/categories above, then dispatch a `/DevTeam Ship` to fix findings. Project complete.
