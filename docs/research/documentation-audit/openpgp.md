# OpenPGP documentation handoff

**Pass 1 is complete and published** as [openpgp-001](modules/openpgp/pass-1/openpgp-001/plan.md).
Baseline: `fe180282a2d77b15eaaa5868d6c8b00e251d1f05`. Documentation and complete evidence committed
and pushed as **`84715ee1cbd45d885417ddc8b7dcb9205784f6ef`** on `yubikit-docs-audit`; local and
GitHub branch HEAD matched on 2026-11-04. Fresh final evidence review passed with zero required corrections.

The [frozen status](modules/openpgp/pass-1/openpgp-001/status.md) preserves the prepublication checkpoint;
this external handoff supersedes its publication-pending next step. This publication-only checkpoint
updates the handoff and campaign index while preserving accepted product content and frozen run bytes.
The campaign branch remains published without a PR or merge.

Whole-module scope includes public contracts, guides, internal/example/test explanations and relevant
shared OpenPGP claims. Sol 6.1 investigated, edited and independently reviewed; Astra coordinated and
validated. User authorized connected-key tests and configurations. Python YubiKey Manager and its history
provided corroborating references for behavior and firmware gates. The accepted house style emphasizes
affirmative, task-oriented prose with useful context.
Implementation follow-ups accumulate for Pass 2; Core dependencies are reserved for the final Core cycle.

## Verified outcomes

- **96 module paths, 39 public types, 423 API entries and 40 interface operations** reconciled;
  **59 authored paths** accepted after independent content/citation review and correction re-review.
- All **49 edited C# paths** preserve executable bodies, directives and imports; formatting changed
  whitespace only. All **20 API snapshots** unchanged. Package metadata changed only its Description.
- Expanded clean sampling **11/32 passed**, including eight API units after corrected PinPolicy and
  timestamp misses. Original findings and their corrections remain visible.
- Clean solution gates passed **3,998 unit tests**, including **186 OpenPGP**, five existing skips and
  zero compiler/analyzer warnings or failures. Documentation/package, architecture and complexity passed.
- **17 complete example groups and the full 19-source OpenPgpTool** compiled with actual Csc evidence;
  final live checks reconciled **407 reference/analyzer entries across two contexts**. The separate
  407-unit documentation inventory has explicit dispositions.

All **49 existing integration methods passed**. Nine supplemental methods retain five successful measured
outcomes and four contract failures; independent same-key X25519 and correct-CRT ATT references passed.
All original setup/mode/touch-window failures and the interrupted initial suite invocation remain
preserved. Final independent restoration matches every initial public-state field, including Att
certificate identity and **all four UIFs Off**. Final readback verified empty normal key/certificate
slots, retries **3/0/3**, usable factory credentials, disabled KDF and initial configuration. Att On
survived reset and was explicitly restored to Off; the existing Att private key was never replaced.

Device scope: serial **103**, USB SmartCard/Linux, metadata **5.8.0.beta.0**; SDK/direct Python **0.0.1**
and CLI application **5.8.0** remain distinct observations. Successful attestation verifies issuer
signature and generated-key binding, with manufacturer-root trust assessed separately. Cancellation
returned `6600`/`Failed` before fresh-session cryptographic recovery; native interruption and timing
causality remain unverified. NFC, older firmware/families, other platforms and AOT runtime remain open.

## Return-visit records

- [Findings](modules/openpgp/pass-1/openpgp-001/findings.md): **14 deferred engineering groups and four
  Core boundary questions**, with exact implementation paths and documentation-reopening triggers.
- [Closeout](modules/openpgp/pass-1/openpgp-001/closeout.md): scope, completed gates and lessons.
- [Hardware results](modules/openpgp/pass-1/openpgp-001/hardware/results.md): all attempts, qualification
  limits, same-key/correct-CRT comparisons and complete final restoration.
- [Validation](modules/openpgp/pass-1/openpgp-001/validation.md) and
  [examples](modules/openpgp/pass-1/openpgp-001/snippet-validation.md): exact source/compiler/gate evidence.
- [Final independent review](modules/openpgp/pass-1/openpgp-001/review-closeout.md): prepublication PASS.

Hardware confirmed **PGP-IMP-001** (extended User PIN salt/retry identity), **PGP-IMP-007** (X25519
decipher framing) and **PGP-IMP-013** (ATT public-key CRT). These defects remain documented and unfixed.
PGP-C022's timestamp semantics extend PGP-IMP-018. The 21 original PGP-DEP labels describe documentation
dependencies rather than extra Core bugs. Re-baseline every selected item before the implementation sweep.

## Publication staging evidence

The [manifest](modules/openpgp/pass-1/openpgp-001/record-manifest.json) freezes **607 artifacts**
(606 text files and one original PDF), **10,666,111 bytes**, excluding itself. The first strict
index-versus-working-byte check stopped at
`src/OpenPgp/src/Yubico.YubiKit.OpenPgp.csproj`: repository attributes specify `text` and `eol=crlf`,
so Git stages LF-normalized text. This is the sole publication byte-normalization exception:

| Representation | Bytes | SHA256 |
| --- | ---: | --- |
| Accepted/compiled working file (19 CRLF,20 LF) | 864 | `d150bc39c7c4545e4cf9b87ed2ad25e967c6116211d66f6026638306a0ac68af` |
| Staged Git text (0 CRLF,20 LF) | 845 | `95796651ce74b03ad736c901cc4dfbe993bccbd45528d97ffd83f96674f94540` |

Replacing CRLF with LF yields exact equality; the accepted Description remains the only semantic
project change. Full original working project bytes remain in compiler source/config snapshots within
the frozen archive. No product or archive bytes were changed to accommodate staging. The subsequent
index check passed with exactly this named/path/hash/attribute normalization and exact bytes
for every other staged file. Fresh-checkout validation must account explicitly for repository line-ending
conversion while retaining the recorded original compiler-input identity.

All **669 content-commit paths** were explicitly staged: 668 matched working bytes exactly, with the
single normalized project above. All frozen archive paths—including the PDF and original compiler-input
snapshots—matched Git index bytes exactly. Manifest/path/hash/size, whitespace, 14 final-review identity
anchors and previous archive/user checks passed. All **2,794 earlier archive identities** and four
original untracked user files remain unchanged. The tracked tree was clean after the verified content
push. Original interrupted/failed hardware attempts and analyzer-capture assumption failure remain
preserved alongside their scoped successors.

Continue the remaining Pass-1 sweep before implementation returns. PIV remains late and Core last,
including dedicated per-applet boundary reviews.
