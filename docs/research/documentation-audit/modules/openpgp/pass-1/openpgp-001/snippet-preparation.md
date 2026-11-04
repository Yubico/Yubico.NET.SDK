# Final documentation example preparation

## Active capture tools — canonical analyzer-package correction

Consumer compilation passed with zero warnings and exit zero in coordinator run
`20261104-153522-834-build`. Its immediate `consumer/first` capture failed at the preparation guard
requiring `EnableNETAnalyzers == true`. The actual compiler received ten analyzers, including both
canonical NetAnalyzers 9.0.0 DLLs. This was a capture-assumption failure, rather than skipped analysis.

Root `Directory.Build.props` initially enables SDK analyzers; root targets reference the centrally
pinned `Microsoft.CodeAnalysis.NetAnalyzers` 9.0.0 package. The actual generated NuGet props import
its `buildTransitive/Microsoft.CodeAnalysis.NetAnalyzers.props`, which conditionally imports
`DisableNETAnalyzersForNuGetPackage.props`. The latter sets `EnableNETAnalyzers=false` and
`_NETAnalyzersNuGetAssemblyVersion=9.0.0`, replacing duplicate SDK built-ins with the explicit package.
The pinned SDK imports directory props before generated NuGet props. Both package analyzer DLLs
participate in actual Csc. Final frozen YubiHSM capture likewise checked actual analyzer membership
without requiring a true built-in-selection flag.

[`analyzer-package-1/manifest.json`](validation/examples/analyzer-package-1/manifest.json) is the
minimal capture/verification correction receipt. It pins the unchanged v1 extraction, original tools,
versioned before-tool snapshots, exact substitutions, new concrete capture/verify/helper identities,
the failed first capture/logs, current workspace, package/import text and analyzer binary hashes.
It references the original bodies and preserves all 17 groups, 407 dispositions and 110 dependencies.

The corrected guard requires this specific pinned replacement: exact root reference/version,
resolved assets, generated NuGet props/targets, package import/disable bytes, both canonical analyzer
paths/hashes and exact equality between actual Csc analyzer arguments and resolved Analyzer items.
Warnings-as-errors, level 9999, enabled analyzer execution, no skip-analyzers, and existing complete
source/reference/configuration/assets and SDK/native-output checks remain active. A different package,
version, import assignment, missing assembly or disabled execution requires fresh adjudication.
Snapshots preserve package/SDK/project XML; analyzer binaries receive identities only.

[`analyzer-package-1/static.log`](validation/examples/analyzer-package-1/static.log) confirms the
precise package/import/Csc explanation, all ten analyzer arguments, warning/no-skip policy, exact tool
transformations and intact workspace. Its [`prepared.log`](validation/examples/analyzer-package-1/prepared.log)
confirms all original groups/dispositions/dependencies still pass prepared verification. This correction
performed no build, test, hardware, formatting, root/SDK/project/output edit or analyzer disablement.

Capture `consumer/second` from the intact workspace first; its successful existing compilation needs
no rebuild. Then build and immediately capture shipping `openpgptool/first`, and use the corrected
verifier with the exact two attempts and `--live`. Original `consumer/first` remains failed historical
evidence. The commands below select the active corrected tools.

## Current outcome

Final post-editor/reviewer/formatter extraction is prepared. The coordinator released the exact 59
reviewed product paths, including 49 whitespace-only C# formatter changes with identical imports.
[`prepare-final.py`](validation/examples/prepare-final.py) reconciles every final reviewed hash,
the independent pre-format acceptance, all five resolved required findings, the authoritative seven-path
correction report, the formatter transform and the Description-only package-project change.

[`v1/manifest.json`](validation/examples/v1/manifest.json) is ready for coordinator compilation:

- **17 complete consumer groups:** 12 unchanged README C# bodies / 197 body lines, two shared generic
  user-presence wording/callback groups, and three meaningful inline task expressions.
- **407 explicit dispositions:** 17 compile, 29 outside-applet, 351 symbolic, four non-C# recipes/
  pseudocode, five non-executable XML layouts/algorithms, and one intentional ellipsis fragment.
- **11 document inputs:** four module Markdown files and all seven assigned shared documents;
  46 full fences, 335 inline units, 22 XML units and four OpenPGP migration-map entries.
- **84 C# XML inputs:** all 44 library, 19 tool and 21 test sources, with complete source identities.
  The five XML `code` units are byte-layout/serialization diagrams or S2K pseudocode; the other 17 are
  inline `c` notation. No executable annotation group needs the annotation host.
- **110 hashed dependencies**, including all 96 final module paths, every reviewed shared document,
  root build policy and bounded Core public-signature snapshots; 30 prepared source/configuration/
  provenance files, seven Python tools and 11 approach records are pinned separately.

The final README is
`db23caf2e117418d76c0c3bbc5c2e14ff84bf267d6c1e0dcd7276dccbfb4e811`.
The extraction manifest is
`5b7879c41ca57ea03f88739db3f2ba10468a7221f5bea8a76bd538809b11a2c4`.
The generated consumer source is
`8248882041e14b263bed7b16633e43d1a49bdc34d85bc7ef5ebcced075b457d6`.
[`v1/preparation-provenance.json`](validation/examples/v1/preparation-provenance.json) binds the
release, original tooling/archive, current source categories, SDK inspection and real Core signatures.

[`verify-v1-prepared.log`](validation/examples/verify-v1-prepared.log) records successful prepared
verification of all current bodies, groups, dispositions and input hashes. The independent static
inspection in [`final-static.log`](validation/examples/final-static.log) also reconciles complete
unchanged source segments, original source hashes, Python/XML syntax, pinned SDK policy and text-only
retention. Actual .NET binding/analyzers and Csc evidence remain coordinator execution gates.

## Prepared scope

Preparation uses the final frozen YubiHSM archive and integrates its SDK-default capture correction
directly into the new tools. The released final extraction adds current complete original bodies,
typed wrappers and exact input provenance. Retention covers source, configuration and text evidence;
compiler results will be captured after the coordinator's real builds.

Ownership covers this record and `validation/examples/`. The OpenPGP run plan and approved
affirmative, task-oriented style govern the handoff. This assignment performed static Python/XML/
provenance inspection, final document/XML extraction, bounded public-signature checks and a read of
the shipping project context. Module investigation, product editing, compilation, tests, formatting,
hardware, delegation and commits remain with their assigned owners.

## Sources and integrated corrections

[`prepare-tools.py`](validation/examples/prepare-tools.py) verifies the final YubiHSM manifest
SHA-256 `9a32ac8f9d2443a65864dbe44432c6d4b1dd40ad7f00b676a6c0ecae950631dc`
and the selected frozen source rows before preparing files. The archive has 477 entries.
[`tooling-provenance.json`](validation/examples/tooling-provenance.json) records all 11 source/target
paths and SHA-256 hashes, literal substitutions and counts, two frozen lesson-source identities,
all current Python-tool identities and the observed OpenPgpTool project hash. The baseline is
`fe180282a2d77b15eaaa5868d6c8b00e251d1f05`. Existing identical preparation is reusable; changed
existing files fail closed.

The substitutions select `AuditPgp001`, OpenPGP/OpenPgpTool paths and
`/tmp/opencode/openpgp-001-examples-v1-*` workspaces. They retain the final inventory and actual Csc
membership algorithms while replacing HSM-specific document selectors and fixed sample counts.
Documentation-unit and group counts are determined from the released final inputs. The shipping
source-set guard requires the actual 19 current OpenPgpTool paths.

The final YubiHSM `sdk-default.py` and `post-format.py` provide the correction provenance. This run
uses integrated tools and direct extraction of post-format inputs, rather than replaying their
historical overlays. [`sdk-policy.py`](validation/examples/sdk-policy.py) checks SDK 10.0.403's exact
default expression, the conditional `Exists` import, the resolved
`/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403/vCurrent/Custom.Before.Microsoft.Common.props`
path and the absence of that target and directory. Capture accepts either an empty early-hook value
or this exact dormant built-in default. A different SDK, hook value or newly present target requires
fresh adjudication. Actual capture retains the SDK source snapshots and observed policy.

## Hosts and actual-tool traversal

The recursively unique project-filename selectors are:

- `AuditPgp001.Examples.v1.consumer`: genuine OpenPgp project reference and canonical repository
  build/analyzer policy; `Examples.cs` contains the 17 final typed groups.
- `AuditPgp001.Examples.v1.annotations`: genuine SDK/Tests.Shared references and the inherited real
  xUnit integration-test package/analyzer context. Activate this host only when final annotation
  examples need it; this extraction leaves it inactive because the XML units are diagrams/pseudocode
  and inline notation. Compilation alone supplies no test execution evidence.
- `AuditPgp001.Examples.Shipping.OpenPgpTool`: Restore/Clean/Build/Rebuild traversal of the actual
  `src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj`, including its real package/project references.

The shipping traversal retains ordinary sample `bin/obj`, restore assets and initial/current
extensions paths. Its late hook sets only a capture workspace and `ProvideCommandLineArgs`, with
targets conditional on the actual `OpenPgpTool` project name. It observes compilation without
substituting a reduced sample host or changing output, warning or analyzer policy. Capture verifies
the exact active late-hook path and retains its bytes.

## Released final extraction

The coordinator explicitly released final extraction after editing, correction re-review and scoped
formatting. The initial 58-path `edit-report.md` remains historical; `edit-corrections.md`, independent
59-path acceptance, `reviewed-files.json` and the exact whitespace-only transform govern final inputs.
The first accepted example revision therefore binds the post-format source bytes directly.

[`extraction-scope.json`](validation/examples/extraction-scope.json) records:

- `editor_complete`, `reviewer_complete` and `formatter_complete` set to true, an affirmative coordinator
  `notification`, and the repository-relative
  `editor_report.path` plus its exact `sha256` under this run;
- the distinct `edited_paths` and explicit `shared_paths` (campaign-approved documentation paths,
  including root `README.md` when assigned);
- `shared_sections`, mapping each section-scoped shared path to its accepted heading selectors,
  and `whole_shared_paths` for documents whose examples are assigned in full.

`inventory.scan()` validates that receipt before inventorying module Markdown, explicitly
assigned shared documents, XML `code`/`example`/`c` units, inline calls/notation and OpenPGP migration
mapping entries. Bodies, source positions and identities are retained; external-applet sections
receive explicit outside-scope classifications. The bounded Markdown/XML algorithms support manual
reconciliation of unusual syntax and overlapping nested XML units. Final extraction reconciles the
coordinator's path list and approved section boundaries instead of inheriting HSM dispositions.

The final `v1/manifest.json` uses the final inherited schema: `revision`,
`status: extracted-final-inputs`, `baseline`, `active_contexts`,
`inventory`, `groups`, `dispositions`, `prepared_files`, `tool_sources`, `approach_provenance`,
`dependencies` and `sample`. Each `sample` records the real `project`, all `source_paths` and the
`traversal_driver`. It pins the final scope/reports, every tool including the SDK helper, host/capture
configuration, project/policy inputs and all original module source identities, including the updated
package Description. The prepared active contexts are `consumer` and `openpgptool`.

Every group retains:

- the complete original fence/XML/inline body and hash in `originals`, bound by `unit_id`, and the
  separate exact concatenated `raw` body ledger and `raw_sha256`;
- the generated `wrapper` and any `type_body` as distinct compilation context, with an explicit
  `transform` describing import placement, enclosing methods/types and any necessary adaptation;
- concrete `prerequisites` using real SDK parameters/delegates and `resource_lifetimes` describing
  session/connection/crypto ownership, sensitive-buffer cleanup, exceptional paths and dependencies.

The README groups preserve their complete methods, cancellation, failure handling and resource
scopes. The P-256 group includes its entire real decode helper. The KDF group preserves the aliased
salt/hash arrays, both KDF declarations, provisioning-before-fresh-verification teardown and original
finally cleanup. The decipher group retains its real typed application consumer delegate and
borrow-until-return lifetime.

The shared wording group compiles every original switch arm and default with a genuine
`UserPresenceContext` parameter. The shared request callback remains byte-identical inside a real
`IUserPresencePrompt` implementation; a supplied typed resolution delegate completes the actual
interface without a fabricated SDK/client method or no-op resolution stub. Explicit PIV session
setup and the implicitly WebAuthn `client.GetAssertionAsync` ceremony receive outside-applet
dispositions based on their actual enclosing context.

Three inline groups bind the real RSA-attribute factory, fresh User-attempt query and certificate
deletion task. The attempt-count prose chain is explicitly adapted to
`(await session.GetPinStatusAsync()).AttemptsUser`; the two session helpers borrow a genuine
caller-owned session, and deletion requires prior Admin authorization. Raw original notation is
retained independently of these receiver/await wrappers. Public API snapshots supply real method,
return and callback signatures; Core implementation remains outside this assignment.

Every inventoried unit has one concrete disposition and reason; compiled-group membership is
complete and disjoint. Deferred contracts, accepted Card 3.4.1/scoped-RFC citations and independently
recorded hardware failures retain their separate evidence status. Extraction changes no product
prose, runtime behavior, implementation finding or coordinator record.

## Coordinator execution after final preparation

The final manifest has passed prepared verification and is ready for these coordinator-owned
commands. Use fresh log/capture names for each attempt, serialize builds and capture immediately
after each real build. Consumer compilation has passed; capture its intact current workspace before
any further toolchain operation. This correction performed no compilation.

```bash
RUN=docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001
EXAMPLES="$RUN/validation/examples"
CORRECTION="$EXAMPLES/analyzer-package-1"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
HOOK="$ROOT/$EXAMPLES/v1/openpgptool/OpenPgpTool.Capture.targets"

python3 "$CORRECTION/verify.py" v1 prepared
python3 "$RUN/validation/run-command.py" examples-v1-consumer-capture-second python3 "$CORRECTION/capture.py" v1 consumer second

python3 "$RUN/validation/run-command.py" examples-v1-openpgptool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$HOOK" dotnet toolchain.cs -- build --project AuditPgp001.Examples.Shipping.OpenPgpTool --clean
python3 "$RUN/validation/run-command.py" examples-v1-openpgptool-capture python3 "$CORRECTION/capture.py" v1 openpgptool first

python3 "$RUN/validation/run-command.py" examples-v1-compiled-verify python3 "$CORRECTION/verify.py" v1 compiled --capture consumer=second --capture openpgptool=first --live
```

If the final ledger activates `annotations`, build `AuditPgp001.Examples.v1.annotations`, capture
`v1 annotations <fresh-attempt>` immediately and include its exact `--capture annotations=<attempt>`
selector in final verification. Each active context needs its own successful build log and accepted
capture; compiler/analyzer warnings remain enabled and warnings-as-errors applies. Inspect the actual
toolchain selection, RESULT block and exit status before accepting a build.

`capture.py` compares actual Csc source/reference/analyzer/analyzer-config arguments with the resolved
MSBuild item sets and requires all 19 original shipping sources. It retains source/configuration,
imported-project and SDK-policy text, compiler arguments, project/policy snapshots and restore assets.
Binary references/analyzers and Csc resources receive exact identities; resource arguments remain in
the ledger. The accepted manifest binds the final extraction identity. Failed captures retain their
partial text and a separately classified `capture-failure.json`; a retry uses a fresh directory.

`verify.py` checks complete current bodies, one disposition per unit, disjoint complete groups,
wrapper/resource/dependency records, exact configuration/tool hashes and every selected compiler
capture. `--live` also rechecks dependency binaries, resources, imported projects and restore assets.
Retained text remains useful after caches rotate; runtime/device behavior retains its separate gates.

## Static evidence and handoff

[`preparation-static.log`](validation/examples/preparation-static.log) records a successful
`python3 "$EXAMPLES/verify-skeleton.py"`: six Python tools parse, eight XML files parse, all 11 exact
transformations and two lesson sources reconcile with frozen provenance, three recursive filename
selectors are unique, and the pinned SDK default/absence/Exists policy is valid. That log records the
initial skeleton checkpoint; its pending-body status is superseded by `final-preparation.log`,
`verify-v1-prepared.log` and `final-static.log`. Preserve it as preparation history. The skeleton-only
verifier intentionally expects no extraction receipt and should remain a historical checkpoint now.

The complete final-input preparation is ready for the exact build/capture/`--live` commands above.
Capture directories and compiled-result manifests will be created only after actual coordinator
compilation. Hardware-probe preparation and runtime qualification remain separate assignments.
