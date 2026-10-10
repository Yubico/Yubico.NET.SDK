# #13 Large-blob entries use a private, incompatible format (YESDK-1621)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | HIGH. Interoperability and data availability. The audit rates it MED. |
| Root cause | SDK only |
| Fix group | C (public shape of the rewrite, and a migration decision) |
| Evidence | unit test; hardware (user present): rejection of an existing spec-format array, and SDK-format entries observed. No end-to-end cross-client round trip. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/Fido2/src/LargeBlobs/` between `fdfcd6fd` and `df1ec06d`. |

## What the audit says

The audit says the SDK stores each large-blob entry in a private format. CTAP requires a CBOR map with three fields. The SDK writes one byte string instead.

> Large-blob entries use a private, incompatible format

## What is right

- Each entry is one byte string, not the §6.10.3 map. `LargeBlobArray.Serialize` writes `EncryptedData` as a byte string ([LargeBlobData.cs#L245-L269](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L245-L269), write at line 253). `Deserialize` reads each entry as a byte string ([LargeBlobData.cs#L206-L239](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L206-L239), read at line 233).
- The plaintext is a private CBOR map with an empty byte-string key. It is not DEFLATE data ([LargeBlobData.cs#L124-L134](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L124-L134)). `Encrypt` does not compress ([LargeBlobData.cs#L95-L122](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L95-L122)).
- AES-GCM uses no associated data ([LargeBlobData.cs#L110-L113](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L110-L113)). The class doc says AAD is empty ([LargeBlobData.cs#L54](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L54)).
- `origSize` is neither stored nor checked. Nonce, ciphertext, and tag are concatenated into one field ([LargeBlobData.cs#L115-L121](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L115-L121)).
- The array trailer is built correctly: the first 16 bytes of SHA-256 over the array ([LargeBlobData.cs#L259-L266](../../../src/Fido2/src/LargeBlobs/LargeBlobData.cs#L259-L266)).
- The repros fail as intended. `YESDK1621_ReadsPythonFido2CompatibleArray` fails where the SDK expects a byte string and finds a map. `YESDK1621_SdkWrittenArrayIsSpecDecodable` fails where the spec expects a map and finds a byte string ([Fido2LargeBlobAuditReproTests.cs#L90-L124](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L90-L124)).
- Hardware (user present) confirms rejection of an existing spec-format array and the presence of SDK-format byte-string entries. On the 5.7.4 test key, the SDK's write fails while it parses an array that already holds a spec entry written by another client ("next CBOR data item is of major type '5'"). On the 5.8.0 test key, python-fido2 reads the two entries left by earlier SDK runs as raw byte strings. Neither direction completed an end-to-end cross-client read/write round trip. The python-to-SDK read interop was not run ([hardware-results.md](../evidence/hardware-results.md), Phase 4).

## What is wrong or imprecise

- The audit attributes the format to CTAP 2.3 §6.10.3. The same map structure appears in CTAP 2.1 and 2.2, so the requirement is older than 2.3.
- The audit describes the write path only. The read path fails too. `GetBlobAsync`, `SetBlobAsync`, and `DeleteBlobAsync` all parse the same array ([LargeBlobStorage.cs#L113-L123](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L113-L123)). A spec-format entry makes each of them throw. This follows from the code. The hardware run covered only the write.
- The audit does not say what the fix does to existing data. After a fix, the entries the SDK wrote are non-conforming, because they are byte strings. §6.10.2 requires a write to contain only conforming entries, so the next write drops them. The 5.8.0 test key holds two such entries. A migration decision is needed (see Proposed fix).
- python-fido2's format is correct. Its divergences are in the decoding and write rules, not the format (see the Python section).

## Why it matters

- Who: any app that uses the public `LargeBlobStorage` on an authenticator that holds spec-format entries, or any app that shares blobs with another spec client.
- Preconditions: an array that already holds a spec-format entry, written by another client. The hardware evidence comes from the 5.7.4 test key.
- Effect: the SDK cannot write, read, or delete on that authenticator. Blobs the SDK writes are opaque to spec clients. They have no DEFLATE step, no AAD, and no map fields.
- Data availability: after a fix, entries the current SDK wrote would be dropped on the next write, unless they are migrated.

## Specification

> The elements of the large-blob array MUST conform to the following large-blob map structure. Conformance, in this context, means that a map MUST include all required elements, MAY include optional elements, and MAY include unknown elements.

Source: CTAP 2.3 PS, §6.10.3 Large, per-credential blobs.

> AEAD_AES_256_GCM ciphertext, implicitly including the AEAD “authentication tag” at the end.

> AEAD_AES_256_GCM nonce. MUST be exactly 12 bytes long.

> Contains the length, in bytes, of the uncompressed data.

Source: CTAP 2.3 PS, §6.10.3, element table (fields 0x01, 0x02, and 0x03).

> Associated data: The value 0x626c6f62 ("blob") || uint64LittleEndian(origSize).

Source: CTAP 2.3 PS, §6.10.3.

> Let plaintext equal origData after compression with DEFLATE [RFC1951].

Source: CTAP 2.3 PS, §6.10.5 Writing per-credential large-blob data for a new credential.

> If the element is not a map conforming to the large-blob map structure defined above, skip this array element.

> If the decryption fails, skip this array element.

> Decompress the resulting plaintext with DEFLATE [RFC1951]. If decompression fails, return an error.

> If the length of the decompression result is not equal to origSize, return an error.

Source: CTAP 2.3 PS, §6.10.4 Reading per-credential large-blob data.

> Platforms SHOULD limit the maximum permitted value of origSize and that maximum SHOULD be at least 1MiB.

Source: CTAP 2.3 PS, §6.10.4, note.

> Platforms MUST ensure that the large-blob array (i.e. without the trailing 16 bytes) is a CBOR array where all entries conform to the large-blob map structure defined below. The maps and array MUST be encoded using the canonical rules. Platforms MUST NOT attempt to write a serialized large-blob array that exceeds the maxSerializedLargeBlobArray reported by the authenticator in the authenticatorGetInfo response.

Source: CTAP 2.3 PS, §6.10.2 Reading and writing serialised data.

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.10.2 entries must conform; canonical encoding; size limit | Yes | Yes | Yes | |
| §6.10.3 map fields 0x01 to 0x03, and the associated data | Yes | Yes | Yes | |
| §6.10.3 unknown elements allowed | Yes | Yes | Yes | Affects how untouched entries are kept on write |
| §6.10.4 skip non-conforming and undecryptable entries; decompression and origSize errors | Yes | Yes | Yes | |
| §6.10.4 note: origSize cap of at least 1 MiB | Yes | Yes | Yes | |
| §6.10.5 DEFLATE before encryption | Yes | Yes | Yes | |

## Canonical Python reference

Python matches the format. It uses raw DEFLATE, the spec associated data, AES-GCM, and the map with keys 1, 2, and 3 ([blob.py#L44-L46](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L44-L46), [#L54-L55](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L54-L55), [#L58-L69](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L58-L69)). Python is correct on the format.

```python
    ciphertext = aesgcm.encrypt(nonce, _compress(data), _lb_ad(orig_size))

    return {
        1: ciphertext,
        2: nonce,
        3: orig_size,
    }
```

Python's decoding and write rules diverge from the spec. Both are candidates for the divergence ledger. Do not copy them.

1. Read side. On a decompression error or a size mismatch, `get_blob` skips the entry ([blob.py#L184-L189](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L184-L189)). §6.10.4 says to return an error. Our earlier notes call this a documented deviation. We found no comment in `blob.py` that says so.
2. Write side. `put_blob` keeps every entry it cannot decrypt, including entries that do not conform ([blob.py#L203-L208](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L203-L208)). §6.10.2 says the platform must ensure that all entries conform.

## Sibling SDKs (context only)

yubikit-android uses the same format: map fields 1 to 3, DEFLATE, and the spec associated data ([LargeBlobs.java#L214-L219](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/LargeBlobs.java#L214-L219); field constants at [#L300-L302](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/LargeBlobs.java#L300-L302)). yubikit-swift uses map keys 0x01 to 0x03 ([Entry+CBOR.swift#L22-L24](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/FIDO/CTAP/LargeBlobs/Entry+CBOR.swift#L22-L24)).

## Reproduction

- Unit: [Fido2LargeBlobAuditReproTests.YESDK1621_ReadsPythonFido2CompatibleArray](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L90-L95). It builds a spec array (raw DEFLATE, AES-GCM with the spec associated data, canonical CBOR map, SHA-256 trailer) and expects the SDK to read it. Today it fails on CBOR major type 5.
- Unit: [Fido2LargeBlobAuditReproTests.YESDK1621_SdkWrittenArrayIsSpecDecodable](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L99-L124). It expects the SDK's output to decode as the spec structure. Today it fails on a byte string where a map is expected.
- Hardware, write (PIN, touch, allow-listed test key): [Fido2LargeBlobAuditReproTests.YESDK1621_SdkWriteIsReadablePerSpec](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L22-L108). Result on the 5.7.4 test key: fails inside `SetBlobAsync` with "next CBOR data item is of major type '5'" ([hardware-results.md](../evidence/hardware-results.md), Phase 4).
- Hardware, read of a python-fido2 write ([Fido2LargeBlobAuditReproTests.YESDK1621_ReadsPythonFido2WrittenBlob](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L114-L131)): not run. The hardware record marks it as superseded by the write-side observation ([hardware-results.md](../evidence/hardware-results.md), Phase 4).
- 5.8.0 test key: the array holds two SDK-format entries, which python-fido2 sees as raw byte strings ([hardware-results.md](../evidence/hardware-results.md), Phase 4).

## Proposed fix

- Recommendation: replace both the entry format and the array format with the §6.10.3 map, in one change.
  1. Entry: expose `Ciphertext` (with the tag), `Nonce` (12 bytes), and `OriginalSize`. To encrypt, DEFLATE-compress the data (raw, RFC 1951), then encrypt with AES-GCM using the associated data `"blob" || uint64LE(origSize)`. Encode the canonical map with keys 1, 2, and 3.
  2. Decrypt: check `origSize` against a cap of at least 1 MiB before decrypting. Use the same associated data. Inflate with an output limit of `origSize + 1`. Return an error if inflation fails or the length differs from `origSize`. Skip entries that do not decrypt or do not conform.
  3. Write: drop non-conforming entries. Keep untouched conforming entries as raw bytes, because §6.10.3 allows unknown elements. Check the serialized size against `maxSerializedLargeBlobArray` from GetInfo before writing. The SDK parses that value ([AuthenticatorInfo.cs#L381-L382](../../../src/Fido2/src/AuthenticatorInfo.cs#L381-L382)) but never checks it on write.
  4. Apply the cleanup (#16), the corrupt-array fallback (#14), and the fragment size (#12) in the same change.
- Options considered (public shape of the entry):
  - A. Model the entry with `Ciphertext`, `Nonce`, and `OriginalSize`, and remove `EncryptedData`. Recommended. Pros: the fields match the spec; no misleading bytes. Cons: a breaking change to a public type. The pre-release status makes this acceptable.
  - B. Keep `EncryptedData`, but store the CBOR map bytes in it. Pros: smaller diff. Cons: the property silently changes meaning, and callers cannot see the fields.
  - C. Keep the private type and add a parallel spec type. Pros: no break. Cons: two formats in the public API, and the old type stays misleading.
- Migration options:
  - M1. No shim. Recommended. We know of no external consumers, and this is pre-release. Consequence: entries the current SDK wrote are dropped on the next write. The 5.8.0 test key holds two of them.
  - M2. A one-time import of private-format entries. Pros: keeps the data. Cons: extra code to maintain, needed only if deployed data exists.
- API impact: option A breaks `LargeBlobEntry.EncryptedData`. Write behavior changes, because non-conforming entries are dropped. `LargeBlobArray.Deserialize` stays strict for direct callers (see #14).
- Proving test: `YESDK1621_ReadsPythonFido2CompatibleArray` and `YESDK1621_SdkWrittenArrayIsSpecDecodable` (unit). Hardware: `YESDK1621_SdkWriteIsReadablePerSpec` on a test key whose array holds a spec entry, and a python-fido2 read of an SDK-written entry.
- Depends on or interacts with: #12 (fragment size), #14 (corrupt-array fallback), and #16 (cleanup). The GetInfo value for `maxSerializedLargeBlobArray` needs the plumbing from #12.
- Open questions for the maintainer:
  1. Option A or option B for the entry shape?
  2. Migration: no shim (recommended), or a one-time import?
  3. Decompression cap: 1 MiB is the spec minimum. Which maximum do we choose?
  4. Should `LargeBlobArray.Deserialize` stay strict for direct callers?

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1621"`. Two tests fail today.
- Hardware (touch and PIN, allow-listed test key only): `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --filter "FullyQualifiedName~YESDK1621"`. The write test creates and deletes its own test credentials. Do not run the python-fido2 interop test on the same credential without sequencing it: writes replace the stored blob.
- CTAP 2.3 PS §6.10.3 and §6.10.4: search for "large-blob map structure" and "Associated data".
- python-fido2 at `5bc9d3a1c8c34a3c4ca408366e630b620db47faa`: read `fido2/ctap2/blob.py` lines 44-69 and 176-214.
