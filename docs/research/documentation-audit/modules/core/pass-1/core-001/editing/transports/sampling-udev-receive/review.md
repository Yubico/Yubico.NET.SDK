# Independent focused review — SAMPLE-B2-003

## Decision

**PASS — the exact ReceiveEvent XML-only additive patch.** No mandatory correction remains.

Accepted `source.patch` SHA-256:
`9405abcd2f52d438f281b809ef8d4b3f5fbd010cfb3e9d17a51ddc9b1140be86` (1,052 bytes).

Target: `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs`.
Before/current-primary SHA-256: `676d31c9d87d67a9b481082caed6cb06a91576f3120aa3c336d2dd9ded1f2f29`.
Accepted after SHA-256: `27276323b17745eaddd0b16d1030d01304ae6122d49d516795a945620d040445`.
The before identity includes accepted Linux sampling patch `a994f308…`, not the older revision-2
Linux source. This focused acceptance does not replace any preceding patch or broaden the audit.

## Source and meaning

Read the complete corrected ReceiveEvent XML/current method, its surrounding poll/signal context,
the listener's null-result/rescan branch, report/source-evidence records and retained native passage.

- Current XML (`82–91`) correctly describes a missing/invalid monitor or native no-device result,
  without assigning a frequency to possible causes. `ReceiveEvent:248–266` returns null for the
  missing/invalid monitor or invalid native device. It does not inspect errno or collect cause
  frequencies; successful event fields retain their existing copied-result behavior.
- Retained systemd **v257** `libudev-monitor.c:192–249` supports both no-available-event and error
  possibilities. After filtered messages, the helper's zero-time wait can return no further readiness
  and produce `-EAGAIN` (`198–216`). The public wrapper maps negative results to NULL with errno
  (`239–249`); its API comment also documents NULL on error (`220–237`). The retained register/manifest
  binds this original to the v257 URL and SHA-256
  `3f0808560973029612b17d936b61ecfb71388504ef0fc5af401f25cb440d9589`.
  These are API/implementation outcome paths, not a deployed error-frequency measurement.
- `LinuxHidDeviceListener.ProcessUdevEvent:220–233` emits the existing unknown-change rescan hint
  on null. Potential notification loss, including removal, is a conservative recovery rationale;
  a null result does not itself prove a lost notification. The corrected XML preserves that distinction
  and claims no observed event storm, ENOBUFS or dropped removal. No ranking such as “most likely” is
  supported by these sources, and the replacement appropriately removes it.
- Existing attempted-shutdown-wake/no-acknowledgment XML remains exact (`93–101`), as do loop-policy
  fake-test limits and native signaling/wait implementation. This edit changes no rescan, cancellation,
  drain, release or error-classification behavior.

Original selected identity `core-001:unit:82f001f6db22d507` (original 79–86), including its source/range
hash and offsets, matches B2's ledger. Its former status is preserved provenance, not renewed clean
sampling credit. SAMPLE-B2-003 remains a documentation sampling correction, not a newly allocated
unique engineering finding.

## Independent static preservation

Fresh read-only Python/text/hash comparisons found zero discrepancies:

- Saved before text matches current primary bytes exactly and the accepted Linux sampling after hash.
  No primary file was written; the primary remains reserved for its owner through the compile cycle.
- The independently regenerated before/current unified diff exactly matches the accepted patch.
  Removing only the ReceiveEvent documentation unit leaves identical full before/after text, including
  the no-wake-ack qualification. The declaration, all member/ordinary comments outside that XML block,
  executable code, imports, directives, defaults, attributes, assertions, strings and APIs are unchanged.
  BOM/final-newline state is preserved; no example is added or changed.
- The other **212 eligible source files** match accepted revision-2 identities with the already
  accepted Linux sampling and macOS seam corrections overlaid. All **47 protected prior records**
  and the four preceding patch identities remain exact, including prior decisions, holds and examples.
- The source-evidence hashes and all six editor records listed by the return manifest match.

This PASS is content/source acceptance plus bounded static preservation, not compiler, tokenizer,
API-binding, native/deployment or device qualification. The editor's recorded XML/Git checks are not
represented as newly executed reviewer checks. No SDK build/tests, native/device/SDK-crypto operation,
formatter, Git command/mutation, subagent, new tooling or broader audit occurred. Only this review and
`review-identities.json` were written; product/editor records and primary source were untouched.

Existing held implementation and runtime/native/physical/producer qualifications remain unchanged.
The sampler/coordinator owns exclusion, quota, category expansion and sampling credit; none is assigned
by this review. Complete existing coordinator validation, Core Pass 1 publication and the deferred
finding handoff, then stop; no new implementation or campaign is authorized.
