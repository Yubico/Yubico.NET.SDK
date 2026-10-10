# #14 Corrupt large-blob checksum throws instead of resetting (YESDK-1622)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED. Loss of availability after a torn or corrupt write. |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/Fido2/src/LargeBlobs/` between `fdfcd6fd` and `df1ec06d`. |

## What the audit says

The audit says the SDK detects a bad checksum but throws. The spec says the platform must discard the array and act as if it were the initial empty array.

> The SDK correctly detects the bad checksum but implements the wrong consequence.

## What is right

- The checksum is checked correctly. `Deserialize` compares the trailing 16 bytes with the first 16 bytes of SHA-256 over the array, in fixed time ([LargeBlobData.cs#L206-L224](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L206-L224)).
- A mismatch throws `ArgumentException` ([LargeBlobData.cs#L221-L224](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L221-L224)).
- The read path does not catch it. `ReadLargeBlobArrayAsync` returns an empty array only for an empty response. For anything else it calls `Deserialize` with no fallback ([LargeBlobStorage.cs#L113-L123](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L113-L123)).
- The repro fails as intended, with `ArgumentException: Large blob array hash verification failed` ([Fido2LargeBlobAuditReproTests.cs#L126-L144](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L126-L144)).

## What is wrong or imprecise

- The audit's quote is edited. It reads "If [the hash does] not [match], the configuration is corrupt...". The spec text is "If not, the configuration is corrupt...". The meaning is the same, but the brackets are not in the spec.
- The audit misses short reads. A non-empty response of 1 to 16 bytes throws "Data too short for valid large blob array." ([LargeBlobData.cs#L206-L211](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L206-L211)). A response that short cannot hold a trailer, so the same recovery applies. Review found this.
- The audit misses the write path. `SetBlobAsync` and `DeleteBlobAsync` read the array first ([LargeBlobStorage.cs#L197](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L197), [#L238](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L238)), so they fail the same way. A single fallback in the read path covers all three.
- Malformed CBOR with a valid checksum is a separate case. The spec's recovery rule covers only the checksum. Keep that case as an error, and decide its policy separately.

## Why it matters

- Who: any app that uses `LargeBlobStorage` after a torn write or device corruption. The spec expects this: "torn writes, platform errors, and storage corruption may result in a situation where an authenticator finds itself having stored an invalid serialized large-blob array."
- Effect today: `GetBlobAsync`, `SetBlobAsync`, and `DeleteBlobAsync` all throw. The per-credential get, set, and delete methods cannot recover automatically. A caller can replace the array explicitly through `WriteLargeBlobArrayAsync` ([LargeBlobStorage.cs#L132-L147](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L132-L147)), which needs PIN/UV authentication and writes without reading first. That discards the existing contents.
- After a fix: a corrupt array reads as empty, and the next write replaces the corrupt contents. The spec requires this. Apps should know that corrupt data is discarded.

## Specification

> Once complete, the platform MUST confirm that the embedded SHA-256 hash is correct, based on the definition above. If not, the configuration is corrupt and the platform MUST discard it and act as if the initial serialized large-blob array was received.

Source: CTAP 2.3 PS, §6.10.2 Reading and writing serialised data.

> The initial serialized large-blob array is the value of the serialized large-blob array on a fresh authenticator, as well as immediately after a reset. It is the byte string h’8076be8b528d0075f7aae98d6fa57a6d3c'

Source: CTAP 2.3 PS, §6.10 authenticatorLargeBlobs, introduction.

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.10.2 discard a corrupt configuration and act as if the initial array was received | Yes | Yes | Yes | |
| Initial serialized large-blob array bytes | Yes | Yes | Yes | |

## Canonical Python reference

Python is correct. `read_blob_array` returns an empty list when the check fails ([blob.py#L131-L134](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L131-L134)). A short buffer goes through the same check, and python-fido2 has no special case for it. `put_blob` then writes a fresh array that holds the new entry ([blob.py#L192-L214](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L192-L214)). That matches the write-path behavior we propose.

```python
        data, check = buf[:-16], buf[-16:]
        if check != sha256(data)[:-16]:
            return []
        return cast(Sequence[Mapping[int, Any]], cbor.decode(data))
```

## Sibling SDKs (context only)

Not checked in this pass.

## Reproduction

- Unit: [Fido2LargeBlobAuditReproTests.YESDK1622_CorruptChecksumActsAsInitialArray](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L128-L144). A stub returns an array with its last byte flipped, and the test expects an empty array. Today it fails with `ArgumentException`.
- Not in the repo yet: a write-path test (a set after a corrupt read) and a short-read test (1 to 16 bytes). Review asked for both.
- Hardware: not required. Not run.

## Proposed fix

- Recommendation: in the storage read path, return `LargeBlobArray.CreateEmpty()` when the response is shorter than 17 bytes or the checksum fails. Keep transport errors and malformed CBOR as errors ([LargeBlobStorage.cs#L113-L123](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L113-L123)). Keep `Deserialize` strict for direct callers ([LargeBlobData.cs#L206-L224](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L206-L224)).
- Options considered:
  - A. Fallback in `ReadLargeBlobArrayAsync`. Recommended. One change covers get, set, and delete. Pros: matches the spec and python-fido2 `put_blob`. Cons: a corrupt array reads as empty, as the spec requires.
  - B. Change `Deserialize` to return an empty array on mismatch. Pros: one place. Cons: changes a public method's contract, and direct callers lose the signal.
  - C. Return a read status that reports corruption, and let callers choose. Pros: transparent. Cons: new public API. The spec says "act as if", so the default still needs the fallback.
- API impact: none. Behavioral recovery. Option B would change a public method's contract.
- Proving test: `YESDK1622_CorruptChecksumActsAsInitialArray` (existing). Add a 1-to-16-byte case and a set-after-corrupt-read case.
- Depends on or interacts with: #13 (the array representation changes under the same reader).
- Open questions: none. The spec is explicit. We recommend option A.

## Check it yourself

- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1622"` (fails today).
- CTAP 2.3 PS §6.10.2: search for "the configuration is corrupt". Section 6.10: search for "The initial serialized large-blob array is".
- python-fido2 at `5bc9d3a1c8c34a3c4ca408366e630b620db47faa`: read `fido2/ctap2/blob.py` lines 117-134 and 192-214.
