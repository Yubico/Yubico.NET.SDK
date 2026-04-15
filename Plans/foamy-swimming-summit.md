# Plan: Stage 4 — Remaining Audit Fixes + All Deferred Items

## Context

After 3 stages of code audit remediation (88 fixes across 18 commits), a re-audit found 23 remaining issues. Additionally, 5 items were previously deferred as "future PRs." An Engineer analysis of each deferred item in full module/Core context determined that 6 are worth doing and 2 should be skipped. This plan addresses everything in a single stage on the existing `yubikey-codeaudit` branch.

## Triage Summary

| # | Item | Verdict | Rationale |
|---|------|---------|-----------|
| 1 | Oath CredentialData IDisposable | **DO** | Sealed class, 6 caller sites, straightforward |
| 2 | Oath DeriveKey/ValidateAsync/SetKeyAsync API | **DO (partial)** | Change params to ReadOnlyMemory\<byte\>, keep DeriveKey returning byte[] |
| 3 | Fido2 Encapsulate DRY V1/V2 | **DO** | Static helper, ~40 lines identical ECDH code |
| 4 | Fido2 CBOR construction DRY | **SKIP** | 3 different auth message formats, abstraction cost > benefit |
| 5 | PIV retry count extraction | **DO** | 5 inline sites → SWConstants.ExtractRetryCount, mechanical |
| 6 | PIV List\<byte\> → ArrayBufferWriter | **DO** | 2 sites, eliminates ToArray() copy, low risk |
| 7 | YubiOtp UpdateConfiguration duplication | **SKIP** | 11 trivial 4-line delegations, CRTP doesn't compose across 3 inheritance levels |
| 8 | SecurityDomain DI delegate firmwareVersion | **DO** | Missing parameter, quick fix |

## Per-Module Fix List

### 1. Piv (6 fixes)

**a. Security: DES inputArr not zeroed** (`PivSession.Authentication.cs:391-411`)
Add `CryptographicOperations.ZeroMemory(inputArr);` in `DesBlockOperation` finally block.

**b. Robustness: BuildAuthResponse BER length** (`PivSession.Authentication.cs:~258`)
Replace single-byte length casts with `BerLength.Write` for consistency and future-proofing.

**c. Bug: GetBioMetadataAsync raw byte parsing** (`PivSession.Bio.cs:58-66`)
Replace positional byte access (`data[0]`, `data[1]`, `data[2]`) with `TlvHelper.DecodeDictionary` matching other metadata methods. If actual wire format is uncertain, add a guarded TODO comment instead of guessing.

**d. Deferred: Retry count extraction** (`PivSession.Authentication.cs`, `PivSession.Bio.cs`)
Replace 5 inline `(response.SW & 0xFFF0) == SWConstants.VerifyFail` + `response.SW & 0x0F` patterns with `SWConstants.ExtractRetryCount(response.SW)`. Update `PivPinUtilities.GetRetriesFromStatusWord` to delegate to `SWConstants.ExtractRetryCount` for the 0x63Cx case while preserving its `0x6983` (blocked = 0) handling.

**e. Deferred: List\<byte\> → ArrayBufferWriter** (`PivSession.Certificates.cs`, `PivSession.DataObjects.cs`)
Replace `List<byte>` + `.ToArray()` with `ArrayBufferWriter<byte>` + `.WrittenSpan` in `StoreCertificateAsync` and `PutObjectAsync`. No signature changes.

### 2. Fido2 (2 fixes)

**a. Bug: ParseDecryptedBlob double SkipValue** (`LargeBlobData.cs:158-159`)
Remove the second `reader.SkipValue()` on line 159. When key is a non-empty byte string, `ReadByteString()` already consumed it — only ONE `SkipValue()` is needed for the value.

**b. Deferred DRY: Encapsulate V1/V2** (`PinUvAuthProtocolV1.cs`, `PinUvAuthProtocolV2.cs`)
Create `src/Fido2/src/Pin/PinUvAuthHelpers.cs` with:
```csharp
internal static class PinUvAuthHelpers
{
    internal static (Dictionary<int, object?> KeyAgreement, byte[] RawSharedSecret) 
        PerformEcdhKeyAgreement(IReadOnlyDictionary<int, object?> peerCoseKey)
}
```
Extracts: peer key validation, ECDH ephemeral key generation, raw secret derivation, COSE key construction. Each protocol's `Encapsulate` becomes ~5 lines: call helper, apply KDF, zero Z, return. Leave `Encrypt`/`Decrypt`/`Authenticate` alone (different IV handling/key slicing).

### 3. Oath (3 fixes)

**a. Minor: StringComparison** (`CredentialData.cs:110`)
Change `path.Contains(':')` to `path.Contains(':', StringComparison.Ordinal)`.

**b. Deferred: CredentialData IDisposable** (`CredentialData.cs`)
Make `CredentialData : IDisposable`. In `Dispose()`: `CryptographicOperations.ZeroMemory(Secret)`. Update 6 caller sites to use `using` (CLI commands, examples, tests). Also zero the `shortened` intermediate in `HmacShortenKey` if it's a newly-allocated array (different reference from input).

**c. Deferred: ValidateAsync/SetKeyAsync params** (`IOathSession.cs`, `OathSession.cs`)
Change `ValidateAsync(byte[] key, ...)` → `ValidateAsync(ReadOnlyMemory<byte> key, ...)`.
Change `SetKeyAsync(byte[] key, ...)` → `SetKeyAsync(ReadOnlyMemory<byte> key, ...)`.
Keep `DeriveKey` returning `byte[]` (over-engineering to wrap in IMemoryOwner for one callsite).
The `byte[]` → `ReadOnlyMemory<byte>` conversion is implicit, so most callers compile unchanged. Update interface + implementation. Check tests/examples for any callers that need `.AsMemory()`.

### 4. YubiOtp (2 fixes)

**a. Security: Access code truncation** (`SlotConfiguration.cs:144`)
Replace `Math.Min(accCode.Length, AccessCodeSize)` with strict validation:
```csharp
if (accCode.Length != YubiOtpConstants.AccessCodeSize)
    throw new ArgumentException($"Access code must be exactly {YubiOtpConstants.AccessCodeSize} bytes.", nameof(accCode));
```

**b. Bug: catch(Exception) swallows cancellation** (`YubiOtpSession.cs:196`)
Change to `catch (Exception ex) when (ex is not OperationCanceledException)` or add rethrow check at top of catch body.

### 5. OpenPgp (5 fixes)

**a. Missed: KdfNone.ToBytes()** (`Kdf.cs:93`)
Change `.AsMemory().ToArray()` → `.AsSpan().ToArray()`.

**b. Security: FormatEcSignPayload hash not zeroed** (`OpenPgpSession.Crypto.cs:114-119`)
The hash buffer is heap-allocated and returned. The caller (`SignDataAsync`) should zero it after use. Add zeroing in `SignDataAsync`'s finally block after the sign operation completes.

**c. Bug: KdfIterSaltedS2k.Dispose() broken chain** (`Kdf.cs:302-311`)
Add `base.Dispose();` at the end of the override.

**d. DRY: EncodeAsn1Integer duplicates AsnUtilities** (`OpenPgpSession.Crypto.cs:223-249`)
`EncodeAsn1Integer` hand-rolls leading-zero trimming, positive-padding, and single-byte DER length encoding. Core already has `AsnUtilities.GetIntegerBytes()` (in `src/Core/src/Cryptography/AsnUtilities.cs`) which does the same value preparation (trim zeros, add 0x00 pad if high bit set). Refactor `EncodeAsn1Integer` to use `AsnUtilities.GetIntegerBytes` for value prep, then wrap with tag+length. Use `BerLength.Write` for the length byte to handle the >=128 case. Also, `EncodeDerSignature` already uses `BerLength`-style encoding for the SEQUENCE — ensure consistency.

**e. Robustness: EncodeAsn1Integer length guard** (`OpenPgpSession.Crypto.cs:237`)
Handled as part of (d) — using `BerLength.Write` for the INTEGER length eliminates the single-byte assumption.

### 6. SecurityDomain (8 fixes)

**a. Bug: GetCaIdentifiersAsync bounds** (`SecurityDomainSession.cs:340`)
Change `while (!caTlvObjects.IsEmpty)` → `while (caTlvObjects.Length >= 2)`.

**b-f. Resource leaks: 5 Tlv disposal sites**
- `StoreAllowListAsync`: wrap `new Tlv(TagSerial, ...)` in `using` per iteration
- `StoreCaIssuerAsync`: use `using var` for nested Tlv objects
- `StoreCertificatesAsync`: use `using var` for inner Tlv before EncodeList
- `GetCertificatesAsync`: use `using var` for request Tlv
- Any other nested TLV construction sites found

**g. Bug: PutKeyAsync(ECPrivateKey) mutates caller's key** (`SecurityDomainSession.cs:638`)
**Remove** `CryptographicOperations.ZeroMemory(parameters.D);`. The `ECPrivateKey` deep-copies `D` in its constructor, so `parameters.D` is the *caller's* internal copy. Zeroing it destroys the caller's key as a side-effect. The caller should call `ECPrivateKey.Clear()` when they're done with the key. Add a comment explaining why zeroing is NOT done here.

**h. DI delegate missing firmwareVersion** (`DependencyInjection.cs`)
Add `FirmwareVersion? firmwareVersion = null` parameter to the `SecurityDomainSessionFactory` delegate and pass it through to `CreateAsync`.

## Execution Strategy

Use `/DevTeam Ship` — dispatch 6 parallel DevTeam Engineer agents (one per module, skip YubiHsm). Each agent:
1. Reads files before editing
2. Implements all fixes for their module
3. Verifies the module builds (including tests)
4. Does NOT commit

After all agents complete:
1. `dotnet build Yubico.YubiKit.sln` — 0 errors
2. `dotnet build.cs test` — 8/9 pass (Fido2 pre-existing)
3. Create per-module commits on `yubikey-codeaudit`
4. `git push` and update PR #455 description

## Items Explicitly Skipped

- **Fido2 CBOR construction DRY**: 3 different auth message formats, abstraction cost exceeds benefit
- **YubiOtp UpdateConfiguration duplication**: 11 trivial 4-line methods, CRTP doesn't compose across 3 inheritance levels
- **YubiOtp PadHmacChallenge ArrayPool**: 64-byte allocation, not a hot path

## Verification

1. `dotnet build Yubico.YubiKit.sln` — 0 errors, 0 warnings
2. `dotnet build.cs test` — 8/9 pass
3. LSP `find_usages` on `PinUvAuthHelpers.PerformEcdhKeyAgreement` (should have 2 usages)
4. LSP `find_usages` on `CredentialData.Dispose` (should have 6+ usages)
5. Manual integration tests (user runs):
   - PIV: mutual auth (retry count), certificate store (ArrayBufferWriter)
   - Fido2: large blob write/read (CBOR fix), PIN operations (Encapsulate)
   - Oath: credential CRUD with IDisposable, validate/setKey with new param types
   - SecurityDomain: EC private key import (no ZeroMemory), SCP11 allowlist
   - OpenPgp: P-521 signing (DER guard), PIN verify

## Critical Files

| Module | Files |
|--------|-------|
| Piv | `PivSession.Authentication.cs`, `PivSession.Bio.cs`, `PivSession.Certificates.cs`, `PivSession.DataObjects.cs`, `PivPinUtilities.cs` |
| Fido2 | `LargeBlobData.cs`, `PinUvAuthProtocolV1.cs`, `PinUvAuthProtocolV2.cs`, NEW: `PinUvAuthHelpers.cs` |
| Oath | `CredentialData.cs`, `IOathSession.cs`, `OathSession.cs`, CLI/test callers |
| YubiOtp | `SlotConfiguration.cs`, `YubiOtpSession.cs` |
| OpenPgp | `Kdf.cs`, `OpenPgpSession.Crypto.cs` |
| SecurityDomain | `SecurityDomainSession.cs`, `DependencyInjection.cs` |
