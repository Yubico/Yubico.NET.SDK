# PRD — ARKG `additional_args` Typed Builder for previewSign Authentication

**Author:** Architect (peer review with Sia)
**Created:** 2026-04-28
**Status:** Draft for Dennis review
**Phase:** 10, item §3 (`Plans/phase-10-previewsign-auth.md:37-52`)
**Branch (recommended):** **fresh** — `webauthn/phase-10-arkg-sign-args` off `webauthn/phase-9.2-rust-port` (justification §9)

---

## 1. Goal & Done Means

**Goal (1 sentence):** Replace the opaque `ReadOnlyMemory<byte>?` `AdditionalArgs` field on `PreviewSignSigningParams` with a typed, layered, Fido2-canonical encoder for the ARKG `COSE_Sign_Args` map so that callers can construct hardware-valid previewSign authentication requests without writing CBOR by hand.

**Done means (binary verifiable):**
1. `Yubico.YubiKit.Fido2.Extensions.PreviewSignCbor.EncodeArkgSignArgs(...)` exists, takes typed inputs, and emits the exact 3-key CBOR map `{3: -65539, -1: bstr, -2: bstr}` per `LEGACY_PREVIEWSIGN_FORENSICS.md §3.4`.
2. A deterministic byte-level unit test in `PreviewSignCborTests.cs` asserts the encoder produces the same bytes the existing `EncodeAuthenticationInput_WithAdditionalArgs_MatchesRustThreeKeyStructure` test currently hand-builds (`src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/PreviewSignCborTests.cs:69-115`) — i.e. the test stops hand-building and consumes the new API instead.
3. `PreviewSignSigningParams` (both Fido2 and WebAuthn layers) accepts a typed `CoseSignArgs` value and the WebAuthn layer delegates encoding to Fido2 with **zero** local CBOR (`src/WebAuthn/CLAUDE.md:` "duplicate ZERO Fido2 behavior" + repo `MEMORY.md` "WebAuthn must duplicate zero Fido2 behavior").
4. The integration test `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` (`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:84-102`) is **un-skipped** and passes on YK 5.8.0-beta hardware after a touch.
5. `dotnet toolchain.cs build` clean; `dotnet toolchain.cs test --project Fido2` green; `dotnet toolchain.cs test --project WebAuthn` green; `dotnet format` clean.

**Explicit non-goal of "done":** The crypto that *produces* `arkg_kh` and `ctx` (i.e. ARKG public-key derivation) is **out of scope** — see §8.

---

## 2. Public API Design

### 2.1 New Fido2 type — `CoseSignArgs`

The COSE_Sign_Args map is a generic CTAP v4 concept (key 3 = alg, plus algorithm-specific data). Today the only inhabitant is ARKG. Model it as an abstract base + sealed leaf so the next inhabitant slots in cleanly without breaking source.

**File:** `src/Fido2/src/Extensions/CoseSignArgs.cs` (new)
**Namespace:** `Yubico.YubiKit.Fido2.Extensions`

```csharp
namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Typed COSE_Sign_Args (CTAP v4, value of key 7 of a previewSign authentication request).
/// </summary>
/// <remarks>
/// COSE_Sign_Args is a CBOR map whose key 3 carries the request algorithm identifier; the
/// remaining keys are algorithm-specific. Today the only inhabitant on YubiKey is
/// <see cref="ArkgP256SignArgs"/> (alg = -65539). New algorithms add new sealed subtypes.
/// </remarks>
public abstract record class CoseSignArgs
{
    private protected CoseSignArgs() { }

    /// <summary>The COSE algorithm identifier written under key 3 of the COSE_Sign_Args map.</summary>
    public abstract int Algorithm { get; }
}

/// <summary>
/// COSE_Sign_Args for ARKG-P256-ESP256 (alg = -65539). Wire shape: {3: -65539, -1: kh, -2: ctx}.
/// </summary>
/// <remarks>
/// <para>
/// <c>KeyHandle</c> is the 81-byte ARKG ciphertext (16-byte HMAC tag || 65-byte SEC1 ephemeral
/// public key) returned by ARKG public-key derivation; <c>Context</c> is the ≤64-byte HKDF
/// context bound to the derivation.
/// </para>
/// <para>
/// Both fields are <see cref="ReadOnlyMemory{Byte}"/> passthroughs — the encoder reads them at
/// CBOR-write time and never copies. The caller owns the buffers and is responsible for zeroing
/// after the request is on the wire.
/// </para>
/// </remarks>
public sealed record class ArkgP256SignArgs : CoseSignArgs
{
    /// <summary>Algorithm identifier on the wire — fixed at -65539 (ESP256_SPLIT_ARKG_PLACEHOLDER).</summary>
    public override int Algorithm => CoseAlgorithm.Esp256SplitArkgPlaceholder.Value;

    /// <summary>The 81-byte ARKG key handle (cipher = 16-byte HMAC tag || 65-byte SEC1 ephemeral pubkey).</summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>The ARKG context (≤64 bytes) bound to the derivation.</summary>
    public ReadOnlyMemory<byte> Context { get; }

    public ArkgP256SignArgs(ReadOnlyMemory<byte> keyHandle, ReadOnlyMemory<byte> context)
    {
        if (keyHandle.Length == 0)
        {
            throw new ArgumentException("ARKG key handle must not be empty.", nameof(keyHandle));
        }
        // 81-byte fixed shape per LEGACY_PREVIEWSIGN_FORENSICS.md §2.7. Hard-validate to fail fast
        // on accidental concatenations / hex-decoded mistakes.
        if (keyHandle.Length != 81)
        {
            throw new ArgumentException(
                $"ARKG-P256 key handle must be exactly 81 bytes (16-byte tag || 65-byte SEC1 pubkey); got {keyHandle.Length}.",
                nameof(keyHandle));
        }
        if (context.Length > 64)
        {
            throw new ArgumentException(
                $"ARKG context must be ≤64 bytes per HKDF length-byte prefix encoding; got {context.Length}.",
                nameof(context));
        }

        KeyHandle = keyHandle;
        Context = context;
    }
}
```

**Why a record-with-init-validating-ctor instead of a fluent builder?** ARKG_P256 has only two payload fields (`kh`, `ctx`) and one fixed alg constant. A fluent builder buys nothing here and adds API surface to maintain. If a future algorithm has 4+ fields, *that* algorithm gets its own sealed type and may use a builder; we don't pre-pay complexity.

**Why abstract base + sealed?** Lets `PreviewSignSigningParams` accept the union without exposing `object?` and without forcing us to ship a `Type` discriminator. Pattern matching at the encoder is exhaustive and the compiler enforces it (§2.4).

### 2.2 New Fido2 encoder — `PreviewSignCbor.EncodeCoseSignArgs`

**File:** `src/Fido2/src/Extensions/PreviewSignExtension.cs` (extend existing static class)
**Namespace:** `Yubico.YubiKit.Fido2.Extensions` (existing)

```csharp
public static class PreviewSignCbor
{
    // ...existing keys, EncodeRegistrationInput, EncodeAuthenticationInput, etc...

    /// <summary>
    /// CBOR keys inside a COSE_Sign_Args map.
    /// </summary>
    private static class CoseSignArgsKeys
    {
        internal const int Algorithm = 3;
        internal const int ArkgKeyHandle = -1;
        internal const int ArkgContext = -2;
    }

    /// <summary>
    /// Encodes a typed <see cref="CoseSignArgs"/> as CTAP2-canonical CBOR. The returned bytes
    /// are the inner payload of authentication input key 7 (the outer encoder still wraps them
    /// as a CBOR byte-string).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the runtime <see cref="CoseSignArgs"/> subtype is not supported by this SDK
    /// build (forward-compat trap — caller has constructed a future algorithm we don't encode).
    /// </exception>
    public static byte[] EncodeCoseSignArgs(CoseSignArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args switch
        {
            ArkgP256SignArgs arkg => EncodeArkgP256SignArgs(arkg),
            _ => throw new ArgumentOutOfRangeException(
                nameof(args),
                $"COSE_Sign_Args subtype '{args.GetType().FullName}' is not supported by this SDK build."),
        };
    }

    private static byte[] EncodeArkgP256SignArgs(ArkgP256SignArgs arkg)
    {
        // Wire shape per LEGACY_PREVIEWSIGN_FORENSICS.md §3.4:
        //   A3 03 3A0001_0002 20 58 51 ...kh(81)... 21 58 LL ...ctx...
        // CTAP2 canonical orders integer keys by ascending unsigned encoding, which means
        // 3 (positive) precedes -1 and -2 (negative) — matches the byte map above.
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);

        writer.WriteInt32(CoseSignArgsKeys.Algorithm);
        writer.WriteInt32(arkg.Algorithm);

        writer.WriteInt32(CoseSignArgsKeys.ArkgKeyHandle);
        writer.WriteByteString(arkg.KeyHandle.Span);

        writer.WriteInt32(CoseSignArgsKeys.ArkgContext);
        writer.WriteByteString(arkg.Context.Span);

        writer.WriteEndMap();
        return writer.Encode();
    }
}
```

### 2.3 Migration of `PreviewSignSigningParams` (Fido2 layer)

Replace the raw `ReadOnlyMemory<byte>? AdditionalArgs` field with a typed `CoseSignArgs? CoseSignArgs` field. See §6 for the breaking-change justification.

**File:** `src/Fido2/src/Extensions/PreviewSignExtension.cs:120-162` (replace existing class)

```csharp
public sealed class PreviewSignSigningParams
{
    public ReadOnlyMemory<byte> KeyHandle { get; init; }
    public ReadOnlyMemory<byte> Tbs { get; init; }

    /// <summary>
    /// Optional typed COSE_Sign_Args. When present, the encoder emits canonical CBOR under
    /// authentication input key 7 (wrapped as bstr). Required for ARKG algorithms.
    /// </summary>
    public CoseSignArgs? CoseSignArgs { get; init; }

    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        CoseSignArgs? coseSignArgs = null)
    {
        if (keyHandle.Length == 0) throw new ArgumentException(...);
        if (tbs.Length == 0)        throw new ArgumentException(...);

        KeyHandle = keyHandle;
        Tbs = tbs;
        CoseSignArgs = coseSignArgs;
    }
}
```

`PreviewSignCbor.EncodeAuthenticationInput` then changes its key-7 branch from:

```csharp
// before
if (signingParams.AdditionalArgs.HasValue)
{
    writer.WriteInt32(AuthenticationInputKeys.AdditionalArgs);
    writer.WriteByteString(signingParams.AdditionalArgs.Value.Span);
}
```

to:

```csharp
// after
if (signingParams.CoseSignArgs is not null)
{
    writer.WriteInt32(AuthenticationInputKeys.AdditionalArgs);
    writer.WriteByteString(EncodeCoseSignArgs(signingParams.CoseSignArgs));
}
```

The outer-bstr-wrap-of-inner-CBOR contract (forensics §3.3) is preserved.

### 2.4 WebAuthn layer — pure delegation, zero CBOR

**File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` (rewrite — current shape at the file shown above)
**Namespace:** `Yubico.YubiKit.WebAuthn.Extensions.PreviewSign`

```csharp
public sealed record class PreviewSignSigningParams
{
    public ReadOnlyMemory<byte> KeyHandle { get; }
    public ReadOnlyMemory<byte> Tbs { get; }

    /// <summary>
    /// Typed COSE_Sign_Args. WebAuthn re-exports the Fido2 type rather than wrapping it —
    /// the no-duplication invariant requires that there be exactly one canonical encoder.
    /// </summary>
    public Yubico.YubiKit.Fido2.Extensions.CoseSignArgs? CoseSignArgs { get; }

    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        Yubico.YubiKit.Fido2.Extensions.CoseSignArgs? coseSignArgs = null)
    {
        if (keyHandle.Length == 0)
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidRequest, "previewSign KeyHandle must not be empty");
        if (tbs.Length == 0)
            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidRequest, "previewSign Tbs must not be empty");

        KeyHandle = keyHandle;
        Tbs = tbs;
        CoseSignArgs = coseSignArgs;
    }
}
```

The `PreviewSignAdapter` (`src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs`) translates this WebAuthn-layer params → the Fido2-layer params by passing `CoseSignArgs` through unchanged. **No CBOR is built in WebAuthn.** This is the no-duplication invariant on the wire (`MEMORY.md` "WebAuthn must duplicate zero Fido2 behavior").

The previously-removed CBOR validation block in WebAuthn's ctor (`PreviewSignSigningParams.cs:84-98`) is no longer needed — the type system enforces "valid CBOR shape" at compile time. Net code reduction.

### 2.5 Convenience static factory (optional, recommended)

```csharp
// In Yubico.YubiKit.Fido2.Extensions.CoseSignArgs
public static CoseSignArgs ArkgP256(ReadOnlyMemory<byte> keyHandle, ReadOnlyMemory<byte> context)
    => new ArkgP256SignArgs(keyHandle, context);
```

Lets callers write `CoseSignArgs.ArkgP256(kh, ctx)` without naming the leaf type. Cosmetic but DX-positive.

---

## 3. Validation Rules

| Field | Rule | Exception | Justification |
|---|---|---|---|
| `ArkgP256SignArgs.KeyHandle` | non-null, length **exactly 81** | `ArgumentException` | Forensics §2.7 — fixed 16+65. Wrong length = guaranteed firmware reject; fail at construct time, not on the wire. |
| `ArkgP256SignArgs.Context` | non-null, length **≤ 64** | `ArgumentException` | Forensics §3.6 — single length-byte prefix bounds context to 64. |
| `PreviewSignSigningParams.KeyHandle` (FIDO2 credentialId) | non-empty | `ArgumentException` (Fido2) / `WebAuthnClientError(InvalidRequest)` (WebAuthn) | Existing rule, preserved. |
| `PreviewSignSigningParams.Tbs` | non-empty | same | Existing rule, preserved. |
| `PreviewSignCbor.EncodeCoseSignArgs(args)` | `args` not null; subtype must be in the switch | `ArgumentNullException` / `ArgumentOutOfRangeException` | Forward-compat trap so a future algorithm subtype added to a newer base library can't silently no-op encode. |

**Device-side error mapping:** When firmware *does* reject an `additional_args` payload (e.g. caller bypassed the typed API by reflection, or future firmware tightens validation), CTAP returns `CTAP2_ERR_INVALID_OPTION` (0x2C) or `CTAP2_ERR_EXTENSION_NOT_SUPPORTED`. These flow through the existing `PreviewSignErrors.MapCtapError` (`src/WebAuthn/src/Extensions/PreviewSign/PreviewSignErrors.cs`) — confirm the mapping covers both codes; if not, extend the map and add a unit test. **No new error codes needed.**

---

## 4. CBOR Wire Format — Final Sample

`additional_args` for an ARKG-P256 sign request, with a 32-byte zero-context placeholder for illustration:

```
A3                                    # map(3)
  03                                  #   key  3 (alg)
  3A 0001 0002                        #   val -65539 (ESP256_SPLIT_ARKG_PLACEHOLDER)
  20                                  #   key -1 (arkg_kh)
  58 51                               #     bstr len 81
    XX XX … XX                        #     [16-byte HMAC tag]
    04 XX XX … XX                     #     [65-byte SEC1 ephemeral pubkey, leading 0x04]
  21                                  #   key -2 (ctx)
  58 20                               #     bstr len 32
    00 00 00 00 00 00 00 00           #     [32 zero bytes]
    00 00 00 00 00 00 00 00
    00 00 00 00 00 00 00 00
    00 00 00 00 00 00 00 00
```

Total: 3 + 5 + 2 + 81 + 2 + 32 = **125 bytes**. Then `EncodeAuthenticationInput` wraps these 125 bytes as `bstr` under outer key 7: `07 58 7D <125 bytes>`. CTAP2 canonical sort places `3` before `-1` and `-2` because positive ints sort before negative ints under canonical encoding (per RFC 8949 §4.2.1 / CTAP2 canonical) — matches forensics §3.4 byte-for-byte.

---

## 5. Testing Strategy

### 5.1 Deterministic byte-level unit test (Fido2)

Convert the existing `EncodeAuthenticationInput_WithAdditionalArgs_MatchesRustThreeKeyStructure` (`src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/PreviewSignCborTests.cs:69-115`) to consume the new typed API:

```csharp
[Fact]
public void EncodeCoseSignArgs_ArkgP256_MatchesForensicsByteMap()
{
    // 81-byte fixture key handle (16-byte tag + 65-byte SEC1)
    var kh = new byte[81];
    for (int i = 0; i < 16; i++) kh[i] = (byte)i;        // tag
    kh[16] = 0x04;                                        // SEC1 leading byte
    // 32-byte zero context
    var ctx = new byte[32];

    byte[] actual = PreviewSignCbor.EncodeCoseSignArgs(
        new ArkgP256SignArgs(kh, ctx));

    // Hand-build the expected bytes from forensics §3.4
    byte[] expected = [
        0xA3,
        0x03, 0x3A, 0x00, 0x01, 0x00, 0x02,
        0x20, 0x58, 0x51, /* 81 kh bytes */,
        0x21, 0x58, 0x20, /* 32 ctx bytes */
    ];
    // ...assemble fully...
    Assert.Equal(expected, actual);
}

[Theory]
[InlineData(0)]    // empty
[InlineData(80)]   // off-by-one short
[InlineData(82)]   // off-by-one long
public void ArkgP256SignArgs_RejectsWrongKeyHandleLength(int len)
    => Assert.Throws<ArgumentException>(() => new ArkgP256SignArgs(new byte[len], ReadOnlyMemory<byte>.Empty));

[Fact]
public void ArkgP256SignArgs_Rejects65ByteContext()
    => Assert.Throws<ArgumentException>(() => new ArkgP256SignArgs(new byte[81], new byte[65]));

[Fact]
public void EncodeCoseSignArgs_NullArgs_Throws()
    => Assert.Throws<ArgumentNullException>(() => PreviewSignCbor.EncodeCoseSignArgs(null!));
```

Keep the existing structure-level test (`EncodeAuthenticationInput_WithAdditionalArgs_MatchesRustThreeKeyStructure`) but rewrite its `additional_args` construction to use `EncodeCoseSignArgs` rather than hand-rolling `argWriter`. This proves the integration point.

### 5.2 Integration test (WebAuthn, hardware)

`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:84-102` — un-skip and rewrite the body. With the typed API and an ARKG derivation helper (out of scope, see §8), the assertion list becomes:

1. Register with `Algorithms = [-65539]` → receive `keyHandle`, `publicKey`, `algorithm == -65539`, `attestationObject`.
2. *(Out of scope for this PRD — assumed available via §8 follow-up)* derive `(arkg_kh, ctx)` from the registration's COSE key.
3. `GetAssertion` with `signByCredential[credentialId] = new(keyHandle: credId, tbs: messageHash, coseSignArgs: new ArkgP256SignArgs(arkg_kh, ctx))` and `AllowCredentials = [credId]` (forensics §1.3 step 2 — required precondition).
4. Assert: `assertion.Signature` non-null, non-empty, **DER-shaped** (`0x30 0x?? 0x02 …`), length in [70, 72] (typical ECDSA-P256 DER).
5. *(Optional, follow-up)* signature verifies against the derived public key.

The skipped block at `PreviewSignTests.cs:91-99` documents the precise ARKG dependency this PRD removes; the un-skip + body rewrite is the literal "Done means" #4 above.

### 5.3 What this PRD does **not** ship a test for

- CBOR fuzzing of `EncodeCoseSignArgs` — overkill; the encoder is 6 lines.
- Round-trip decode of `CoseSignArgs` — the SDK only encodes; firmware never sends it back.

---

## 6. Migration & Backwards Compat

**Decision: replace, don't overload.** `AdditionalArgs (ReadOnlyMemory<byte>?)` becomes `CoseSignArgs (CoseSignArgs?)`. Breaking change. Justification:

1. The codebase is preview-stage (literally `previewSign`, branch `webauthn/phase-9.2-rust-port`, no shipped 2.0 GA).
2. The existing `ReadOnlyMemory<byte>?` field has **zero** known consumers outside the CBOR unit test (`PreviewSignCborTests.cs:69-115`) and the integration test (`PreviewSignTests.cs:84-102`, currently skipped). I grepped — both are inside this repo.
3. Keeping both fields creates a "two ways to do it" trap where the next porter can still pass raw bytes with `-9` and reproduce the exact bug Dennis just fixed. The whole point of the typed API is to **make the bug unrepresentable**. Leaving the escape hatch defeats it.
4. WebAuthn ctor body shrinks (the CBOR-validity check at `PreviewSignSigningParams.cs:84-98` becomes unreachable / deletable).

**Migration story for any external preview consumer (none known):** swap `additionalArgs: someBytes` → `coseSignArgs: new ArkgP256SignArgs(kh, ctx)`. Mention in `Plans/phase-10-previewsign-auth.md` and the WebAuthn `CLAUDE.md` "Known Gotchas" section under a new "Phase 10 breaking changes" subhead.

If Dennis disagrees with the breakage call: the additive-overload alternative is to keep `AdditionalArgs` and add `CoseSignArgs`, with a ctor-time XOR check (exactly one of the two non-null). Document the trade-off and don't ship both — the trap is real.

---

## 7. Risks & Open Questions

> **DECISIONS LOCKED 2026-04-28 (Dennis):**
> 1. **Alias to stable** — add `CoseAlgorithm.ArkgP256` constant aliasing `Esp256SplitArkgPlaceholder`'s `-65539` value; use the alias on `ArkgP256SignArgs.Algorithm`
> 2. **Keep `CoseSignArgs`** — name matches wire spec
> 3. **Separately** — multi-cred probe stays Phase 10 §1, not this PRD
> 4. **Pass** — sig verification helper out of scope
> 5. **Closed union** — `private protected` ctor, locked to assembly
> 6. **Passthrough + XML doc** — `ReadOnlyMemory<byte>` no-clone, caller zeros, documented
> 7. **Fixtures grounded in python-fido2** — verified: KH = 81 bytes (16-byte HMAC tag `t` ‖ 65-byte uncompressed P256 point `c'`); CTX = ≤64 bytes, typical 22-24 bytes (e.g. `b"ARKG-P256.test vectors"`); `tbs` is **prehashed SHA-256** client-side (32 bytes); deterministic vectors live at `python-fido2/tests/test_arkg.py:36-73` — mirror these for C# encoder unit tests
> A. **Replace, breaking** — `AdditionalArgs (ReadOnlyMemory<byte>?)` → `CoseSignArgs (CoseSignArgs?)`
> B. **Stay on `webauthn/phase-9.2-rust-port`** — overrides handoff's `yubikit-applets` recommendation; not ready for `yubikit-applets` yet

> **CRITICAL CONSTANT NOTE (do not confuse):** python-fido2 has TWO `_PLACEHOLDER` constants in `fido2/cose.py`:
> - `ESP256_SPLIT_ARKG_PLACEHOLDER = -65539` — the **signing operation** alg ID, used at COSE_Sign_Args key 3 (THIS is what goes on the wire as the request alg). ✓ matches our shipped fix and Legacy `fe82b007`.
> - `ARKG_P256_PLACEHOLDER.ALGORITHM = -65700` — the **derived seed-key COSE key** alg ID (different layer; not what we send at sign-args.alg).
> Engineer must use **`-65539`** for `ArkgP256SignArgs.Algorithm`'s wire value.


1. **Naming of the placeholder algorithm.** The Cose enum has `Esp256SplitArkgPlaceholder` (`src/Fido2/src/Cose/CoseAlgorithm.cs:56`). "Placeholder" implies temporary; if Yubico publishes a final ARKG-P256 alg ID we'll have a rename. **Decision needed:** keep the placeholder name on `ArkgP256SignArgs.Algorithm`'s wire value, or alias it to a stable `Cose.Algorithm.ArkgP256` constant. *Recommend: keep the existing constant; add an XML doc cross-reference.*

2. **`CoseSignArgs` vs `Fido2SignArgs` naming.** "COSE_Sign_Args" is the spec term but the type lives in `Fido2.Extensions`. Risk of collision with general COSE library types in `src/Fido2/src/Cose/`. *Recommend: keep `CoseSignArgs` — it matches the spec name on the wire and the namespace disambiguates.*

3. **Multi-credential probe (Phase 10 §1).** This PRD is single-credential only. The `signByCredential` dictionary still maps `credId → PreviewSignSigningParams`, so the multi-credential probe API can be layered on later without churning `CoseSignArgs`. **No blocker.**

4. **Signature verification helper (Phase 10 §2).** Out of scope here. But: the typed `ArkgP256SignArgs.KeyHandle` and `Context` make it natural to later add `PreviewSignDerivedKey.Verify(message, signature)` that takes the same fields. **Design lock-in concern is low.**

5. **Should `CoseSignArgs` be a closed union enforced at compile time?** C# 14 has no first-class discriminated unions; `abstract record + sealed leaves + private protected ctor` gets us 90% there. The remaining 10% (downstream cannot add a new leaf in their assembly) is solved by `private protected`. **Risk: a future Yubico-internal algorithm in a different assembly cannot extend.** Acceptable — when that day comes, change `private protected` to `internal` and add a friend-assembly attribute, or move the base into `Yubico.YubiKit.Fido2.Cose`. Explicit Dennis decision: scope of subclassability.

6. **`ReadOnlyMemory<byte>` ownership of `KeyHandle` / `Context`.** Per repo `CLAUDE.md` (Security section), `ReadOnlyMemory<byte>` passthrough in a `readonly record struct` is safe — the caller owns and zeros. `ArkgP256SignArgs` is a `record class` (heap-allocated reference) but the slot is still a passthrough; **no internal clone**. Caller still owns the underlying buffer and is responsible for zeroing after the request lands on the wire. **Document this in the type's XML doc** (already drafted above).

7. **Test fixture realism.** The 81-byte zero-pattern KH and 32-byte zero-CTX in §5.1 are *byte-level* fixtures, not crypto-valid. That's fine for an encoder test. Hardware integration test (§5.2) uses the real ARKG output. **No issue.**

---

## 8. Out of Scope

This PRD does **not** cover:

- **ARKG public-key derivation** (Yubico.Core port of `ArkgPrimitivesOpenSsl.cs:130` — the OpenSSL-backed P-256 ECDH + HKDF + RFC 9380 expand_message_xmd implementation, ~568 lines in legacy). That is a Yubico.Core deliverable and gates the *production* of `(arkg_kh, ctx)`. This PRD only covers their *encoding* once produced.
- **Multi-credential probe** (Phase 10 §1, `Plans/phase-10-previewsign-auth.md:16-27`).
- **Signature verification helper** (Phase 10 §2). The hardware integration test will assert presence/shape only; cryptographic verify lands separately.
- **CTAP error-code expansion** beyond confirming `PreviewSignErrors.MapCtapError` already covers `CTAP2_ERR_INVALID_OPTION` and `CTAP2_ERR_EXTENSION_NOT_SUPPORTED`.
- **Renaming** `Esp256SplitArkgPlaceholder` (open question §7-1, deferred to a separate cleanup commit if/when Yubico finalises the alg ID).

---

## 9. Recommended Branch Strategy

**Recommendation: fresh branch `webauthn/phase-10-arkg-sign-args` off the current `webauthn/phase-9.2-rust-port`** — *not* off `yubikit-applets`.

**Justification (against the handoff guidance that Phase 10 lives on a fresh branch off `yubikit-applets`):**

The handoff is generally correct — Phase 10 wants its own branch — but the *base* should be `webauthn/phase-9.2-rust-port`, not `yubikit-applets`, for three concrete reasons:

1. **The fix it depends on is on this branch, not `yubikit-applets`.** Today's previewSign+ARKG registration fix on YK 5.8.0-beta (alg `-65539` not `-9`) is part of the Phase 9.2 work in progress here. Branching off `yubikit-applets` would require cherry-picking that fix forward, which is more risk than it removes.
2. **The encoder integration tests already assume the Phase 9.2 wire-format encoder shape** (`PreviewSignCborTests.cs:69-115`). Re-basing onto `yubikit-applets` would force re-validating those byte fixtures against an older encoder.
3. **The `Plans/phase-10-previewsign-auth.md:52` "Path B candidate" line literally already names this:** *"do this work in a separate branch off `webauthn/phase-9.2-rust-port` so the encoder ship from Phase 9.2 is not blocked"*. Author of that plan agreed at write time — I concur on re-read.

When Phase 9.2 merges into `yubikit-applets`, this branch rebases cleanly (or merges via PR) — there's no conflict surface because the Phase 10 work only adds new types and modifies one ctor signature.

**If Dennis prefers strict adherence to handoff (`yubikit-applets` base):** acceptable cost is one cherry-pick of the alg-`-65539` fix commit + re-running the encoder fixture tests to confirm. ~30 min of work, no new risk. Either base is defensible; I recommend the simpler one.

---

## Appendix — File touch list

**New:**
- `src/Fido2/src/Extensions/CoseSignArgs.cs` (new file, ~75 lines including XML doc)

**Modified:**
- `src/Fido2/src/Extensions/PreviewSignExtension.cs` — add `EncodeCoseSignArgs`, change `PreviewSignSigningParams.AdditionalArgs` to `CoseSignArgs`, update `EncodeAuthenticationInput` key-7 branch
- `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/PreviewSignCborTests.cs` — convert hand-built test (line 69-115), add length-validation tests
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` — replace `AdditionalArgs` with `CoseSignArgs`, drop the runtime CBOR-validity check (lines 84-98)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs` — pass `CoseSignArgs` through to Fido2 layer (no CBOR built here)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:84-102` — un-skip `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`, rewrite body using typed API

**Documentation:**
- `Plans/phase-10-previewsign-auth.md` — update §3 status to "shipping", point at this PRD
- `src/WebAuthn/CLAUDE.md` — add Phase 10 entry under "Future Work" referencing the typed API and the no-duplication invariant

---

**End of PRD.**
