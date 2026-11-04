# Documentation/example compilation preparation

## Authoritative capture-only correction — SDK default path

The coordinator reports both actual builds **PASS, zero warnings**: consumer at 13:40:35 and
HsmAuthTool at 13:41:21. `post-format-1/evidence/consumer/first` is accepted. HsmAuthTool's first
capture failed only at the preparation assertion requiring an empty `CustomBeforeMicrosoftCommonProps`.
Its partial capture and `capture-failure.json` remain unchanged. This is a capture-assumption error,
separate from the successful compiler result.

Installed SDK **10.0.403** `Current/Microsoft.Common.props:88` supplies
`$(MSBuildExtensionsPath)\v$(MSBuildToolsVersion)\Custom.Before.$(MSBuildThisFile)` when the property
is empty. Line 92 imports that value only if `Exists(...)`; `Sdk.props:49` supplies the enclosing
Common.props import context. The captured value is exactly
`/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403/vCurrent/Custom.Before.Microsoft.Common.props`.
**That file and its vCurrent directory are absent** at inspection. This is a dormant SDK-default
path, rather than a file whose contents were imported. No bytes/hash are invented for the absent target.

[`sdk-default-1/manifest.json`](validation/examples/sdk-default-1/manifest.json) pins exact full SDK
source bytes and paths, version, default expression/import condition and observed target absence.
Source snapshots are retained under `sdk-default-1/sdk-source/`: Common.props is 11,765 bytes,
SHA-256 `87a2707df41f5abbbd8ad5678715c169ba40ee7543fef4a98d7852ff17e94e48`; Sdk.props is 2,487 bytes,
SHA-256 `41dc552e294cd1f0c6e640c302c112b0788fa8776ecac73e7fa013382a3bf713`.

[`sdk-default.py`](validation/examples/sdk-default.py) invokes the unchanged post-format dispatcher
and original tools with narrow counted in-memory substitutions. The guard accepts its previous empty
case or exactly this pinned SDK version/path while the default target remains absent. Other external
hooks stay rejected. All native bin/obj, initial/current extensions, assets, actual Csc membership,
analyzer and current-body checks remain active. `tool-adaptations.json` records full before/after
virtual-program hashes and the pinned source/runner transformations. The original tools, post-format
provenance and v1 records remain unchanged; consumer/first is reused directly.

Static checks passed in [`sdk-default-1/prepared.log`](validation/examples/sdk-default-1/prepared.log).
They verify SDK source/default/absence, unchanged failed first and accepted consumer records, intact
compiler workspace text and both layered after-programs. Prepared group verification also passed.
The six actual HsmAuthTool workspace text files match the retained first attempt byte-for-byte, so
the coordinator can capture **second without rebuilding**. The second capture retains the correction
manifest identity; final verification requires that identity for HsmAuthTool only.

### Current commands — capture second, then verify both accepted contexts

```bash
RUN=docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001
EX=$RUN/validation/examples

python3 "$RUN/validation/run-command.py" examples-post-format-1-hsmauthtool-capture-second python3 -B "$EX/sdk-default.py" capture hsmauthtool second
python3 "$RUN/validation/run-command.py" examples-sdk-default-1-final-verify python3 -B "$EX/sdk-default.py" verify compiled --capture consumer=first --capture hsmauthtool=second --live
```

These commands remain coordinator-owned. Preserve first and use the exclusive new second destination;
keep any further capture/dependency mismatch as evidence rather than overwriting or rebuilding here.
No shipping/SDK/toolchain edits, builds, tests, formatter or hardware operations were performed by
this correction. The generic verifier's prepared-only pending banner remains separate from the
coordinator's already recorded successful builds.

## Authoritative post-format checkpoint

The **post-format-1 input overlay** supplied the coordinator's compilation. It reuses v1's exact
raw bodies, consumer source, projects, shipping traversal/late hook and generic capture/verifier
by reference. V1 artifacts and earlier records remain unchanged; the pre-format commands below
are historical preparation instructions, superseded by this block.

[`post-format-1/manifest.json`](validation/examples/post-format-1/manifest.json) binds the accepted
35-path `evidence/review-path-manifest.json`, `validation/pre-format-content.json`, actual scoped
formatter command log, v1 manifest and 26 exact before/after source identities. Its independent
comparison requires the accepted-review and v1 hashes to equal each captured pre-format body.
It preserves every byte after the initial import header except one terminal LF, preserves all
other header text/comments, and permits only these named ordinary-import removals:

- `IYubiKeyExtensions.cs`: `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu` and `.Scp`.
- Integration `HsmAuthSessionTests.cs`: `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu`.
- Unit `HsmAuthSessionByteLevelTests.cs`: `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu`.

The one import/header-group reorder is in unit `HsmAuthRetryExceptionTests.cs`. Import changes
require directive-free source; aliases/static/global imports receive no removal exemption. All
26 files lose exactly their final LF. The complete accepted non-C# files and ten inventoried docs
retain their hashes, all six README bodies remain unchanged, and the 476-unit inventory still
matches. Runtime changes, documentation-body changes, additions and unlisted input changes fail
closed. Coordinator Roslyn token/directive/import-removal proof remains a separate gate.

[`post-format.py`](validation/examples/post-format.py) reuses the pinned original Python tools
through a few exact, counted in-memory substitutions. The overlay records full original and
adapted-program hashes and the literal substitutions. Only the 26 proven dependency identities
are updated for verification; capture output moves to `post-format-1/evidence/<context>/<attempt>`.
Every new actual Csc capture retains/binds the overlay and coordinator transform identities.
All original source/ref/analyzer/config/assets/native-output and current-body checks stay active.
There are no new project names, duplicate raw groups, duplicate capture tools or copied old results.

Independent reconciliation and `post-format.py verify prepared` both exited 0. Evidence is
[`post-format-1/prepared.log`](validation/examples/post-format-1/prepared.log). The coordinator's
exact transform record is pending at this checkpoint. `bind-transform` requires its baseline,
zero formatter exit, exact 26-path before/after hash set, four named removals, zero additions and
affirmative non-import equivalence to agree with the independent proof. It exclusively creates
`transform-binding.json`; capture/compiled verification require that binding and recheck all inputs.

### Original post-format build/capture commands — retain as history

Run serially from the primary checkout. First produce the coordinator's exact format-transform
record and separate Roslyn proof. Then bind it at the path below (or pass its actual repo-relative
filename). These commands were prepared here and remain coordinator-owned.

```bash
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
RUN=docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001
EX=$RUN/validation/examples

python3 -B "$EX/post-format.py" bind-transform "$RUN/validation/format-transform.json"
python3 -B "$EX/post-format.py" verify prepared

python3 "$RUN/validation/run-command.py" examples-post-format-1-consumer-build dotnet toolchain.cs -- build --project AuditHsm001.Examples.v1.consumer --clean
python3 "$RUN/validation/run-command.py" examples-post-format-1-consumer-capture python3 -B "$EX/post-format.py" capture consumer first

python3 "$RUN/validation/run-command.py" examples-post-format-1-hsmauthtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EX/v1/hsmauthtool/HsmAuthTool.Capture.targets" dotnet toolchain.cs -- build --project AuditHsm001.Examples.Shipping.HsmAuthTool --clean
python3 "$RUN/validation/run-command.py" examples-post-format-1-hsmauthtool-capture python3 -B "$EX/post-format.py" capture hsmauthtool first

python3 "$RUN/validation/run-command.py" examples-post-format-1-final-verify python3 -B "$EX/post-format.py" verify compiled --capture consumer=first --capture hsmauthtool=first --live
```

Capture immediately after each associated build. Shipping compilation keeps ordinary native
bin/obj and the existing late-only observation hook. The unchanged consumer host still contains
all eight complete groups; actual HsmAuthTool still requires all 22 sources. Read matching RESULT
blocks/raw diagnostics separately from capture status. The formatter log records workspace-loading
warnings and exit 0; it supplies no compiler/analyzer warning-free verdict. Builds, tests, formatter
execution, hardware operations and shipping/toolchain edits were not performed by this preparation.

## Historical v1 extraction checkpoint

Sol prepared final extraction **v1** on 2026-11-04 after the coordinator released editor
`ses_e599c7023ffetNl1WFvH5NVqnX`. Ownership covers this record and `validation/examples/`;
hardware-probe files and all hardware records remain coordinator-owned. The completed
`edit-report.md` acceptance identity is recorded in `extraction-scope.json` and `v1/manifest.json`.
All six complete README body hashes match the editor report. Twenty-nine of its 35 whole-file
hashes matched; six paths advanced with late response-format/cancellation qualifications:
README, module CLAUDE, shared interaction, CalculateSessionKeys sample, IHsmAuthSession and
SessionKeys. Their reported/current identities are retained for coordinator review reconciliation.
The current README SHA-256 is `258f0f0a21522640bb6cf1618dc5e39a1294701f411bd408efb2f0d4f424dce5`.

Preparation reads root instructions, the run plan/style, final editor report, final documentation
and module XML comments, actual HsmAuthTool project context and frozen final OTP tools. Initial
baseline document inspection used `git show a74792facb657413e98223e059876d8b213ffff3`; extraction
uses the final current bodies. Core/other-module implementations and API snapshots stayed outside
scope. Builds, tests, hardware, formatter, agents, product/toolchain edits and commits remain
coordinator-owned. Current status is **prepared and statically reconciled; compilation pending**.

## Prepared hosts and provenance

| Context | Prepared identity and binding |
| --- | --- |
| Consumer | `AuditHsm001.Examples.v1.consumer`: real YubiHsm project reference, canonical root props/targets, warnings-as-errors and production analyzer policy. |
| Annotations, inactive | `AuditHsm001.Examples.v1.annotations`: genuine centrally versioned xUnit-v2/VSTest/TestShared skeleton. Final module/test guides contain no trait or C# examples needing this context, so it remains outside compilation results. |
| Complete shipping sample | `AuditHsm001.Examples.Shipping.HsmAuthTool`: uniquely named traversal delegates Restore/Clean/Build/Rebuild to the actual `src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj`. Its real packages/references and all 22 baseline source paths are preserved. |

Consumer metadata/binaries use `/tmp/opencode/yubihsm-001-examples-v1-consumer`. Its complete
`Examples.cs`, raw original groups and final extraction manifest are retained under `v1/`.
The inactive annotation skeleton intentionally has no `Examples.cs`; leave it outside builds.

The shipping sample uses ordinary `HsmAuthTool/{bin,obj}` paths and normal restore assets.
`v1/hsmauthtool/HsmAuthTool.Capture.targets` observes only `MSBuildProjectName == HsmAuthTool`
through the late `CustomAfterMicrosoftCommonTargets` hook. It sets only the capture workspace and
`ProvideCommandLineArgs`. The command removes any early `CustomBeforeMicrosoftCommonProps` hook.
Capture requires native bin/obj paths and initial/current extensions-path agreement, carrying the
corrected OTP approach from the outset. Project-import guidance was checked with Context7 and
[Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build).

[`tooling-provenance.json`](validation/examples/tooling-provenance.json) records eight exact
transformed template copies, four source/adaptation bindings, complete source/target SHA-256
identities and literal substitution counts, newly authored tool hashes and the observed sample
project identity. `prepare-tools.py` checks the frozen 433-row OTP manifest against the current
baseline JSON and selected source rows before copying. Existing identical targets are reused;
changed targets fail closed. Prior bodies, results, old output hooks and caches remain in their
original runs. No frozen run is modified or republished as HSM evidence.

## Complete inventory and groups

Ten current Markdown/YAML documents cover four module guides and six explicitly scoped shared
files: migration guide/gaps/map, user interaction, v1/v2 comparison and v2 highlights. The inventory
records **37 fences**, including **34 C# fences**: six complete README bodies and two complete generic
interaction bodies are selected; 24 migration fences outside YubiHSM plus the explicit PIV session
and implicit WebAuthn GetAssertion ceremony receive other-app dispositions. The other three fences
are shell recipes. The implicit `client` is identified from its surrounding `WebAuthnClient` /
`PublicKeyCredentialRequestOptions` prerequisites; no stand-in client is created.

All **409 inline spans**, **25 XML c-spans** across **44 module C# files**, and **five symbolic map
entries** receive explicit dispositions, for **476 units** total. The XML scan includes all 26 edited
C# paths and finds zero `code`/`example` bodies. One meaningful inline one-shot LIST call is compiled.
Private helper signatures and `TouchRequired`/`Application` value notation stay descriptive. The
historical v1 `GetApplicationVersion()` and the prose transport-options ellipsis retain explicit
historical/noncompiling-by-design classifications. Module/test guides contain no remaining fenced
anti-pattern or trait snippets; earlier bad bodies are preserved in history rather than copied here.

The **eight consumer groups** are Listing, Enrollment, SymmetricHandshake, ManagementRotation,
PromptSession, SharedOperationWording, SharedIntentPrompt and InlineOneShotList. Raw groups retain
full original bodies and terminal newlines. Namespace imports move to file scope; complete task
members remain inside a containing class, prompt types receive valid declaration scope, and statement
fragments receive real typed parameters. The shared wording wrapper returns its actual message.
The shared intent callback implements the real `IUserPresencePrompt` interface.

Enrollment retains its real typed provisioning delegate and complete key-clearing scope. Rotation
retains durable storage before PUT. The handshake keeps the **entire original ISymmetricHsmExchange
contract and method**, owned adapter/arrays, borrowed password, final host-use callback, `await using`
and `finally` lifetimes. No fake enrollment/connector implementation or empty callable helper is added.
**HSM-IMP-008 remains a runtime compatibility gap:** the full supplied-cryptogram sketch is compiled
as qualified documentation, without substituting cryptogram omission as authenticated HSM behavior.

`inventory.scan()` first verifies
the explicit completed-editor checkpoint. `verify.py` requires a final extraction manifest and
exactly one disposition for every fence/inline/XML/mapping unit, plus current complete bodies,
typed group wrappers and source/configuration identities. `capture.py` requires an active final
context before creating evidence. It reconciles actual Csc source/reference/analyzer/config sets,
requires genuine analyzers and warnings-as-errors, and records complete current source/config/
assets text plus dependency hashes. The shipping context requires all 22 original source paths.
Binary/cache artifacts remain outside retained evidence. Raw build failures and capture failures
keep separate classifications. A matching build log supplies compiler success and warning counts.

## Historical pre-format coordinator commands — superseded

Run serially from the primary checkout after preparation acceptance. The two active contexts are
consumer and actual shipping HsmAuthTool; annotation compilation is intentionally absent. These
commands remain unexecuted preparation instructions. Use fresh log/attempt names for retries.

```bash
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
RUN=docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001
EX=$RUN/validation/examples

python3 -B "$EX/verify.py" v1 prepared
python3 "$RUN/validation/run-command.py" examples-v1-consumer-build dotnet toolchain.cs -- build --project AuditHsm001.Examples.v1.consumer --clean
python3 "$RUN/validation/run-command.py" examples-v1-consumer-capture python3 -B "$EX/capture.py" v1 consumer first

python3 "$RUN/validation/run-command.py" examples-v1-hsmauthtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EX/v1/hsmauthtool/HsmAuthTool.Capture.targets" dotnet toolchain.cs -- build --project AuditHsm001.Examples.Shipping.HsmAuthTool --clean
python3 "$RUN/validation/run-command.py" examples-v1-hsmauthtool-capture python3 -B "$EX/capture.py" v1 hsmauthtool first

python3 "$RUN/validation/run-command.py" examples-v1-final-verify python3 -B "$EX/verify.py" v1 compiled --capture consumer=first --capture hsmauthtool=first --live
```

Capture each host immediately after its matching build, then recheck live dependency identities
late after the last intended source/build change. Logs and capture destinations use exclusive
creation; retries receive fresh names. A passing capture establishes provenance and binding to
final source bodies. The matching RESULT/exit status must separately establish zero compiler/
analyzer warnings and errors. Example, CLI, test, connector and device execution remain separate.

## Preparation evidence

`prepare.py preview` released final reads through the explicit completed-editor checkpoint;
`prepare.py prepare` retained the complete final groups and exclusive-create manifest. It also
reconciled the frozen OTP template/adaptation identities and literal substitutions. Final static
verification, `python3 -B "$EX/verify.py" v1 prepared`, exited 0; its retained output is
[`verify-v1-prepared.log`](validation/examples/verify-v1-prepared.log). It reconciles all 476
dispositions, exact original/wrapper bodies, source/configuration identities, Python/XML syntax,
unique active project selectors and all 22 shipping source paths. `verify-skeleton.py` remains the
earlier source-only checkpoint utility; final verification supersedes its pending-extraction checks.

C# compilation/analyzers and actual Csc captures remain pending. A later source/comment/formatter
change can invalidate the pinned final source identity even when executable tokens stay unchanged;
prepare a fresh revision or explicitly scoped correction record before accepting new captures.
The coordinator also reconciles the six advanced whole-file hashes with final content review.
