# #12 The largeBlob fragment length is fixed at 1024 bytes (YESDK-1620)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED. Benign on the test keys, whose maxMsgSize is 1536 (a limit of 1472). It fails on an authenticator that omits maxMsgSize (limit 960) or advertises less than 1088. |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test; GetInfo of both test keys (recorded in the hardware results) |
| Since the audit | `LargeBlobStorage.cs` and `AuthenticatorInfo.cs` are unchanged. `FidoSession.cs` changed, but only in user-presence lines. The GetInfo lines it uses (156-157) are unchanged. The bug and the fix are unaffected. |

## What the audit says

The audit says the read request asks for 1024 bytes, but the authenticator's limit can be 960. The SDK never uses the device's maxMsgSize for this.

> The SDK therefore requests 1024 bytes where the permitted maximum is 960.

## What is right

- The default is fixed. Both `LargeBlobStorage` constructors default `maxFragmentLength` to 1024 ([LargeBlobStorage.cs#L74-L81](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L74-L81), [#L94-L104](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L94-L104)).
- The same value drives reads and writes. Reads send it as `get` ([LargeBlobStorage.cs#L271-L297](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L271-L297), [#L302-L315](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L302-L315)). Writes use it as the chunk size ([LargeBlobStorage.cs#L337-L355](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L337-L355), line 347).
- GetInfo's maxMsgSize is parsed but not kept. `AuthenticatorInfo` reads key 0x05 ([AuthenticatorInfo.cs#L357-L358](../../../src/Fido2/src/AuthenticatorInfo.cs#L357-L358)). `FidoSession` reads GetInfo during initialization and keeps only the firmware version ([FidoSession.cs#L156-L157](../../../src/Fido2/src/FidoSession.cs#L156-L157)).
- The repro fails as intended. `YESDK1620_DefaultReadFragmentIs960Bytes` expects 960 and gets 1024 ([Fido2LargeBlobAuditReproTests.cs#L72-L86](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L72-L86)).

## What is wrong or imprecise

- The audit covers reads only. The write path has the same defect. `WriteRawBlobAsync` chunks at the same value ([LargeBlobStorage.cs#L347](../../../src/Fido2/src/LargeBlobs/LargeBlobStorage.cs#L347)). CTAP 2.3 §6.10.2 also limits `set`, as quoted below.
- Wording. When maxMsgSize is absent, maxMsgSize defaults to 1024, which makes maxFragmentLength default to 960 (§6.10.2). The audit gives the same numbers.
- The defect is not visible on the test keys. Their maxMsgSize is 1536, so the limit is 1472, and 1024 fits. The audit says the same: "This does not fail with every authenticator."

## Why it matters

- Who: any app that reads or writes large blobs through `LargeBlobStorage` on an authenticator that omits maxMsgSize or advertises less than 1088.
- Effect: the first request is larger than the limit. A conforming authenticator rejects it with CTAP1_ERR_INVALID_LENGTH, and the SDK fails the operation. The SDK does not retry with a smaller size. We have not tested such a device.
- The write path has the same effect. The audit does not mention it.

## Specification

> A per-authenticator constant, maxFragmentLength, is here defined as the value of maxMsgSize (from the authenticatorGetInfo response) minus 64.

> If no maxMsgSize is given in the authenticatorGetInfo response) then it defaults to 1024, leaving maxFragmentLength to default to 960.

Source: CTAP 2.3 PS, §6.10.2 Reading and writing serialised data. The spec text has a stray closing parenthesis after "response"; it is quoted as printed.

> If the value of get is greater than maxFragmentLength, return CTAP1_ERR_INVALID_LENGTH.

> If the length of the value of set is greater than maxFragmentLength, return CTAP1_ERR_INVALID_LENGTH.

Source: CTAP 2.3 PS, §6.10.2.

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.10.2 maxFragmentLength definition and default of 960 | Yes | Yes | Yes | |
| `get` and `set` limit errors | Yes | Yes | Yes | |

## Canonical Python reference

Python is correct here. It sets the fragment size to the authenticator's GetInfo maxMsgSize minus 64 ([blob.py#L109-L110](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/blob.py#L109-L110)). The default for an absent maxMsgSize is 1024, which gives 960 ([base.py#L91](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L91)). The session uses the GetInfo value after its first call ([base.py#L251-L253](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L251-L253)).

```python
        self.ctap = ctap
        self.max_fragment_length = self.ctap.info.max_msg_size - 64
```

## Sibling SDKs (context only)

Not checked in this pass.

## Reproduction

- Unit: [Fido2LargeBlobAuditReproTests.YESDK1620_DefaultReadFragmentIs960Bytes](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L72-L86). It reads through a stub session and expects `get = 960`. Today it fails: 1024 is sent.
- Hardware: not applicable to the failure. GetInfo on both test keys reports maxMsgSize 1536 ([hardware-results.md](../evidence/hardware-results.md), GetInfo section). The default read is valid on those keys.

## Proposed fix

- Recommendation: take the limit from the session's GetInfo, cache it per storage instance, and use it for reads and writes.
  - `LargeBlobStorage` calls `IFidoSession.GetInfoAsync` ([IFidoSession.cs#L61](../../../src/Fido2/src/IFidoSession.cs#L61)) once, before the first read or write. It uses `maxMsgSize - 64`, or 960 when maxMsgSize is absent.
  - Keep `maxFragmentLength` as an explicit override, bounded by the device limit.
  - Apply the same value in `ReadRawBlobAsync` and `WriteRawBlobAsync`.
- Options considered:
  - A. Query GetInfo lazily inside `LargeBlobStorage`. Recommended. Pros: no change to the session type; uses an existing interface member. Cons: one extra GetInfo call on first use. The test stub must return an `AuthenticatorInfo`.
  - B. Pass `AuthenticatorInfo` into the constructor. Pros: explicit. Cons: constructor signature change; callers must fetch GetInfo.
  - C. Store the GetInfo value on `FidoSession` and read it from there. Pros: one GetInfo call. Cons: more state on the session; no current caller needs it.
- API impact: the default changes from 1024 to a value derived from the device (behavioral). If the parameter becomes `int?`, callers that pass an int still compile.
- Proving test: `YESDK1620_DefaultReadFragmentIs960Bytes`, updated to stub GetInfo without maxMsgSize. Add a case with maxMsgSize 2048 that expects a get of 1984. Add a write-path test that expects no `set` longer than 960 bytes when maxMsgSize is absent.
- Depends on or interacts with: #13 (multi-fragment writes follow the same fragment rules); #7 (AuthenticatorInfo plumbing, option B only).
- Open question: option A (lazy GetInfo, recommended) or option B (pass the value in)?

## Check it yourself

- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1620"` (fails today).
- CTAP 2.3 PS §6.10.2: search for "maxFragmentLength, is here defined" and for "leaving maxFragmentLength to default to 960".
- python-fido2 at `5bc9d3a1c8c34a3c4ca408366e630b620db47faa`: read `fido2/ctap2/blob.py` lines 109-110 and `fido2/ctap2/base.py` line 91.
