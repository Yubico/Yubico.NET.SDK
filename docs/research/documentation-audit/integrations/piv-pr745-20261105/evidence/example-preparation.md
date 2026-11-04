# Integrated guide example preparation handoff

## Current update: versioned compiler capture verified

The coordinator reports the 19-group build passed with zero warnings. I captured the held compiler
workspace successfully with a narrowly adapted v2 capture and then ran its retained-evidence check.
No additional build, compiler, test, example or hardware execution occurred.

The original assertion equating `Version` and `YubiKitVersion` was invalid for the observed evaluation.
The host records `1.0.0` and `2.0.0-alpha.2` respectively. The root Version assignment has no project-name
condition; actual Core/OpenPGP/SecurityDomain restore entries are also `1.0.0`, their actual compiler
reference FusionNames report assembly version `1.0.0.0`, and generated assembly informational versions
are `1.0.0+0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`. The correction records these observed domains
separately instead of forcing either property or any SDK version.

Focused installed SDK-source inspection additionally confirms the evaluation-order explanation:
Microsoft.Common.props imports Directory.Build.props before NuGet.props imports Directory.Packages.props;
the SDK later defaults an empty Version to 1.0.0. The earlier Version assignment is not a lazy link to
the later YubiKitVersion definition. Exact installed-source hashes, line references and the default block
are retained in `validation/version-evaluation.md`. This is independent of host naming, and no SDK policy
change was made.

Preservation:

- Original `examples.py`: unchanged, SHA-256 `7a8a4327642048a1e121a37ce02891d25ddff25227ec8232aba2b3949e63374f`.
- Original extraction manifest: unchanged, SHA-256 `6fa660fa570161a5d3035c2492b27651aea306bd9a882f858bdd2ace6f7a37d1`.
- `capture-v1-failure/` preserves copies of those files and the exact policy (SHA-256
  `ef072cb30326126d990698ef12c8beb1ded11e8e55222abc8c098b5acdbc7404`), plus the reproduced original
  failed capture's exact command/exit/stdout/traceback. The receipt labels this as a reproduction of the
  coordinator-reported invocation, not the original terminal output.
- `capture-v2.py` pins the old hashes and adapts only the assertion and destination in memory. All original
  warning/no-skip and Csc membership checks remain, with explicit C#14/net10.0 and genuine SDK project,
  restore, compiler-reference-path and observed assembly-version checks added.
- `compiler-evidence-v2/` contains the successful source/reference/analyzer capture, actual arguments,
  configuration/assets/source snapshots, binary identities and exact adapted tool source.
- `capture-v2-receipt.json` binds the adapter, prior hashes, observed SDK versions, all retained evidence
  and every compiler-workspace file. The before/after workspace file identities agree.

Results: **5 compiler sources, 180 references, 10 analyzers**. Both guide files are compiler inputs.
Warnings-as-errors, warning level 9999, analysis, no-skip checks and actual Csc membership pass. The
recorded `EnableNETAnalyzers=false` does not replace checking the actual package analyzers present in Csc.

Correct capture command (already executed; preserved output rejects replacement):

```bash
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/capture-v2.py capture
```

Repeatable retained-capture verification command:

```bash
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/capture-v2.py check
```

The remainder records the initial preparation handoff before the coordinator's successful build and this
capture correction. Its original capture command and pending-status language are historical.

## Historical preparation handoff

Prepared the new compilation context at:

`docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/`

Only files under that newly assigned validation path and this handoff were authored during this task.
Product guides, frozen module examples and previous integration reports remain unchanged by this task.
The coordinator's prose correction is included in the final source identity.

## Coverage and actual count

- OpenPGP: all 13 current README C# fences, 206 original body lines. Original imports, complete parameter
  lists, every method/helper, KDF scopes, finally blocks, callbacks and certificate/session disposal remain.
  The new catalog example is included, along with both current `KeyType.P256` migration sites.
- Security Domain: every current README C# fence (five), plus the whole inline SCP11 constructor expression,
  assembled into the six original consumer guide groups: ReadOnly, Authenticate, Import, Generate, Reset
  and OpenScp11. The fully qualified `SecurityDomain.KeyInfo` is copied from current source into Authenticate,
  Import and Generate.
- The frozen SD count of ten includes four groups outside the README: contributor logging plus three
  hardware-test snippets. Those unchanged examples retain historical context; this minimal guide host
  includes the six affected README groups, not a purported ten README groups.
- Total: 19 complete groups in two source files, one compile-only consumer-library project. The explicit
  source membership is OpenPgpExamples.cs and SecurityDomainExamples.cs; compiler-generated SDK source
  files are separately checked against actual Csc arguments during capture.

## Final source and wrapper identity

| Input | SHA-256 |
| --- | --- |
| Current OpenPGP README | `480a8b6c66cba1cc147e369dffb1f678a04648ad93d3a841da4c39c84d3f1d72` |
| Current Security Domain README | `5633e809fe423abfddbb76db2f17453c04de201bd2f9f02281a051cf3be9a0a1` |
| `src/OpenPgpExamples.cs` | `5923a9e63643f13480fa16dbdc9cda461a28b4c904f28cc319fa900dd56360db` |
| `src/SecurityDomainExamples.cs` | `ff8620114a43604b6b0dfa49a0526e46ec319dda10b2f20bf8222f0ee620007f` |
| `manifest.json` | `6fa660fa570161a5d3035c2492b27651aea306bd9a882f858bdd2ace6f7a37d1` |

The initial source checker rejected the coordinator's prose-only update because the whole-file hash
changed. I verified all 13 C# bodies and line ranges unchanged, then patched only the manifest byte count
and fourteen whole-source hash bindings. Generated wrappers/raw bodies stayed byte-identical. The final
`examples.py check` passes against the corrected introduction.

## Ready-to-run coordinator commands

Serialize with the existing coordinator gate. Run from repository root:

```bash
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/examples.py check
dotnet toolchain.cs -- build --project Yubico.YubiKit.AuditPiv745Integration.Examples --clean > docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/examples-build.log 2>&1
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/examples.py capture
```

Exact unique project:

`docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/src/Yubico.YubiKit.AuditPiv745Integration.Examples.csproj`

Static discovery reproduces the toolchain predicates and finds exactly one project for this full selector.
It also verifies the three explicit references resolve to the actual Core/OpenPGP/SecurityDomain .csproj
files. Root props/targets, central versions and analyzers remain in effect. The project is non-packable,
has no program entry point and adds no suppressions. Its own temporary outputs and Csc capture hooks use
the unique `/tmp/opencode/piv-pr745-20261105-examples` workspace.

`capture` requires actual compiler source/reference/analyzer membership and warnings-as-errors policy;
retains source/configuration snapshots, reference/analyzer binary identities, project assets, evaluated
version/SDK settings and Csc arguments under `compiler-evidence/`; and refuses to replace an earlier
capture. Build success remains the separate toolchain RESULT/exit-status gate. Retain the associated
toolchain result/log before its normal rotation, in addition to the redirected integration build log.

Fresh static checks passed: extraction/source identities, complete wrappers, XML syntax, unique discovery,
real project reference paths, catalog inclusion, both migrated OpenPGP call/check sites and qualified
Security Domain declarations. Builds/tests/hardware/formatting/commits/pushes/subagents were excluded and
remain unrun by this session. The coordinator's reported repository gates and complexity justifications
remain coordinator-owned; they are separate from this still-pending guide compilation.
