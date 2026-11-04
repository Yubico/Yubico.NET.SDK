# Final boundary correction acceptance

**Verdict: PASS for the inspected CTAP ↔ WebAuthn documentation boundary at the hashes below.**
BF-R3 is corrected and independently re-reviewed; prior BF-R1/BF-R2 and authorized BF-O1 acceptance
remains valid. **No required boundary documentation corrections remain.**

**2026-11-04. Reviewer:** GPT-6.1 Sol, same independent final-boundary reviewer, not Astra/editor.
**PRIMARY:** `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
**Unchanged baseline/HEAD:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

Read Astra's explicit BF-R3 ownership/correction in `correction-boundary.md`, complete Known Gotchas
paragraph/list at `src/WebAuthn/CLAUDE.md:540–554`, prior follow-up's original P4 evidence and the
complete CTAP outer registration decoder/remarks at `PreviewSignCbor.cs:220–267`. Applicable root/
module/canonical workflow/style and verification guidance remain as read in this review session.
This is the requested narrow final correction review, not another whole-WebAuthn audit.

## BF-R3 — corrected / PASS

`src/WebAuthn/CLAUDE.md:544` now states:

> **previewSign registration flags belong to the inner attestation** — The draft's signed registration
> extension value supplies the selected algorithm at key 3. Policy flags belong inside the generated-key
> attestation. The CTAP decoder's tolerated outer key 4 is not a standard optional field or evidence of
> a beta/device variant.

This accurately separates the vendor draft from the SDK decoder's tolerated shape. Already independently
checked original [P4/V4 §10.2.1](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension)
places flags **only** in the attestation embedded in att-obj; its ordinary signed outer registration
output carries algorithm key3. Actual `PreviewSignCbor.DecodeRegistrationOutput` requires algorithm,
tolerates flags key4 and returns a nullable outer flags value. No inner-policy verification, beta
hardware evidence or universal optional-field contract follows. The surrounding map-selection,
context-specific key7, UP/UV and session-notification guidance is retained unchanged.

No parser/output-policy/API change was requested or made. CTAP-IMP-038 and WA-IMP-006/019 and the
related profile/algorithm/ownership decisions remain deferred. Original P4 evidence is reused and
re-read, not represented as a new remote retrieval or device observation.

## Exact final three corrected-path hashes

SHA256 hashes exact PRIMARY file bytes **before the coordinator's later formatter application**.

| Corrected path | Final accepted SHA256 |
| --- | --- |
| `docs/architecture/webauthn-extension-processing.md` | `56ce5bf608fa1452402100b3d6e51af6b8a3b8277d0f3ada1e8b4b342bffe09c` |
| `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` | `2ed3b03efcb3e82fd58980b91518abe0638b064207e7ccae025e920aa2a30fb7` |
| `src/WebAuthn/CLAUDE.md` | `1d47c824d5c7eb16e7e0ec18f64300c621f542a7bd19d9d2d4bdeea8495918e9` |

Fresh read-only Python compared all28 source/claim hashes from the preceding boundary-final snapshot:
**only WebAuthn CLAUDE changed; other27 match**, preserving BF-R1/BF-R2/BF-O1 acceptance. Reversing
only the exact Known Gotchas item3 replacement **in memory** reproduces prior CLAUDE SHA256
`b058a8b1089549410867550550d24ab7c4540b76a3c47aaa08af995c6b56fe2a`. Thus no other guide bytes
changed in this correction. Original G map remains SHA256
`097c23d16d9f72f16a0a05db730b5bee4a5e0fda6a6d872fe59e2e668c713c36`; HEAD remains baseline.

Only this final report and current dispositions/hashes in `boundary-final.md` were authored. Original
G map, initial review and earlier follow-up remain preserved as their historical checkpoints.
No source edits, builds, tests, hardware, commits, delegation, formatter application, image rendering
or compiler/token gate occurred here. **Format verification is not a review PASS claim.**

WA-XREF-001's documentation dependency remains independently satisfied; all original WA engineering
IDs and DEC-002 remain open. COORD-001 wire-presence caveats, legitimate higher-level policy differences,
experimental/profile/production warnings, sequencing, ownership, cancellation-success race and
no-rollback limitations remain as assessed in boundary-final. This is not implementation completion,
whole-WebAuthn re-audit, hardware qualification, generated-image acceptance or authorization for Pass2.

Coordinator owns later formatting/import/EOF changes, exact post-format manifest and separate body/
directive/API/token/compiler/example/image/remote gates. If those changes affect meaning or behavior,
reopen the precise affected claims; byte-only final snapshot changes still require updated identity
records, not silently treating these pre-format hashes as the eventual committed bytes.
