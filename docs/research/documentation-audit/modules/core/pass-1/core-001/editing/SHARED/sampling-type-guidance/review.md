# Independent review — type-selection and migration sampling correction

**PASS for the exact focused two-document patch.** No mandatory correction remains in this delta.

Accepted patch SHA-256:
`3c066d098c68a289da1ebc4dda2ded4fb2db66357f07907f5269a7061b334623`.

| Accepted path | After SHA-256 |
| --- | --- |
| `CLAUDE.md` | `6e4708a941776bbfd20aa84beae02a3f07e9e114a463d488d4ca22306b61c383` |
| `docs/v1-to-v2-comparison.md` | `4797f0c57ce247952d35bf2e03a63a826f3b63f21b995d4da877484249dafb13` |

This focused review addresses SAMPLE-B4-001 / `core-001:unit:001c58b1d1862d76` and
SAMPLE-B4-002 / `core-001:unit:a9e434874ba55d57`. Both edited overlaps receive **zero clean-sampling
credit**; the coordinator owns category expansion and sampling reconciliation. It is not a new whole-guide
audit or a change to the project's type-selection policy. Only this review and
[review-identities.json](review-identities.json) were written.

## Content and source decisions

### Type-selection rationale — PASS

Root `CLAUDE.md:175–191,230–235,430–442` now distinguishes the project's preference for small,
immutable value types from the language/runtime facts that explain their costs:

- The retained ≤16-byte preference is described as **two 64-bit words**, not two machine words on
  every architecture. The preference, hot-path considerations and default-to-class advice remain.
- `readonly` describes struct state, not deep immutability of referenced mutable objects. The wording
  no longer grants universal stack allocation, absence of GC pressure or absence of all copying.
- The defensive-copy benefit is scoped to non-readonly instance-member calls in readonly contexts.
  The decision matrix's corresponding copying/allocation/immutability cells no longer contradict
  those conditions. Existing selection preferences are not recast as CLR restrictions on struct fields.
- `List<T>` and `T[]` value-type elements are stored unboxed; this does not remove collection/backing-array
  allocations. Struct-to-object/interface conversion and object-based non-generic storage are separately
  identified boxing contexts. The text does not promise that arbitrary generic collections, enumerators,
  operations, containing objects or referenced data produce no allocation or GC work.

The named Microsoft documentation references retained by the editor after its Context7 `/dotnet/docs`
orientation were read directly: `sources/struct.md:24–50,169–171` supports value copying, shallow readonly
state, defensive-copy context and boxing conversions; `sources/when-to-use-generic-collections.md:13–19`
and `sources/generics.md:12–16,59` support unboxed value-type generic storage versus object-based storage.
These are exact captured **moving-main documentation snapshots**, identified by their hashes, not pinned
runtime implementation or benchmark results. No example benchmark was run or adopted as a performance
guarantee, and no new allocation experiment is required for this bounded prose correction.

### Migration introduction — PASS

`docs/v1-to-v2-comparison.md:27–42` now limits internalization to selected retired low-level extension
points and internal protocol composition, while acknowledging public raw sessions/connections and
selected TLV, Base32 and cryptographic helpers. It no longer implies that every low-level consumer API
is internal or removed.

Actual current Core declarations were checked, including enclosing type accessibility:

- `RawSmartCardSession.cs:34,50,66,125,140,154`, `RawCtapHidSession.cs:28,40,64` and
  `RawOtpHidSession.cs:31,43,69` expose public raw sessions and operations.
- `IConnection.cs:26–29` and `ISmartCardConnection.cs:19–72` are public connection contracts.
- `Tlv.cs:25,42–54`, `TlvHelper.cs:23,34,112,156` and `Base32.cs:15,22,47` expose the selected
  utility surfaces; the two Base32 overloads are decoding, not a restored encoding framework.
- `CryptographyProviders.cs:53,93–283` exposes selected public creators, while `:310–315` keeps the
  ARKG/ECDH/CMAC primitive creators internal. The new wording does not reopen those extension points.
- `ProtocolFactory.cs:27` is an internal enclosing type despite its public member declarations;
  `IProtocol.cs:46` is internal. Effective accessibility, not a grep for public methods, supports the
  internal-composition statement.

The specific removed-API table, its rationale, and the explicit public Base32 decoding pointer remain
byte-identical. No broader historical migration claim or newly restored API is inferred.

## Independent preservation and static verification

Fresh read-only checks found:

- Both complete before files match their accepted predecessor hashes. Both complete after files match
  the actual product files and accepted hashes above. Reconstructing the two conventional unified diffs
  produces the exact focused patch and requested SHA-256.
- Exactly these **two** paths differ from the accepted predecessor chain. The other **24** accepted
  guides remain byte-identical, including the accepted lifecycle correction.
- All **45** prior artifact preservation entries match. Original full-26-path review/identity records,
  revision-2 patch and lifecycle patch/review remain immutable predecessor evidence, not relabeled seals
  for the new bytes. The lifecycle patch still hashes to
  `37074361c54bb2232ff8406ae37af482a73c51b029a3dfd8328e467f697e274e`.
- All **17** complete fenced bodies in the two documents remain byte-identical. Independently extracted
  before/after opening and closing coordinates, language, full body and SHA-256 match every receipt.
  The migration capability table through its Base32 pointer is also byte-identical.
- All **14** source-evidence entries match actual bytes: **3** retained Microsoft references and **11**
  Core source files, including every captured declaration line. Accessibility was inspected in actual
  enclosing type/namespace context. These checks do not certify all utility behavior or compile examples.
- `git diff --check` and read-only focused reverse-application check each exit **0**. No unclosed fence
  was found. HEAD remains `da66f5d01ba594993974ddd26723e8c300c0a454`, and the index is unchanged.

This PASS accepts only the bounded type-guidance/migration correction. It grants no clean credit to the
edited samples, no new example-compilation credit, no universal allocation result, and no product/API
policy or runtime acceptance. No product/prior-record edit, new tooling file, build/test/compiler/formatter,
native/device/cryptographic operation, Git mutation or subagent action occurred.

Category expansion, final sampling/boundary reconciliation, validation, publication and the precise
deferred-owner handoff remain coordinator gates. Existing deferred qualifications remain intact.
Finish Core Pass 1, then stop; no broader audit or implementation project is started here.
