# CTAP2 compile-input preparation — no compilation verdict

2026-11-04. PRIMARY: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

Prepared a rerunnable fail-closed extractor, explicit selection, retained original/raw/complete
sources and six disjoint SDK-context configurations. **No .NET build, restore, compiler/snippet
execution, test, hardware operation, commit, delegation or preparer invocation occurred.**
No audited content documentation or Management historical record was edited. Checker, context adapter and templates
were not modified; only their owned `context/contexts-manifest.json` configuration was added.

## Authoritative inputs and integration prerequisite

- A–E reviewed source/XML is read from PRIMARY. The accepted five XML blocks are not taken from
  F's stale sibling source checkout.
- Initial guide/document inputs were read from `/tmp/opencode/ctap2-001-edit-f`, with current
  `edit-f.md`/`correction-f-review.md` dispositions. Coordinator steering subsequently confirmed
  F follow-up PASS and all **162 content paths** (A–E132 + F30) integrated into PRIMARY. The retained
  final extraction now uses **PRIMARY for both roots** via the extractor's defaults. Narrow fresh
  boundary review remains active. Its corrections require another extraction/hash rerun; changed
  bodies fail closed. The two F csproj checkout EOL-only differences are retained as actual PRIMARY
  document/configuration byte hashes, not silently normalized or treated as runtime/API changes.
- Read root/module/test guidance, available skill descriptions, `edit-a.md` through `edit-f.md`,
  `correction-f-review.md`, `validation-plan.md`, `validation-tooling.md`, F snippet/external/reference
  inventories and the current generic `prepare-project-context.py` schema. Applied verification
  discipline only; skill example build/test commands do not override this assignment's prohibition.
- Current real source signatures, namespaces, public helper accessibility, owning scopes and
  original project/package configuration were inspected. Source/API evidence is hashed under
  `manifest.json:source_api_evidence` (32 paths), not replaced by declarations or fake SDK code.
- Context7 `/xunit/xunit` was consulted for v3 generated executable entry-point wiring. The actual
  repository projects and central versions remain authoritative: xunit.v3 **3.0.0**, xunit
  **2.9.3**, NSubstitute **5.3.0**, SkippableFact **1.5.23**, Test.Sdk **18.0.1**, CBOR **10.0.2**,
  NetAnalyzers **9.0.0**. No package version was changed or restored.

## Retained inputs and exact counts

Under `validation/compiled-examples/`:

- `extract.py`: no .NET invocation; `--materialize` regenerates only named run-local artifacts,
  `--patch` emits a patch for review/application, `--check` is a read-only deterministic comparison.
- `selection.py`: explicit block hashes, routing, wrapper shapes, typed parameters, imports,
  public-source evidence, prerequisites and excluded reasons. Changes to a pinned body—including
  its first-line anchor—or to a document's block count fail before any materialization.
- `verify-inputs.py`: independent static body/hash/partition and genuine source-package-closure
  checks. It reads project XML directly, never invokes/imports the context preparer or .NET.
- `manifest.json`: exact roots, document hashes, ordinal/heading/opening/closing line locators,
  complete extracted text/hash, original fenced/XML-span text/hash, selected/excluded dispositions,
  all groups, raw/wrapped hashes, dependencies and provisional DLL observations.
- Each of the **33 groups** has `readable.raw` and `complete.cs`. Raw includes source labels;
  each underlying block's unlabelled exact extracted text/hash is retained separately in the manifest.
  For XML only `///` prefixes/delimiters are removed; original XML spans remain verbatim. LF-normalized
  extraction and actual document-byte SHA256 are deliberately distinct. No escaped XML C# operator
  is silently rewritten: a future entity-containing code block requires explicit review.
- `strict-raw-not-applicable.json`: explicit empty optional raw partition, **not a vacuous passing
  compiler invocation**. Every selected group starts in its approved SDK/MSBuild context.

| Universe | All blocks | Selected blocks | Explicitly excluded |
|---|---:|---:|---:|
| Module Markdown | 39 | 24 | 15 |
| Current PRIMARY XML | 5 | 4 | 1 |
| Selected current shared documents, including implicit dependencies | 51 | 11 | 40 |
| **Total** | **95** | **39** | **56** |

The complete module Markdown universe is mechanically reconciled, not selected by CTAP keywords.
The extractor scans all non-generated module C# XML docs and requires the same exact five-block
universe. The 39-fence/five-XML checks are fail-closed if F corrections introduce or remove a block.
The shared universe comprises all fences in `docs/usage/user-interaction.md` (4),
`docs/architecture/raw-access-tiers.md` (8), `docs/architecture/sdk-architecture-diagrams.md` (8:
seven Mermaid plus one shell fence), `docs/TESTING.md` (22), and `src/WebAuthn/README.md` (9).
Selection does not certify unrelated PIV/OTP/Management content in those documents.

The module's 16 exclusions are exact: **13 shell/installation/runner/sample-command fences**, the
directory-tree sketch, schematic source-field declarations and the protocol-2 binary-layout XML.
Feature source-field and protocol-layout examples are excluded representation sketches, **not no-op
stubs**. Shared exclusions retain every full block, with historical/internal/undefined scaffold
reasons where applicable: notably the internal ProtocolFactory migration, application
`LoadScpParametersFromSecureStorage`, undefined `rpValidationService` and historical WA migration.
No internal API is impersonated through an assembly/friend name.

Reference inventory retains **66 exact current paths** (including every extant active path in F's
external discovery table) and **30 document/URL literal records**. These are local document hashes
and literal reference discovery, not remote-content hash/HTTP verification or whole-module audit
verdicts. F run records are fresh provenance; prior Management result counts/manifests are not reused.

## Approved groups and real contexts

The exact exhaustive assignment is in `validation/checker-source/context/contexts-manifest.json`.
Context IDs contain no module name, `UnitTests` or `IntegrationTests` discovery token. Future host
names follow the unchanged tooling convention `AuditCt001.Examples.<id>`; no host is prepared now.

| Context ID | Real source project | Output shape | Groups |
|---|---|---|---:|
| `consumer-lib` | `src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj` | Library, factored typed methods/classes | 19 |
| `consumer-demo` | Same | Exe, one complete top-level CTAP README flow | 1 |
| `unit-v3` | Real CTAP UnitTests csproj | Exe, genuine package-generated v3 entry point | 4 |
| `hardware-v2` | Real CTAP IntegrationTests csproj | Library, whole v2 methods/fragments | 4 |
| `consumer-wa-demo` | `src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj` | Exe, complete existing WA demo | 1 |
| `consumer-wa-lib` | Same | Library, typed client/prompt/policy fragments | 4 |

`consumer-lib`: guide-session, guide-info, guide-pin-create, guide-extensions, guide-admin, guide-raw,
guide-memory, guide-protocol, guide-threshold, readme-discoverable, readme-admin,
readme-prompt-factory, xml-info, xml-session-alternatives, xml-builder, xml-output,
shared-operation-wording, shared-intent-callback, shared-raw-hid.

`consumer-demo`: readme-paired. `unit-v3`: unit-versions, unit-retries, unit-cbor-fixture,
unit-extension. `hardware-v2`: hardware-info, hardware-create, hardware-set-pin,
shared-hid-management. `consumer-wa-demo`: webauthn-paired. `consumer-wa-lib`:
shared-intent-task, webauthn-pin-config, webauthn-prompt-factory, webauthn-pair-policy.

Each group declares its exact wrapper shape (`top-level`, `async-method`, `sync-method`,
`test-method` or interface `class-members`), typed parameters/imports/modules/references and
operational prerequisites. Initial using directives are relocated without touching bodies or
multiline literals; duplicate additions are avoided. Added code is only typed enclosing
method/namespace/class/interface boundaries and imports, not SDK/harness/helper implementations.
Documented `TouchPrompt`/`PinPrompt` bodies are retained originals, not generated replacements.

### Useful flow preservation

- `readme-paired` composes fences 2/3/4/7: real discovery/session/GetInfo, actual rp/user/credentialId
  creation, allow-list assertion, then the complete PIN creation + distinct permission/hash/credential
  assertion inside its original try/finally. No dead-scope variable, deleted await or synthetic
  `credentialId` is substituted. All disposal, token/auth/PIN clearing, conditional guards and tasks stay.
- Discoverable first/count/N−1 continuation is a separate alternative with typed live session and
  fresh hash32. It requires an existing discoverable `example.com` credential; the nonresident demo
  does not populate that search. No overlap/intervening command is introduced.
- `readme-prompt-factory` retains the actual alternative factory and whole prompt class separately
  from the original owning factory. It does not open a second session on a live physical key.
- XML ordinary/SCP scopes remain exactly as corrected: ordinary session disposal completes first;
  caller-owned SCP03 keys enclose secure-session disposal. No flattened overlapping alternative.
- Unit retry example retains the awaited ClientPin action, capture, runner cancellation token and all
  assertions. Hardware creation retains complete callback, normalization prerequisites, task,
  assertions and finally cleanup. No empty test or stub was added.
- WA's useful implicit client tasks retain both ceremonies, zero-match return, cached result metadata,
  selection and PIN cleanup. Prompt alternatives remain separate. `ReadPinAsync` is a typed application
  delegate input, never an invented implementation.

### Genuine references; no xUnit mixing

All hosts bind actual Release Core/Ctap2 DLLs. `hardware-v2` explicitly adds Management and these
actual DLL paths:

```
src/Tests.Shared/bin/Release/net10.0/Yubico.YubiKit.Tests.Shared.dll
src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/bin/Release/net10.0/Yubico.YubiKit.Ctap2.IntegrationTests.dll
```

The actual accessible helper is **`public static class Ctap2TestStateExtensions`** in
`Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions`, not `Ctap2IntegrationTestExtensions`
(no such symbol exists in current PRIMARY). Its real `WithCtap2SessionAsync`/known-PIN fields are used
verbatim from the integration assembly. Tests.Shared supplies real WithYubiKey, YubiKeyTestState,
traits and WithManagementAsync. Nothing is copied/reimplemented. Unit v3 gets neither helper DLL,
Tests.Shared nor SkippableFact/xUnit2. WA contexts explicitly add WebAuthn, not test dependencies.

All **six existing DLL hashes are provisional observations**: Core, Ctap2, Management, WebAuthn,
Tests.Shared and CTAP integration. Existence or hash retention does not establish freshness. The
coordinator must perform the final clean SDK/helper dependency build, then regenerate extraction and
prepare contexts so the real collector captures final compiler/DLL/analyzer inputs after host builds.
No current DLL was loaded/executed by this preparation.

## Exact commands and later serialization

Preparation commands actually executed, from PRIMARY (all Python/static only):

```bash
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/extract.py --root /home/rupicola/Code/y/Yubico.Yubikit.NET --guide-root /tmp/opencode/ctap2-001-edit-f --patch > /tmp/opencode/ctap2-001-snippet-inputs.patch
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/extract.py --root /home/rupicola/Code/y/Yubico.Yubikit.NET --guide-root /tmp/opencode/ctap2-001-edit-f --materialize
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/extract.py --root /home/rupicola/Code/y/Yubico.Yubikit.NET --guide-root /tmp/opencode/ctap2-001-edit-f --check

# Final rerun after coordinator confirmed all content integrated into PRIMARY:
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/extract.py --materialize
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/extract.py --check
python3 docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples/verify-inputs.py
git diff --check -- docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/compiled-examples docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/checker-source/context/contexts-manifest.json docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/snippet-preparation.md
```

The early temporary patch predates final retention refinements; it is not the final manifest or a
compiler result. Materialization was rerun after those refinements, followed by fresh static checks.
Source/configuration edits used the patch tool; retained raw/wrapped/hash artifacts are generated by
the authorized extractor, not manually rewritten. Its `--patch` mode remains available to review
any integration refresh before application.

**Later, coordinator only**, after any final boundary corrections and the final clean dependency build
(F acceptance/integration is now confirmed):

```bash
RUN=docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001
TOOLS="$RUN/validation/checker-source"
python3 "$RUN/validation/compiled-examples/extract.py" --materialize
python3 "$RUN/validation/compiled-examples/extract.py" --check

# Execute this sequence separately/serially for each of the six explicit context IDs above.
CTX=consumer-lib
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX"
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX" --check
dotnet toolchain.cs -- build --project "AuditCt001.Examples.$CTX" --clean
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX" --collect
```

Use build skill/canonical coordinator tooling and retain complete fresh logs. Require exit0,
RESULT PASSED, Warnings0 for every host; collector success alone is not compilation success.
Repeat with consumer-demo, unit-v3, hardware-v2, consumer-wa-demo and consumer-wa-lib. Never run
host executables or test targets. Existing one-shot context preparation/evidence must be retained;
an input change after preparation requires explicit fresh reviewed IDs, not overwritten evidence.
There is no strict-raw invocation selected. Shell installation/sample/runner fences are not executed.

## Static evidence, likely issues and limits

Fresh deterministic extractor check: **95 blocks / 39 selected / 56 excluded / 33 groups**, exit0.
Independent Python comparison checks every selected body remains a contiguous exact body inside its
complete source after only initial import relocation, plus every retained raw/complete hash. All three
Python sources pass `ast.parse`; context assignment is disjoint/exhaustive and genuine source paths
exist. Module XML is structurally parsed in the full scan. This is not C# parsing/binding/analyzer
execution, SDK public-API verification or a compiled-example PASS.

Source checks establish the public CM constructor needs **Ctap2Session**, DeleteCredentialAsync takes
**PublicKeyCredentialDescriptor**, HmacSecret uses the real protocol/shared-secret/agreement/salt
overload, credential public key is raw ReadOnlyMemory, and shared intent uses real
**PublicKeyCredentialRequestOptions**. These were used as typed prerequisites, not guessed APIs.
No new confirmed source-binding documentation defect was found in the retained current bodies.

Potential future analyzer diagnostics remain unsuppressed—especially unconfigured awaits in Library
consumer wrappers (e.g. CA2007 if selected by the actual analyzer policy). Do not add ConfigureAwait,
remove awaits, alter prompt/helper bodies or suppress diagnostics to make a gate pass; record actual
diagnostics and route a documentation correction to its owner. Intentional fragments/scaffolds were
classified before compilation, not reclassified after a failed compiler run.

The guide-raw fragment sends without an encoded-request finally; its surrounding cleanup obligation
does not make that fragment exception-safe. It was retained, not repaired. Known literal-PIN string
erasure, caller/output lifetime, discoverable-state, retry/normalization and enforcement limitations
remain in the original owner records/prerequisites. None is closed by compilation preparation.

The adapter's existing root-only/unconditional/net10.0/property limits apply. Consumer-shaped hosts
import canonical consumer compiler/NET analyzer policy and real package closure; they do not claim
shipping-library AOT/public-API policy identity or source-project-local suppression parity. Actual
reference resolution/NET and xUnit analyzers/DLL inputs need later collection and successful builds.
No RP verification, cryptographic conformance, hardware UP/UV/PIN/bio/blob/reset behavior, CLI workflow,
platform qualification or final documentation closeout is established here.

Final PRIMARY snapshot: extraction manifest SHA256
`bc195ed012a8dfae5ab5e519011d9e5ceae337fd4b4a17972c242530ebb5b2e4`;
configuration SHA256 `cc320fbcd5aa385dbe278552a250e84e89b11f3eb56d633326633a001e03c7df`.
Exact static commands/exit codes and hashes are retained in
`validation/compiled-examples/static-verification.json`. No build/compiler verdict is attached.
