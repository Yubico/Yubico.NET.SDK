# 25e BER-TLV long-form length decoding, with #20 (YESDK-1633; #20 is YESDK-1628)

| | |
| --- | --- |
| Audit severity | MED for item 25e (row 25). LOW for #20. |
| Our verdict | Confirmed, with a correction (25e). Confirmed (#20). The verification report did not give #20 its own verdict. It is covered here. |
| Our severity | LOW |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit tests (run today), static (code and the BER specification), Python (run) |
| Since the audit | Unchanged. `Tlv.cs` is identical at `df1ec06d`. |

Abbreviations used in this file: API is the application programming interface. ITU-T is the International Telecommunication Union's standards sector. X.690 is the ITU-T specification of the basic encoding rules (BER). BER-TLV is the tag-length-value (TLV) encoding under those rules. ISO/IEC is the joint ISO and IEC standards series; ISO/IEC 8825-1 has the same text as X.690. SDK is the software development kit.

## What the audit says

The audit says that a long-form length can overflow, and that a truncated long-form header escapes the parser's normal rejection. For #20 it says that the parser reads `buffer[0]` without checking that a byte remains.

> "BER-TLV length overflow accepts truncated lengths"

> "It immediately evaluates buffer[0] without verifying that one byte remains"

BER-TLV is the tag-length-value encoding from the ITU-T X.690 basic encoding rules (BER).

## What is right

- The long-form loop reads `buffer[0]` without checking that a byte remains ([Tlv.cs:237-241](../../../src/Core/src/Utilities/Tlv.cs#L237-L241), read at [line 239](../../../src/Core/src/Utilities/Tlv.cs#L239)). For `5A 81` and `5A 82 01`, the read throws `IndexOutOfRangeException`. Repro: [YESDK1628_TlvTruncatedLongFormLength_ThrowsParseError](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L137-L145). Result today: failed. The actual type was `IndexOutOfRangeException`. The test covers both `Tlv.Create` and `TlvHelper.DecodeList`.
- The length accumulator is a 32-bit `int`, and the number of length octets is not limited ([Tlv.cs:235-241](../../../src/Core/src/Utilities/Tlv.cs#L235-L241)). `5A 85 01 00 00 00 01 AA` wraps to length 1 and is accepted. Repro: [YESDK1633_TlvFiveByteLengthOverflow_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L111-L118). Result today: failed, "No exception was thrown".
- The initial octet 0xFF is accepted. Any value above 0x80 is treated as a long form ([Tlv.cs:233-235](../../../src/Core/src/Utilities/Tlv.cs#L233-L235)), so 0xFF means 127 length octets. X.690 forbids this value. We ran it: `5A FF` followed by 127 zero octets is accepted as an empty TLV.

## What is wrong or imprecise

- **The audit's example is already rejected.** `5A 84 FF FF FF FF AA` decodes to -1. The slice at [Tlv.cs:244](../../../src/Core/src/Utilities/Tlv.cs#L244) then throws `ArgumentOutOfRangeException`, which is an `ArgumentException`. Guard: [YESDK1633_TlvFourByteLengthNegative_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L124-L131). It passed today.
- **"Truncated" is the wrong word for the overflow case.** `85 01 00 00 00 01` is not truncated. It declares a length that does not fit in 32 bits, and that length wraps.
- **The audit cites `Tlv.cs:227-251` for #20.** The defect is the read at line 239.

## Why it matters

- **Who can trigger it:** any code that decodes BER-TLV from device responses, when the data is malformed or hostile.
- **Overflow effect:** the parser accepts a wrong length. It then skips to the wrong offset and misreads the TLVs that follow.
- **Truncation effect:** `IndexOutOfRangeException` escapes the parser's own error family. Callers that catch `ArgumentException` do not catch it.
- **Severity:** LOW. No key material is exposed. The effect is a wrong or failed parse on malformed input.

## Specification

ITU-T X.690, clause 8.1.3.5 (long-form length octets):

> "bits 7 to 1 shall encode the number of subsequent octets in the length octets, as an unsigned binary integer with bit 7 as the most significant bit"

> "the value 11111111 shall not be used."

> "shall be the encoding of an unsigned binary integer equal to the number of octets in the contents octets"

Source: [ITU-T X.690](https://www.itu.int/rec/T-REC-X.690). We checked the quotes in a copy of the 1995 edition, [ISO/IEC 8825-1:1995](https://cdn.standards.iteh.ai/samples/16295/e4b22bedfb39418da45f233014be689e/ISO-IEC-8825-1-1995.pdf), which has the same clause numbering. We did not check the 2021 ITU-T text.

The SDK's own contract is in `ParseData`. Each explicit truncation check throws `ArgumentException` with an "Insufficient data for ..." message, for example "Insufficient data for length" ([Tlv.cs:220](../../../src/Core/src/Utilities/Tlv.cs#L220); the same pattern appears at [lines 199](../../../src/Core/src/Utilities/Tlv.cs#L199) and [209](../../../src/Core/src/Utilities/Tlv.cs#L209)).

## Canonical Python reference

**Python is correct about the tested overflow and truncation inputs.** The length is read as an arbitrary-precision integer, and the declared end must match the buffer length exactly. Python does not reject the reserved initial octet `0xFF` either. It accepted `5A FF` followed by 127 zero octets as an empty TLV (we ran it), so the same gap is a candidate for the divergence ledger. Python also accepts redundant length octets: `5A 85 00 00 00 00 01 AA` has length 1, as the SDK does.

- [yubikit/core/__init__.py, lines 293-297](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/__init__.py#L293-L297): `bytes2int` reads every length octet. There is no limit.
- [lines 342-345](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/__init__.py#L342-L345):

```python
self._tag, self._value_offset, self._value_ln, end = _tlv_parse(self)
if len(self) != end:
    raise ValueError("Incorrect TLV length")
```

We ran `Tlv` on four inputs: `5A 85 01 00 00 00 01 AA`, `5A 84 FF FF FF FF AA`, `5A 81` and `5A 82 01`. Each raised `ValueError: Incorrect TLV length`.

For `5A 81`, Python does not raise `IndexError`. The slice returns fewer bytes, and the exact-length check rejects the input. The [`IndexError` mapping in lines 300-301](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/__init__.py#L300-L301) only covers a missing tag or length octet.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [YESDK1633_TlvFiveByteLengthOverflow_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L111-L118). Asserts `ArgumentException`. Result today: failed, "No exception was thrown".
- Unit: [YESDK1628_TlvTruncatedLongFormLength_ThrowsParseError](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L137-L145), cases `[5A 81]` and `[5A 82 01]`. Result today: failed, actual `IndexOutOfRangeException`.
- Guard: [YESDK1633_TlvFourByteLengthNegative_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L124-L131). Result today: passed.
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** In `Tlv.ParseData` ([Tlv.cs:232-242](../../../src/Core/src/Utilities/Tlv.cs#L232-L242)):
  1. Reject the reserved initial octet `0xFF`. X.690 forbids it.
  2. Before reading the length octets, check that every declared octet is present. Throw `ArgumentException("Insufficient data for length")` otherwise.
  3. Accumulate in a `long`. Reject a value above `int.MaxValue`, and a declared length above the remaining buffer. Accept leading-zero length octets. They do not change the value, and X.690 clause 8.1.3.5, note 2, allows redundant octets.
- **Options considered (count of length octets).**
  - **(a) No count limit, with the overflow check (recommended).** It accepts every valid BER length, and it rejects overflow.
  - **(b) A four-octet profile limit.** Simpler to state, but it rejects valid BER encodings such as `5A 85 00 00 00 00 01 AA`. If chosen, document it as a restriction that rejects some valid encodings.
- **API impact.** Behavioural, for malformed input only. Valid BER input is unchanged. Overflowing or truncated lengths, and the reserved `0xFF`, now throw `ArgumentException`.
- **Proving test.** Both repro tests pass after the change. Keep the guard test. Add the valid leading-zero case and the `0xFF` case as positive and negative checks.
- **Depends on / interacts with.** Item 25b. `DeviceInfoReader` may need to map `ArgumentException` to `BadResponseException`.
- **Open questions for the maintainer.** Accept any number of length octets with the overflow check (recommended), or a documented four-octet limit?

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1633_Tlv"`, and `--filter "FullyQualifiedName~YESDK1628"`, from the worktree root.
- Read `src/Core/src/Utilities/Tlv.cs` lines 197-248.
- Read `yubikit/core/__init__.py` lines 293-301 and 342-345 at `4ca60f7`. Then parse `5A 81` with `Tlv(bytes.fromhex("5a81"))`.
