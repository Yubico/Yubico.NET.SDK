# PIV example tooling readiness

## Current checkpoint: final v4 source contexts are prepared

The final v4 release is bound to the independently accepted 59 post-format product paths in
`evidence/review-accepted-v4.json`, SHA-256
`59a4be1b906b31aa95f7ea4a1fd77621c41f2e9af607719c713548dc05a329b9`, and the matching 50-file
formatter/import-preservation reconciliation. This release adds documentation in KeyGenerationResult
and PrivateKeyOperations and qualifies key generation's DER SPKI JSON field in the tool README.
The other 56 previously accepted product identities remain unchanged. Current source identities
remain distinct from the successful historical v3 compiler/binary context.

Fresh extraction reconciles **45 unchanged groups / 47 selected bodies**, **60 unchanged current
complete bodies / 61 baseline originals**, and **730 actual unit dispositions**. All 111 current
module C# identities, four module Markdown files and six assigned shared documents are pinned.
The inventory reconciles prose/XML reference units against their actual current contents rather than
assuming a fixed denominator. Both generated hosts differ from their v3 sources only by the explicit
v4 namespace; the corrected annotation prerequisite/range assertion and all original resource scopes
remain intact. The actual shipping project still compiles all 36 original sources with ordinary bin/obj.

Active tools are in **`validation/examples/v4-tools/`**; current inputs and fresh source snapshots are
in **`validation/examples/v4/`**. The single `v4/tooling-provenance.json` receipt records nine exact
adaptations, the accepted/editor correction records, the new host graph registrations and **551
preserved historical artifacts**. All v2/v3 tools, manifests, sources, captures and successful/failed
verification logs remain immutable. No original capture or tested binary is rebound to new source.

The working project-closure and unique-ID/order-neutral graph comparison functions are byte-identical
to the proven v3-tools v4 helper. Exact graph memberships remain consumer **5/12**, annotation **2/4**,
and actual PivTool **5/10**. Canonical compiler/analyzer/source/reference/config/assets guards,
warnings-as-errors, no-skip policy and independent host/SDK version recording remain active.
The new unified receipt checks the new tools' identities; the historical ordering receipt remains
pinned ancestry rather than being applied to the new host paths.

`v4-tools/final-prepared.log` records three fresh static checks, each with exit 0: prepared-input
verification, complete source/body-context verification and derivation/preservation/namespace/graph
algorithm verification. Fresh v4 compilation, immediate actual-Csc captures and live verification
remain coordinator work. The accepted comment/prose corrections supply no reason for another device
run; existing hardware and tested-binary preservation records stay with their owners.

### Exact next commands for the current v4 release

Run serialized clean builds and capture each context immediately. Use these version-specific tools;
earlier directories retain their earlier source/evidence scope.

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
EXAMPLES="$RUN/validation/examples"
TOOLS="$EXAMPLES/v4-tools"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET

python3 "$RUN/validation/run-command.py" examples-v4-consumer-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v4.consumer --clean
python3 "$RUN/validation/run-command.py" examples-v4-consumer-capture python3 "$TOOLS/capture-v4.py" v4 consumer first

python3 "$RUN/validation/run-command.py" examples-v4-annotations-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v4.annotations --clean
python3 "$RUN/validation/run-command.py" examples-v4-annotations-capture python3 "$TOOLS/capture-v4.py" v4 annotations first

python3 "$RUN/validation/run-command.py" examples-v4-pivtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EXAMPLES/v4/pivtool/PivTool.Capture.targets" dotnet toolchain.cs -- build --project AuditPiv001.Examples.Shipping.PivTool --clean
python3 "$RUN/validation/run-command.py" examples-v4-pivtool-capture python3 "$TOOLS/capture-v4.py" v4 pivtool first

python3 "$RUN/validation/run-command.py" examples-v4-compiled python3 "$TOOLS/verify-v4.py" v4 compiled --capture consumer=first --capture annotations=first --capture pivtool=first --live
python3 "$RUN/validation/run-command.py" examples-v4-current-source-context python3 "$TOOLS/verify-source-context-v4.py"
python3 "$RUN/validation/run-command.py" examples-v4-preparation-preservation python3 "$TOOLS/verify-preparation.py"
```

The assignment performs static preparation only: no build, test runner, capture, formatter, hardware
operation, product edit, agent or commit. Prior results below retain their original scope and chronology.

## Prior v3 checkpoint: ordering-only verification correction

All three v3 builds and immediate `first` captures passed with zero build warnings. Existing capture
counts are consumer 4 sources / 192 references / 10 analyzers, annotation 5 / 205 / 12, and actual
PivTool 39 / 190 / 10. The final live verifier then compared nondeterministic graph traversal order
as if it were an identity. The coordinator's retained three-context diagnosis establishes equality of
all complete graph values after ordering their node lists by project path.

The additive `v3-tools/project_graph_v4.py` retains the v3 helper and changes only that comparison:
it creates in-memory comparison copies, orders their `graph` node lists by project path, and rejects
duplicate project paths or project IDs. Complete dictionary equality still checks every field and
value. Closure, resolved membership, deferred XML edges, source/DLL/assets identities, canonical
analyzers and all compiler-policy checks stay intact. The original captured data remains untouched.

`v3-tools/verify-v4.py` imports the v4 helper and validates the single
`v3-tools/graph-order-v4.json` receipt, including its own tool identity. The receipt pins original/new
hashes and exact bounded transformations, the original v3 manifests/captures, diagnosis and failed
`examples-v3-compiled.log`. The unchanged parent acceptance still validates the original v3 tools.
No extraction, build or capture is repeated or relabeled.

`v3-tools/graph-order-v4-preparation.log` records exit 0 for transformation replay, original-evidence
preservation, read-only normalized graph replay in all three contexts, independent duplicate-path/ID
rejection, and preservation of identity/asset differences as comparison failures. This is the bounded
ordering replay; the coordinator runs the complete final verifier next:

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
TOOLS="$RUN/validation/examples/v3-tools"

python3 "$RUN/validation/run-command.py" examples-v3-compiled-v4 python3 "$TOOLS/verify-v4.py" v3 compiled --capture consumer=first --capture annotations=first --capture pivtool=first --live
python3 "$RUN/validation/run-command.py" examples-v3-current-source-context-v4 python3 "$TOOLS/verify-source-context-v3.py"
python3 "$RUN/validation/run-command.py" examples-v3-preparation-preservation-v4 python3 "$TOOLS/verify-preparation.py"
python3 "$RUN/validation/run-command.py" examples-v3-ordering-preservation-v4 python3 "$TOOLS/prepare-ordering-v4.py" verify
```

The two existing v3 auxiliary checks remain applicable. The last command replays only the additive
ordering correction and preservation receipt. Original failed logs and all evidence bytes remain
immutable. This correction runs no overall capture/verifier, build, test runner, formatter or device
operation and changes no product source. Hardware/restoration outcomes retain their separate owners.

## Prior checkpoint: final v3 extraction, now built and captured

The coordinator's final input release is bound to the independently accepted post-format
`evidence/review-accepted-v3.json`, SHA-256
`f42cd302c8210179a32d7e13419f7a2dfaaf41aa66b1ab9aa70b9c0bb229ae11`, and the matching regenerated
reviewed-files/formatter reconciliation. All 57 current product identities match. The two late
Markdown paths are `src/Piv/CLAUDE.md` and `src/Piv/README.md`; the other 55 final paths and all
48 C# formatter/import-preservation records remain unchanged.

The current CLAUDE identity is
`f97b64c75b941c90e339f9b9b9e6b51a269f4bafeaae3d58e3a5bc2d22578d65`.
Its complete annotation body now uses the real metadata prerequisite and assertion:

```csharp
[WithYubiKey(Capability = DeviceCapabilities.Piv, MinFirmware = "5.3.0")]
// The complete retained method reads actual PinMetadata and checks:
Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries);
```

The actual generated method retains `[SkippableTheory]`, the `YubiKeyTestState` argument and its full
async session scope. It binds to genuine xUnit v2 and Tests.Shared types; it introduces neither a
nullability suppression nor a substitute SDK type. The exact accepted fence SHA-256 is
`4464f14bda4ed114d1cbbb0c3994bc0af74fab338095a7eb6ee9ba5a756d213e`.

The README identity is
`1725cae8c82139750743613680a256160f5691955473bef2bee02a49a54599b8`.
Its three inline maintainer commands now use `--project Yubico.YubiKit.Piv`, preserving canonical
project selection in the presence of the archived hardware probe. The explanatory sentence and
selector semantics are accepted in `review-selector-correction.md`. These three inline command
hash changes have their own ledger entries; they change none of the fenced C# consumer bodies.

### Prepared counts and immutable ancestry

[`v3/body-change.json`](validation/examples/v3/body-change.json) reconciles exactly one complete
body change, the CLAUDE annotation fence, plus three separate README inline-command changes.
The denominator remains **60 current complete bodies** (42 module and 18 shared), **61 original
baseline bodies**, **47 selected bodies in 45 typed groups**, and **730 explicit current dispositions**.
All 44 consumer groups and their 46 complete bodies remain unchanged. The one annotation group uses
the corrected current body. All 111 module C# identities remain unchanged. Four module Markdown
files and six assigned shared documents are current-hash bound.

The extraction deliberately uses fresh v3 hosts and compiler workspaces for all three contexts.
The consumer source differs from its v2 source only by its explicit v3 namespace; the annotation
source differs by that namespace and the exact two reviewed lines. Both new project filenames are
unique. This avoids inferring new-version compilation from the successful historical v2 consumer
capture. The actual shipping context still builds the original full 36-source PivTool project with
normal bin/obj paths and the v3 late observation hook.

[`v3/manifest.json`](validation/examples/v3/manifest.json), SHA-256
`b91b443122e1cc87b4cda16017b388bcda11cb59d06105ec12807d61a2d14e80`, binds the fresh extraction.
[`v3/source-context.json`](validation/examples/v3/source-context.json) retains the full original/current
body ledger and fresh text snapshots of every current shipping source/project. The source-only
[`v3/tooling-provenance.json`](validation/examples/v3/tooling-provenance.json) records nine exact
versioned tool adaptations, both accepted correction reviews and 247 preserved historical artifacts.
The original v2 tools, extraction, first failed/second successful consumer captures, original annotation
source and xUnit2002 build failure remain byte-identical. Nothing labels those results as v3 evidence.

The active scripts live in **`validation/examples/v3-tools/`**. Their
[`v3/project-graph-acceptance.json`](validation/examples/v3/project-graph-acceptance.json) registers the
new v3 host identities while preserving the proven exact direct/closure sets: consumer **5/12**,
annotation **2/4**, and actual shipping **5/10**. It derives from the unchanged parent graph acceptance;
the graph, actual-Csc source/reference/analyzer/config membership, canonical package/DLL/import/assets,
warnings-as-errors and no-skip algorithms stay intact. Host Version/AssemblyVersion and YubiKitVersion
remain independently recorded. The three v3 compiler workspaces await fresh builds and captures.

[`v3-tools/final-prepared.log`](validation/examples/v3-tools/final-prepared.log) records three fresh
static checks, each with exit 0: prepared-input verification, full source/body-context verification,
and additive provenance/current annotation/selector/namespace/graph-context verification. The initial
release gate correctly held when README changed concurrently; its original output and explanation
remain in `v3-tools/preparation.log` and `v3-tools/release-gate-hold.json`. The final release preparation
and extraction passed in `v3-tools/preparation-v3-release.log`.

### Exact next coordinator commands for v3

Use the version-specific scripts below; root-level `capture-v3.py` and `verify-v3.py` remain the
preserved v2 graph-correction tools. Run each build and its immediate capture serially, then verify
the exact three capture attempts. Read each build's final RESULT, actual selector, warning count and
exit status. The annotation's failed v2 source stays historical; the following host compiles the
corrected v3 source.

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
EXAMPLES="$RUN/validation/examples"
TOOLS="$EXAMPLES/v3-tools"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET

python3 "$RUN/validation/run-command.py" examples-v3-consumer-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v3.consumer --clean
python3 "$RUN/validation/run-command.py" examples-v3-consumer-capture python3 "$TOOLS/capture-v3.py" v3 consumer first

python3 "$RUN/validation/run-command.py" examples-v3-annotations-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v3.annotations --clean
python3 "$RUN/validation/run-command.py" examples-v3-annotations-capture python3 "$TOOLS/capture-v3.py" v3 annotations first

python3 "$RUN/validation/run-command.py" examples-v3-pivtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EXAMPLES/v3/pivtool/PivTool.Capture.targets" dotnet toolchain.cs -- build --project AuditPiv001.Examples.Shipping.PivTool --clean
python3 "$RUN/validation/run-command.py" examples-v3-pivtool-capture python3 "$TOOLS/capture-v3.py" v3 pivtool first

python3 "$RUN/validation/run-command.py" examples-v3-compiled python3 "$TOOLS/verify-v3.py" v3 compiled --capture consumer=first --capture annotations=first --capture pivtool=first --live
python3 "$RUN/validation/run-command.py" examples-v3-current-source-context python3 "$TOOLS/verify-source-context-v3.py"
python3 "$RUN/validation/run-command.py" examples-v3-preparation-preservation python3 "$TOOLS/verify-preparation.py"
```

Each corrected attempt receives a fresh name and preserves the earlier output. This assignment
performs static extraction/provenance checks only: no build, test, capture, formatter, hardware
operation, product edit, agent or commit. Existing repository and hardware outcomes/restoration
records stay with their owners. Fresh v3 compilation/analyzer results, immediate actual-Csc captures
and final live dependency verification remain pending.

## Prior v2 graph-correction checkpoint, preserved history

The consumer's 44-group build passed with zero warnings and exit 0 in
`validation/examples-v2-consumer-build.log`. Its immediate `first` capture stopped in the original
project-reference provenance guard: the host declares five direct references, while the late SDK
ProjectReference item set includes twelve reachable projects. This is a distinction between the
declared direct set and the restored transitive graph. The compiled examples and their workspace
remain intact; **capture the consumer again as `second` without rebuilding it**.

The active additive tools are [`capture-v3.py`](validation/examples/capture-v3.py),
[`verify-v3.py`](validation/examples/verify-v3.py) and
[`project_graph_v3.py`](validation/examples/project_graph_v3.py). They bind the explicit versioned
acceptance in [`project-graph-1/receipt.json`](validation/examples/project-graph-1/receipt.json).
The original v2 manifest, tools, provenance, source-context, XML-whitespace preparation failure and
consumer `first` failure/captured text remain byte-identical. The receipt pins 175 existing artifacts,
the original consumer compiler text/restore workspace, and every new tool identity. It adds an evidence
tool version to the existing extraction; it leaves the original manifest's old-tool claim unchanged.
Each corrected capture records the new acceptance receipt hash alongside the original extraction hash.

The graph helper reuses `project_closure_proof` directly from the working hardware
`validation/hardware-probe/capture-v3.py`. Its full source hash, extracted function hash, exact bounded
substitutions and authored adapter hash are recorded. The hardware files remain unchanged. The
adaptation accepts an evidence directory, selects the exact context-specific node set, and reads the
resolved output/source columns from the existing two- or three-column metadata form. It preserves
the original recursive XML/restored target, exact late-item membership and actual obj/bin DLL checks.

| Context | Exact declared direct projects | Exact reachable dependency projects |
| --- | --- | --- |
| Consumer | 5 | 12 |
| Annotation | 2 | 4 |
| Actual PivTool | 5 | 10 |

These sets come from each unchanged project's metadata and are pinned by exact node paths and hashes.
The closure excludes its root host: PivTool is a dependency of the consumer, while the shipping context
uses PivTool as its root. The preparation's first count check had anticipated eleven shipping
dependencies; `project-graph-1/preparation-first-failure.json` retains that static preparation failure.
The corrected preparer derives and pins the recursive node sets rather than carrying a guessed closure
count into runtime evidence. No capture was performed while preparing this correction.

At coordinator capture time, the helper requires the restored direct set to match the accepted direct
set. Recursive XML and restored `net10.0` target edges must agree; conditional/computed edges retain
their explicit restored-graph authority. Every restored project node must be reachable. The late
ProjectReference set and resolved project-output set then match that complete closure. Each project
binds one actual Csc reference DLL under its own `obj` and the matching resolved DLL under its own
`bin`. Graph project/SDK import text and restore assets retain their identities. Final live verification
repeats these graph, reference and output identity checks.

The original source/reference/analyzer/config membership checks, all 36 shipping sources, canonical
NetAnalyzers DLL/import/assets proof, warnings-as-errors and no-skip policy remain active. Host Version
and AssemblyVersion remain separate from YubiKitVersion. The restored Tests.Shared runtime allowlist
configuration is outside this consumer's project closure and compiler inputs; the correction adds no
unrelated configuration equality requirement.

[`project-graph-1/preparation-static.log`](validation/examples/project-graph-1/preparation-static.log)
records exit 0 for original-artifact preservation, exact hardware-helper and capture/verify
transformation replay, Python syntax and the three direct/recursive metadata contexts.
[`project-graph-1/current-inputs-static.log`](validation/examples/project-graph-1/current-inputs-static.log)
records exit 0 for corrected prepared-input verification, original current-source-context verification
and host-scope verification. Counts remain 45 typed groups, 47 selected complete bodies, 730 explicit
dispositions and the actual 36-source tool. These are preparation checks; the new graph capture and
live binary/analyzer/assets proofs remain coordinator operations.

### Historical v2 graph-correction commands

This is the historical v2 sequence; use the v3 commands above for the current release.
The sequence used the intact consumer workspace first, then build/capture
the annotation and actual shipping contexts. Retain each failure and use a new attempt name when
correcting it. The original `first` consumer capture remains preserved.

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
EXAMPLES="$RUN/validation/examples"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET

python3 "$RUN/validation/run-command.py" examples-v2-consumer-capture-second python3 "$EXAMPLES/capture-v3.py" v2 consumer second

python3 "$RUN/validation/run-command.py" examples-v2-annotations-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v2.annotations --clean
python3 "$RUN/validation/run-command.py" examples-v2-annotations-capture python3 "$EXAMPLES/capture-v3.py" v2 annotations first

python3 "$RUN/validation/run-command.py" examples-v2-pivtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EXAMPLES/v2/pivtool/PivTool.Capture.targets" dotnet toolchain.cs -- build --project AuditPiv001.Examples.Shipping.PivTool --clean
python3 "$RUN/validation/run-command.py" examples-v2-pivtool-capture python3 "$EXAMPLES/capture-v3.py" v2 pivtool first

python3 "$RUN/validation/run-command.py" examples-v2-compiled-v3 python3 "$EXAMPLES/verify-v3.py" v2 compiled --capture consumer=second --capture annotations=first --capture pivtool=first --live
python3 "$RUN/validation/run-command.py" examples-v2-current-source-context python3 "$EXAMPLES/verify-v2.py"
python3 "$RUN/validation/run-command.py" examples-v2-current-host-scope python3 "$EXAMPLES/host-scope-v2.py" verify
python3 "$RUN/validation/run-command.py" examples-v2-project-graph-preservation python3 "$EXAMPLES/project-graph-1/verify.py"
```

The coordinator retains the existing hardware outcomes/restoration evidence separately. This correction
performs no build, test, capture, formatter or device operation and changes no product source, example
body, compiled project or old extraction/tool/failure artifact.

## Released v2 extraction checkpoint, preserved before graph correction

The coordinator released the final reviewed and formatted inputs for extraction. The active v2
manifest binds all 57 final accepted product hashes to the accepted pre-format content and the
48 whitespace-only C# transforms with unchanged imports. PIV-REV-001–003 are resolved at the pinned
correction acceptance. This checkpoint supersedes the earlier waiting-for-release status below.

[`v2/final-prepared.log`](validation/examples/v2/final-prepared.log) records three fresh static checks,
each with exit 0: the prepared-input verifier, full source/body-context verifier and explicit host-scope
receipt verifier. These checks establish preparation and identity reconciliation. Compiler/analyzer
execution, actual Csc captures, live dependency checks and device work remain separate coordinator gates.

### Final denominator and contexts

- All **111 module C# files** are current-hash bound: 33 library, 36 shipping-tool, 18 unit and
  24 integration sources. The inventory includes all four module Markdown files and six assigned
  shared documents.
- All **61 original complete-body hashes** reproduce from the immutable baseline and original editor
  ledger. The retained current ledger contains **60 complete bodies: 42 module and 18 assigned shared**.
- **47 current bodies** form **45 complete typed groups**. The other 13 curated bodies are three
  historical v1 migration bodies, three other-applet FIDO/OTP bodies, five shell examples and two
  protocol-flow diagrams. The mixed raw migration body retains its full Before/After original and
  compiles its complete current After half with an explicit bounded transformation.
- The broader current inventory has **730 explicit dispositions**: 55 fences, 599 inline spans,
  72 XML `example/code/c` units and four migration mappings. Nested XML example containers and inline
  symbols retain their own dispositions without inflating the complete-body compilation denominator.

| Actual Csc context | Groups / selected complete bodies | Exact coordinator selector |
| --- | --- | --- |
| Consumer | 44 / 46 | `AuditPiv001.Examples.v2.consumer` |
| Annotation | 1 / 1 | `AuditPiv001.Examples.v2.annotations` |
| Actual PivTool | Full original 36-source project | `AuditPiv001.Examples.Shipping.PivTool` |

[`final-plan-v2.json`](validation/examples/final-plan-v2.json) records every group, prerequisite,
lifetime, wrapper and original-body transformation. [`v2/manifest.json`](validation/examples/v2/manifest.json)
binds the generated hosts, full raw bodies, release, plan, tools and current dependencies. The consumer
references the real PivTool, Management, Cli.Shared and WebAuthn projects alongside PIV. Public tool
helper examples bind to those actual sample APIs. Device-backed private certificate signature generators
remain inside the complete actual shipping compilation rather than copied helper stubs. The shared
WebAuthn intent example uses the genuine client/request-option types and preserves its awaited using
scope; it adds no WebAuthn execution claim. The annotation context retains the genuine xUnit v2 and
Tests.Shared configuration.

[`v2/source-context.json`](validation/examples/v2/source-context.json) additionally retains the 61
baseline bodies, 60 current bodies, all module/document identities, the exact original shipping project
and **all 36 full current shipping source texts** under `v2/native-source/`. These are immutable source
snapshots; the traversal still compiles the original project at its normal location and bin/obj paths.
The historical 19-group integration guide evidence stays in the frozen integration checkpoint.

The corrected F9 task reads `result.AttestationCertificate` and retains one using owner per returned
property. Its editor-normalized complete-body hash is
`c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc`. The ledger explicitly records
the corresponding JSON qualification: `attestationCertificate` carries F9 metadata and
`intermediateCertificate` remains null. Certificate retrieval adds neither chain trust nor slot binding.

The PIN-protected administration groups preserve the full enable/reopen/recover/disable method or
current migration method, then invoke it through typed caller inputs. Their enclosing wrapper checks
the supplied active key is 24 bytes with the explicitly known Triple-DES/AES-192 algorithm and clears
the caller-owned PIN/key arrays in finally. The application retains an independent secure backup through
partial transitions. The store example retains its actual `Action<Memory<byte>>` contract and complete
pooled-buffer finally block. The SCP loader is a typed forwarding local function over a supplied real
secure-store delegate. All presence callbacks retain their complete original implementations. Borrowed
sessions, prompts, commands and input data remain borrowed where specified; caller-owned credential
arrays receive explicit enclosing cleanup. The direct SDK plaintext/signature examples retain owned
mutable `Memory<byte>` cleanup; read-only sample result limitations stay qualified without writable casts.

### Preparation lineage and bounded corrections

The first baseline-body context check retained indentation from the closing XML tag in one historical
contention example. [`v2/source-context-first-failure.json`](validation/examples/v2/source-context-first-failure.json)
preserves the original command, exit, tool hash, observed/expected body hashes and exact normalization
correction. The corrected preparation reproduces all 61 original hashes and retains leading code
indentation. This was a static provenance failure before source-context output, separate from compiler
or hardware execution.

[`v2/host-scope-1/receipt.json`](validation/examples/v2/host-scope-1/receipt.json) records the explicit
application namespace inserted into each generated host, matching the canonical final OpenPGP host
structure. Original extraction manifest, source-context and both host texts remain pinned under its
`before/` snapshots. Complete original bodies/wrappers, references, compiler policy and the actual
shipping project stay unchanged. The active manifest and source-context bind the namespace-scoped final
inputs; the earlier `v2/prepared.log` remains the historical first-extraction checkpoint.

### Initial coordinator sequence, superseded by the graph correction above

The following retains the original sequence for lineage; use the corrected commands above now.
Capture each remaining context immediately after its build;
read the actual selected project, exit status, final RESULT and warning count. The two real builds of
PivTool (as a consumer dependency and through the shipping traversal) remain distinct; only the late-hook
shipping attempt supplies the native shipping compiler capture.

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
EXAMPLES="$RUN/validation/examples"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET

python3 "$RUN/validation/run-command.py" examples-v2-consumer-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v2.consumer --clean
python3 "$RUN/validation/run-command.py" examples-v2-consumer-capture python3 "$EXAMPLES/capture.py" v2 consumer first

python3 "$RUN/validation/run-command.py" examples-v2-annotations-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v2.annotations --clean
python3 "$RUN/validation/run-command.py" examples-v2-annotations-capture python3 "$EXAMPLES/capture.py" v2 annotations first

python3 "$RUN/validation/run-command.py" examples-v2-pivtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EXAMPLES/v2/pivtool/PivTool.Capture.targets" dotnet toolchain.cs -- build --project AuditPiv001.Examples.Shipping.PivTool --clean
python3 "$RUN/validation/run-command.py" examples-v2-pivtool-capture python3 "$EXAMPLES/capture.py" v2 pivtool first

python3 "$RUN/validation/run-command.py" examples-v2-compiled python3 "$EXAMPLES/verify.py" v2 compiled --capture consumer=first --capture annotations=first --capture pivtool=first --live
python3 "$RUN/validation/run-command.py" examples-v2-current-source-context python3 "$EXAMPLES/verify-v2.py"
python3 "$RUN/validation/run-command.py" examples-v2-current-host-scope python3 "$EXAMPLES/host-scope-v2.py" verify
```

Every failed attempt stays retained and receives a new name when corrected. Actual compiler captures
require source/reference/analyzer/config argument membership, canonical package/DLL/import/assets proof,
warnings-as-errors and no-skip policy. The final live check rechecks binary/analyzer/assets/project
identities. Host Version/AssemblyVersion and SDK YubiKitVersion remain separate fields; the actual SDK
reference DLL identities establish binding. Existing hardware outcomes and supplemental probes retain
their separate owners and evidence. This preparation runs no build, test, formatter or device operation.

## Earlier source-only preparation and reusable procedure

Use the prepared tools after the coordinator releases final reviewed and formatted inputs. Sol
prepared sources, schemas and empty hosts on 2026-11-05 from baseline
`727fe43afc430c5d8364d868a6436de50626e476`. The current investigator continues to own product
prose and evidence. This assignment covers `validation/examples/` and this record; it leaves
product sources, builds, tests, formatting, hardware and publication with their assigned owners.

Write later records and wrappers in the approved voice: start with the task and actual actor
behavior, preserve useful rationale, and keep ownership, lifetime and feature conditions nearby.

[`prepare-tools.py`](validation/examples/prepare-tools.py) prepares 13 source/configuration files from
the final frozen OpenPGP archive, SHA-256
`71698c428044b99165fb68759a2e47b6af701187f0ddb7b5838e5bc3278bf7ee` (607 entries).
[`tooling-provenance.json`](validation/examples/tooling-provenance.json) records every original
path/hash, target path/hash, literal substitution/count, removed historical-receipt region and appended
inventory extension. It also pins the newly authored preparation/extraction/dependency/static helpers
and records the project/root-configuration metadata inspected for orientation. Historical example
bodies, manifests, compiler results and caches stay in their original archive.

The active [`capture.py`](validation/examples/capture.py),
[`verify.py`](validation/examples/verify.py) and
[`compiler_policy.py`](validation/examples/compiler_policy.py) derive directly from corrected final
OpenPGP `analyzer-package-1` algorithms. Their preparation replaces OpenPGP identity/context names,
the 19-source sample count with 36, and historical receipt coupling with this run's tooling provenance.
It adds real project-reference evidence and separately recorded host/SDK versions. The earlier
`analyzer_policy.py` helper stays intact as the preceding preparation checkpoint; these new tools use
`compiler_policy.py`.

## Empty hosts and actual shipping project

The immutable `v1` skeletons contain configuration only. Each C# host expects `Examples.cs` from a
future released extraction; the current preparation supplies neither placeholder source nor test
methods. Fresh final-input revisions start at **v2**.

| Context | Unique selector | Configuration |
| --- | --- | --- |
| Consumer skeleton | `AuditPiv001.Examples.v1.consumer` | Canonical root props/targets, real PIV project reference, .NET 10/C# 14, library output |
| Annotation skeleton | `AuditPiv001.Examples.v1.annotations` | Canonical test policy, real PIV and Tests.Shared references, current xUnit v2 integration packages |
| Actual shipping traversal | `AuditPiv001.Examples.Shipping.PivTool` | Invokes the real `src/Piv/examples/PivTool/PivTool.csproj`; ordinary shipping bin/obj |

The traversal points to the actual project and all 36 shipping sources. It preserves current Core,
Management, Cli.Shared, Cli.Commands and Spectre dependencies through that project, rather than
substituting a reduced compilation. Its observation hook is imported late through
`CustomAfterMicrosoftCommonTargets` and acts only on `PivTool`. It leaves shipping output/restore
paths under the real project's `bin/obj`. The SDK-default helper retains the pinned 10.0.403
optional-early-hook/Exists proof.

Consumer and annotation output/restore paths use
`/tmp/opencode/piv-001-examples-vN-<context>/`. New revision filenames use the same `vN`, for example
`Yubico.YubiKit.AuditPiv001.Examples.v2.consumer.csproj`. Both static and final verifiers recursively
check project **filenames** against the exact selectors, including archived locations.

## Release and complete current-body ledger

The coordinator creates `validation/examples/extraction-scope.json` from the separate
[`release-scope.template.json`](validation/examples/release-scope.template.json) only after fresh
review and the scoped formatter. The release supplies:

- The baseline, affirmative editor/reviewer/formatter completion and an explicit coordinator release.
- Repository-relative identities for the editor report, final review and formatter receipt, each pinned
  by SHA-256. The final review uses `baseline` and `files[{path, final_sha256}]`.
- Exact `edited_paths` whose current bytes match final review. Shared paths and selected heading
  names are explicit; `whole_shared_paths` intentionally includes generic callback/helper guidance
  whose code serves the PIV examples even when its body omits a PIV keyword.
- `task_units` for additional complete test/example/callback tasks represented as bounded source spans:
  `{id, path, start_line, end_line, reason}`. Use module paths or explicit shared scope. Name the
  complete setup/body/cleanup or helper unit, then describe its relation to the task. The coordinator
  reconciles this supplementary list with investigator coverage.

[`inventory.py`](validation/examples/inventory.py) verifies the released record identities and final
edited hashes before reading current product bodies. It inventories all module Markdown fences and
inline spans, all module source XML `code/example/c` bodies, assigned shared fences/spans/mappings,
and explicit task units. It reads entire source XML blocks and retains complete extracted bodies.
Shared fences receive heading-based scope labels; implicit callbacks receive the selected whole/shared
scope without keyword filtering. Full inventory comparison pins document/source identities and every
body, including additions/removals, instead of inheriting the older inline/mapping filtering shortcut.

After release, retain the first final inventory:

```bash
RUN=docs/research/documentation-audit/modules/piv/pass-1/piv-001
EXAMPLES="$RUN/validation/examples"
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET

python3 "$RUN/validation/run-command.py" examples-v2-inventory python3 "$EXAMPLES/prepare-final.py" inventory v2
```

The coordinator then writes a manual plan under `validation/examples/`, following
[`final-input-schema.json`](validation/examples/final-input-schema.json). Pin the retained
`v2/inventory.json` SHA-256. Give **every** fence/inline/XML/mapping/task identity one disposition
with a useful reason: compile, historical, noncompiling-by-design, symbolic, other-applet, non-csharp
or non-executable. Keep other-applet material outside the task's compilation claim. The actual
shipping-tool source set is an additional complete compilation context.

Each compile group names exact unit IDs, a consumer/annotation context, typed prerequisites,
resource lifetimes and a complete wrapper template. Insert each whole body once using
`{{unit:EXACT-UNIT-ID}}`. Keep imports separate in the plan. Explicit bounded body changes record
`before`, `after`, occurrence `count` and a useful `reason`; the original full body remains retained.
Compile shared callbacks together with the task that uses them. Application-owned helpers carry
real types/responsibilities; genuine SDK calls bind to actual references. Current Core's KeyType
catalog and post-PR745 PIV APIs come from those references. This preparation carries no copied enum,
curve-catalog body or substitute SDK implementation.

The plan explicitly acknowledges application-helper review. Namespace declarations under `Yubico`
are rejected in wrappers; semantic helper/API authenticity and appropriate task grouping retain
independent review. Additional dependencies use exact existing `src/**/*.csproj` paths in
`additional_project_references`, keyed by context. Fresh generated host projects retain those explicit
references and the final compiler proof checks their actual resolved DLLs.

```bash
python3 "$RUN/validation/run-command.py" examples-v2-extract python3 "$EXAMPLES/prepare-final.py" extract v2 --plan "$EXAMPLES/final-plan-v2.json"
python3 "$RUN/validation/run-command.py" examples-v2-prepared python3 "$EXAMPLES/verify.py" v2 prepared
```

Extraction creates new revision hosts, raw complete bodies, generated wrappers and a manifest binding
all dispositions, dependencies, preparation/tool sources and release/plan identities. It preserves
the empty v1 checkpoint and rejects changed existing extraction outputs. A changed input requires a
fresh revision and corresponding release update. Unused annotation hosts stay outside active results;
meaningful annotation groups activate their genuine context.

## Coordinator compilation and immediate capture

Run serialized commands from the primary checkout. Use a fresh attempt name for every capture.
The following commands illustrate final v2; the coordinator uses the actual released revision and
active contexts from its extraction manifest.

```bash
python3 "$RUN/validation/run-command.py" examples-v2-consumer-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v2.consumer --clean
python3 "$RUN/validation/run-command.py" examples-v2-consumer-capture python3 "$EXAMPLES/capture.py" v2 consumer first

python3 "$RUN/validation/run-command.py" examples-v2-annotations-build dotnet toolchain.cs -- build --project AuditPiv001.Examples.v2.annotations --clean
python3 "$RUN/validation/run-command.py" examples-v2-annotations-capture python3 "$EXAMPLES/capture.py" v2 annotations first

python3 "$RUN/validation/run-command.py" examples-v2-pivtool-build env -u CustomBeforeMicrosoftCommonProps CustomAfterMicrosoftCommonTargets="$ROOT/$EXAMPLES/v2/pivtool/PivTool.Capture.targets" dotnet toolchain.cs -- build --project AuditPiv001.Examples.Shipping.PivTool --clean
python3 "$RUN/validation/run-command.py" examples-v2-pivtool-capture python3 "$EXAMPLES/capture.py" v2 pivtool first

python3 "$RUN/validation/run-command.py" examples-v2-compiled python3 "$EXAMPLES/verify.py" v2 compiled --capture consumer=first --capture annotations=first --capture pivtool=first --live
```

Run annotation commands only when the manifest includes that context, and omit its capture selector
otherwise. Read each build's final RESULT, exit status, selected project and warning count. The capture
records actual Csc/source/reference/analyzer/config membership and policy; the matching build log
establishes compiler success and diagnostics. Preserve both compiler failures and capture/provenance
failures, then use a new named attempt for a corrected run.

The tools retain actual sources, analyzer configs, imported projects, compiled/context projects,
restore assets and canonical root configuration as text. Binary references/analyzers/resources receive
exact path/size/hash identities. Capture requires:

- Actual `/warn:9999` and `/warnaserror+`, enabled compiler/analyzer execution and exact argument/item
  membership. Consumer/tool contexts retain AllEnabledByDefault; annotations retain Recommended.
- Canonical NetAnalyzers 9.0.0 package/pin/import/assets proof and both exact DLLs in actual Csc.
  `EnableNETAnalyzers=false` reflects the package's replacement of duplicate SDK built-ins in this
  pinned configuration. Actual DLL membership and no-skip/warnings policy establish analyzer execution.
- Real direct ProjectReference paths matching the compiled project's metadata and one actual Csc DLL
  per direct dependency. The capture records all actual SDK reference identities, plus generated NuGet
  and resolved-project-reference items. Current PIV/Core references participate in every context.
- Separate host Version/AssemblyVersion and YubiKitVersion fields. Root import order can produce
  host 1.0 and SDK 2.0; this is recorded alongside actual binding identities, rather than compared as
  equal metadata. .NET 10/C# 14/Release and canonical YubiKitVersion remain explicit checks.
- All 36 current shipping source paths within the actual PivTool Csc set, ordinary native output/assets
  locations, the exact late capture hook, and the pinned SDK optional-hook evidence.

Final verification binds each exact revision/context/attempt and extraction manifest, rechecks current
body/disposition/group/dependency identities, retained text and analyzer proof, and verifies all current
binary/analyzer/assets/project-reference identities with `--live`. Readiness, compilation, snippet
execution and hardware qualification remain separate dimensions. Hardware/reference-source owners
retain their own scope; primary-PDF registration remains later coordinator work.

## Static evidence and readiness boundary

[`preparation-static.log`](validation/examples/preparation-static.log) records exit 0 for 13 replayed
frozen-source transformations; Python/JSON/XML syntax; three unique recursive selectors; immutable
empty hosts; canonical imports and late native-output capture configuration. It establishes
source-preparation integrity. Final current-body inventory, manual dispositions/typed wrappers, builds,
actual compiler captures and live verification await the reviewed/formatted release. Current product
prose and snippet bodies remained unread during this preparation.

Keep preparation snapshots and later attempts immutable. Retain generated binary/cache outputs outside
the run and use the existing campaign byte-preserving evidence attributes. The coordinator performs
the run's eventual final manifest freeze after final review and the last in-run evidence write.
