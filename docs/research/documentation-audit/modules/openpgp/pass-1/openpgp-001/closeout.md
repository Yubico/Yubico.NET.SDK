# OpenPGP openpgp-001 closeout

## Scope and current outcome

Baseline `fe180282a2d77b15eaaa5868d6c8b00e251d1f05`, branch `yubikit-docs-audit`. Whole-module
investigation reconciled **96 paths, 39 effective public types, 423 API snapshot entries and 40 declared
interface operations**. Accepted authored scope is **59 product paths**:52 module paths and seven
shared-document sections. Remaining44 module paths retain their initial working bytes.

Fresh Sol investigation/editing/original-citation review and correction re-review are complete.
Astra adjudicated evidence, expanded clean sampling, inspected test/probe sources, coordinated touch,
ran gates and verified device restoration. The five required review groups corrected seven paths,
including a new timestamp-contract correction found during sampling; all59 final semantic paths are
accepted. Complete example compilation passed with actual Csc/live dependency evidence. Fresh
[final evidence review](review-closeout.md) passed with zero required corrections. Final manifest
freeze and verified publication remain coordinator-owned.

The approved house style is an acceptance criterion: direct, calm, precise and approachable; actual
behavior and actor first, actionable tasks, useful rationale and local conditions. Consequential
limitations explain supported actions. Source comments describe current intent/constraints; test
comments describe assertions actually reached. Public guides remain consumer guidance rather than
private audit transcripts.

## Consumer improvements

The guide now follows discovery, capability inspection, PIN change/recovery, KDF provisioning,
generation/import, host signature verification, raw decipher/agreement, certificates/attestation,
configuration and deliberate reset. Twelve complete C# bodies preserve197 lines of executable task
examples, with typed dependencies and full owned/borrowed resource scopes.

Contracts distinguish User PIN identity from VERIFY mode, uncertain retry metadata, KDF transition
and alias lifetime, algorithm-specific hashing/output formats, raw key operations versus OpenPGP
packet processing, transferred result buffers, multi-command mutation/cache effects, and attestation
certificate overwrite/trust responsibilities. Reset and fixed-policy exceptions are explicit. Model
docs clarify accessibility, encoding/units, parser limitations and unspecified/signed-carrier timestamps.
Tool limitations and firmware/application-version qualifications are linked to practical SDK use.

Original specification verification identified the URL named3.4 as **Card application3.4.1, March18,
2020**. Applicable RFC claims stay within their algorithm/profile scope. Pinned Python YubiKey Manager
source/tests/history trace salient firmware checks, while original advisory/manual/extension passages
establish their respective applicability. Commercial5.2.x versus source thresholds and old-family
behavior retain bounded evidence gaps. No inferred sibling agreement becomes normative conformance.

## Engineering return

The21 original candidates plus sampled **PGP-C022** map to **14 deferred engineering groups and four
Core boundary questions**. C022 extends parser/carrier policy PGP-IMP-018; the21 PGP-DEP labels in the
investigation are documentation dependencies, not additional Core bugs. [Findings](findings.md) bind
stable IDs to implementation paths, affected documentation and precise reopening triggers.

Device evidence confirmed three consequential groups:

- **PGP-IMP-001:** extended User verification uses Reset Code identity for salt/retry fallback. Correct
  User PIN with distinct salts failed6982; actual User attempts fell3→2 while the typed count reported0.
- **PGP-IMP-007:** SDK X25519 decipher returned6A80 after valid imported public-key control; a fresh
  Python session on that same key, without intervening reset/import, matched the RFC shared secret.
- **PGP-IMP-013:** SDK ATT public-key read returned6A80; correct vendor CRT public read parsed RSA
  and matched the preserved certificate's SPKI.

Other groups cover KDF transition/ownership, reset status verification, partial mutation/cache behavior,
firmware/capability enforcement, ROCA test/generation boundaries, NEO formats/deletion, tool behavior,
test oracles, parser/carrier policy and error mapping. Pass1 documents current limitations; runtime and
assertion changes await the campaign's second sweep. PIV remains late; Core remains last with dedicated
per-applet boundary reviews. Re-baseline findings before implementing fixes.

## Verification

- **3,998 unit tests passed**, including186 OpenPGP, five existing skips, zero compiler/analyzer
  warnings/failures. Clean solution, documentation/package, architecture and complexity gates passed.
- **All49 existing integration methods passed** in48+1 invocations. An earlier120-second attempt was
  interrupted after24 reported passes; its incomplete evidence remains separate from the complete run.
- **Nine supplemental methods executed in13 invocations:** five successful final measured outcomes
  (four touch operations plus cancellation/recovery) and four retained intended-contract failures
  (KDF identity, retry reporting, X25519 decipher and ATT public read). Two independent reference
  operations passed. Preserve earlier setup, mode and touch-window failures alongside later outcomes.
- Touch signatures/plaintext use independent host comparisons. Attestation verified issuer signature
  and generated-key SPKI binding, with ordinary User verification; extended-only verification was
  rejected on this device. This establishes no all-firmware exclusivity or manufacturer-root trust chain.
- No-touch cancellation ended6600/Failed, followed by a fresh-session host-verified signature before
  reset. It establishes terminal observation/recovery rather than immediate native interruption.
- Final independent restoration/readback **matches every initial public-state field**: allfour UIFs
  Off, normal keys/certificates empty, retries3/0/3, KDF disabled, Once/counterzero, usable factory PINs
  and unchanged attestation identity. Att On survived reset and was explicitly restored. Existing Att
  private key was never replaced.
- **49 edited C# paths** preserve executable tokens/directives/imports. Formatting changed whitespace
  only; all20 API snapshots unchanged. The package project differs only in its accepted Description.
- Expanded clean sampling **11/32 passed**, including eight API units after material misses; corrected
  PinPolicy/timestamp units are excluded. All59 acceptance hashes bind to final bytes.
- Hardware actual-Csc contexts/source/config/assets are retained; all228 latest live identities matched
  after final solution build. All17 complete documentation groups (including12 README bodies) and the
  entire19-source tool compiled warning-free; actual Csc and407 reference/analyzer context entries
  passed final live checks. The separate407-unit snippet inventory has explicit dispositions.

Recorded hardware scope: serial103, USB-A YubiKey5 NFC Enhanced PIN, Linux/Omarchy USB SmartCard;
metadata5.8.0.beta.0, CLI card3.4/application5.8.0, SDK/direct-Python session0.0.1. Version-gated Python
certificate/UIF results were superseded by explicit source-checked public reads without private-version
override; originals remain retained. NFC, older firmware/families, other platforms, native cancellation
causality and Native AOT runtime remain unverified. No hardware job is active.

## Records and publication boundary

[Validation](validation.md), [examples](snippet-validation.md), [hardware results](hardware/results.md), [sampling](sampling.md),
[coverage](coverage-final.json), [review](review.md) and [re-review](review-corrections.md) retain exact
scope and outcomes. Original sources, one registered primary PDF, failed attempts, full probe sources,
compiler contexts and correction identities are preserved. Previous2,794 archive identities and four
original user files remain unchanged. Final manifest freezes this run after fresh evidence review;
the external [handoff](../../../../openpgp.md) records commit/push/remote verification after freeze.

Carry forward: trace firmware origins to exact history and profile; use independent crypto oracles;
inspect setup/cleanup beyond test traits; distinguish token/mode/PIN identity; capture compiler evidence
immediately after real builds; qualify state-dependent configuration; sample summary-only contracts;
preserve failed expectations and current status; restore proprietary reset exceptions explicitly.
