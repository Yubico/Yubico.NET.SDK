# YubiHSM Auth — independent final evidence review

Fresh GPT-6.1 Sol reviewer, 2026-11-04, primary checkout. Baseline and current HEAD at review:
`a74792facb657413e98223e059876d8b213ffff3`, branch `yubikit-docs-audit`.

**PASS for prepublication record/evidence review. Required corrections: zero.** The current closeout,
status and external handoff accurately distinguish accepted documentation, recorded execution,
deferred engineering and outstanding qualification. This verdict permits coordinator manifest freeze
and publication; it certifies neither an as-yet nonexistent final manifest nor a future commit/push.

## Review boundary

Read root instructions, campaign workflow/style, run plan/status/closeout, findings/corrections,
final coverage/reviewed hashes/sampling, validation/snippet records, hardware results and independent
content/citation review plus follow-up. Checked the exact source captures, compiler provenance,
formatter proof and raw result evidence needed to substantiate the final claims. This is the planned
compact final-record review, not a second whole-module content audit. The original reviewer already
accepted the affirmative, task-oriented voice with source, ownership, firmware and failure qualifications.
That acceptance remains bound to current bytes; no preference-driven editorial changes are required.

No product/tool edits, builds, tests, hardware operations, formatter runs, commits or agents were
performed. Read-only Python verifiers used `-B`. Core/other-module implementations, APIs and docs were
outside investigation; archive, API-snapshot and binary identity comparisons were mechanical preservation
checks. Narrow shared-infrastructure execution reads by the coordinator are disclosed in hardware
authorization, not represented as a dedicated Core/boundary audit.

## Reconciliation

| Dimension | Evidence and verdict |
| --- | --- |
| Complete scope | Independently reconciled the baseline tracked path set with inventory/final coverage: **56 module paths**, **30 edited**, **26 unchanged against initial working bytes**. Four unchanged projects had working CRLF versus Git LF already at baseline; their normalized Git differences are not project edits. Coverage reconciles **eight effective public types and 83 snapshot entries**, including generated/extension/interface overlap. |
| Acceptance chain | Independently compared all **35** original accepted identities with reviewed-files.json and current bytes. The **26 C#** pre-format full-source hashes equal original acceptance and formatter-before hashes; formatter-after hashes equal current files. Nine Markdown/shared paths retain accepted bytes, with shared acceptance limited to the assigned sections. Both Roslyn logs bind exactly the same 26 paths/hashes. |
| Runtime/API preservation | Inspected the checker's nonvacuous source selection, full token text/kinds, anchored directives/disabled/skipped text and conservative ordinary-import exceptions. Retained strict pre-format and final import-accounted results each have zero failures. Exact byte proof independently preserves all non-import content through the formatter. All **20 API snapshots** were independently compared byte-for-byte with baseline. No runtime fix or changed shipping/test assertion is included. |
| Formatter exceptions | Exactly four named ordinary imports across three files: Apdu and Scp in IYubiKeyExtensions.cs; Apdu in HsmAuthSessionTests.cs and HsmAuthSessionByteLevelTests.cs. One import grouping/order change in HsmAuthRetryExceptionTests.cs, plus terminal-LF normalization across 26 files. The original schema-failure evidence remains beside the corrected recorder; no checker relaxation is claimed. Formatter workspace-load warning is distinct from the final zero compiler/analyzer warnings. |
| Clean sampling | **17 eligible units** after excluding edited U01/U04/U07; **U08/U14/U18, 3/17**, pass across API, example and internal/test categories. Checked stored body hashes and current eligible bodies. No material miss/category expansion is recorded. |
| Findings and sources | Twelve original candidates plus HSM-IMP-008 remain distinct from **eight deferred engineering groups and three Core dependencies** with reopening triggers. Incorrect three-parent test-link subfinding is explicitly rejected; correct target retained. Original independent source captures match their manifests. Descriptive manuals/vendor host implementations remain corroboration, not versioned normative conformance. Historical Python 56-byte handling versus C suppression/strict-48 behavior supports the compatibility qualification; actual suffix meaning, firmware range and GAP-SPEC-1 remain open. |
| Repository execution | Retained raw clean-build log, full RESULT block and unit-project results support **3,998 passed, including 91 YubiHSM; five existing skips; zero failures/compiler/analyzer warnings**. Docs QA **73 active documents/10 package READMEs**, architecture **six map entries/seven images**, and complexity **eight files/four methods within thresholds** passed. Static Native AOT contract QA is not runtime qualification. |
| Examples and shipping tool | **476 units** dispositioned across ten scoped docs/44 module C# files. Eight complete groups include all six README bodies, two shared callback/interaction bodies and one inline LIST, with the prompt declaration/session pair grouped together. Read the actual generated consumer bodies: enrollment, full typed HSM adapter/handshake and cleanup scopes survive extraction. Actual Csc captures contain **4 consumer sources/179 references/10 analyzers** and **25 shipping sources (22 real)/208 references/10 analyzers**. All **407** reference/analyzer identities passed fresh live verification. |
| Genuine compilation context | Read the shipping traversal and late capture-only hook: the real HsmAuthTool project compiles with ordinary bin/obj/assets. Nonempty Csc/source/reference/analyzer/config membership, warnings-as-errors and compiler/analyzer execution checks prevent traversal-only or empty-source success. Original v1 extraction is untouched; the exact post-format overlay updates input identities without rewriting example bodies. |
| SDK-default correction | Inspected the layered verifier/capture substitutions and pinned SDK-source/default/Exists checks. The exception permits only an empty hook or the exact **SDK 10.0.403 standard default pointing to an absent, nonsymlinked optional file**. Other hooks remain rejected. Original failed shipping/first evidence and accepted consumer/first remain intact; shipping/second accepts the same successful compilation without rebuilding. This is a capture-assumption correction, not a new build success or changed compiler policy. |
| Prior preservation | Fresh read-only checks passed **2,317 prior archived identities** and all **four original user files**. Before this report there were **475 UTF-8 run artifacts, zero registered PDFs**, no retained binaries/caches, balanced record fences and resolving checked local links. Final artifact count necessarily changes with this report and coordinator publication records. |

## Hardware evidence: successful scope and retained failures

Read raw runner/operation evidence and the full independent oracle, probe and cleanup sources. Existing
integration evidence supports **13 passed in 11+2 invocations**, with both touch-tagged methods limited
to stored metadata. The separate real CALCULATE probe supplies touch-operation evidence.

- **Original v1 failed:** four RFC 4493 vectors, omitted-cryptogram three-key equality and typed
  63C7/seven retries preceded supplied-valid-laboratory-cryptogram **56-byte/strict-48 parser failure**.
  Later cryptogram-bearing retry-restoration assertions were unreached; finally cleanup passed.
- **V2 omission-only KDF/retry passed:** independent S-ENC/S-MAC/S-RMAC comparisons and **8→7→8**.
  Separate user/admin password transitions passed new-password three-key equality; neither actor took
  the source's explicitly skipped unsupported-instruction branch.
- **Original v2 cancellation expectation failed:** cancellation at **1,009 ms**, drain **14,308 ms**,
  CALCULATE **6982/Failed**, rather than the expected OCE/6985 alternatives. Recovery was unreached.
- **V3 measured outcome/recovery passed:** cancellation **1,011 ms**, drain **14,270 ms**, **6982/Failed**,
  then fresh-session LIST/DELETE/PUT and independent non-touch key equality **before any reset**. Exact
  v2→v3 source comparison changes the method/classification/status expectation, retaining the recovery
  assertions. It does not retroactively pass v2 or establish native cancellation/timeout causality.
- **Human-ready touch passed:** raw log records one PolicyRequires request, one Completed resolution,
  token None, independent three-key equality and cleanup. Source asserts the identical context instance
  and exact lifecycle counts. Coordinator/user readiness and physical-touch record supply human context.
- **Independent vendor-Python final cleanup passed:** reset, fresh connection, default-key PUT/LIST/DELETE,
  then another fresh readback establish empty applet, eight retries and usable default management key.
  The original nine-byte-password validation failure remains retained; the corrected script differs by
  explicit 16-byte padding. Initial private credential erasure is disclosed, not called restoration.

The oracle actually computes AES-block CMAC and all three vendor-mapped derivations, uses distinct
long-lived keys and constant-time comparisons, and self-checks all four RFC vectors before device binding.
This is stronger than key-length/metadata evidence, while still only laboratory applet/host calculation.
Original full v1/v2 source/config/asset snapshots pass identity checks; current v3 and all **227** captured
dependencies still match after the final solution build. Original empty-Csc capture rejection and its
immediate clean-build correction remain separate retained attempts.

Qualification remains **serial 103, USB SmartCard, Linux/Omarchy**: device metadata **5.8.0.beta.0**,
SDK/direct-Python session **0.0.1**, ykman CLI app display **5.8.0**. Actual HSM/connector enrollment and
authenticated interoperability, raw 56-byte region interpretation, NFC, older/other firmware, other OSes,
native interruption/admission timing, a specific timeout cause and Native AOT runtime remain unverified.
The current documents and handoff preserve these distinctions and the engineering reopening triggers.

## Fresh read-only verification and exact identities

Executed successfully in this review:

```text
python -B docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/validation/check-preserved.py
python -B docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/validation/check-records.py --reviewed --markdown docs/research/documentation-audit/yubihsm.md
python -B docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001/validation/examples/sdk-default.py verify compiled --capture consumer=first --capture hsmauthtool=second --live
```

Additional read-only assertions independently checked baseline/current path sets, original-to-final hash
bindings, captured full sources/transform/log hashes, API byte identity, clean-unit text/hash membership,
all 227 current hardware dependencies, all three retained hardware-input revisions and 16 original
citation captures. They also compared the exact v2→v3 probe diff and raw build success/warning totals.

Key SHA-256 identities, relative to this run (these identify evidence, not a final archive manifest):

| Artifact | SHA-256 |
| --- | --- |
| reviewed-files.json | `2d44dd52249640a34f659bfec9a421008ba8be4c8013126fae54af103da4c284` |
| coverage-final.json | `6068feff4b1aee0c176645e11e6414ec4749bd151617c3c7cd9c42db3e61c437` |
| sampling-final.json | `8cc5de823f45a109717c62d9e6fb87c02b17fcc1a4c6d8d7daee46cbf52dc224` |
| evidence/review-path-manifest.json | `2bcc374bb844a268008671873f6568aea05a868905e5ab0e59b71b65719ad927` |
| validation/format-transform.json | `839d1203f3451c5baad5313111ecf7775491d5844187a87a88bfed46a72b1c41` |
| validation/final-tokens-corrected.log | `b5655b093dd7ac0a346de37401ba042e193f6541b20b4c91f73730db67672944` |
| validation/final-api.log | `f2b9355f2c2f19c57f827eadca92b70af5c34e2136a74542b00c4b82b6f6666b` |
| validation/final-repository.log | `f7a126611a75ebf32f58838e2f07440c3231ba6756baa5a976883addf0c88de6` |
| validation/examples/v1/manifest.json | `916fdba909347f8e13503adfff9fdd2bf27a26b10a4dafae5482d75f3ce5d300` |
| validation/examples/sdk-default-1/manifest.json | `eef89ce824485ba80e276fa2ae91c514d944008cfea155b2784a3ea15eeceabb` |
| validation/examples/post-format-1/evidence/consumer/first/manifest.json | `a4b400d467c81597d1bef84d09a060a987586ded306b54f27e51c31bcac1a917` |
| validation/examples/post-format-1/evidence/hsmauthtool/second/manifest.json | `47fb07290bee6cc9b56a1012313984bc1706329587d38746a4904eb9b963a4b8` |
| validation/hardware-final-dependencies.json | `c9e98ebfea934bd0570245b6f5f66178a251dea959b647a0e2b8981a5f2afc2d` |
| hardware/final-cleanup-corrected.log | `0a50d43eb94d0a866effd8c5e617afb596ae0a9ced1065a3452c085bc364b3fc` |

## Remaining coordinator-owned publication steps

Record this verdict in current status/handoff, perform the final record/preservation checks including
this report, freeze and verify the actual artifact manifest, explicitly stage/commit/push owned changes,
and verify remote HEAD. Record the eventual commit/remote hash in the external handoff after freeze.
Earlier pending statements in original review/preparation phases are historical checkpoints; current
status and final validation supersede them. None of those historical statements requires editorial churn.

At review, no final record-manifest.json existed and HEAD remained the named baseline. Publication
and remote durability remain outstanding until their own evidence is recorded. Content/source changes
after the identities above require focused rebinding/review; record-only publication updates require
final record and manifest verification. Deferred implementation and qualification remain separate work.
