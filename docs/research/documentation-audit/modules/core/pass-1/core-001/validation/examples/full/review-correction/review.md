# Independent Sol confirmation: four Full-example preparation fixes

## Decision

**PASS — all four requested adapter/wrapper fixes are satisfied.** No additional framework, qualified
tool change, accepted-body edit or compiler-context redesign is requested.

**Current source admission remains pending one known accepted transition.** The corrected profile
`cc9da7b2d9520c7b69fa54471b443744fa95e8e1b42c539f7649cb44ac11ef46` accurately records the reviewed
four-fix preparation, but is no longer an executable current-source profile after the accepted Security
Domain prose transfer. Preserve it as a predecessor. The owner must supply a fresh source/map/manifest
admission checkpoint before coordinator execution; do not silently repin or invoke this stale profile.
Only the two expected mismatches below were found.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. Later metadata advancement is not itself a source change.
The original Full review remains exact at
`f1c2d367252874dd6cb8b6b895fa4b5726103986a35ca26853c3899b310f03e0`.

## Four focused correction decisions

| Original finding | Confirmed correction |
| --- | --- |
| FULL-R1: descriptor project base | `capture.py` checks the pinned pilot source and replaces exactly one descriptor-call expression in an in-memory observer instance. `full_descriptors` checks the pinned qualified parser, copies its function-global dictionary and binds **only PROJECT** to the explicit selected primary project. It calls a private `FunctionType`; the qualified module's PROJECT/ROOT and source remain unchanged. Relative Source Link, quoted comma/space embedded paths, absolute paths and resource descriptors retain the proven grammar and raw argument text. The actual observer call site uses this private resolver. |
| FULL-R2: FULL-211 / alias FULL-238 | The import splitter accepts genuine namespace/static using directives and excludes `using var` or a using block. FULL-211's complete original resource declaration and subsequent statements remain inside `CompileOnly`, preserving the same method/disposal scope. FULL-238 points to the canonical FULL-211 source/prefix/suffix/hash in the same actual Core.UnitTests context; it does not materialize a second alias source. |
| FULL-R3a: FULL-244 | The generated synchronous fragment uses `public void CompileOnly(YubiKeyTestState state)`. No dummy await, body edit or warning suppression was added. |
| FULL-R3b: FULL-027 | The generated wrapper adds `using System.Text;` for the unchanged original `Encoding.UTF8` call. No replacement method or fake helper was introduced. |

I read the actual adapter, generator and verifier diffs against their preserved predecessors. The
generator is corrected as well as the three generated files. The private-global adapter changes no
other observer statement; exact source/reference/analyzer/config/file membership, unchanged before
inputs, raw Csc text, output equality and original receipt/preservation prerequisites remain in force.

## Fresh read-only checks and unchanged population

After inspecting its imports/operations, ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/review-correction/verify.py
```

Exit **0**. It independently constructs expected descriptor paths for the Full consumer, Core.UnitTests
and Management.IntegrationTests projects, checks raw argument equality and unchanged qualified helper
PROJECT/ROOT, and rejects relative/foreign/non-project selectors and wrong-project membership. It also
checks genuine imports versus resource declarations and literal reconstruction of all four affected
records, including the canonical alias and synchronous/import fixes. It does not call a capture phase,
write compile evidence or execute managed code.

Separate byte/JSON/source checks confirmed:

- The submitted corrected profile hash is exact; the original profile remains
  `806ce07d050835dbcac7e4ce33ffb2b0006c5c8e9937e4321f3af6f569a4dc5c`.
  All predecessor snapshots and the original review match their preservation records.
- **1,707 original descriptor rows are retained exactly in the corrected descriptor.** Five existing
  participants change (Full adapter/verifier and three wrappers); the old manifest participant is
  replaced by the corrected manifest; the corrected generator and focused verifier are added. This
  gives **1,715 fixed rows**, with no compile-source membership or 25-project closure change. This
  describes the preparation delta, not a claim that the later two source/map bytes still match.
- Across **all 662 additional authority records**, every raw/body/source/authority/review/disposition
  field is unchanged. Only the `compiled` metadata of FULL-027, FULL-211, FULL-238 and FULL-244 changes;
  the top-level correction reference is additive. Original raw/decoded bodies were not rewritten.
- An independent read-only reconstruction, separate from current-profile admission, checked all
  **300 generated sources across ten contexts**, including same-body aliases, and all **538 retained
  raw source regions** against current bytes. The affected ownership declaration stays inside its
  method. All **107 applet-batch example records** retain their source/body evidence. The 34 previously
  accepted noncompiled qualifications and original pilot accounting are unchanged.
- Qualified pilot/parser/logger/checker sources and prior reviews are not changed by these fixes.
  This confirmation does not reopen or rerun the coordinator's separately completed final DocCheck
  build/token/API lane. No further checker change or rebuild follows from this correction review.

## Separate known admission refresh — Security Domain prose only

The current full verifier was also run read-only. It returned **exit 1 at its full-document hash guard**
(`full/verify.py:34`). That failure is correctly preserved as a current source-admission failure, not
reported as a pass. A complete independent profile scan found exactly these two mismatches:

| Fixed input | Profile/predecessor SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `src/SecurityDomain/CLAUDE.md` | `76408f4a4ce1995712731ca7201b390836b9d4ded236268c619424a460d3915a` | `dd24dbcc43606abd77888fecdf9368b0c103b74b860fba1c46d3c10ccdab9022` |
| `editing/integration/current-products.json` (run-relative) | `d33ac76a90640420d868b77dcbfa285e9e1c84ac1c8ab10b644e314fcbb0d23f` | `45ca220e401ba48082e14f138d703df0e29a969dd7ae7d5edc89d2ad805075c2` |

The explicit authority is run-relative `editing/integration/accepted-sampling-SD.json`, SHA-256
`d0998fb931d71ffa990b25a03e5ab31d9c43c14013dd4b4ebbc62720a2a9884e`, accepting patch
`b1cb5c00f569c94499060cd1539edf2bafa67057763126e3786c3f0e83c8ab65`.
Its retained source/review/diff/identity records all match their registered hashes. Comparing the exact
retained predecessor to current source confirms only the two Related Modules bullets changed: the SCP
namespace is `Core.Protocols.SmartCard.Scp`, with `StaticKeys` owned there rather than Cryptography.
The example source regions and bodies are unchanged; this is not a fifth code/wrapper defect.

**Required remaining action is only the promised accepted-source refresh:** preserve the prior map,
manifest and profile; record this receipt/path/hash transition explicitly; bind the current document
and accepted map in a fresh final manifest/descriptor and select that admission consistently in the
verifier. Confirm no further input drift. No new framework, body re-extraction rewrite, broad source
investigation or qualified-tool update is needed. The four fixes need not be reimplemented or reopened.

## Conditional coordinator sequence

Do not run with the stale `cc9da7b2…` profile. Once the owner supplies the fresh final admission and it
passes the bounded source/map check, with exclusive checkout release and the current build skill:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
PROFILE=<accepted-fresh-final-profile-path>
P=<accepted-fresh-final-profile-sha256>
python3 -B "$F/review-correction/verify.py"
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-first \
  --profile "$PROFILE" --profile-sha256 "$P"
```

The first genuine consumer compilation remains the representative before scaling serially to the
nine actual test contexts. Require successful complete zero-warning original command/capture/output
and immediate preservation evidence. Retain first failures. No example or injected test DLL is run;
restore ordinary uninjected test outputs through the final normal clean solution build before any SDK
test lane, including after a failed/interrupted injection attempt. Earlier original pilot/compiler
results remain their own proof, not bookkeeping rebuild targets.

## Reviewed correction identities

Paths below are relative to `validation/examples/full/`.

| File | SHA-256 |
| --- | --- |
| `reviewed-inputs-corrected.json` | `cc9da7b2d9520c7b69fa54471b443744fa95e8e1b42c539f7649cb44ac11ef46` |
| `manifest-corrected.json` | `2f24bb423c7d2751017bf302f6b153308da026c9954acf3eebd40f64a9c0db4d` |
| `capture.py` | `175480934087da62df5c9cfeee3fa4335a1c2727440480408fcb62d4fa4486f2` |
| `test-contexts.py` | `e34f9f79e98b7805596a80024f5c77ad791bcf8394e777c78960ac8be5631ea8` |
| `verify.py` | `39cfa3cd71241849dfeb777e60a47170dcf40dd16145ee101a5b58801c4f009d` |
| `review-correction/profile-delta.json` | `d1cbc439e17f2d5df45a724e08826de829efc170772ed0dbb56f6567b6708cd0` |
| `review-correction/preservation.json` | `ba1f6fa5b200440750cd3299136fbbffd7619a74b5414b41929c9addf1db5069` |

Only this review is written by this focused reviewer. No SDK build/test, actual capture/replay,
runtime/checker/native/device/crypto operation, product/editor-original edit, formatter, Git mutation
or subagent ran. Prior reviews and preparation history remain intact. Complete the remaining
authorized Core Pass-1 gates/publication and deferred handoff, then stop; platform/physical lanes,
Windows handoff and PIV physical deferral retain their existing bounds.
