# 25f TlvHelper.EncodeDictionary underallocates its output buffer (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (item 25f of the MED row 25) |
| Our verdict | Confirmed |
| Our severity | LOW |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test (run today), static (code), Python (read) |
| Since the audit | Unchanged. `TlvHelper.cs` is identical at `df1ec06d`. The two callers, `DeviceConfig.cs` and `SecurityDomainTlvEncoding.cs`, are also identical. |

Abbreviations used in this file: API is the application programming interface. BER-TLV is the tag-length-value (TLV) encoding of the ITU-T X.690 basic encoding rules (BER). SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that the BER-TLV encoder (basic encoding rules, tag-length-value) allocates too small an output buffer.

> "TLV encoder underallocates its output buffer"

## What is right

- The buffer size is estimated as `2 + value length` for each entry ([TlvHelper.cs:238](../../../src/Core/src/Utilities/TlvHelper.cs#L238)). The estimate assumes a one-byte tag and a one-byte length.
- A two-byte tag, or a value of 128 bytes or more (which needs a two-byte length), makes the real encoding longer than the estimate. The copy then fails at [TlvHelper.cs:250](../../../src/Core/src/Utilities/TlvHelper.cs#L250).
- The repro shows it. Repro: [CoreNumericBoundaryAuditReproTests.YESDK1633_EncodeDictionaryLongTagOrLength_EncodesAllBytes](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L153-L166), with `(0x7F49, 126)` and `(0x53, 254)`. Result today: failed for both, with "Destination is too short. (Parameter 'destination')".

## What is wrong or imprecise

- The failure is not silent. The copy throws `ArgumentException`, so no truncated output is returned.
- The failure depends on the data. `ArrayPool` rounds the requested size up to a pool size, and the copy fails only when that buffer is smaller than the real encoding. Both repro vectors hit a pool size exactly: their estimates are 128 and 256 ([2 + 126 and 2 + 254](../../../src/Core/src/Utilities/TlvHelper.cs#L238)).
- The estimate is too small for any value of 128 bytes or more, not only at pool boundaries. Whether the copy fails depends on the pool size.

## Why it matters

- **Who can trigger it:** application code that calls the public `TlvHelper.EncodeDictionary` with long values or two-byte tags.
- **Effect:** an exception, not corrupt output, for some inputs. The size at which it fails depends on the data.
- **Internal callers are not affected.**
  - `DeviceConfig` uses one-byte tags (`0x03` to `0x17`, [DeviceConfig.cs:32-40](../../../src/Management/src/DeviceConfig.cs#L32-L40)). Its values are small. The lock codes are checked to be 16 bytes before encoding ([ManagementSession.cs:158-163](../../../src/Management/src/ManagementSession.cs#L158-L163)).
  - `SecurityDomainTlvEncoding` uses one-byte tags (`0xD0` and `0xD2`, [SecurityDomainTlvEncoding.cs:22-23](../../../src/SecurityDomain/src/SecurityDomainTlvEncoding.cs#L22-L23)) and one-byte values.

## Specification

The SDK contract is the XML summary of `EncodeDictionary`: "Encodes a mapping of tag-value pairs into BER-TLV data ordered by ascending tag." ([TlvHelper.cs:223](../../../src/Core/src/Utilities/TlvHelper.cs#L223)). The summary describes the content and order of the output. It does not describe a size limit or a failure mode. The length rules are in ITU-T X.690, clause 8.1.3 (see item 25e).

## Canonical Python reference

**Python is correct here.** The Python helper builds each TLV exactly and joins the results. There is no pre-sized buffer, so this defect cannot occur.

[yubikit/piv.py, lines 410-413](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L410-L413) at `4ca60f7`:

```python
def _dump_tlv_dict(values: dict[int, bytes | None]) -> bytes:
    return b"".join(
        Tlv(tag, value) for tag, value in values.items() if value is not None
    )
```

Each `Tlv` is sized exactly when it is built ([yubikit/core/__init__.py#L317-L330](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/__init__.py#L317-L330)).

One difference, not part of this item: the .NET method sorts the entries by tag ([TlvHelper.cs:246](../../../src/Core/src/Utilities/TlvHelper.cs#L246)). The Python helper keeps the dictionary's insertion order.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreNumericBoundaryAuditReproTests.YESDK1633_EncodeDictionaryLongTagOrLength_EncodesAllBytes](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L153-L166), cases `(0x7F49, 126)` and `(0x53, 254)`. Asserts that the output equals the encoding of one `Tlv`. Result today: failed for both, "Destination is too short. (Parameter 'destination')".
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** Build the `Tlv` objects first, add up their `TotalLength` values, allocate once, and copy. This is how `EncodeList` already works ([TlvHelper.cs:116-127](../../../src/Core/src/Utilities/TlvHelper.cs#L116-L127)). Keep the `using` on each `Tlv` so that the temporary objects are still disposed.
- **Options considered.**
  - **(a) Exact sizing (recommended).** It matches `EncodeList` and allocates no more than needed.
  - **(b) A worst-case estimate** of 2 bytes (tag) + 5 bytes (length) + value length for each entry. It is simpler, and it allocates more.
- **API impact.** None. The output does not change for inputs that already work.
- **Proving test.** The existing repro, with both vectors, passes after the change. Optionally, also check that the output equals the concatenation of each `new Tlv(tag, value)`.
- **Depends on / interacts with.** Nothing.
- **Open questions for the maintainer.** Exact sizing or a conservative estimate? We recommend exact sizing.

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1633_EncodeDictionary"`, from the worktree root.
- Read `src/Core/src/Utilities/TlvHelper.cs` lines 112-137 and 234-264.
- Read `yubikit/piv.py` lines 410-413 at `4ca60f7`.
