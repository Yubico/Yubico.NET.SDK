# Formatter-only import-removal preservation proof

**2026-11-04; validation-only preparation.** The coordinator reports initial SDK gates passed
(3,998 unit cases, 492 CTAP cases, zero warnings), and formatter verification reports 152 EOF,
19 import-order and 30 IDE0005 diagnostics across 152 edited C# files. Those are earlier reported
gate/diagnostic counts, **not removal counts or a final validation result**. No formatter, build,
test, helper, hardware operation, commit or delegation was run by this follow-up. No SDK file,
immutable Management run or original framework evidence was changed.

## Separate mode; unchanged strict check

Retained source: `validation/checker-source/src/Program.cs`. New schema:
`validation/checker-source/import-removals.schema.json`. After the coordinator supplied actual formatter
evidence, `validation/import-removals.json` was materialized from those records with exact hashes/names;
no source capture, namespace name or approval group was guessed. Existing captures/logs were not edited.

The existing **`tokens <repo>` implementation is unchanged** and still rejects import deletion.
New opt-in mode:

```bash
RUN=docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001
CHECKER=/tmp/opencode/ctap2-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Ctap2.DocCheck.dll

# Coordinator's next isolated helper build; not run in this task:
dotnet toolchain.cs -- build --project AuditCt001.Checker --clean

# After actual scoped formatting, capture and review:
dotnet "$CHECKER" tokens-import-removals "$PWD" "$RUN/validation/import-removals.json"

# Original mode remains available, with no exemption:
dotnet "$CHECKER" tokens "$PWD"
```

Compiler/Emit diagnostic handling, analyzer policy, API byte comparison and all existing strict
token/directive shape functions and using-order allowance are unchanged. This is an explicit import
exception, not a claim that all C# syntax remained identical or an SDK behavior fix.

## Manifest contract

Use a file named `import-removals.json` in this run. All stored paths are canonical repository-relative
forward-slash paths, not absolute paths, `..`, symlinks or aliases. Manifest/captures/logs must reside
inside `docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/`. The schema describes shape;
the checker additionally enforces semantic restrictions and rejects duplicate/unknown JSON fields.

- Root: `schema_version: 1`, exact full `baseline: 5b09a9b6942ded3913b5c9340c3e126617e61a82`,
  shared `formatter` provenance and nonempty `files` array. No default or abbreviated baseline.
- Each file: unique changed executable C# `path`; SHA-256 `baseline_sha256`; `before` and `after`
  capture objects; nonempty `removals` array.
- Capture objects: `path` is a retained JSON bundle; `bundle_sha256` hashes its exact bytes; `sha256`
  hashes the selected complete source file. Entries are selected by the owning file's exact `path`,
  never a line/index inferred from a formatter diagnostic. `before` refers to `pre-format-content.json`
  and its full `content`/`sha256` entry under the exact baseline. `after` refers to `format-transform.json`
  and its approved `after_sha256`; the complete current source is read and checked against that hash.
  This reuses the existing complete pre-format capture and frozen post-format digest rather than inventing
  duplicate snapshot files. UTF-8 re-encoding of captured pre-format text must match its source-byte hash.
- Shared formatter object: `diagnostic: "IDE0005"`, retained `command_log`/`command_log_sha256`,
  diagnostic `log`/`log_sha256`. The command log's first line must be the actual scoped
  `$ dotnet format ... --include ...` command containing each approved source path, and the diagnostic
  log must contain IDE0005 evidence. This avoids duplicating the full 152-path command in each entry.
- Every removal object has only `namespace`: exact namespace-name token text from the actual removed
  compilation-unit using directive, for example the name between `using` and `;`. No token ranges,
  method names, arbitrary replacement text, exemptions, diagnostic suppressions or regex selectors.

Do not derive namespace names from diagnostic line positions: the formatter may report positions in
its **in-memory sorted** import list. The coordinator owns exact pre-/post-format byte capture and
actual directive-difference extraction. Correlate those removed syntax nodes with real IDE0005/formatter
evidence and retain that reconciliation for independent review. The checker cannot authenticate the
origin of a coordinator-supplied log or independently prove semantic unusedness from syntax.

Only files with actual approved removals go in `files`; EOF/order-only files remain strict. Count
IDE0005 diagnostics and actual unique removed directives separately. A nonempty manifest is mandatory
for the optional mode; use strict mode if no imports were actually removed.

## Fail-closed proof mechanism

1. Enumerate exactly the same baseline-changed/untracked executable-source roots as strict mode.
   Manifest paths must be canonical, unique members of that changed-source set. Added/deleted/untracked
   executable source still fails its baseline lookup, rather than becoming an exemption.
2. Hash actual baseline bytes from Git against `baseline_sha256`. Hash both retained source captures
   and retained IDE0005 diagnostic evidence. Hash current disk bytes against the approved post-format
   digest. The transform must record exit 0, no added imports and no non-using/non-whitespace change;
   its exact before hash and removed namespace set must match each approval. A syntactic strict
   comparison independently checks the reported no-collateral-change claim rather than trusting it.
3. Compare **baseline → pre-format capture with the original strict token/directive comparison**.
   Existing pre-format executable/declaration/attribute/import changes cannot be hidden by approval.
4. Parse baseline, pre-format and current source with C#14. The narrow exception accepts only valid,
   **entirely directive-free** files, also rejecting disabled/skipped/malformed source. This is deliberately
   stricter than merely proving a particular import unconditional; even an unrelated `#nullable`/`#pragma`
   means this minimal mode will refuse the file. Other files retain the original directive-preservation
   check. Do not remove preprocessor directives or weaken this guard to make formatting pass.
5. Each listed namespace must identify exactly one actual using directive in baseline and pre-format
   source, both in the compilation-unit using list. Reject namespace-scoped/ambiguous/duplicate directives
   and alias, static, global, unsafe or alias-qualified/type imports. Require the namespace absent from
   every current using directive. A `using` statement/declaration inside a method is never selectable.
6. Remove **only those validated using nodes** from comparison trees, not source files. All members,
   declarations, attributes, arguments, literal text, method bodies, other using directives and other syntax
   nodes are untouched. Compare normalized pre-format → current and normalized baseline → current using
   the original strict shape comparison. Unlisted deletion, changed import, added import or any other
   token change fails. Existing directive-free using-order normalization remains the only order allowance.
7. Every nonmanifest file uses the original strict token/directive shape comparison. Successful optional
   output identifies each actual listed removal/provenance, labels exception files versus strict files,
   and reports total files, exception files, listed removals and failures. It explicitly states syntax is
   **NOT identical**. Rejected evidence or an unapproved change exits nonzero, with no passing verdict.

The exception does not simply subtract arbitrary token strings from the source: it can select only
specific validated Roslyn `UsingDirectiveSyntax` nodes. A manifest therefore cannot authorize dropping
method/declaration tokens or attributes. Pre-format capture comparison prevents approval from legitimizing
a change made before formatting; post-format comparison prevents hidden collateral changes during it.

## Static verification and remaining gates

Static source checks confirmed the original strict token/directive/API source regions are unchanged
against the proven implementation (with this run's existing baseline adaptation), and using-order
normalizer code is preserved. The explicit mode normalizes only the two validated compilation-unit using
lists; strict compiler binding/Emit warning handling and warning level 9999 remain intact, with no new
specific-diagnostic override. JSON shape checks confirmed manifest/schema field agreement, exact
30-file transform membership, baseline hashes, pre-format content hashes, current after hashes, both
bundle hashes, command/diagnostic-log hashes and path-level IDE0005 provenance. Actual namespace names
came from retained transform records and were checked against baseline/before/current source, not
diagnostic line indices. These static checks passed; they are not helper execution or C# compilation.
Context7 `/dotnet/roslyn`
was consulted for current using-directive syntax APIs; .NET API documentation was also consulted for
file/link validation. All writes in this task use the patch tool.

Coordinator next steps: build revised helper in its reserved lane; apply scoped formatter with exact
before/after captures; generate the actual removal manifest; retain strict failed-import-deletion output
separately from the explicit optional proof; check optional preservation and API snapshots; run fresh
final compiler/analyzer/unit gates. Astra records and reconciles the actual exceptions. A fresh independent
reviewer must check the actual manifest/captures/provenance and the final build/API/test evidence.
No imports should be removed manually based only on source-line inference or presumed unusedness.

## Materialized actual exception

The coordinator's formatter application completed with exit 0. Actual records list **31 ordinary
namespace-import removals in 30 files**:

- 29 removals of `Yubico.YubiKit.Ctap2`.
- One `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu` and one
  `Yubico.YubiKit.Core.Protocols.SmartCard.Scp`, both from `src/Ctap2/src/IYubiKeyExtensions.cs`.

The two external namespaces are valid ordinary-import candidates; there is **no ancestor-only namespace
restriction**. Their compiler-unused status comes from actual formatter/IDE0005 evidence, not an assumption
that namespace ancestry makes a directive removable. Aliases/static/global/unsafe/conditional imports remain
forbidden, as do added imports and all declaration/body/attribute/preprocessor token changes.

Manifest inputs: `validation/pre-format-content.json` (152 full accepted texts/hashes),
`validation/format-transform.json` (actual per-path before/after hashes/removals),
`validation/format-initial.log` (diagnostics) and `validation/format-apply.log` (actual command/output).
`validation/pre-format-tokens.log` retains the coordinator's **152-file strict pre-format pass**.
It is contextual evidence, not a newly executed helper result in this task. Every manifest baseline hash
was read from Git at the full baseline; every pre-format content hash and current post-format hash was
checked against the real retained records. There are no added namespace imports in the transform.

Materialized manifest SHA-256:
`3b53ba417658b6e760569bb7ea5709645dcd2e3ed4f0993301f3d70dc061da24`.

Expected final proof wording, **only after the coordinator builds and executes the new mode successfully**:
152 checked files, 30 explicit exception files, 31 listed namespace imports removed, other files strict,
and zero other token/directive changes under the preserved order allowance. Do not call all syntax identical.
Actual helper output is authoritative if final changed-file scope differs. New source trivia hashes are
recorded as capture integrity, not fresh semantic/source-comment review claims.

Limit: this is a syntactic preservation proof with an explicitly reviewed compiler-unused namespace-import
exception. It is not independent semantic equivalence, protocol/device interoperability or a hardware result.
