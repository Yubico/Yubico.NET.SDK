# CTAP2 validation retention — generated archive duplicates externalized

2026-11-04. **Archival pruning only:** no SDK/content documentation/extractor/body/host/configuration/
analyzer-policy change, build, test, assembly/snippet/device execution, commit or delegation.
Final six-context/33-group PASSES remain exactly as recorded in `snippet-validation.md` and
`validation/compiled-examples/context-runs/final-result.json`.

## Final policy and disposition counts

| Original archive disposition | Files | Exact bytes | Current storage |
|---|---:|---:|---|
| Meaningful text/source/raw/wrapped/scripts/metadata | 138 | 2,176,955 | Original in-run archive paths |
| **PRIMARY-PACKAGE proof exception** | **1** | **645,594** | Original pinned xUnit nupkg snapshot |
| Generated compiled/native/apphost/PDB/cache/marker duplicates | 101 | 40,827,466 | Approved external temporary root |
| **Original set, all records verified** | **240** | **43,650,015** | Explicit overlay resolves every record |

Durable archive payload: **139 files / 2,822,549 bytes**. No text relocation occurred. Existing
archived text under historical obj/bin paths remains clearly historical, not active build output.
The sole non-text file anywhere in this run is:

```
validation/compiled-examples/failure-xunit-package-content/files/005-xunit.v3.core.3.0.0.nupkg.snapshot
```

This is the pinned **xunit.v3.core 3.0.0 PRIMARY-PACKAGE proof artifact**, not generated host output.
It preserves offline byte verification of retained `DefaultRunnerReporters.cs` and
`buildTransitive/xunit.v3.core.targets`; no NuGet cache, network, restore or .NET execution is needed
for that package-byte proof.

The generated exclusions are exactly 101 records: compiled assemblies/native libraries, apphosts,
PDBs, generated caches and the up-to-date marker. Opaque caches are not substituted for dependency/
compiler evidence: actual assets/dgspec/props/targets/generated source/Compile/Csc/reference/analyzer/
policy records remain intact. No raw/wrapped input, script, helper metadata, meaningful text or
package proof was removed from durable evidence.

Whole-run inventory after authored retention updates: **529 files / 10,564,493 bytes** (decimal
bytes, including all pre-existing reports/logs and the new explicit retention metadata). The initial
read-only measurement was 626 files / 51,086,794 bytes. The overlay separately records the precise
pre-application inventory after archival-tool authoring; these are different observation points.

## Immutable originals and explicit overlay

All three original failure manifests remain byte-identical:

| Original path relative to run | SHA256 |
|---|---|
| `validation/compiled-examples/failure-using-hoist/archive-manifest.json` | `ae2323b699c1b3ac15408be3c1ede485c30b637d129db93a17d9973d3616beee` |
| `validation/compiled-examples/failure-using-hoist/workspace-manifest.json` | `fab4476972e0e705416dffa16fd819e5928d84a7d87c10b2adad0d167f640674` |
| `validation/compiled-examples/failure-xunit-package-content/archive-manifest.json` | `088344ccfdf6f055d72f736ea2f99d3740db70ab937608a982a5f6e0131a71de` |

`validation/compiled-examples/retention-overlay.json` is additive, not a rewritten failure manifest.
Its SHA256 is `00c52dbc594d18a78199ec64748802eade707671dfa2fbd5015c0df52172f9c2`.
It records **every one of the 240 originals**, including original-manifest membership, original
relative archive path, provenance source path, SHA256, byte size, retained/excluded disposition,
reason and exact current location. There is no blanket missing-file skip or inferred omission.
The empty `retained_text_relocations` list explicitly records zero text moves.

All 101 externalized copies are under:

```
/tmp/opencode/ctap2-001-omitted-build-artifacts/<original run-relative archive path>
```

The approved external root is not a Git record. Its contents are generated duplicate artifacts,
not inputs needed to reconstruct the examples or compiler workflow. The durable overlay retains
their identity/size/provenance and external disposition; the current machine verifier additionally
checks every external copy's bytes. If temporary storage is later removed, this full-copy check
will fail explicitly rather than silently treating missing copies as verified. Meaningful durable
text and the primary-package byte proof remain independently retained.

## Safe move sequence and protected finals

`validation/compiled-examples/retention.py` was authored through patch. Before moving anything,
all **240 original files** were hashed and checked against their existing immutable manifests.
Only records mechanically identified as known generated duplicates were selected.

All selected files were copied to the approved dedicated external root. **Every destination hash/
size, and the entire original set again, was verified before the first original copy was unlinked.**
No unverified deletion, broad directory removal, unknown/user-file removal or live build-output
mutation occurred. Empty archive directories are not Git content; retained obj text is historical.

The overlay also protects **199 final input/evidence files**: source extractor/selection, manifest,
all raw/wrapped sources, source verification and package collector, generated hosts/configuration,
actual compiler-input evidence, full successful logs/results and six-PASS aggregate. Their hashes
and sizes are unchanged. No final compiler input snapshot, SDK reference, analyzer configuration/
policy, host or PASS result was regenerated or rewritten for retention.

`verify-validation.py` was adapted only as the archival verifier. It resolves original archive
records through exact overlay entries, then performs the original body, raw, context/build/result,
compiler input/reference/analyzer and policy checks. The source extractor and all original compiler
snapshot hashes are untouched. Historical full-workspace creation claims remain historical with
a leading retention update in `snippet-validation.md`.

## Exact commands and fresh outcome

From PRIMARY, no .NET commands:

```bash
RUN=docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001
python3 "$RUN/validation/compiled-examples/retention.py" --plan
python3 "$RUN/validation/compiled-examples/retention.py" --apply
python3 "$RUN/validation/compiled-examples/retention.py" --verify
python3 "$RUN/validation/compiled-examples/verify-validation.py"
```

`--apply` is intentionally one-shot and refuses existing overlay/external storage; never rerun it
to overwrite provenance. `--verify` is read-only and rerunnable.

Fresh verifier output: **PASS, all 240 original archive records resolve/hash-match, all 101
excluded dispositions match their external copies, all 138 text records and the single package
proof hash-match, offline primary-package source/targets equality PASS, 199 protected final files
unchanged**. The integrated verifier again confirms **six clean PASSES / zero warnings / 33 groups,
95 original blocks unchanged, all 33 readable.raw files unchanged**, with no host/test/device execution.
No final independent-review verdict is inferred; accepted build evidence and pending review remain
separate.
