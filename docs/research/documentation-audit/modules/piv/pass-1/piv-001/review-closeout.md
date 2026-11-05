# PIV final independent evidence review

**PASS — the final documentation closeout is supported at the identities below. No required
content, record or evidence correction remains from this review.** The coordinator can proceed to
final record checks and archive freeze. Commit, push and remote verification remain publication work;
this verdict supplies no publication-complete or implementation-complete claim.

Reviewer: fresh independent GPT-6.1 Sol, November 5, 2026. Baseline/observed HEAD:
`727fe43afc430c5d8364d868a6436de50626e476`, branch `yubikit-docs-audit`. Read root guidance, the run
plan/status/closeout, findings/investigation, current coverage/dispositions/reviewed identities,
sampling, validation, example and hardware reports, and the original review/correction chain. Followed
the consequential final claims into retained raw records. This is a bounded evidence closeout, with
mechanical project/API/archive checks; Core and other-module implementation reviews remain outside it.

Only this file was authored. All checks used read-only Python with bytecode disabled or read-only Git.
No product/tool edits, builds, tests, hardware operations, formatter, commits or subagents were run.

## Reconciled outcome

- **59 accepted product paths:** 53 module and six shared; 50 C# documentation/comment paths and nine
  Markdown/YAML paths. All actual hashes match both final acceptance and `reviewed-files.json`.
  The 123-path inventory matches the baseline Git tree; the remaining 70 module paths retain their
  captured working-baseline bytes. Four unchanged project files have recorded CRLF checkout differences
  from Git blobs. My initial stricter Git/working byte comparison exposed that distinction; checking
  both recorded hashes and exact CRLF-to-LF equality resolved the assumption without changing files.
- **944 distinct original semantic units:** 927 module and 17 shared, each with a concrete final
  disposition and matching current full-path identity. CertificatesMenu and DataObjectProtocol retain
  explicit unchanged/finding dispositions. The PIV-local diagram context preserves its scoped decision;
  the record does not imply a broader Core architecture audit.
- **354 current XML blocks parse.** Independently re-extracted 61 original and 60 current complete
  bodies; every original body hash matches the original ledger. All 50 raw pre-format UTF-8 content
  strings hash to their receipts before comparison. Their current transformations preserve every
  non-whitespace byte, exact imports and ordered non-line-comment source. The retained strict Roslyn
  result separately binds all 50 token/directive streams. All 20 tracked API snapshots match baseline.
  Package/project metadata and executable assertions/help strings remain outside the authored delta.
- **64/385 distinct eligible samples across 18 paths:** 25 API, ten example, 23 internal/test and six
  narrative units; minimum 39. Selected body/file hashes match, edited overlaps are excluded, and the
  ten-test plus three-internal-prose expansions remain explicit. Comment-free context reads add no units.
- **Fresh v4 compilation evidence:** 45 groups/47 selected bodies, 730 explicit dispositions, all 111
  module C# identities, four module Markdown files, six shared documents and the actual 36-source tool.
  Raw build results report zero warnings. Actual Csc source/reference/analyzer counts are 4/192/10,
  5/205/12 and 39/190/10; direct/closure project counts are 5/12, 2/4 and 5/10. The 619 reference/analyzer
  entries count context membership. A fresh read-only compiled/live verification passed all three.
- **Final repository evidence:** raw results reconcile 13 passing unit projects, 4,124 executions,
  289 PIV, five existing skips, zero failures and zero warnings. QA covers 73 documents/ten package
  pages; architecture covers six map entries/seven images. Complexity reports 17 files/eight methods,
  one unchanged AdminData debt and zero new/worsened cases. Static AOT/declaration checks retain their
  static scope. These are reviewed coordinator executions, not executions repeated by this reviewer.

## Raw evidence anchors

Paths below are relative to this run. For supplemental rows, `H` means
`validation/hardware-probe/evidence/`; `E` means `validation/examples/v4/`.

| # | Raw anchor | Independently checked meaning |
| --- | --- | --- |
| 1 | `evidence/review-accepted-v4.json:files`, `coverage-final.json:module`, `unit-dispositions-final.json:units`, `sampling-final.json:samples` | Exact live identities, unique membership and reconciled denominators above; original investigator/first-review pending fields remain historical. |
| 2 | `validation/pre-format-content.json:files`, `validation/late-pre-format-content.json:files`, `validation/final-tokens-v2.log:2–52` | Original 48 plus late two paths; UTF-8 receipts, whitespace-only transforms, exact imports and final token/directive hashes agree. |
| 3 | `validation/final-repository-v2-20261105-090952-590-build+test+docs-qa+docs-architecture+complexity/result.json`; `validation/final-repository-v2.log:101–159` | Actual final project totals, zero warnings, QA/architecture and unchanged complexity debt; no failed supplement is folded into this unit result. |
| 4 | `E/consumer/evidence/first/manifest.json`, `E/annotations/evidence/first/manifest.json`, `E/pivtool/evidence/first/manifest.json`; corresponding `validation/examples-v4-*-build-*/result.json` | Actual source/reference/analyzer/configuration identities, genuine obj/bin graph bindings and three fresh zero-warning build results. |
| 5 | `validation/examples-v4-compiled.log:1–8`; `validation/examples/v4-tools/verify-v4.py:35–62,90–124`; `project_graph_v4.py:164–200` in that tools directory | Current complete bodies and exact live dependencies checked afresh. Graph normalization sorts only unique graph nodes; identities, edges, closure and bindings remain compared. |
| 6 | `validation/examples-v2-annotations-build.log:25`; `validation/examples-v2-consumer-capture.log`; `validation/examples-v3-compiled.log`; `H/p01-x25519-v2/compiler-capture.log`, `H/p01-x25519-v3/compiler-capture.log` | Real xUnit2002 failure, direct/transitive-reference assumptions, graph-order mismatch and initial analyzer-selection guard remain failures in their original contexts. Final qualification uses the canonical explicit NetAnalyzers package and actual Csc sets, rather than equating EnableNETAnalyzers=false with absent analysis. |
| 7 | `validation/integration-unattended-103-20261105-011511-789-test/01-test-Piv.IntegrationTests.log:22–117`; `validation/integration-two-keys-20261105-080854-529-test/01-test-Piv.IntegrationTests.log` | Parsed 83 plus three disjoint passed method names; the original three skipped names are the later two-key passes. Source matrix has 89 distinct methods; exactly three physical-lane methods remain. Test-target durations are 483.8 and 117.6 seconds. |
| 8 | Unattended raw log above, lines 84–85; `evidence/hardware-methods.json` | Original-message Ed25519 host-verification and the named PIN-isolation regression actually passed. These cases retain their particular oracles and supply no universal Core isolation guarantee. |
| 9 | `H/p01-x25519-v4/probe-events.jsonl:stage=P01-rfc7748-verified`, `vendor-x25519.json:stage,metadata,shared_secret_matches_rfc7748`; `validation/hardware-probe/run-probe-v4.py:140–159` | SDK RFC Alice public value/Bob shared secret and independent installed-Python agreement on the same still-imported key passed before cleanup. No vendor re-import substitutes a different key. |
| 10 | `H/p02-pin-policy-v4/probe-events.jsonl:stage=P02-second-always-operation,P02-reverify-and-once-controls` | Second unverified Always operation returned InvalidOperationException without a signature; reverification and two Once signatures passed. Attribution to 6982 is the SDK source mapping, not a separately captured raw status. |
| 11 | `H/p03-key-cert-v4/probe-events.jsonl:stage=P03-independent-objects-verified` | Moved-key SPKI/signature binding and unrelated certificate DER retention through move/delete passed; key and certificate storage remain separate. |
| 12 | `H/p04-attestation-v4/probe-events.jsonl:stage=P04-attestation-verified` | Preserved F9 issuer signature and generated/metadata/attested SPKI binding passed; manufacturerTrustEvaluated=false remains explicit. |
| 13 | `H/p05-tool-rsa-v4/probe-events.jsonl:stage=P05-actual-sample-result`; `dotnet.log:24–38` in that directory | Direct SDK EMSA control verified; actual helper reported success while original-message verification was false. Intended-contract assertion is FAILED, exit 1, one execution/zero skips. |
| 14 | `H/p06-tool-ed25519-v4/probe-events.jsonl:stage=openssl-verification,P06-actual-sample-result`; `dotnet.log:24–38` | OpenSSL SDK-original passed, helper-original failed, helper-SHA256(message) passed. Intended-contract assertion remains FAILED, exit 1. P05/P06 confirm existing PIVIMP-005 rather than two new finding groups. |
| 15 | `hardware/cleanup-control-103.json:reset_completed,guard_failure`; `hardware/cleanup-control-v2-103.json:readbacks`; all six `H/*-v4/vendor-before.json` and `vendor-after.json` | V1 stopped before mutation on the chained F9 comparison assumption. V2 and every executed probe's before/after cleanup report restoration plus independent factory authentication; receipt readback hashes match retained files. |
| 16 | `hardware/baseline-103.json`, `baseline-125.json`, `final-103.json`, `final-125.json`; `hardware/final-reconciliation.json`; `hardware/test-allowlist-original.json` | Fresh comparison of the entire captured JSON, excluding only utc/mode, matches each key's own baseline. All 25 slots and distinct 793-byte F9 certificate/SPKI identities remain. The original tracked allowlist matches its preserved bytes exactly. Protected PRINTED is explicitly excluded. |
| 17 | All six `H/*-v4/compiler/capture.json:records,live_output_binaries`, `probe-events.jsonl:stage=runtime-assembly`, `result.json`; `validation/tested-project-binary-preservation.json:files` | Fresh strict checks passed 2,166 captured input/runtime-copy entries, loaded-assembly/compiled hashes and all 24 externally preserved original project binaries. Exactly two root bin outputs regenerated: Piv and PivTool; their tested copies remain exact. Required paths were read directly, so missing bytes would fail. |
| 18 | `evidence/source-manifest.json`, `followup-manifest.json`; `evidence/nist-sp800-73-4-part1.txt:1–39`; `evidence/rfc8017.txt:1255–1259`; `prior-archive-baseline.json`, `user-work-baseline.json`, `integration-baseline.json` | All 21 captured source identities match. The combined 800-73-4 archive cover establishes revision/date/withdrawal despite its historical filename; the RFC gives the leading-zero premise for the corrected deterministic RSA fit argument. Fresh preservation checked all 3,401 frozen artifacts, four original user files and 114 separate integration files. |

## Conditions carried into closeout

The accepted correction chain resolves F9 property/JSON shape, partial credential rotation, the
SHA-256 and differential-Ed25519 overclaims, the value-type annotation assertion/firmware prerequisite,
canonical README selectors, helper-produced DER SPKI/factory memory semantics and the valid-RSA-block
fit rationale. The final hashes bind that accepted content. Historical 57/48-path reports and earlier
compiler/runtime source snapshots retain their own meanings; current scope is 59/50. Static-only
preparation output labelled PENDING is superseded for compilation by the completed compiled/live gate.

Hardware establishes Linux USB SmartCard behavior on serial 103 (5.8.0.beta.0) and 125
(5.8.0.alpha.2), with raw PIV GET VERSION `000001` retained separately from CLI/Management metadata.
Two-key nonempty-signature/timing assertions establish liveness in their measured cases, including
both RSA4096 directions; they supply no independent cross-key cryptographic binding. P04 establishes
use of the preserved F9 key at its checkpoint, separately from public-state equality and root trust.
The six executed supplements each have one execution and zero skips: four PASS and two FAIL.

The user explicitly deferred physical tests. The read-only prerequisite printer, touch-signing and
hotplug methods, plus P07 cancellation/recovery-touch, remain unexecuted. NFC, Bio/enrollment, older
firmware/NEO, FIPS products, SCP-specific paths, other operating systems, manufacturer trust and Native
AOT runtime remain separate qualifications. The later comment corrections imply no hardware rerun.
Ten PIVIMP groups and three PIVCORE questions remain deferred; completion of this documentation review
preserves the first-sweep/implementation separation and Core-last boundary review.

Fresh read-only `check-preserved.py`, `check-records.py --reviewed` and
`examples/v4-tools/verify-v4.py v4 compiled --capture consumer=first --capture annotations=first
--capture pivtool=first --live` all exited 0. Before this report was added, record checking counted
1,959 UTF-8 artifacts/four registered PDFs and verified all 59 reviewed hashes. After adding this report,
the repeated record check passed 23 record Markdown files, 1,960 UTF-8 artifacts, four registered PDFs
and all 59 reviewed hashes. Inline independent
checks additionally reconciled the counts, original/current source framing, raw results, hardware
identities and complete captured-state equality described above. No optional audit expansion or prose
rewrite is requested. Final record checking must include this report before freeze/publication.

## Reviewed checkpoint identities

These SHA-256 values identify the reviewed checkpoint, rather than a future frozen/publication state.

| Run-relative path | SHA-256 |
| --- | --- |
| `closeout.md` | `eaf057dcb614a519177853581b1d9eeaf9e0411e742118b06a1421eed9743cf7` |
| `status.md` | `af243f58630af661deab5d2ef63db237e2c74d6e5d1728c0299b9410c1975cdf` |
| `evidence/review-accepted-v4.json` | `59a4be1b906b31aa95f7ea4a1fd77621c41f2e9af607719c713548dc05a329b9` |
| `coverage-final.json` | `44a766a4da976e1bfff0531491ea8103f67cdf6c18a512ee959bc04326d4cb8b` |
| `unit-dispositions-final.json` | `0414625b178a9965668ca8ce7e145f925014b032edb1c20706b91802b9f73ef7` |
| `sampling-final.json` | `4446e450cd075d82b0eb239397112779dd6fbb36906fe714f0286f4368d2a58d` |
| `validation/examples/v4/manifest.json` | `010a9af35b6fe76da276f767423938bae67493fa97229deff0c501e7a80a42a2` |
| `validation/examples-v4-compiled.log` | `f3f621e86a37916c9c89fbea1513c736c1b772c801a7d9906cad4078a2185552` |
| `hardware/final-reconciliation.json` | `235213e8f79a4cddcc79f8ed922e4b1f74b55984891593f7edd9a02dafb51d2c` |
