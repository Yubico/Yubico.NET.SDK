# Integrated software validation

The retained software commands passed against resolved product tree
`05d4409729dd81c7717c99a15a2697d49eb10906`, with zero compiler/analyzer warnings and no test failures.
This tree joins audited parent `d5d01ef99f1478e4d49911194c17667b2450d2fd` and incoming parent
`e322712d609aaf03b9bd1cf2964a99326728866a`. Final narrative reconciliation changes no C# source;
the 1,363-file active-source fingerprint remains
`c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02`.

Astra executed the retained build/test/complexity/package batch on the Linux integration host.
GPT-6.1 Sol inspected its complete RESULT blocks, project logs, receipt file hashes and source
selection, then executed the scoped formatter and static reconciliation. The receipts preserve
commands and capture-time actors; this summary does not relabel predecessor execution as Sol's.

| Retained lane | Command after `dotnet toolchain.cs` | Observed result |
| --- | --- | --- |
| `verifier-build/` | `-- build --project Yubico.YubiKit.WindowsMerge20261113.VerifierCompile --clean` | Fresh real-project-reference aggregate compilation passed, zero warnings. Mac and Linux verification contexts are referenced, not executed. |
| `clean-solution-build/` | `-- build --clean` | Full Release solution rebuilt with `--no-incremental`, zero warnings/errors. |
| `ordinary-unit-suite/` | `-- test --filter "Category!=RequiresHardware&Category!=RequiresUserPresence"` | 4,620 executed, zero failed, five existing skips; all 13 projects passed. Core: 2,236 passed / four skipped. PublicApi: 25 passed. |
| `portable-evidence/` | `windows-hid-verification-evidence-test` | 21 child/coordinator guards and 19 publication guards passed. The log also preserves two managed metadata-default checks. No Windows native APIs ran; publication fixtures are synthetic files. |
| `device-event-lifecycle/` | `-- test --integration --project Yubico.YubiKit.Core.IntegrationTests --filter FullyQualifiedName~Yubico.YubiKit.Core.IntegrationTests.Devices.DeviceEventLifecycleTests` | Exact three-case selection: three executed, zero failed/skipped. No physical hotplug or listener-native acceptance. |
| `complexity-from-audit/` | `-- complexity --complexity-args "--base d5d01ef99f1478e4d49911194c17667b2450d2fd --json artifacts/windows-merge-complexity-from-audit.json"` | Three new/worse methods relative to audited source; all are received unchanged methods with incoming justifications. |
| `complexity-from-incoming/` | `-- complexity --complexity-args "--base e322712d609aaf03b9bd1cf2964a99326728866a --json artifacts/windows-merge-complexity-from-incoming.json"` | 17 unchanged violations, zero new/worse. Both complete JSON reports are retained alongside these records. |
| `docs-inclusive-pack/` | `-- pack --include-docs` | Ten local `1.0.0` packages, zero warnings. Static inspection verified ten XML/readme pairs; each package README equals its module's `PACKAGE_README.md`, not its full README. |

The three incoming complexity justifications apply to `WindowsFidoHidNativeOwner.CompleteRequest`
(15/18), `WindowsFidoHidNativeOwner.Advance` (14/14), and
`HidConnectionSlot.OpenBuiltInConnectionAsync` (12/1). Their executable lines are unchanged from
incoming. The original justifications are retained in `execution/incoming-complexity-justifications.txt`
and must accompany the merge commit. No new integration state machine is waived by that inheritance.

## Inventory, API and test accounting

`BoundaryInventoryTests.Actual_core_source_has_a_reviewed_inventory_with_nonzero_counts` appears
explicitly running in the retained Core log. Its source scans actual Core, requires exactly 245 unique
sites, checks classification counts and rejects stale/unclassified entries. Static reconciliation
independently counted the checked-in 245 entries, 59 operations / 104 unique runnable profile links
and 31 required public entries / 31 rows. The ordinary suite passed the source/profile/public-sync
registry tests and linked managed behavior tests; the Ctap2 and YubiOtp projects ran their cross-layer
cases in that same suite. Those links are not 104 extra test executions, and no duplicate focused
suite was run. The historical 229 unchanged / 16 migrated identity split remains a receive-time mapping.
All source families retain `outstanding` and none acquires `verified` status.

All 20 API snapshot files are byte-identical to audited HEAD. Relative to incoming, only the three
accepted public Base32 entries differ. PublicApi's 25 executed cases are already included in 4,620.
Core's four skips are the Windows registration test, two SCP hardware tests, and the child-only audit
body whose outer process runner remains in ordinary coverage. Cli.Commands supplies the fifth skip.
Do not sum historical platform cases, registry links, nested child bodies or portable guards into
the ordinary-unit total. The separately selected lifecycle cases are three additional executions.

The final example selector preserves 66 bodies (64 fences and two XML bodies) and found no renamed
dependencies in the 302 accepted wrapper bodies. No example was recompiled or executed. The existing
Core001 example campaign retains its original source and preparation counts; the fresh verifier
aggregate proves its real project contexts compile without duplicating that campaign.

## Formatter scope and inherited findings

Sol ran `dotnet format` with `--verify-no-changes --no-restore`, separate space-delimited `--include`
arguments and JSON reports. Both commands exited zero with empty reports and rewrote no files.
MSBuild evaluation confirmed all 15 integration-edited Core paths are actual Compile documents of
the solution's Core project, and the remaining `BlockingReportConnection.cs` is an actual document
of the separately selected Linux verifier project. Thus the 16-document result is not an empty match.

The solution formatter reports duplicate PublicAPI additional-file notices; they are inherited
workspace notices, not formatting changes or compiler warnings. No imports or declarations were
changed. Other received source paths, file-based `scripts/toolchain` inputs, Markdown and props are
outside this formatter claim. Frozen audit formatter failures and their import/declaration and
context-sensitive whitespace qualifications remain unchanged; this is not a repository-wide clean claim.
See `format-core/`, `format-linux-verifier/` and `evidence-reconciliation.json` for actual membership,
commands, reports, package hashes and verified predecessor receipt identities.

## Platform and publication boundary

These are software checks on a Linux host, not a new Linux native/device acceptance campaign.
Windows first-FIDO-open failure and the original attended 17-pass/1-fail ceremony aggregate remain.
Each of the two historical unsealed JIT bundles independently retains 10 scenarios / 65 checks and
its 207-file artifact map; neither proves HID-driver races or sealed Native AOT execution. Raw Mac
artifacts remain unavailable locally, with reported FIDO/PCSC passes PR-summary-only and OTP
permission/expert-topology blocks explicit. AOT, driver, native listener/touch/removal, loaded-producer
and PIV physical isolation/power/access/cancellation gaps remain separate. Normal PIV reset retains
F9 attestation material. No credential operation, physical campaign or broader deferred fix was run.

Sol's final `dotnet toolchain.cs -- docs-qa docs-architecture` passed after active documentation edits,
with zero warnings: 74 active documents, ten package READMEs, six mapped diagram entries and seven
fresh diagram images. `final-documentation/` retains the complete result and logs. No package README,
XML comment or C# changed after packaging, so no additional runtime suite or packaging replay is needed.
Independent publication review and exact remote identity are separate gates in the handoff; software
passes alone do not authorize publication.
