# YubiHSM Auth yubihsm-001 closeout

## Scope and outcome

Pass 1 from `a74792facb657413e98223e059876d8b213ffff3` on `yubikit-docs-audit`. All **56 tracked module
paths**, **eight effective public types** and **83 API snapshot entries** have semantic dispositions.
Independent review accepted **35 authored paths**: 30 module paths and five scoped shared-document
sections. The other 26 module paths preserve the initial working bytes. Generated record/extension
representations and interface duplication are reconciled rather than counted as independent prose tasks.

Fresh Sol investigation, editing, original-citation/content review and late hardware/formatter follow-up
are complete. Astra triaged findings, sampled clean units, inspected probe sources, executed validation,
coordinated touch and preserved evidence. The user-approved affirmative style was included in every task
packet and independently accepted: behavior and actor first, useful conditions preserved, negatives used
where the reader needs a specific consequential limitation.

Completed improvements cover matching HSM enrollment, distinct secret roles and identity mapping,
recoverable management-key rotation, typed application-dependent handshake responsibilities, credential
retry recovery/deletion versus management blocking, actual typed-error coverage, firmware policy,
owned/borrowed key material, metadata equality/ordering, presence/cancellation, diagnostic-tool behavior
and assertion-based test explanations. Full resource/enrollment scopes remain alongside API contracts.

Current gate state: source/API/formatter checks, clean solution gates, both example builds and all planned
device execution have completed. Shipping compiler capture and final live checks passed after the narrow
SDK-default correction. Fresh [final evidence review](review-closeout.md) passed with zero required
corrections. Manifest freeze and publication are the remaining coordinator steps.
The external [handoff](../../../../yubihsm.md) will record
verified publication without rewriting the frozen run to embed its own eventual commit hash.

## Engineering handoff

The twelve original candidates plus the late hardware discovery map to **eight deferred engineering
groups and three Core dependencies**, each with implementation paths and documentation-reopening triggers
in [findings](findings.md). Core implementation, dedicated applet boundary reviews and Pass-2 changes
remain scheduled for the final Core cycle and later campaign sweep.

The consequential new **HSM-IMP-008** is a confirmed supplied-cryptogram response/parser mismatch on
serial 103: the SDK expects 48 bytes and receives 56 after a valid laboratory symmetric card cryptogram.
Vendor Python has handled 56 bytes since 2023, while vendor C suppresses the eight-byte symmetric
cryptogram and requires 48. Original code/history support the compatibility caveat; actual byte-region
meaning, versioned normative framing, firmware range and real HSM interoperability remain open.
The bounded Pass-1 investigation stops with truthful documentation and a precise implementation return.

Other groups cover generic GET_CHALLENGE retry errors, sample exceptional cleanup/input/output policy,
unknown/malformed metadata parsing, symmetric-only tool dispatch/prompt integration, test oracles and
cryptographic input/response validation. The initial incorrect test-guide broken-link subfinding was
rejected by editor, reviewer and coordinator; its original report is retained as historical evidence.

## Verification

- **3,998 unit cases passed**, including **91 YubiHSM**, five existing skips, zero compiler/analyzer
  warnings/failures. Clean solution, docs/package QA, architecture and complexity gates passed.
- **13 existing integration methods passed** in two invocations. The two touch-tagged cases assert stored
  metadata; a separate successful human-touch CALCULATE verified an actual operation and key outputs.
- **Four final scoped probe lanes passed:** omission-only independent three-key calculation and retries
  8→7→8; user/admin password transitions with new-password key equality; measured 6982/Failed after
  cancellation followed by fresh-session recovery before reset; human touch with one PolicyRequires/
  Completed lifecycle and independent three-key equality.
- Original v1 supplied-cryptogram and v2 cancellation-expectation attempts **failed** and remain retained.
  Later omission/measurement probes isolate other behavior; they preserve the original unresolved gaps.
- Independent vendor-client final reset/default-key PUT/LIST/DELETE/fresh connection verified **empty
  applet, eight retries and usable default management key**. Initial credential was erased by the approved
  test reset. Readable device metadata matches initial inventory; all client operations drained.
- **26 edited C# paths** preserve executable bodies/directives; four explicitly accounted unused imports
  in three files, one import-group/order change and EOF normalization are the formatter-only differences.
  All **20 API snapshots** unchanged; all 35 accepted hashes bind through transforms to final content.
- Final independent clean sampling **3/17** passed across API, example and internal/test categories.
- **Eight complete consumer groups and the entire 22-source HsmAuthTool built warning-free.** Actual
  compiler-source/reference/analyzer/config/asset evidence and final live checks passed; all 407
  reference/analyzer identities reconcile. Full bodies and genuine compilation contexts are retained.

Hardware scope: serial **103**, USB SmartCard, Linux/Omarchy, metadata **5.8.0.beta.0**, direct SDK and
vendor-Python session **0.0.1**, ykman CLI app display **5.8.0**. The version observations keep their
respective meanings. Live HSM/connector, raw 56-byte regions, older firmware, NFC, other platforms and
Native AOT runtime remain outside this qualification. Cancellation measurement records the terminal
error/drain/recovery; native admission/interruption and touch-timeout interpretation remain unverified.

## Evidence and preservation

See [validation](validation.md), [hardware results](hardware/results.md), [sampling](sampling.md),
[coverage](coverage-final.json), [examples](snippet-validation.md), [independent review](review.md) and
[follow-up](review-followup.md).
Original sources, failed commands, pre-format/full probe sources, compiler contexts and specific
corrections are preserved with their own identities. Hardware capture's initial empty Csc arguments,
formatter JSON-schema mismatch, vendor-helper password padding failure and SDK-default-hook assumption
retain original evidence and narrowly corrected successors.

Previous **2,317 archived artifact identities** and the four original untracked user files remain
preserved. This run's required logical records are combined where useful. Generated binaries/caches
stay outside the frozen artifact set; captured text/configuration/provenance remain durable. A final
manifest and remote backup complete the cycle; status.md is authoritative until the external handoff
records publication. Re-baseline stable finding IDs before future implementation work.

## Lessons carried forward

Use independent cryptographic comparisons in addition to length/metadata assertions. Preserve actor and
version distinctions, and make successful retry recovery explicit. Match response parsing to verified
protocol variants before claiming interoperability. Test the actual touch operation separately from
stored policy. Capture Csc evidence immediately after real compilation and distinguish SDK built-in
hooks from custom hooks. Resolve links from the containing directory. Retain failed expectations beside
later observations, with clear current status and implementation-reopening triggers.
