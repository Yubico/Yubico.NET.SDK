# Integrated guide compiler preparation

Status: coordinator reports the 19-group build passed with zero warnings. Versioned compiler capture
and its independent retained-evidence check pass. See `capture-v2-receipt.json` and the correction below.

## Capture correction

The original `examples.py` and extraction `manifest.json` retain their exact prepared hashes. The original
capture failed at line 186 because it required evaluated host `Version` to equal the separately recorded
`YubiKitVersion`. The held build records `Version=1.0.0` and `YubiKitVersion=2.0.0-alpha.2`. The root
`Directory.Build.props:33` assignment has no project-name condition, and actual restored Core, OpenPGP and
Security Domain references also report project version `1.0.0`, assembly version `1.0.0.0`, and informational
version `1.0.0+0c301252d7e8abfd1e84cb0cbc0ce17925f0b963`. The evidence supports separate observed version
fields rather than a host/catalog equality requirement. This task preserves those values and leaves SDK
version evaluation and packaging unchanged.

`capture-v2.py` checks the original tool and extraction manifest against fixed SHA-256 identities, applies
only two in-memory edits (the equality assertion and a new destination), then runs the original capture
algorithm with all its warning/no-skip and actual Csc membership checks intact. Additional checks bind
net10.0/C#14, the three declared project references to restore project entries and actual compiler binary
paths, their observed FusionName/assembly-source versions, and the host's generated assembly attributes.

The retained v2 capture has **five compiler sources, 180 references and ten analyzers**. Both generated
guide sources belong to the five sources. `EnableNETAnalyzers=false` is the recorded SDK analyzer-selection
property; actual package analyzers are present in Csc arguments, `RunAnalyzersDuringBuild=true`, no skip
flag is active, and warnings-as-errors remains enabled at warning level 9999.

- `capture-v1-failure/` retains the exact original tool, extraction manifest and failure policy, plus a
  reproduced failed Python capture command, exit code, stdout and traceback. This reproduction reads the
  held workspace and invokes no build or compiler.
- `compiler-evidence-v2/` retains the successful compiler capture and the exact adapted algorithm source.
- `capture-v2-receipt.json` binds original/adapted tooling, SDK projects/binaries/versions, all retained
  evidence and every held compiler-workspace file. The before/after workspace identities agree.

The original `examples.py capture` remains historical and continues to reject its invalid version
assumption. Use the versioned command below. The successful capture is already recorded; `check` is the
repeatable verification command, while `capture` deliberately refuses to replace it.

```bash
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/capture-v2.py capture
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/capture-v2.py check
```

This context binds the current integrated guides to a new consumer-library host. Frozen module examples
retain their original source/API dependencies and historical results. This preparation leaves those
examples and their records unchanged.

## Included source

- All 13 current OpenPGP README C# fences: 206 original lines, including complete KDF, recovery,
  signing/helper, decipher/callback, certificate and disposal scopes, plus `ShowKeyMetadata`.
- All five current Security Domain README C# fences and its complete SCP11 constructor expression.
  The six consumer groups are ReadOnly, Authenticate, Import, Generate, Reset and OpenScp11. Import and
  Generate each retain the full original authentication block in a separate method, as in the frozen host.
- The frozen Security Domain host has ten groups across several documents: these six README groups,
  one contributor-guide logging group and three hardware-test snippets. This integration host includes
  the six affected guide groups; the other four retain their historical compilation context.

`manifest.json` preserves each complete original body, body/source SHA-256, source line, raw extraction,
complete wrapper, dependencies and lifetime/transformation explanation. It also binds root build policy,
the actual three SDK project references, generator/configuration identities and historical wrapper sources.

The OpenPGP method wrappers follow the frozen consumer layout, using the original imports and every
current complete method/helper. The entry-point fence moves its exact namespace imports to file scope
and wraps every remaining statement in an async method. The Security Domain wrappers follow its frozen
consumer layout, with actual `IYubiKey`, `KeyReference` and `ECPublicKey` parameters. The host supplies
containing classes and typed dependency scopes, while the actual SDK projects supply all SDK APIs.

## Final source binding

| Source | SHA-256 |
| --- | --- |
| `src/OpenPgp/README.md` | `480a8b6c66cba1cc147e369dffb1f678a04648ad93d3a841da4c39c84d3f1d72` |
| `src/SecurityDomain/README.md` | `5633e809fe423abfddbb76db2f17453c04de201bd2f9f02281a051cf3be9a0a1` |

During preparation, the coordinator clarified the OpenPGP introduction's session-owning methods versus
catalog-only example. The initial checker correctly rejected its old whole-file binding
`43f3fb18ad325f9a80c16b7ec0bba05f11e03770eb08fb1c0651a9451fff2e13`. All 13 original C# bodies and line
ranges remained identical. The final manifest updates only that source's byte count and hash bindings;
the generated source and raw extractions remain unchanged. The final extraction check passes against
the new source hash.

## Project and build policy

Project:
`src/Yubico.YubiKit.AuditPiv745Integration.Examples.csproj` under this validation directory.

Selector: `Yubico.YubiKit.AuditPiv745Integration.Examples`.

The host references the actual Core, OpenPGP and Security Domain projects, inherits root
`Directory.Build.props` and `Directory.Build.targets`, and uses the root central package versions and
editor configuration. It adds no analyzer suppressions. The output is a library with two explicitly
included source files and no execution entry point. Its own output/intermediates and capture hooks use
`/tmp/opencode/piv-pr745-20261105-examples`; project-reference builds retain their normal SDK outputs.
Serialize this build with the coordinator's other builds.

Static discovery reproduces the current toolchain's `/src/`, project-prefix and filename-filter rules
and finds exactly this project for the full selector. The path sits outside the shipped SDK-library
layout and has `IsPackable=false`.

## Coordinator commands

Run from the repository root, after the current build has finished. The following build redirects output
to a fresh integration log while preserving the command's exit status. Inspect its RESULT block and exit
status before calling compilation successful.

```bash
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/examples.py check
dotnet toolchain.cs -- build --project Yubico.YubiKit.AuditPiv745Integration.Examples --clean > docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/examples-build.log 2>&1
python3 docs/research/documentation-audit/integrations/piv-pr745-20261105/validation/capture-v2.py capture
```

The versioned `capture` independently rechecks source/wrapper hashes and requires actual Csc source/reference/analyzer
membership to match the hooks. It checks inclusion of both generated files, all three real SDK assembly
references, actual .NET analyzers, warnings-as-errors and analyzer-execution policy. It retains compiler
arguments, source/configuration snapshots, dependency binary hashes, assets and evaluated version/SDK
properties in `compiler-evidence-v2/`. It refuses to overwrite an existing capture. Capture is a membership
and provenance check; the toolchain RESULT supplies the separate build outcome.

Keep the toolchain run's RESULT/log alongside the redirected output before rotating toolchain retention
can remove them. After any guide-body, build-policy or reference-project change, prepare a separately
identified extraction rather than replacing this context's evidence.

## Verification boundary

Preparation checks pass: current original bodies and complete wrappers match, the final source hashes
match, XML files parse, the unique project selector discovers one project, the qualified Security Domain
`KeyInfo` appears in all three administrative scopes, and current OpenPGP `KeyType` calls/checks plus the
catalog method are present.

This session ran no build, test, example, hardware operation or formatter. The coordinator reports the
successful zero-warning build; this session captured and verified its actual compiler membership separately.
Compilation establishes API/source binding for these groups, separately from runtime behavior, device
support, trust verification and the whole-module PIV audit.
