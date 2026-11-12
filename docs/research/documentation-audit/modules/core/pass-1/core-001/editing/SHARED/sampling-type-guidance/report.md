# Bounded type-selection and migration sampling correction

This documentation-only successor addresses **SAMPLE-B4-001**, selected original unit
`core-001:unit:001c58b1d1862d76`, and **SAMPLE-B4-002**, selected original unit
`core-001:unit:a9e434874ba55d57`. The coordinator owns edited-unit sampling exclusion and any expansion;
this return claims no clean sampling credit or independent acceptance.

Only root `CLAUDE.md` and `docs/v1-to-v2-comparison.md` change. Both exact before-files were checked
against primary current accepted bytes before editing. Original SHARED/revision-2 patches, editor and
review records, and the separate lifecycle sampling correction remain unchanged, including focused
patch `37074361c54bb2232ff8406ae37af482a73c51b029a3dfd8328e467f697e274e`.

## Source verification and bounded changes

### SAMPLE-B4-001 — allocation, boxing and shallow immutability

Context7 `/dotnet/docs` was queried for the readonly-struct reference-member note and generic collection
boxing guidance. The named Microsoft documentation sources were read/captured in this correction's
directory; exact retrieval identities and relevant text are in `source-evidence.json`.

- `docs/csharp/language-reference/builtin-types/struct.md`, readonly struct section: read-only struct
  state does not prevent mutation of referenced mutable objects. Value-type storage is not a universal
  stack-allocation or zero-GC guarantee.
- `docs/standard/collections/when-to-use-generic-collections.md` and `docs/standard/generics.md`:
  generic collections can store value types without the per-element boxing of object-based collections.
  `List<T>` and `T[]` value-type elements are unboxed; object/interface conversions and non-generic
  collection use can box. The container/backing array can still allocate.

The project preference for small value types and immutable data stays intact. The prose now separates
that preference from storage/boxing behavior and shallow readonly guarantees. The retained 16-byte
comparison explicitly means two **64-bit** words, not two words on every architecture. The nearby
decision matrix's allocation/readonly rationale receives the same qualification instead of retaining
contradictory blanket cells. All example bodies are byte-identical; no architectural/type/API policy is
added and no experiment or benchmark ran.

### SAMPLE-B4-002 — narrower retired extension surface

Current SDK declarations were checked directly: `RawSmartCardSession`, `RawCtapHidSession`,
`RawOtpHidSession`, `IConnection` and `ISmartCardConnection` are public; `Tlv`, `TlvHelper`, `Base32`,
and selected `CryptographyProviders` helpers are public. `ProtocolFactory` and `IProtocol` remain internal.
Public versus internal provider creator members are separate. Exact source paths/identities and
declaration excerpts are in `source-evidence.json`.

The migration introduction now refers to retired extension points and internal protocol composition,
while acknowledging the public raw and selected utility/crypto surfaces. The specific removed-API
table and its rationale, public Base32 decoding pointer, and unrelated migration text remain unchanged.
No API or runtime behavior changes.

## Transfer and verification artifacts

| Record | Purpose |
| --- | --- |
| `before/CLAUDE.md`, `before/v1-to-v2-comparison.md` | Exact primary-matching accepted before-files. |
| `after/CLAUDE.md`, `after/v1-to-v2-comparison.md` | Complete current after-files. |
| `product.patch` | Focused two-document before-to-after patch with conventional a/b prefixes. |
| `changed-path-sha256.json` | Exact before/current/patch hashes and original sample IDs. |
| `source-evidence.json`, `sources/*` | Named Microsoft source captures and SDK declaration identities; source evidence, not execution. |
| `example-bodies.json` | Complete unchanged example bodies and before/after boundaries/hashes for both documents. |
| `preserved-record-identities.json` | All prior SHARED artifacts and original editor report, preserved byte-exact. |
| `verification.json` | Two-path scope, previous other product paths and lifecycle correction preserved, complete examples/table unchanged, fence/local-target/whitespace/patch checks. |
| `output-identities.json` | New artifact hashes, excluding itself. |

No C#, product runtime/config/script, other checkout, original editor/review artifact, central sampling
record or API snapshot is edited. No build/test/formatter/compiler/native/crypto/device operation,
Git index/ref/commit/push or subagent runs. Source/document checks supply no platform or device
qualification. Existing deferred findings remain unchanged. Complete focused independent review,
Core Pass-1 publication and the deferred handoff, then stop; no broader guide audit or new project follows.
