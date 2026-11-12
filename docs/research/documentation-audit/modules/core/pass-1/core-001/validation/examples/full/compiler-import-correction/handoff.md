# Consumer-second: generated import correction

**Ready for focused review; no compiler pass claimed.** Consumer-first remains a failed restore.
Consumer-second restored successfully, reached Csc, then failed. No example/test assembly was executed.

## Complete diagnostic triage and bounded fix

The complete 379-line build log contains **177 unique emitted diagnostics** after deduplication by
source location/code/message. `actual-diagnostics.json` retains each diagnostic;
`diagnostic-triage.json` maps every diagnostic to its body ID, document and root cause:

| Root cause | Count | Ownership |
| --- | ---: | --- |
| CS0105 duplicate using directive | 146 | Wrapper additions duplicated original directives or other additions. |
| CS0234 nonexistent `Yubico.YubiKit.Ctap2.Ctap` | 31 | Wrapper-added namespace; the actual CtapCommand declaration belongs to `Yubico.YubiKit.Ctap2`. |

All observed diagnostics are generated-context defects. **No accepted-body diagnostic was emitted in
this log.** That does not establish successful body/API binding: a retry may expose later errors. Return
any genuine body error with exact document/body ID/diagnostic to the coordinator/editor; do not repair it here.

The Full-local `compiler_imports.py` gives original accepted directives precedence, preserving their
exact ordered segment. It removes only duplicate wrapper additions and the demonstrated nonexistent
wrapper namespace. All three generators use it; no fake helper, suppression, warning disablement,
SDK/root/project/capture/parser change or generic framework is introduced.

`generated-imports.delta.json` records exact source hashes and diffs for **82 materialized sources**:
**70 consumer + 12 test**. Every non-prefix remainder is byte-exact. All original directive bytes and
accepted raw/decoded bodies, authority/review fields and source boundaries remain exact. The two existing
same-body/context aliases remain aliases; EXT-234's metadata now points explicitly at its already selected
EXT-204 canonical source/prefix/hash, not a new compilation or materialized namespace change.

## Whole-population checks and preserved evidence

- All **244 consumer + 56 selected test sources** checked, not just emitted failures.
- Before: 46 sources had duplicate imports; 37 had the bogus added namespace (including test preparations).
- After: **zero duplicate imports; zero nonexistent repository namespace imports**.
- `scan-final.json` distinguishes `using static CryptographicDefinitions` as a type import, correcting
  the initial scan's namespace heuristic. That accepted directive remains unchanged.
- Composer positive cases cover original precedence, repeated additions, additions on both sides of the
  original segment, static-versus-namespace imports and exact separators; a resource statement is rejected.
- `preservation.json` binds **281 immutable records** from both failed attempts, seven preparation
  predecessors and a single exact **300-source** text snapshot. Old profiles and failure receipts stay
  at their original paths; no failure is relabelled. The regression verifier reads the qualified retained
  log, not the rotating `artifacts/toolchain` copy.
- `verify.py` passes raw authority/body reconstruction, all 300 sources, prior bounded review guards and
  original pilot record/copy identities. All ten source contexts pass the fresh fixed-input profile.

`verification.json`, `preparation.delta.json` and `profile-delta.json` contain the static results and
exact participant changes. An unreleased candidate snapshot records the final verifier adjustment to
read the immutable retained log. It was never used for a build or accepted as a compiler proof.

## Fresh candidate profile

`../reviewed-inputs-imports-fixed.json`

**SHA-256:** `72753dce86098c486a9098bb79c1498efb2127fcc0192a1e6999ef300a8ed9c5`

Derived from unchanged predecessor `54170deeb614b2683903b11ed2a52495a16335f0eb787c39a71f1a4b3f66f9ae`:
**86 changed existing rows** (82 generated sources plus four preparation/verifier files), **one manifest
replacement**, **eight explicit added inputs**, **1,628 predecessor rows unchanged**, **1,723 final rows**.
The additions are the composer, regression verifier, external generator provenance, old manifest and
four directly consumed correction-evidence records. Preservation binds historical dependencies transitively.
The **1,330 compile-source paths**, **25 projects**, **361 accepted product paths**, accepted product map,
root/product/qualified tooling and ten contexts remain unchanged. The host-only restore fix remains intact.

## Coordinator retry after focused acceptance

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=72753dce86098c486a9098bb79c1498efb2127fcc0192a1e6999ef300a8ed9c5
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-third \
  --profile "$F/reviewed-inputs-imports-fixed.json" --profile-sha256 "$P"
```

Use the unchanged qualified logger/capture and canonical clean build pattern. Require the actual
complete zero-warning/error receipt and Csc/output/preservation proof before continuing the nine
existing test contexts serially with this profile. Preserve any failure under its fresh label.
Never execute injected example/test assemblies. The final ordinary uninjected clean solution build
must restore test outputs before SDK tests. No SDK build, test, capture replay, native/device/provider
execution, formatter, Git mutation or subagent ran for this correction. Finish Core Pass 1, then stop.
