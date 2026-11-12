# B6 APDU/SCP investigation checkpoint

**State: incomplete, resumable source investigation.** Complete reading covers all
33 primary shipping files and 15 primary C# test/helper files: **48 of 50 files,
7,551 of 9,441 lines**, plus all **16 actual shared-read IDs**. Two vector files
(1,890 lines) and final narrow per-unit triage remain outstanding. This report
requests continuation rather than marking B6 or the Core sweep complete.

Source stays `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Initial metadata HEAD was
`c7ff868c6f1b89e2d72eaba526bf98d9c28adb05`; the later observed records-only checkpoint
is `bb94f0884f63f260156227957d4cc112d984e300`. Fresh static joins verify every assigned
raw file/range SHA-256 and selected pinned-file identity with explicit CRLF/LF comparison.
Identity checks include unread vectors and add no semantic coverage for them.

Only `investigation/B6/*` and this report were written. Product code, tests/assertions,
central inventories/status/source registers, frozen archives and other workers' files
remain outside write scope. No build/test/formatter/native-crypto/device command,
SDK/package import, subagent, index/ref update, commit or push ran. Python did record
assembly, read-only source/hex/hash inspection and HTTP retrieval; pdftotext converted
two retained original PDFs for passage reading. No new scratch used the exhausted tmpfs.

## Durable checkpoint

| Record | Purpose / limit |
| --- | --- |
| [Summary](investigation/B6/checkpoint-summary.json) | Exact current counts, two unread paths and separate unrun gates |
| [Unit dispositions](investigation/B6/unit-dispositions.json) | All 668 primary and 16 shared original IDs, offsets/line ranges/raw and range hashes, read state, authored context, provisional associations and pending triage |
| [File dispositions](investigation/B6/file-dispositions.json) | Complete versus assigned-range versus unread scope, pinned blob/normalized equality and distinct per-file observations |
| [Semantic notes](investigation/B6/semantic-notes.json) | Explicit investigator readings; the join tool never infers reading from hashes |
| [Candidates](investigation/B6/candidates.md) | Sixteen local provisional groups, observed/desired/decision/qualification distinctions and concrete reopening map |
| [Unchanged paths](investigation/B6/unchanged-paths.json) | Explicit preserve-and-refer decision for all primary/shared product paths, including pending narrow editing decisions |
| [Ownership / handoffs](investigation/B6/ownership-and-handoffs.md) | Source/parameter/contained-object/session/intermediate owners and exact B5/C/B1/applet boundaries |
| [Inherited recommendations](investigation/B6/inherited-dispositions.json) | All eight assigned provenance keys, aliases and six nonadditive groups; authoritative registry states unchanged |
| [Original sources](investigation/B6/sources/register.md) | Exact final versus public-review status, actually read passages, actors/strength/conditions and bounded failures |
| [Download manifests](investigation/B6/sources/initial-download-manifest.json), [follow-up](investigation/B6/sources/focused-followup-manifest.json) | Original URLs, timestamps, HTTP status, byte counts and hashes; failed attempts preserved |
| [Clean nominees](investigation/B6/clean-nominees.json) | Eight substantive API/test units with actual alias or published-vector oracles; unsampled and unexecuted |
| [Prepared examples](investigation/B6/prepared-examples.md) / [probe plan](investigation/B6/probe-plan.md) | Complete typed credential/parameter/session tasks, UNCOMPILED; bounded coordinator-only proposals, unrun |
| [Record verification](investigation/B6/verification-receipt.json) | Fresh check of all 684 IDs/raw-range identities, eight inherited keys, eight nominees, original download hashes and authored local links/fences; not SDK execution or content review |

Current dispositions total **684 IDs**: **18 read-clean**, **662 read-related candidate
context**, **two read historical context**, **two unread**. The 662 context associations
are conservative file-context referrals, not 662 defective units or final exact edit
ranges. Narrow candidate-versus-clean triage is explicitly pending. The eight substantive
clean nominees are distinct from boilerplate/accessor units and must be removed from
sampling eligibility if later edits overlap. No independent review or sampling occurred.

## Source-backed issues for triage

- **Wire/task precision (C001/C003/C015):** always-present Le, actual whole-command
  ceilings versus fragment data width, no corrected-length retry, fixed follow-up
  routing, warning/error mapping, configuration branches and first-plain-fragment
  failure qualification. Complete tasks need failure-safe source/parameter/session scopes.
- **Cleanup and ownership (C002/C006/C007):** response.Clear reaches its own clone,
  not transport staging, previous fragments or the decrypted temporary cloned into
  another response. SCP03 pre-state-return cleanup differs from the later initializer
  guard. Parameters retain exact disposable objects; record copying is not deep cloning.
- **SCP mutation and phase (C004/C005):** Encrypt advances the counter before a
  provider operation can fail, while the caller records stateAdvanced only after
  return. The unauthenticated external-authenticate response exception is keyed to
  INS82 alone, not initialization phase. Actual failure/release state remains distinct.
- **Profile and sources (C009–C012):** P-256/profile/trust validation is narrower than
  the parameter API's accepted shapes. SCP11c's returned key is static in the read
  draft even though comments call it ephemeral. Conditional HostID/CardGroup rules
  cannot become blanket missing-field violations. Final SCP03 explicitly advances
  the counter without encrypting an absent command field, while current code pads
  it. Protected response-chaining graph/oracles require independent wire qualification.
- **Tests/representations (C008/C013/C016):** KeyReference cached bytes can diverge
  from init-modified properties; ATR content equality and array-identity hashing differ.
  Several tests prove lengths/disposed refusal rather than cryptographic correctness
  or retained-byte erasure. The synthetic card authenticates statuses the inspected
  final SCP03 edition classifies as unprotected errors. These are separate from its
  useful no-replay/guard/drain assertions.
- **Logging (C014):** current command trace is header/length only; arbitrary SELECT
  identifiers and external exception text require their own provenance. Reuse C's
  logging policy and B1's different post-open logger concern, without duplicate counts.

These are local `CORE-B6-C001`–`CORE-B6-C016`, not accepted Core IDs. Do not frame
wire/parameter/recovery defects as intended contracts or infer hardware behavior from
matching Python. A source-text check corrected an initial visual scalar-width miscount:
the actual test helper scalar is 64 hex characters / 32 bytes, so it supplies no early
width-failure finding. The correction is explicit in the candidate record.

## Original-source result and remaining bounds

Retained original SCP03 **v1.1.2 Public Release, March 2019**, SHA-256
`973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`, supports the
inspected KDF/challenge/MAC/encryption and command-table clauses. Code instead cites
**v1.1.2.6**, whose located original is **Public Review, December 2019, target 1.2**;
its direct download returned 403. Keep exact edition and section coordinates separate.

Retained SCP11 **v1.3.0.13 Public Review, August 2023**, SHA-256
`5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`, supplies the
actually inspected a/b/c actors, certificate processing and conditional KDF/wire rules.
The official catalogue confirms final **v1.4** publication in March 2024; final text
and exact YubiKey profile remain unverified. The older vendor-linked final 1.2 fetch
also returned 403. Initial plus one focused search/follow-up is complete; retain this
gap rather than extending an unlimited latest-spec search.

Context7 supplied current stackalloc initialization guidance. BCL ArrayBufferWriter
internal-source retrieval failed at two attempted pinned paths, leaving hidden growth
copy erasure unverified; C002 relies on the visible SDK copy graph. C's pinned provider/
AsnWriter/OpenSSL/native-package qualifications remain intact. Complete Python SmartCard
and SCP source files were independently read at shared pin
`4ca60f706af930459138d8dc0f0f953480e1c7a4`. Its selected tests/history still need B6's
independent reading. The sibling graph differs in response chaining/SELECT and has
different ownership; neither agreement nor disagreement is a normative verdict.

## Exact continuation work

1. Read complete vector files, not selected grep rows:
   - `core-001:unit:59c33b8c7c8f294c`: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp/ansx963_2001.rsp`, 555 lines.
   - `core-001:unit:24a290b7fd0ed693`: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp/ansx963_2001.txt`, 1,335 lines.
2. Verify original CAVS/vector provenance and claim-driven ISO7816 edition/passages;
   finish shared Python device-test/history reading. Existing bounded GP-final gaps
   stay gaps unless the coordinator supplies the missing original.
3. Finish narrow dependency/oracle trace where marked pending and replace the 662
   conservative context associations with exact candidate/unchanged-clean decisions.
   Preserve original IDs/hashes and this checkpoint's provenance before finalizing.
4. Coordinator triage then assigns fresh editor/reviewer and serialized validation;
   no source, assertion or central inventory change follows from this checkpoint.

B5's completed 932-ID return and relevant C001/C004/C005/C006/C008/C009 handoffs are
incorporated narrowly in the ownership record. B4 remains active; future Windows code
is inspected only after user readiness. All eight inherited keys retain source-run
identity. The original 100-row/70-group registry and 21 PGP labels remain nonadditive.
Serial 125's 6A88 setup failures occurred before EXTERNAL AUTHENTICATE/RESET; plain
reset success still gives no SCP-reset proof. PIV physical deferrals remain in force.
