# #16 Large-blob plaintext is not cleared from memory (YESDK-1624)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction |
| Our severity | LOW-MED. The exposure needs local memory access (a heap or crash dump). It is not remote. |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | code inspection. The unit probe aims at the wrong buffer. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/Fido2/src/LargeBlobs/`. The root `CLAUDE.md` changed in other places; the zeroing rule at lines 59-60 is unchanged. |

## What the audit says

The audit says decrypted and encrypted large-blob plaintext stays in managed memory after use, because the SDK never clears those buffers.

> None of the three arrays is zeroed before becoming unreachable.

## What is right

- The decrypted plaintext is not cleared. `TryDecrypt` allocates the buffer, decrypts into it, parses it, and returns without clearing it ([LargeBlobData.cs#L74-L85](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L74-L85); allocation at line 77, parse at line 81).
- The parser makes another copy with `plaintext.ToArray()` ([LargeBlobData.cs#L136-L140](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L136-L140), copy at line 140). The extracted value is a further array from `ReadByteString` (line 154).
- `SetBlobAsync` and `DeleteBlobAsync` call `TryDecrypt` only to test for null. They discard the plaintext of a matching entry without clearing it ([LargeBlobStorage.cs#L199-L208](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L199-L208), call at line 203; [#L240-L255](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L240-L255), call at line 246).
- Encryption leaves its CBOR plaintext uncleared. `CreatePlaintext` builds it, `Encrypt` encrypts it, and nothing clears it ([LargeBlobData.cs#L95-L113](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L95-L113), call at line 103).
- The repository rule applies. CLAUDE.md says "ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`", and it treats decrypted plaintext as sensitive ([CLAUDE.md, lines 59-60](../../../CLAUDE.md)). The module guide has the same rule ([src/Fido2/CLAUDE.md#L326-L327](../../../src/Fido2/CLAUDE.md#L326-L327)).

## What is wrong or imprecise

- The probe aims at the wrong buffer. `YESDK1624_DecryptedTemporaryPlaintextIsCleared` calls the private `ParseDecryptedBlob` with a test-owned array, and checks that the array is zeroed ([Fido2LargeBlobAuditReproTests.cs#L146-L158](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L146-L158)). The buffer that needs clearing is the one `TryDecrypt` allocates (line 77). A correct fix would still fail this probe, and #13 removes the probed method. Review found this, so the probe is not evidence for the defect. The evidence is code inspection.
- The audit's wording goes beyond what we showed. "An attacker or crash-dump collector can later read stale managed-heap memory" is plausible, but no test reads heap memory. We verified only the code paths.
- Severity. The audit rates it MED. The exposure needs local memory access and sensitive content, so we rate it LOW-MED.
- Some copies are inside the base class library (BCL) CBOR reader and writer. The SDK cannot clear those.
- The plaintext returned by `GetBlobAsync` belongs to the caller. The SDK cannot clear it before returning, so the caller must. That should be documented.

## Why it matters

- Who: any app that stores sensitive data in a large blob, on a machine where process memory can be read locally (crash dumps, debuggers, or local malware).
- Preconditions: local memory access, sensitive blob content, and buffers that are not cleared or reused.
- Effect: decrypted blob content can stay readable after use, until the garbage collector (GC) reuses the memory. This is local only. This finding concerns blob content. We did not assess the largeBlobKey.

## Specification

No Client to Authenticator Protocol (CTAP) or WebAuthn clause governs this. The governing contract is the repository rule in CLAUDE.md:

> ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`

> Classify byte data by semantic meaning, not by direction. ... YubiKey-returned authentication material, token material, decrypted plaintext, or secret-derived output does.

Source: [CLAUDE.md](../../../CLAUDE.md), lines 59-60, Security section. The ellipsis in the second quote omits examples of device metadata that need no special zeroing.

No version table applies.

## Canonical Python reference

Python has no equivalent. Python `bytes` are immutable and cannot be cleared in place, so python-fido2 has no zeroing model to compare with. It returns the decrypted bytes as a new object ([blob.py#L77-L79](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L77-L79)).

```python
        aesgcm = AESGCM(key)
        compressed = aesgcm.decrypt(nonce, ciphertext, _lb_ad(orig_size))
        return compressed, orig_size
```

## Sibling SDKs (context only)

Not checked in this pass.

## Reproduction

- Unit probe: [Fido2LargeBlobAuditReproTests.YESDK1624_DecryptedTemporaryPlaintextIsCleared](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L146-L158). Today it fails: 6 of 6 bytes of the test buffer remain nonzero. The probe targets the wrong buffer (see above), so its failure does not prove the defect.
- Code inspection points: decrypt buffer at [LargeBlobData.cs#L77](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L77), copy at [#L140](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L140), encrypt plaintext at [#L103](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L103), and discarded results at [LargeBlobStorage.cs#L203](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L203) and [#L246](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L246).
- Hardware: not applicable. This is managed memory ownership.

## Proposed fix

- Recommendation: clear every buffer the SDK owns, on success and on failure, with `try`/`finally` and `CryptographicOperations.ZeroMemory`.
  1. `TryDecrypt` ([LargeBlobData.cs#L74-L85](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L74-L85)): clear `plaintext` in a `finally` after the parse. Parse a `byte[]` directly instead of calling `plaintext.ToArray()`, which removes a copy (line 140).
  2. `Encrypt` ([LargeBlobData.cs#L102-L118](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L102-L118)): clear the CBOR plaintext after encryption.
  3. `SetBlobAsync` and `DeleteBlobAsync` ([LargeBlobStorage.cs#L199-L208](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L199-L208), [#L240-L255](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L240-L255)): clear the non-null results that are discarded.
  4. Document that `GetBlobAsync` returns caller-owned plaintext, which the caller must clear.
- Options considered:
  - A. Clear owned buffers in `finally`, and remove the extra copy. Recommended. Pros: local and small. Cons: BCL-internal copies remain.
  - B. Rewrite the parser on spans to avoid allocations. Pros: fewer copies. Cons: a larger change; BCL-internal copies remain.
  - Recommendation: take option A now, and fold it into #13, which replaces these buffers.
- API impact: none. The returned plaintext's ownership is documented.
- Proving test: no deterministic unit test can show this today. After #13, the probe could target the decrypt buffer through an internal seam, if such a seam is acceptable. Otherwise rely on code review. Remove the current probe, because it checks the wrong buffer and gives a false signal.
- Depends on or interacts with: #13 (the rewrite changes these buffers) and #14 (the fallback path).
- Open question: confirm the returned-plaintext ownership contract. We recommend documenting it as caller-owned.

## Check it yourself

- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1624"` (fails today; see the note on the wrong buffer).
- Read the code at the lines cited above.
- Repository rule: [CLAUDE.md](../../../CLAUDE.md), lines 59-60.
