# Audit Gate 1 — WebAuthn Phases 1-6

**Branch:** webauthn/phase-6-extensions
**Audit date:** 2026-04-22
**Scope:** src/WebAuthn/** + Phase 2 Fido2 fix files
**Audit mode:** Read-only, no source modifications.

## Summary

- Critical: 0
- High: 4
- Medium: 7
- Low/Info: 9
- **Block-ship?** YES — H-1, H-2, and H-3 each individually block (security correctness, memory leak, dead-code retry).

## Findings

### CRITICAL

(none)

### HIGH

#### H-1. PIN buffer is over-zeroed: `ZeroMemory(pinOwner.Memory.Span)` clears the whole rented block, including bytes never used; correct, but the inverse risk — leak of unused PIN data — is also real on the `>= pinByteCount` slice.
- **Severity:** High
- **Category:** security / memory
- **File:** `src/WebAuthn/src/Client/WebAuthnClient.cs:568, 679`
- **Description:** In `MakeCredentialCoreAsync` and `GetAssertionCoreAsync` the entire pool buffer is zeroed (`pinOwner.Memory.Span`). Compare with the convenience overload (lines 388, 461) which zeros only `[..pinByteCount]`. Zeroing the whole rented block is *safer* in the core paths, but the convenience-overload pattern only zeroes the used prefix — the tail of the rented block may still contain copied PIN bytes if `MemoryPool` over-allocates and the same slice is later returned as `Memory.Span` (whose length equals `pinByteCount`). Because `IMemoryOwner<byte>.Memory.Span` returns the *full* rented buffer (not a slice), Lines 388/461 leave any bytes between `pinByteCount` and the full pool length zeroed too — actually safe. The real concern is the **opposite** asymmetry: lines 388/461 use `[..pinByteCount]` while the value being copied was written via `Encoding.UTF8.GetBytes(pin, pinOwner.Memory.Span)` (line 350/423) — `Encoding.UTF8.GetBytes` writes exactly `pinByteCount` bytes, so this is fine. Net: the inconsistency is cosmetic, not a bug — but reviewers will mis-read it.
- **Evidence:**
  ```csharp
  // line 388 (convenience overload)
  CryptographicOperations.ZeroMemory(pinOwner.Memory.Span[..pinByteCount]);
  // line 568 (core path)
  CryptographicOperations.ZeroMemory(pinOwner.Memory.Span);
  ```
- **Recommended fix:** Standardize on `pinOwner.Memory.Span` (whole rented block) in all four places. Zero-cost defense-in-depth and removes reviewer confusion.

#### H-2. `PinUvAuthTokenSession` is `IDisposable` only — `WebAuthnClient.MakeCredentialAsync(..., pin, useUv, ...)` never disposes the token session if `MakeCredentialStreamAsync`'s inner `MakeCredentialCoreAsync` throws *and* the producer is still running.
- **Severity:** High
- **Category:** security / lifetime
- **File:** `src/WebAuthn/src/Client/WebAuthnClient.cs:218-256, 282-319`
- **Description:** `MakeCredentialStreamAsync` and `GetAssertionStreamAsync` use `Task.Run(...)` to start a producer which calls `MakeCredentialCoreAsync` — and `MakeCredentialCoreAsync` owns the `PinUvAuthTokenSession` in its `finally` (correct). However, if a consumer of the **stream** breaks out of `await foreach` *without* cancelling the token (e.g., user-thrown exception in consumer body), the producer keeps running because the `cancellationToken` was passed to `Task.Run` but never linked to the iterator's lifetime. The producer eventually finishes, disposes the token correctly — but during the gap (potentially seconds) the PIN/UV token bytes remain in memory after the consumer has moved on. More subtly, `await producerTask` at line 255/318 will *hang* if the producer is blocked on `channel.WriteAsync` waiting for a consumer that has gone away (note: unbounded channel — won't block on Write, so this risk is mitigated; but it does mean the producer races to completion, leaking effort).
- **Evidence:**
  ```csharp
  var producerTask = Task.Run(async () => { ... }, cancellationToken);
  await foreach (var status in channel.Reader(cancellationToken).ConfigureAwait(false))
      yield return status;
  await producerTask.ConfigureAwait(false); // races with consumer's break
  ```
- **Recommended fix:** Use a linked `CancellationTokenSource` inside `MakeCredentialStreamAsync` so consumer-disposal of the iterator (via `await foreach`'s implicit `DisposeAsync`) cancels the producer. Wire `EnumeratorCancellation`'s effective token into a CTS that also gets cancelled in the `finally` of the iterator.

#### H-3. `AcquirePinUvTokenWithRetryAsync` retry on `PinAuthInvalid` is **dead code** in the streaming path: the same PIN bytes are reused across all attempts.
- **Severity:** High
- **Category:** correctness / security
- **File:** `src/WebAuthn/src/Client/WebAuthnClient.cs:733-783`
- **Description:** The retry loop catches `CtapException(PinAuthInvalid)` and re-invokes `_backend.GetPinUvTokenAsync(...)` with the **same** `pinBytes` parameter. Because the only cause of `PinAuthInvalid` here is a wrong PIN or a stale shared-secret encryption mismatch, retrying with identical PIN bytes will burn PIN attempts on the YubiKey (potentially locking it after 3 wrong tries). The streaming path emits `WebAuthnStatusRequestingPin` only **once** (lines 524-541, 621-638) — there is no mechanism to ask the consumer for a fresh PIN between retries. Either: (a) the retry exists to handle a transient encryption-state mismatch and should re-init the protocol but not re-prompt; or (b) it's intended to handle wrong PIN and should re-emit `WebAuthnStatusRequestingPin`. Today it does neither correctly. Test coverage was deferred ("Phase 3 deferred") and these tests would have caught it.
- **Evidence:**
  ```csharp
  while (attempt < MaxPinAuthRetries)
  {
      attempt++;
      try {
          var session = await _backend.GetPinUvTokenAsync(method, ..., pinBytes, ...);
          return session;
      }
      catch (CtapException ex) when (ex.Status == CtapStatus.PinAuthInvalid && attempt < MaxPinAuthRetries) {
          previousSession?.Dispose();   // never reached: only set if the loop succeeded once
          previousSession = null;
          // No PIN re-prompt, no protocol re-init — just retry with same pinBytes.
      }
  }
  ```
- **Recommended fix:** Either (a) document & limit retry to protocol-state errors only (use a different CTAP code or detect "encryption mismatch" subtype); or (b) bubble `PinAuthInvalid` to the consumer immediately and let *them* re-call `MakeCredentialAsync` with fresh PIN bytes; or (c) re-prompt via channel callback before retry. Today this loop will burn PIN attempts on a wrong PIN.

#### H-4. `MatchedCredential.SelectAsync(CancellationToken)` ignores its `cancellationToken` parameter entirely.
- **Severity:** High
- **Category:** API correctness
- **File:** `src/WebAuthn/src/Client/Authentication/MatchedCredential.cs:93-99`
- **Description:** The method accepts `CancellationToken` and documents it, but the body returns `_responseFactory.Value` and the `Lazy<Task<...>>` was constructed with `CancellationToken.None`. Callers passing a token will mistakenly believe cancellation is plumbed. Comment at line 95-97 acknowledges this — "ignores the cancellationToken for simplicity" — but that's an API contract violation: the parameter exists in the public signature.
- **Evidence:**
  ```csharp
  public Task<AuthenticationResponse> SelectAsync(CancellationToken cancellationToken = default)
  {
      // Note: The Lazy pattern ignores the cancellationToken for simplicity,
      return _responseFactory.Value;
  }
  ```
- **Recommended fix:** Either remove the parameter (since the assertion is pre-computed, cancellation is genuinely meaningless) or honor it (e.g., `await _responseFactory.Value.WaitAsync(cancellationToken)` to let callers abandon waiting). Removing is cleaner; keeping it lies to callers.

### MEDIUM

#### M-1. `ParsedExtensions` dictionary lookups have no null/empty-value guards before CBOR parsing.
- **Severity:** Medium
- **Category:** correctness / robustness
- **File:** `src/WebAuthn/src/Extensions/Adapters/CredBlobAdapter.cs:44, 67`, `CredProtectAdapter.cs:43`, `MinPinLengthAdapter.cs:34`, `PrfAdapter.cs:108, 137`
- **Description:** All adapters do `extensions.TryGetValue(id, out var rawValue)` then immediately wrap `rawValue` in a `CborReader`. If a malicious authenticator returns an empty value or one with the wrong CBOR type, behavior depends on `CborReader` — it will throw a non-`WebAuthnClientError` (e.g., `CborContentException`) which escapes through `BuildRegistrationResponse` and is not caught/wrapped. Per the convention, all errors crossing the public boundary should be `WebAuthnClientError`.
- **Evidence:**
  ```csharp
  if (!extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var rawValue))
      return null;
  var reader = new CborReader(rawValue, CborConformanceMode.Lax);
  if (reader.PeekState() == CborReaderState.Boolean) ...
  ```
- **Recommended fix:** Wrap each adapter's parse in `try/catch (CborContentException) { return null; }` or rethrow as `WebAuthnClientError(InvalidState, ...)`. Also validate `rawValue.IsEmpty` before reading.

#### M-2. `CredBlobAdapter.ParseAuthenticationOutput` returns the byte string without validating CTAP2.1 length constraint (1-32 bytes).
- **Severity:** Medium
- **Category:** correctness / spec compliance
- **File:** `src/WebAuthn/src/Extensions/Adapters/CredBlobAdapter.cs:65-79`
- **Description:** Per CTAP2.1, credBlob is 1-32 bytes. Input is validated (`CredBlobInput.Validate()`) but assertion output is accepted blindly. A misbehaving authenticator could return 0 or 1000 bytes and the SDK passes it through.
- **Evidence:**
  ```csharp
  if (reader.PeekState() == CborReaderState.ByteString) {
      return new Outputs.CredBlobAssertionOutput(reader.ReadByteString());
  }
  ```
- **Recommended fix:** After `ReadByteString()`, validate `result.Length is >= 1 and <= 32` else return null.

#### M-3. `CredProtectAdapter.ParseRegistrationOutput` calls `reader.ReadInt32()` which throws for non-integer CBOR; per M-1 this escapes uncaught.
- **Severity:** Medium
- **Category:** correctness
- **File:** `src/WebAuthn/src/Extensions/Adapters/CredProtectAdapter.cs:43-50`
- **Description:** Same root cause as M-1; the silent fall-through to `null` only covers out-of-range *values*, not type mismatches.
- **Recommended fix:** Inspect `reader.PeekState()` first; return null on type mismatch.

#### M-4. PRF allow-list filter uses `.First()` after `.Where()`, silently picking only one credential's eval and discarding the rest.
- **Severity:** Medium
- **Category:** correctness / spec compliance
- **File:** `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:48-66`
- **Description:** Per WebAuthn PRF spec, `evalByCredential` is a per-credential map. CTAP only supports a single salt pair per request, so the SDK has to pick one — but the current code picks "the first match" deterministically based on `allowCredentials` ordering. There is no logging, no warning, and no signal to the caller that other entries were dropped. Worse: this happens *before* the authenticator selects which credential to use, so the "first" eval may not match the credential the user actually authenticates with. The Swift reference implementation either fails or rotates through; this silently corrupts.
- **Evidence:**
  ```csharp
  var filteredEvals = input.EvalByCredential.Where(...).ToList();
  if (filteredEvals.Any()) {
      var firstEval = filteredEvals.First().Value;  // arbitrary pick
      builder.WithPrf(prfInput);
      return;
  }
  ```
- **Recommended fix:** Either error if `filteredEvals.Count > 1` (forcing the caller to scope to a single credential), or split into multiple GetAssertion calls per the WebAuthn spec PRF processing model. Document the chosen behavior.

#### M-5. `LargeBlobAdapter.ApplyToBuilder` ignores `LargeBlobInput.Support` (Required vs Preferred).
- **Severity:** Medium
- **Category:** correctness / spec compliance
- **File:** `src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs:24-28`
- **Description:** The input type carries `LargeBlobSupport` (Required/Preferred), but the adapter unconditionally calls `WithLargeBlobKey()` without checking whether to fail-on-unsupported. If support is `Required` and the authenticator returns no key, registration silently succeeds with `Supported: false` instead of throwing. Phase 6 was scoped down — this needs explicit "limitation documented" marking.
- **Evidence:**
  ```csharp
  public static void ApplyToBuilder(ExtensionBuilder builder, Inputs.LargeBlobInput input)
  {
      // For registration, signal largeBlobKey request
      builder.WithLargeBlobKey();  // ignores input.Support
  }
  ```
- **Recommended fix:** Either honor `Support == Required` by validating output presence and throwing `WebAuthnClientError(NotSupported, ...)` in `ParseRegistrationOutput` when `Required` was requested, or document the deferral explicitly.

#### M-6. `ExtensionPipeline.BuildAuthenticationExtensionsCbor` for `LargeBlob` sets `hasExtensions = true` but writes nothing — produces an empty CBOR map sent to the authenticator.
- **Severity:** Medium
- **Category:** correctness
- **File:** `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:107-112`
- **Description:** When `inputs.LargeBlob is not null` for authentication, the code marks `hasExtensions = true` but never calls any builder method, then later returns `builder.Build()`. Result: an extensions map containing only entries from PRF (or empty if no PRF). If only LargeBlob is present, the encoded CBOR is `{}` which is a valid-but-meaningless extensions field that wastes bytes and may confuse strict authenticators.
- **Evidence:**
  ```csharp
  if (inputs.LargeBlob is not null) {
      // Phase 6 simplified scope - just signal support
      hasExtensions = true;     // <-- but no builder call follows
  }
  ```
- **Recommended fix:** Either (a) call a real builder method; or (b) don't set `hasExtensions = true` when scope is deferred; or (c) explicitly comment "this branch is intentionally a no-op until Phase 7."

#### M-7. `FidoSessionWebAuthnBackend` allocates `byte[]` via `.ToArray()` for every PinUvAuthParam transmission — twice in hot path.
- **Severity:** Medium
- **Category:** memory / perf
- **File:** `src/WebAuthn/src/Client/FidoSessionWebAuthnBackend.cs:150, 200`
- **Description:** Every MakeCredential / GetAssertion call allocates a new byte array via `.ToArray()` to satisfy the underlying `MakeCredentialOptions.PinUvAuthParam` signature (which is `byte[]`). Per CLAUDE.md memory rules this is a documented exit, but the underlying Fido2 API ought to accept `ReadOnlyMemory<byte>`. Flag as tech-debt against the Fido2 module's API.
- **Recommended fix:** Update `MakeCredentialOptions.PinUvAuthParam` and `GetAssertionOptions.PinUvAuthParam` (in Fido2 module) to `ReadOnlyMemory<byte>`. Out of audit scope but worth a follow-up ticket.

### LOW / INFO

#### L-1. `WebAuthnClient` has zero structured logging; `LoggingFactory` does not exist anywhere in `src/`.
- **Severity:** Low / Info
- **Category:** observability
- **File:** all of `src/WebAuthn/src/`
- **Description:** Per CLAUDE.md the convention is `private static readonly ILogger Logger = LoggingFactory.CreateLogger<T>();`. Searched: `grep -rn "LoggingFactory" src/ --include="*.cs"` returns ZERO matches in source (only in compiled XML doc artifacts). The project actually uses `Microsoft.Extensions.Logging.ILogger` injected (see `Yubico.YubiKit.Core.YubiKitLogging.Configure`). The CLAUDE.md guidance is **stale** — no class named `LoggingFactory` exists. WebAuthn module has no logging at all (no `ILogger`, no log statements). For a module that handles PIN auth, this is observability debt but not a security gap (since logging would more likely *introduce* leak risk than prevent one).
- **Recommended fix:** Either (a) add structured logging using `Yubico.YubiKit.Core.YubiKitLogging` (the actual factory), being careful not to log PIN/token bytes; or (b) update CLAUDE.md to remove the stale `LoggingFactory` reference. **Document the decision either way.**

#### L-2. `Aaguid` is a `readonly struct` storing a privately-cloned `byte[]`.
- **Severity:** Low / Info (acceptable per CLAUDE.md exception)
- **Category:** API design
- **File:** `src/WebAuthn/src/Cose/Aaguid.cs:28, 41, 68`
- **Description:** Per CLAUDE.md, "NEVER store privately-cloned `byte[]` of *sensitive* data in a struct." AAGUID is a public identifier — explicitly *not* sensitive — so this is fine. However, defensive copies of the struct are wasteful (16-byte heap-allocated array per instance, copied by reference but boxed if used in collections). At 16 bytes the data fits in a `Guid`/`Int128`. Reviewer-confusion risk: someone may mis-classify Aaguid as sensitive.
- **Recommended fix:** Add a `// NOTE: AAGUID is a public identifier per WebAuthn spec, not sensitive — byte[] storage in struct is intentional and safe.` comment near `_bytes` field. Optionally refactor to use `Guid` internally.

#### L-3. `Aaguid.Equals` uses `SequenceEqual` (not `FixedTimeEquals`) — correct for non-secret data.
- **Severity:** Info
- **Category:** security
- **File:** `src/WebAuthn/src/Cose/Aaguid.cs:110`
- **Description:** Acceptable — AAGUID is public.

#### L-4. `ExtensionPipeline` has no public-facing surface (`internal sealed class`) — its inputs/outputs are public records, but the pipeline class is internal. Test coverage at unit-test level is therefore validation of a non-public API. This is fine, but the approach makes integration-test-only validation of e.g. PRF impossible without exposing internals.
- **Severity:** Info
- **Category:** testability
- **File:** `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:26`

#### L-5. `WebAuthnClient.ValidateRegistrationOptions` does not validate `PubKeyCredParams` entries (e.g., zero-value `CoseAlgorithm`).
- **Severity:** Low
- **Category:** validation
- **File:** `src/WebAuthn/src/Client/WebAuthnClient.cs:685-714`
- **Description:** Validates count > 0 but accepts any `CoseAlgorithm` value, including likely-bogus entries. Authenticator will reject, but the error is delayed.

#### L-6. `WebAuthnClient.AcquirePinUvTokenWithRetryAsync` declares `previousSession` and never assigns to it (only reads on dispose).
- **Severity:** Low / Info
- **Category:** dead code
- **File:** `src/WebAuthn/src/Client/WebAuthnClient.cs:741, 764`
- **Description:** `previousSession` is never set inside the try-block. The dispose call in catch is a no-op. Unused variable cosmetic noise.

#### L-7. Multiple `throw new InvalidOperationException` from public-or-near-public decode paths (CoseKey, AttestationStatement, WebAuthnAttestationObject) — should be wrapped as `WebAuthnClientError(InvalidState, ...)` per the design intent.
- **Severity:** Low
- **Category:** API consistency
- **File:** `src/WebAuthn/src/Cose/CoseKey.cs:59,68,70,86,88,90,98,100,108,110`, `src/WebAuthn/src/Attestation/AttestationStatement.cs:133,204,259`, `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs:104`
- **Description:** Per requirement, "only `WebAuthnClientError`, never bare `InvalidOperationException` thrown publicly." These are reachable from `BuildRegistrationResponse` → `CoseKey.Decode` and `WebAuthnAttestationObject.Decode`.
- **Recommended fix:** Convert to `WebAuthnClientError(WebAuthnClientErrorCode.InvalidState, "...")` or wrap at the WebAuthnClient boundary.

#### L-8. Test `WebAuthnStatusStreamTests.MakeCredentialStream_NoPin_EmitsRequestingPin_AndResumesAfterSubmit` (line 88) and the drain convenience test create `PinUvAuthTokenSession` with a freshly-allocated `PinUvAuthProtocolV2` that is never disposed. Test only.
- **Severity:** Low
- **Category:** test hygiene
- **File:** `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/Status/WebAuthnStatusStreamTests.cs:99, 220`

#### L-9. `Base64Url.Decode` is publicly static but allocates two intermediate strings (`Replace` × 2 + concat for padding) on every call. Hot path: `clientDataJSON` construction is called once per operation, so impact is bounded.
- **Severity:** Info
- **Category:** perf
- **File:** `src/WebAuthn/src/Util/Base64Url.cs:45-65`

## Concerns investigated

### 1. PinAuthInvalid retry coverage
**Verdict: FAIL — see H-3.** The retry loop exists and is reachable, but it reuses the same PIN bytes without re-prompting. This will burn PIN retry attempts on the hardware on any genuine wrong-PIN scenario. Tests were deferred and would have caught it. Mark as blocking.

### 2. Stream cancellation race
**Verdict: NEEDS-DISCUSSION — see H-2.** The `Task.Run` producer's lifetime is not tied to the iterator's `DisposeAsync`. With `Channel.CreateUnbounded`, the producer cannot block on writes (mitigates the worst hang scenario), but an early consumer break still leaves the producer running with sensitive token material in memory until the producer's own `finally` zeroes it. Severity is High because a misbehaved consumer extends PIN/token lifetime; it's not "Critical" because the bytes do eventually get zeroed.

### 3. Pipeline output parsing
**Verdict: FAIL on multiple counts** — see M-1, M-2, M-3, M-4, M-5, M-6.
- CredBlob input is validated (1-32 bytes ✅) but output is not (M-2).
- PRF allow-list filter silently picks the first match (M-4).
- LargeBlob does nothing on auth side and silently empty-encodes (M-6).
- All parsers throw raw `CborContentException` on malformed input (M-1).
- All adapters read from `ParsedExtensions` without null-checking the dictionary value (covered by M-1 — the `TryGetValue` does check the key, but the value could be empty `ReadOnlyMemory<byte>`).

### 4. LoggingFactory absence
**Verdict: PASS-AS-IS / DOC-FIX** — see L-1. Confirmed: `LoggingFactory` does not exist anywhere in `src/`. The actual logging facade is `Yubico.YubiKit.Core.YubiKitLogging`. The WebAuthn module has zero log statements — silent absence is intentional (or oversight). This is **observability debt, not a security gap** (since logging PIN material would be the bigger risk). Recommend updating CLAUDE.md to fix the stale reference and either adding logging or documenting the omission.

### 5. Aaguid struct safety
**Verdict: PASS — see L-2.** Aaguid is explicitly a public identifier per WebAuthn spec; it's not sensitive. The `byte[]` storage in a struct is acceptable. Recommend a code comment to prevent reviewer confusion.

## Closing notes

- **Pattern: PIN handling is mostly clean.** All four `IMemoryOwner<byte>` rentals have paired `Dispose()` in `finally` blocks and `ZeroMemory` precedes disposal. The only concern is the tiny inconsistency in zero-region (H-1), which is cosmetic.
- **Pattern: CBOR parsing is fragile.** Five out of six adapters trust authenticator output without validation. A malicious authenticator could throw raw CBOR exceptions out of the WebAuthnClient surface (M-1, M-2, M-3). Add a single try/catch wrapper in `ExtensionPipeline.ParseRegistrationOutputs` / `ParseAuthenticationOutputs` to harden everything at once.
- **Pattern: Phase 6 deferred work is partly silent.** LargeBlob in particular has half-implementations (`ApplyToBuilder` ignores Support, `BuildAuthenticationExtensionsCbor` produces empty CBOR, auth output parser is a `return null` placeholder). These are **not test-debt — they are silent feature-gaps that ship false-positive APIs.** Either gate them behind `NotSupportedException` or document them prominently in XML doc comments as "no-op until full implementation."
- **Deferred tests classification:**
  - **Phase 3 PinAuthInvalid retry tests** — *Blocking* (would have caught H-3).
  - **Phase 6 tests 2/3/4** — *Blocking* in part (extension output parsers have multiple correctness bugs M-1..M-6).
  - **Phase 6 integration 7/8** — *Test-debt* (require hardware; the unit-level bugs are findable without them).
- **No Critical findings.** No PIN bytes stored in fields, no log-leak vectors, no `FixedTimeEquals` violations on secret data, no `ToArray()` in tight loops over sensitive data, no LINQ on byte spans (the PRF LINQ on credential-id sets is over collections of `ReadOnlyMemory<byte>`, not byte data).
- **Three concrete things the team should do before merging:**
  1. Fix H-3 (retry loop semantics) and add the deferred Phase 3 tests.
  2. Fix H-2 (link producer cancellation to iterator disposal).
  3. Add a single CBOR-error → WebAuthnClientError wrapper in ExtensionPipeline (kills M-1, M-2, M-3, L-7 in one stroke).
