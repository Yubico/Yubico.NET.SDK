# PIV documentation handoff

**Pass 1 is complete and published** as [piv-001](modules/piv/pass-1/piv-001/plan.md).
Documentation and evidence were committed and pushed as
**`bf0c0207b5accb446492b282cef1e602869976ee`** on `yubikit-docs-audit`; local and GitHub HEAD
matched on November 5, 2026. Fresh independent final evidence review passed with no required corrections.

## Post-PR745 baseline

The audit began only after PR #745 merged into `yubikit` at
`b1362127203beda5e9c797e8648b28e2716c496c`. The recurring watcher and a fresh coordinator query
verified containment before the watcher was paused. User approval covered proceeding once that gate cleared.

[Integration checkpoint](integrations/piv-pr745-20261105/integration.md)
**`727fe43afc430c5d8364d868a6436de50626e476`** preserved both upstream and campaign histories,
resolved their overlaps, passed independent review and validation, and was pushed before investigation.
PIV initially matched the merged PR exactly. Integration runtime/API changes and the necessary OATH
fixture migration remain separate from this documentation-only cycle. No documentation PR was created.

The run's [status](modules/piv/pass-1/piv-001/status.md) and
[closeout](modules/piv/pass-1/piv-001/closeout.md) preserve their reviewed prepublication checkpoint.
This external publication handoff supersedes their remaining freeze/push step; the frozen run is unchanged.

## Completed documentation and checks

- **123 module paths, 23 public types, 370 API entries and 42 interface operations** reconciled.
- **59 authored paths accepted:** 53 module paths and six shared sections. All 944 original semantic
  units have explicit final dispositions; 70 module paths retain their original bytes.
- **50 edited C# paths** preserve executable tokens, directives, imports and existing assertions.
  Formatting is whitespace-only. All **20 API snapshots** remain unchanged; package/project metadata
  is unchanged. Earlier runtime findings resolved by #745 were re-baselined rather than repeated.
- Independent content/original-citation reviews, required correction re-reviews and late hardware-impact
  review passed. Compiler validation caught and corrected a value-type assertion in a documentation
  example. Final coverage checks corrected SPKI result semantics and the RSA re-wrap rationale.
- Expanded clean sampling **64/385 passed**, including test and internal-prose expansion after misses.
- Clean solution gates passed **4,124 unit tests**, including **289 PIV**, five existing skips and zero
  compiler/analyzer warnings or failures. Documentation/package, architecture and complexity checks passed.
- **45 typed example groups / 47 selected bodies** and the actual **36-source PivTool** compiled
  warning-free. Final actual Csc/project-graph/live verification reconciled **619 reference/analyzer
  entries across three contexts**. All 60 current bodies, 61 originals and 730 snippet dispositions remain.

The accepted voice is direct, calm, precise and approachable: affirmative tasks and actors first, useful
rationale and nearby conditions preserved. Original NIST/RFC/vendor revisions, publication status and
profile applicability are explicit. Pinned Python YubiKey Manager source/tests/history corroborate behavior
and firmware origins, with normative requirements kept distinct from implementation comparisons.

## Hardware outcomes and limits

**86/89 distinct canonical methods passed:** 83 single-key methods, followed by all three two-key
methods. Original fixture skips remain preserved alongside their later successful dedicated execution.
The user explicitly deferred the three physical-lane methods—its read-only prerequisite printer,
touch-signing and hotplug—and the supplemental touch-dependent cancellation/recovery case.

Six unattended supplemental methods produced **four passes and two preserved intended-contract failures**:

- X25519 matched the RFC 7748 secret in both the SDK and an independent vendor operation on the same key.
- Repeated PIN-policy enforcement, key/certificate independence and attestation signature/key binding passed.
- Actual PivTool RSA output failed original-message verification despite helper success, while the direct
  SDK control passed. Its Ed25519 output verified SHA256(message), rather than the original message.
  These failures confirm existing **PIVIMP-005** and remain unfixed for Pass 2.

Both dedicated keys' final independent readbacks match every captured stable baseline field, excluding
observation time and capture label. Serials **103 and 125** retain default PIN/PUK 3/3, AES192/default/Never
management metadata, empty normal slots/certificates and distinct preserved F9 identities/policies.
Factory PIN and management-key authentication were independently verified. Existing F9 private keys
were never replaced. Protected PRINTED content was deliberately unread, so no separate observed
byte-absence claim is made for that object. The original tracked allowlist was restored byte-for-byte.

Recorded scope is Linux USB SmartCard on development firmware **5.8.0.beta.0** and **5.8.0.alpha.2**.
Raw PIV `00 00 01` remains distinct from CLI/Management observations. Two-key timing/nonempty-signature
assertions retain their actual limits. Attestation proves issuer/key binding within the recorded scope;
manufacturer-root trust remains separate. Touch/cache/hotplug/native cancellation, Bio, NFC, SCP-specific
PIV paths, older firmware/NEO, FIPS products, other OSes and Native AOT runtime remain unverified here.

## Durable evidence and return visit

- [Findings](modules/piv/pass-1/piv-001/findings.md): **ten engineering/qualification groups and three
  Core questions**, with stable IDs, source paths and documentation-reopening conditions.
- [Closeout](modules/piv/pass-1/piv-001/closeout.md): scope, outcomes, corrections and qualifications.
- [Hardware results](modules/piv/pass-1/piv-001/hardware/results.md): exact successes, failures and restoration.
- [Validation](modules/piv/pass-1/piv-001/validation.md) and
  [example compilation](modules/piv/pass-1/piv-001/snippet-validation.md): compiler and execution provenance.
- [Final independent review](modules/piv/pass-1/piv-001/review-closeout.md): PASS with 18 evidence anchors.
- [Frozen manifest](modules/piv/pass-1/piv-001/record-manifest.json): **1,965 artifacts**, comprising
  **1,961 text files and four original PDFs**, totaling **41,981,898 bytes**, excluding the manifest.

All **2,027 content-commit paths** were explicitly staged and matched working bytes exactly. The frozen
path/hash/size set, nine final-review checkpoint hashes, 3,401 preceding artifact identities, 114 integration
files and four original user files passed preservation checks. Earlier helper/capture assumptions,
the actual annotation analyzer failure, required prose corrections and failed signing expectations remain
attached to their original inputs and results.

The six tested runtime contexts retain their original copies and source snapshots. All 2,166 captured
Csc-input/runtime-copy entries remained exact after the final comment-only correction. The two regenerated
root bin outputs are distinguished from those tested copies; all 24 original project obj/bin identities
remain preserved externally with exact hashes. Future live checks must report missing or changed external
bytes explicitly rather than treating historical evidence as a fresh build.

The tracked tree was clean after the verified content push; the four original untracked user files remain
untouched. This publication-only checkpoint updates the handoff/index while preserving all accepted source
and frozen-run bytes. Continue the first sweep before implementation returns; Core remains the final target,
including dedicated per-applet boundary reviews and re-baselined dependencies.
