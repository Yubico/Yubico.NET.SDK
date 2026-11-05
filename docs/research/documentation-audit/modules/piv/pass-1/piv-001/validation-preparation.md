# PIV validation tooling preparation

## Ready for coordinator execution

Sol prepared source-only utilities on 2026-11-05 from baseline
`727fe43afc430c5d8364d868a6436de50626e476`. Ownership covers this record and `validation/`.
Root instructions, the run plan, campaign workflow, approved style and SDK house style govern
the preparation. Module investigation and evidence, product edits, compilation, tests, formatting,
hardware operations and publication remain with their assigned owners.

Use affirmative, task-first prose throughout later records and product documentation: name the actor
and actual behavior, preserve useful rationale and keep local conditions beside their claims.
Reserve negatives for consequential limitations and pair them with supported actions.

[`prepare-tools.py`](validation/prepare-tools.py) verifies the final frozen OpenPGP manifest
`71698c428044b99165fb68759a2e47b6af701187f0ddb7b5838e5bc3278bf7ee` (607 entries), then verifies
each selected source's exact size/hash before transformation. The retained
[`tool-provenance.json`](validation/tool-provenance.json) records original and target paths/hashes,
literal substitutions with counts, removed-region markers/hashes, the generator and all four baseline
record identities. Reproduction reuses identical prepared files and rejects changed targets.
Historical results stay in their frozen archives; these files describe fresh preparation only.

## Start retaining coordinator commands immediately

Run from the primary checkout and serialize toolchain/device operations. Assign a fresh lowercase
evidence name to each attempt. The wrapper exclusively creates the full combined-output log with the
exact command and exit status, then immediately copies discovered UTF-8 toolchain artifacts before
five-run rotation. Each artifact receives path/size/hash and a retention/exclusion decision. Binary
and cache artifacts receive identities; their contents remain outside the run. The wrapper rejects
symlinked artifacts and a frozen destination. Command authority stays with the coordinator.

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
CHECKER=/tmp/opencode/piv-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.AuditPiv001.Checker.dll

python3 "$RUN/validation/run-command.py" checker-build-1 dotnet toolchain.cs -- build --project AuditPiv001.Checker --clean
python3 "$RUN/validation/run-command.py" unit-1 dotnet toolchain.cs -- test --project Piv
python3 "$RUN/validation/run-command.py" final-tokens dotnet "$CHECKER" tokens "$ROOT"
python3 "$RUN/validation/run-command.py" final-api dotnet "$CHECKER" api "$ROOT"
python3 "$RUN/validation/run-command.py" final-preservation python3 "$RUN/validation/check-preserved.py"
python3 "$RUN/validation/run-command.py" final-records python3 "$RUN/validation/check-records.py" --reviewed
```

The exact unique project selector is **`AuditPiv001.Checker`**, filename
`validation/checker/src/Yubico.YubiKit.AuditPiv001.Checker.csproj`. Outputs use
`/tmp/opencode/piv-001-doccheck/{bin,obj}/`. Recursive filename discovery includes archived project
names and verifies this single match; later hosts use the separate `AuditPiv001.Examples.*` prefix.
The coordinator reads the actual build's selected project, exit status and final RESULT warning count.

## Preservation and final reconciliation

The checker exposes only `tokens` and `api`. Strict Roslyn comparison retains active executable
tokens, directives with their token anchors, disabled text and skipped tokens. Directive-free
using-list order receives the inherited normalization. Import-removal and ad-hoc compilation routes
were removed. A missing baseline source, deleted source or empty changed source set fails closed.
API mode requires the complete baseline set of **20** snapshots and exact byte equality, covering
the repository surface alongside the two PIV snapshots.

[`check-preserved.py`](validation/check-preserved.py) verifies all seven original manifest identities
and their **3,401** entries, then the four original user files and **114** PR745 integration files.
The integration identity includes its original compilation/capture failures, corrected captures and
compiled-guide evidence. The checker reads these archives and records without changing them.

The existing inventory supplies **123 paths / 111 C#**: 33 library, 36 PivTool, 18 unit and 24
integration sources, plus four Markdown, four project, two API and two runner files. Every row pins
working bytes and Git bytes separately. [`reconcile.py`](validation/reconcile.py) later verifies
baseline Git hashes, exact accepted final hashes and unchanged inventory bytes. For unchanged paths,
it explicitly reports a working/Git or CRLF-to-LF normalization difference rather than treating it
as an accepted content edit.

After the final reviewer accepts the formatted inputs, supply a run-local manifest with
`baseline` and `files` rows containing repository-relative `path` and `final_sha256`:

```bash
python3 "$RUN/validation/run-command.py" final-reconciliation python3 "$RUN/validation/reconcile.py" --reviewed "$RUN/reviewed-files.json"
```

The coordinator owns `reviewed-files.json`; the utility prints its receipt and leaves that record
unchanged. Coverage denominator, effective accessibility, sampling eligibility, accepted shared-file
scope and Description-only project changes retain explicit investigator/coordinator reconciliation.
The small utility provides hash/normalization evidence alongside those semantic records.

`check-records.py --reviewed` verifies the same final-hash schema. Its ordinary mode checks top-level
run Markdown links/fences, all retained UTF-8 text, and registered primary PDFs. Additional campaign
handoffs use repeated `--markdown` arguments. External local links receive existence checks; complex
Markdown and external URL/content acceptance retain independent review.

## Final example preparation and compiler policy

[`examples/readiness.json`](validation/examples/readiness.json) reserves fresh revision/context/attempt
identities and records the requirements for later preparation. Final product example extraction starts
after fresh content acceptance and the scoped formatter. This assignment supplies a bounded reusable
analyzer-policy helper rather than premature snippet bodies, generated hosts or copied compiler results.

[`examples/analyzer_policy.py`](validation/examples/analyzer_policy.py) reuses the corrected frozen
OpenPGP `analyzer-package-1` proof through `prove()`. Its historical receipt/result coupling was removed
with an explicit provenance transformation. It retains the pinned 9.0.0 package and SDK10.0.403 import
proof; a toolchain change requires a fresh adjudicated revision.

Apply these lessons in the actual capture owner’s new tools:

- Verify real `/warn:9999`, `/warnaserror+`, enabled compiler/analyzer execution and exact actual-Csc
  source/reference/analyzer/config membership. Retain the Csc arguments immediately after compilation.
- Accept `EnableNETAnalyzers=false` only with the canonical package proof: generated NuGet imports,
  restore assets and both exact NetAnalyzers9.0.0 DLL identities participating in actual Csc. The package
  selects its explicit assemblies in place of duplicate SDK built-ins; the property alone establishes
  neither analyzer execution nor analyzer absence.
- Record host `Version` and `YubiKitVersion` independently. Existing root import order can yield
  host1.0 and SDK2.0; actual resolved SDK reference identities establish binding. A false equality gate
  would confuse host metadata with SDK provenance.
- Compile the actual **36-source PivTool** project using its real dependencies, canonical policy and
  ordinary shipping `bin/obj`. Keep its exact late capture hook identity and actual SDK/import sources.
  Unique traversal/host selectors prevent the old Management-style archived-project collision.
- Retain complete source/config/import/project/assets text and binary dependency identities. Bind each
  capture to the final extraction manifest and reviewed source hashes. Preserve compiler failures and
  capture/provenance failures separately, then name corrected attempts explicitly.
- Use complete meaningful bundles with typed prerequisites and resource lifetimes. Compilation supports
  binding/analyzer claims; hardware probes belong to a separately assigned future owner.

## Static readiness and eventual freeze

[`preparation-static.log`](validation/preparation-static.log) records exit0 for Python/XML syntax,
replayed transformations against frozen originals, unique checker discovery, both inventory hashes and
campaign evidence attributes. This is source-preparation evidence. The coordinator still executes
the actual .NET checker build, token/API comparisons, preservation, final accepted examples, repository
gates and separately scoped hardware work.

The existing campaign attributes preserve bytes/whitespace under `modules/**/validation/**`,
`modules/**/evidence/**` and `modules/**/hardware/**`, including new projects and records in those scopes.
[`primary-binary-sources.json`](validation/primary-binary-sources.json) starts as `[]`. Later adjudicated
primary PDFs require run-relative `path`, exact `bytes` and `sha256`; record and freeze tools check
the `%PDF-` signature and registered identity. Other retained artifacts stay UTF-8 text. Build outputs,
caches and symlinks remain outside the archive.

Keep preparation/checkpoint evidence immutable and add uniquely named later attempts. The run stays
open through final review and the final in-run evidence write. Then freeze once:

```bash
python3 "$RUN/validation/manifest.py" write
python3 "$RUN/validation/manifest.py" verify
```

Capture those two commands externally, for example `/tmp/opencode/piv-001-manifest.log`; an in-run
logger would change the archive during freeze. The manifest exclusively records this run/baseline
and the exact artifact path/size/hash/kind set, excluding itself. Publication and later status updates
belong in the external handoff once frozen.
