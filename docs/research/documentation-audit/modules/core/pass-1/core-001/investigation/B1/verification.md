# B1 record verification and exact sampling coordinates

November 11, 2026. A fresh read-only Python reconciliation checked the assigned packet against B1's records, source bytes and pinned Git contents. The record writer only serialized already-completed source dispositions into `investigation/B1/`; it supplied no semantic reading or SDK test result.

The check verified:

- Exactly 1,204 unique original IDs: 1,078 primary and 126 assigned shared units, with no omission/addition.
- Exactly 49 primary paths and 9,747 inventory lines; all 49 have distinct complete-file evidence rows.
- All 81 primary/shared paths match their recorded current raw SHA-256 and the pinned Git content with explicit CRLF/LF normalization. Original inventory source/range hashes equal current hashes.
- Exactly twelve local candidate headings and the same twelve IDs in affected unit rows.
- All fourteen successfully fetched original-source files match their retained hashes; the NIST 403 remains an explicit failed original-PDF fetch.
- Final dispositions: **1,104 READ-CLEAN, 99 READ-CANDIDATE, one READ-EXCLUSION**. Primary: **986 clean, 92 candidate**. Shared: **118 clean, seven candidate, one exclusion**.

An initial record-serialization assertion exposed the inventory's binary-diagram range hashing: it decodes PNG bytes with UTF-8 replacement for administrative coordinates. The writer now reproduces that hash while preserving the original raw binary hash as authority. A line-boundary candidate association was narrowed so the unrelated Usb transmit method remains a clean nominee. These are record-coordinate corrections, rather than product changes.

No C# build, test, formatter, native/device probe, commit/push or index/ref write was performed. Independent review, current consumer compilation and platform/native/hardware qualification remain pending. Coordinator metadata advanced independently; the product source pin remained fixed.

## Exact clean nominees

These IDs are eligible clean source units, with rationale and full caller/error context in the final report's sampling table. A reviewer should read the named unit plus its necessary neighboring ownership path; the list does not replace the coordinator's required stratified sample denominator.

| Original unit ID | Context |
| --- | --- |
| `core-001:unit:434207031d9829c6` | DisposalGate's substantive XML/source-context contract, followed through Run and TearDownAsync |
| `core-001:unit:2b246b861eb2a455` | DisposalGate.Run shared completion publication and inline start |
| `core-001:unit:b119bc4b27c6aa8a` | ISmartCardConnection raw transmit contract and borrowed-input boundary |
| `core-001:unit:b4b6eb9acc9f1458` | HID registered-open claim transfer and pre-cancel ownership |
| `core-001:unit:061986123e3984b8` | External SmartCard factory cleanup/unrecovered contract in source context |
| `core-001:unit:b1a1e66c96c6de98` | Usb transmit's borrowed command and cleared pooled receive staging |
| `core-001:unit:a729caf65ae47857` | Factory policy snapshot test including restoration in finally |
| `core-001:unit:aecf6402749a08f1` | Reader-name cleanup/managed-copy success oracle |
| `core-001:unit:b90961aa2f6e6878` | Listener/connection fake context isolation while native transmit is held |
| `core-001:unit:eecf5130cc1a34d8` | Discovery timeout, retained claim and successful late native cleanup |
| `core-001:unit:adbf7e12c809265a` | Core README sharing migration with direct-factory/process/physical bounds |

The PublicApi compatibility test unit IDs `core-001:unit:4ac2fe58e44afe38` and `core-001:unit:0d5ab92e7b06095a` are additional clean shared nominees. Their `ExerciseReportsAsync` helper was read as dependency context; it asserts report copy/zero and fake receive bytes. None is a native transport acceptance result.
