# Transport revision 2 — focused independent re-review

## Decision

**PASS — content and citations for the complete successor 62-file B1–B4 documentation batch.**
M1, M2 and M3 are resolved. No mandatory correction remains in this review's scope. Acceptance
combines the prior independent full-batch review with the four-file correction and verified
preservation of the other 209 eligible files; it is not a new whole-packet audit.

- Base/observed HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
- Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
- **Accepted full successor patch SHA-256:**
  `109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`.
- Accepted focused-delta SHA-256:
  `b4f3ce617eba070cd6d56ccd9aedf89a310ca1fb8f7b688638638345ae0c48f2`.
- Accepted complete 213-path identity ledger, `current-path-hashes.json`, SHA-256:
  `0d440261ba1d64ab54bc415689a3f42bb8a199703e0c53cf9c3738f52d69f2e5`.

The successor full patch was independently compared byte-for-byte with the current Core Git diff
against the named base. It contains the complete 62-file edit, not merely the focused correction.
The predecessor patch and review remain immutable historical inputs; this PASS applies to the
successor identities above, not retroactively to the predecessor's unresolved wording.

## Closed mandatory corrections

| Review item | Accepted current unit and candidate | Rechecked evidence and conclusion |
| --- | --- | --- |
| M1 | `src/Core/src/Transports/Hid/MacOS/IOKitHelpers.cs:26–108`; CORE-B3-C003 / FPT-042 | Both device parameters now require a valid caller-maintained IOHIDDeviceRef during the query and explicitly do not require an opened report connection. Complete helper flow queries the property without opening. Unchanged `MacOSHidInterface.GetList:58–109` queries properties before report open and retains manager/set ownership through its finally. This does not require a separate CFRetain for every query or repair the held nullable-set/CFNumberType findings. |
| M2 | `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs:76–101`; CORE-B4-C004 / FPT-051 | The remarks now name zero desired access first, then the read/write retry when opening or capability retrieval throws UnauthorizedAccessException. Constructor success is not zero-access success proof. Rechecked constructor, GetCapabilities, both open routes, exception filter and cleanup (`38–49,183–288`): first-handle capability failure disposes then rethrows; only the selected exception enters the read/write retry; retry failure has its own cleanup. Neither metadata route performs feature GET/SET. Separate feature-open flags and environmental/historical qualifications are unchanged. |
| M3 | `src/Core/src/Transports/Hid/HidDeviceListener.cs:69–85,127–149` and `Windows/WindowsHidDeviceListener.cs:151–178`; CORE-B4-C002 / FPT-049 | Stop describes a shutdown/resource-cleanup request; Dispose(bool) describes managed disposal state and explicitly qualifies unresolved native cleanup. The collected-target callback now says the finalizer can attempt unregister, not that it succeeds. Unchanged Windows Stop/finalizer paths still discard unregister status (`129–149,280–315`). Unchanged macOS `StartGeneration`/Stop (`74–118`) can retain an ambiguously registered generation with no started worker. These remain implementation findings, with CORE-B3-C005 / FPT-044 as nonadditive supporting evidence. |

Original unit identities remain M1 `cef6b843926775c3`, `549db4976f21f028`; M2
`97f3c89d0819e76e`, `3ac34ce64d139f54`; M3 `6f60e2b189737226`, `c40476271e99068b`,
`8aa16b126b5c3c3b`, each with the unchanged `core-001:unit:` prefix. The successor records preserve
their candidate/proposal identities and do not allocate new accepted engineering findings.

## Focused authority check

Reused and re-read the exact retained originals; no new research or library-API question was needed:

- Apple IOKitUser `323ead896d04424f87184d8f6ff0cce811aab106`, `IOHIDDevice.h:87–102`: open is
  required before commands changing device state. This native-caller contract is not an open-report
  prerequisite for the local property-query flow. SHA-256:
  `64aad6babb2e4bbbf31386f54c658fd16fbaa1aa271fb8f396126c22d6f833bd`.
- Retained Microsoft CreateFile reference, `dwDesiredAccess`/`dwShareMode`: zero access permits certain
  metadata queries, with access/share compatibility conditions. SHA-256:
  `b946f24eb9b62fbb37093e74741e9bc3aad5c96cf82d6ed552ef716118d248eb`.
- Retained HidD_GetFeature reference, Parameters/Remarks: feature retrieval is a separate operation
  on a collection handle using the caller's report buffer. SHA-256:
  `744407c7ab7fe1d1cecfe17229e8976ec4629738891d2a9a4107f38511cb2684`.
- Retained CM_Unregister_Notification reference, Return value/Remarks: CR_SUCCESS differs from CR
  errors; unregister waits for pending callbacks and must not run inside the notification callback.
  SHA-256: `eacdd31a133642644779abe5a7144213e06e25591a02fa528a4c4913bf92092e`.

The Microsoft captures remain the prior review's retained live Win32 reference snapshots, not
version-pinned deployed-driver observations. Their authority is native API contract; the SDK's
actual retry and ignored-status behavior comes from the current source, not an external guarantee.
All four original hashes match `focused-citations.json`. None supplies a new access, pre-open,
unregister, callback-drain, release or physical-device execution result.

## Independent preservation checks

Static read-only reconciliation completed with zero discrepancies:

- All **213 current raw and LF hashes** match the successor ledger and file decisions. Decisions
  remain **62 EDIT / 135 RETAIN / 16 HOLD**.
- Exactly the four files above differ from the predecessor's reviewed hashes. The saved four
  predecessor texts match the original file-disposition identities. The focused diff independently
  regenerated from those texts exactly matches `focused-review-delta.patch`.
- The other **209 source files and their complete decision objects are unchanged**. All **16 held
  decision objects** remain exactly equal to their predecessor objects, with no evidence hold closed.
- Only FPT-042, FPT-049 and FPT-051 proposal objects differ. Their changes are the correction notes,
  local review markers and revision status; the other **33 proposal objects** are exactly equal.
- All **ten prior editor/review records** retain their exact prior hashes, including the original
  patch and both independent-review records. The eight predecessor input hashes also match the prior
  review's own identity record; its two review-file hashes match the identities returned by that review.
- The complete example inventory is byte-identical to the predecessor. Static extraction across all
  213 paths still finds exactly **three current XML code bodies**, equal to their recorded full bodies.
  Their real-API, typed-prerequisite and complete resource-scope classifications carry forward.
- Verbatim nonblank lines excluding only full-line `//` comments match the base across all 213 files
  after explicit BOM/newline handling. The focused four-file comparison also preserves those lines
  against the reviewed texts. Inline/block comments remain in the comparison. BOM and final-newline
  state are preserved. No executable, assertion, import, directive, declaration, default, attribute,
  runtime-string or API-identity change was found by this bounded text comparison.
- All 14 successor records listed in `return-identities.json` match their hashes. No editor record
  was rewritten by this re-review.

These are static identity/preservation checks, not a C# tokenizer/parser, API-binding check, compiler
result or substitute for coordinator validation. No broad source re-audit or new tooling was added.

## Qualification and handoff

This PASS is **documentation content/citation acceptance only**. Combined compiler, token/API and
example validation remain coordinator-owned and pending. Examples remain **VALID-NOT-CHECKED** for
compilation and **NOT-EXECUTED**. No SDK build/tests, formatter, native library/device operation,
cryptographic SDK operation, Git mutation or subagent ran. Only these two owned successor review
records were written.

Every prior physical, producer, deployment, historical and platform qualification remains held:
NIST original-PDF gap versus the separately retained PC/SC specification; requested UNPOWER versus
observed power/PIN-state clearing; conditional Linux LP64 PCI applicability versus Windows/macOS;
IOReturn/CFNumberType implementation findings; source versus deployed NativeShims producer proof;
Linux native-only versus physical results; historical macOS profiles and serial-9681620 cause.
The future Windows concurrency delta remains unready and unreviewed.

Proceed through the existing coordinator gates for this exact successor batch, publish Core Pass 1,
provide the deferred finding handoff, then stop. This acceptance authorizes no Pass 2 implementation,
new native/device campaign or additional planning project.
