# Independent narrow review — consumer-proof namespace correction v2

## Decision

**PASS for the namespace correction and evidence-name adapter, as source-only verification.**
No required change was found in those bounded transformations. **This is not approval of a completed
compiler proof or an assertion that the proposed recapture will pass.** An independent inspection found
a separate, reproducible next-stage blocker in the original Core compiler observations: duplicate
AdditionalFiles. The unchanged strict verifier will reject those observations. The coordinator must
explicitly account for this finding rather than treating namespace readiness as complete capture readiness.

Reviewer: independent Sol consumer-proof correction review, 2026-11-11.
Only this review file was written. No receipt, original record, compiled workspace, C# body, hook,
product file, git index/ref, or other evidence file was modified. No SDK build, restore, SDK test,
capture, formatter, native/device execution, commit, or subagent operation was performed.

## Exact reviewed identities

Paths below are relative to `validation/consumer-proof/` in this integration folder.

| File | SHA-256 |
| --- | --- |
| `proof.py` | `54cb56e12e3b683d2c662c133dc6e23eadef355d7e515f478a87b36a87f25281` |
| `proof-v2.py` | `78024e05a44e82b309c3ec70a5814c13513f0a9436ccc345c224e0b19380b695` |
| `nuget-imports-v2.py` | `2949c4359d893cb600042ba29eb48e7ccadf08e9af6e176c51fc9e1e9c17007f` |
| `run-lane-v2.sh` | `34b9bfc6a85276e1db8993302f2d4980b9d200df11850615a3133e63bc31c3d5` |
| `correction-v2/receipt.json` | `0b76f9cf9222a69f6b97f6916b0b6a0b93812a999bb7268e07f16e6fb3d6f783` |
| `correction-v2/handoff.md` | `00ec820628f75b3d4af029d761c4856e9e36e1ac851672cb6a07fb309fd55c66` |

The effective transformed original source hashes to
`e5ab2196456726c6bb297a02f5b12ea7ef0a4ac4d31250ec554294d3f7386ae8`.
The adapter's receipt check independently passed, including every protected-original/new-tool identity,
the preservation-manifest identity, exact transformation text/counts, and effective-source hash.

## Namespace defect and bounded correction

- The original failure at `proof.py:237` is consistent with the actual generated documents:
  `ElementTree.iter('Import')` returns no standard-namespace imports. The files use exactly
  `http://schemas.microsoft.com/developer/msbuild/2003`.
- Re-read and revalidated all **44 recorded generated documents in 22 restored project contexts**
  against their recorded inspection identities. Seven other contexts remain explicitly unclaimed.
- The actual canonical `Project` expressions and complete `Exists(...)` guards already equal the
  original expected strings. No path-normalization or arbitrary property-expression interpretation
  is needed or added.
- The helper requires the exact standard-namespace Project root, exactly one canonical package import,
  its unchanged Exists expression, the enclosing ExcludeRestorePackageImports ImportGroup guard,
  installed canonical package-file correspondence, and the canonical NuGetPackageRoot declaration in
  generated props. A wrong/missing/duplicate canonical import is not silently accepted.
- Independently invoked the transformed `analyzer_proof` for **both Probe and Core**, using a read-only
  identity collector instead of a retaining/writing collector. Both passed the full analyzer-specific
  checks, including original package metadata, actual canonical analyzer membership, installed disable
  assignment, generated props/targets, and SDK import ordering. This was not capture.
- Canonical analyzer hashes independently agree with the handoff:
  NetAnalyzers `572bf5c83b409a919d01da199c63cc58c7f7cec6cb8037419075471b47cb84c6`;
  CSharp.NetAnalyzers `cd27a044799acfc9e04c451488b75dd20cd49ded5fd2dc7c6ff2dac0656cf1d5`.

## Original guard preservation and evidence routing

A textual diff and top-level AST comparison show changes only in `analyzer_proof`, `capture`, and
`verify`: one import-selection replacement, two destination replacements, and additive manifest
identity fields. The source pin and exact occurrence counts prevent broader implicit replacement.

`compiler_project`, `build_receipt`, `graph_proof`, `restored_binary_bindings`, `current_inputs`,
`check_seal`, and their strict membership/identity helpers remain unchanged. Consequently the original
warning-as-error, warning level, analyzer/compiler skip, SDK/compiler provenance, Csc source/reference/
analyzer/config/additional-file equality and duplicate rejection, source inclusion, project closure,
graph edge, restored asset, actual output, emitted XML member/body identity, and live-byte guards
have not been loosened. The original exact Exists comparison remains after the helper call.

The evidence-name alias does not select compiler observations. `attempt` still selects
`RUNTIME/attempts/<attempt>/raw` and its binary-retention directory; only the new evidence destination
uses `capture_name`. Capture still checks the exact successful build selector and actual Csc project
closure against `context`. Verify additionally binds manifest attempt, capture name, v2 revision,
and correction-receipt hash before the original retained/live identity checks. Thus aliasing the
evidence name does not bypass context, warning, or skip-policy acceptance.

New verifier tools are captured as post-build tools, not substituted into the original sealed build
inputs. Original seal and current-input checks independently passed, including all 30 prepared bodies,
the five host projects, original source snapshots, input bindings and hook/tool identities.

## Actual probe observations and separate blocker

The original `consumer-probe-build-first` receipt passed the unchanged read-only `build_receipt`
validation: correct checkout/selector/target, successful complete/quiescent logger receipt, fresh
selected build log, and retained RESULT with zero warnings. Actual raw compiler folders equal exactly
the expected Probe-plus-Core closure.

The four input categories reported in the handoff were independently compared with actual Csc arguments
after the same path resolution as the verifier; their sets match with no duplicates:

| Project | Sources | References | Analyzers | Configs |
| --- | ---: | ---: | ---: | ---: |
| Probe | 4 | 178 | 10 | 3 |
| Core | 315 | 177 | 14 | 3 |

Both actual policies retain SDK 10.0.404, warnings-as-errors, level 9999, build analyzers enabled,
no compiler/analyzer/design-time skip, no WarningsNotAsErrors exemption, and EnableNETAnalyzers=false
with the explicit package analyzers present.

**Finding: Core's fifth category, AdditionalFiles, fails the unchanged duplicate guard.**

In `/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof/attempts/probe-first/raw/Yubico.YubiKit.Core/`:

- `additional-files.txt:1-4` lists `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, then the same
  two absolute paths again. Each resolved identity has multiplicity two.
- `csc.txt:224-227` contains the same duplication as two relative `/additionalfile:` arguments
  followed by their two absolute equivalents. Resolving relative arguments to the actual Core project
  produces the same two identities twice.
- `proof.py:393` calls `items('additional-files.txt')`; `items` at lines 382-384 calls `unique`.
  Independently applying that exact unchanged helper to the actual observed paths raises
  **`ValueError: Duplicate additional-files.txt`**. This occurs before Core's analyzer proof.
  The `/additionalfile:` argument duplicate guard would likewise reject the resolved duplicate arguments.

This finding is not caused by capture-name aliasing or the namespace helper, and it is not evidence
that the successful compilation failed. It is an existing verifier-versus-observation issue that the
original capture had not reached because Probe's generated import failed first. No deduplication,
guard suppression, source edit, rebuild, or recapture was performed in this review. A claim that all
strict Csc categories already pass, or that no further capture issue is known, would be inaccurate.
Its disposition is outside this namespace-only correction and requires explicit coordinator review.

## Preservation and caller-command consistency

Independently read and hashed **all 981 original and preserved record pairs**, including **411 binary
records**. Both original and retained bytes match each manifest identity; there were zero mismatches.
The manifest hash remains
`7993195c1d68e4f893f1306a7e3580ac02ece135298593891b060a27840fc1cc`.
The original failed capture and successful build remain intact, as do the separately classified
failed-build-retention misuse and static inventory-helper failure described in the handoff.

The handoff's representative caller commands are syntactically consistent with the adapter:

```bash
python3 -B "$P/proof-v2.py" static
python3 -B "$V/run-command-v2.py" --label consumer-probe-capture-second -- \
  python3 -B "$P/proof-v2.py" capture --context probe --attempt probe-first \
    --capture-name probe-first-v2 --build-label consumer-probe-build-first
python3 -B "$V/run-command-v2.py" --label consumer-probe-live-second -- \
  python3 -B "$P/proof-v2.py" verify --attempt probe-first --capture-name probe-first-v2 --live
```

These commands preserve the actual original attempt/build receipt and select distinct new evidence;
**they were not executed here, and capture is expected to encounter the AdditionalFiles rejection
described above.** Do not rebuild or rerun the `probe first` lane to conceal or replace these observations.
Any coordinator-run failed capture must remain retained under its new capture identity.

`bash -n` passed for `run-lane-v2.sh`. Its context/selector/build-label/attempt/capture-name combinations
and final-live command naming agree with the handoff; the script refuses an existing compiler attempt
or evidence destination and retains the original hook. Scale-up remains conditional on actual accepted
representative capture/live proof, which this review does not supply.

No whole-Core/source-integration audit was repeated. Late Windows source work, hardware qualification,
runtime/native interoperability, and any subsequent revised verifier are outside these reviewed pins.
