# Transport documentation revision 2

## Outcome and identities

Applied the three mandatory documentation corrections from `../review.md` in exactly four source
files. Current control flow supports M1, M2 and M3; no contradiction was found. Other accepted content
is unchanged. This successor needs a focused independent re-review, not a whole-packet re-audit.

- Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`.
- Base and observed HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
- Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
- Original full patch, preserved byte-exact at `../source.patch`:
  `86b0c6ed78d5d1c3cf2cd68ec4b529ad8903e9b0ad3258dfa8b2bd7cb1656ef6`.
- **Successor full patch:** `source.patch`, SHA-256
  **`109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`**.
- Focused delta from the reviewed source: `focused-review-delta.patch`, SHA-256
  `b4f3ce617eba070cd6d56ccd9aedf89a310ca1fb8f7b688638638345ae0c48f2`.

The successor full patch contains the complete B1–B4 C# documentation diff against the named base,
not just the four-file correction. Primary A/C integration is outside this revision's source baseline.
Use the revision-2 identities for the current source; the original editor/review records remain
historical inputs and are not rewritten to describe the successor.

## Verified corrections

### M1 — Property queries do not require an open report connection

`src/Core/src/Transports/Hid/MacOS/IOKitHelpers.cs` now describes both `device` parameters as a valid
IOHIDDeviceRef that the caller keeps valid during the query. An opened report connection is not required.
This describes reference lifetime, not a new requirement to call CFRetain separately for each query.

The decisive source is `MacOSHidInterface.GetList`: it creates a manager, obtains the copied device set,
reads each device's properties, and releases manager/set references in its enclosing finally. It does
not open each device before those queries. The helpers themselves query IOHIDDeviceGetProperty without
opening a device. Retained Apple `IOHIDDevice.h:87–102` distinguishes open before state-changing commands;
it is not evidence that every property query requires an open report connection.

The existing absence, mismatched-type and lossy/out-of-range conversion descriptions remain intact.
The separately held nullable-manager/set and CFNumberType ABI findings remain unresolved.

Mapping: **CORE-B3-C003 / FPT-042**; original units
`core-001:unit:cef6b843926775c3` and `core-001:unit:549db4976f21f028`.

### M2 — Metadata retrieval includes the access fallback

`src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs` now states that constructor metadata
retrieval first tries zero desired access, then retries with GENERIC_READ | GENERIC_WRITE if opening
the handle or retrieving capabilities throws UnauthorizedAccessException. Constructor success alone
does not identify the successful access route. Neither metadata route performs feature GET/SET.

The complete `OpenHandleForMetadata` flow confirms the distinction: its inner catch disposes the first
handle before rethrowing a capability-query exception; the outer exception filter selects only
UnauthorizedAccessException for `OpenReadWriteHandle`. Other exception types do not select this retry.
The retry has its own capability query and failure cleanup. Retained CreateFile desired-access/share
contracts and HidD_GetFeature buffer/operation contracts support the API distinction, not a claim that
either route works on a particular deployment.

OpenFeatureConnection's separate zero-access choice, environmental and historical qualifications,
all access flags and executable guidance are unchanged.

Mapping: **CORE-B4-C004 / FPT-051**; original units
`core-001:unit:97f3c89d0819e76e` and implementation dependency
`core-001:unit:3ac34ce64d139f54`.

### M3 — Listener prose describes cleanup attempts, not release proof

`src/Core/src/Transports/Hid/HidDeviceListener.cs` now describes Stop as a shutdown/monitoring-resource
cleanup request. Dispose(bool)'s summary describes managed disposal state as part of the platform
cleanup attempt; its adjacent remarks identify unresolved native cleanup and the limits of a returned
call or diagnostic status.

`src/Core/src/Transports/Hid/Windows/WindowsHidDeviceListener.cs` now says that the finalizer can attempt
unregister when the weak target has been collected. Dropping that hint does not establish successful
unregister. The existing weak-root and retained-state safety rationale is preserved.

Current Windows Stop/finalizer paths discard CM_Unregister_Notification status before clearing callback
state. The retained Microsoft reference distinguishes CR_SUCCESS from CR errors and describes the
pending-callback wait and callback-thread deadlock restriction. Actual successful unregister is
therefore distinct from invoking it. The unchanged macOS partial-start flow can retain an ambiguously
registered generation without a started thread; Stop can return without cleanup on that path. These
are existing implementation findings, not newly repaired shutdown behavior.

Mapping: **CORE-B4-C002 / FPT-049**; original units
`core-001:unit:6f60e2b189737226`, `core-001:unit:c40476271e99068b` and
`core-001:unit:8aa16b126b5c3c3b`. Supporting macOS evidence remains
**CORE-B3-C005 / FPT-044**, unchanged and nonadditive.

## Successor records and preservation

| Record | Purpose |
| --- | --- |
| `review-inputs.json` | Exact original editor/review hashes, reviewed 213-path identities and four pre-correction source texts |
| `source.patch` | Complete successor primary C# patch against the named base |
| `focused-review-delta.patch` | Only the bounded changes since the reviewed patch |
| `current-path-hashes.json` | Current raw/LF hashes for all 213 eligible paths, with base, pin and reviewed identities |
| `file-dispositions.json` | Full successor file decisions; four edited-path explanations updated |
| `proposal-dispositions.json`, `revision-corrections.json` | Revised candidate/path decisions and exact original review/unit/proposal mappings |
| `example-bodies.json`, `example-identity-check.json` | Byte-exact prior inventory and verification that all three current bodies retain their identities |
| `focused-citations.json` | Reused retained Apple/Microsoft authority identities and bounded current-source rationale |
| `xml-check.json`, `static-preservation.json` | XML syntax and static preservation results with explicit limits |
| `prior-record-preservation.json` | Byte-exact preservation of all ten existing editor/review records, including the original patch |

All 213 paths retain their original decisions: **62 EDIT, 135 RETAIN, 16 HOLD**. Only four current source
hashes change from the reviewed edit. The other 209 source files and every held-file decision object
remain unchanged. The other 33 proposal records are unchanged; only the three mapped proposal records
receive revision-2 notes. The original shared/external follow-ups and evidence-dependent holds remain
authoritative; this bounded correction closes none of their implementation or qualification gaps.

The three documentation example bodies are unchanged, including their complete resource scopes and
original body inventory. Compilation remains **VALID-NOT-CHECKED**; examples remain **NOT-EXECUTED**.
XML checks cover comment-fragment well-formedness, not cref binding, analyzers or compilation.

Static preservation compares verbatim nonblank lines after excluding only full-line `//` comments,
with explicit BOM/newline handling. Inline comments, block comments, executable tokens, declarations,
directives, imports, defaults, attributes, assertions and runtime strings stay in that comparison.
The focused delta contains ordinary/XML comment changes only. BOM and final-newline policy are preserved.
Fresh final check results and both patch identities are recorded in `static-preservation.json`.

No original editor/review record, retained authority, primary-checkout file, shared guide, external C#
file, configuration or snapshot was rewritten. No SDK build, tests, formatter, native/device/crypto
execution, Git index/ref mutation or subagent ran. New records are confined to this home-backed
`revision-2` directory; Git use is read-only static inspection.

## Focused re-review handoff and stopping point

Bind the successor hash above, then read the complete four affected documentation units and these
specific dependencies:

1. **M1:** both helper parameter blocks and existing outcome XML; compare the unchanged complete
   MacOSHidInterface.GetList/helper query flow and retained Apple open contract.
2. **M2:** OpenFeatureConnection's complete remarks plus the constructor, OpenHandleForMetadata,
   exception filter, capability query and read/write-open path. Confirm the first-attempt/retry
   distinction without implying feature transfer or deployed access success.
3. **M3:** Stop and Dispose(bool) summaries/remarks, the collected-target callback branch, Windows
   Stop/finalizer unregister paths, and the unchanged macOS no-worker retention path. Confirm request/
   attempt wording while preserving safety rationale and implementation findings.

Independent successor content acceptance and coordinator-owned existing validation gates remain pending.
No whole-packet re-audit or new framework is requested. Finish Core Pass 1 publication, provide the
deferred finding handoff, then stop; no Pass 2 implementation or native/device campaign is authorized.
