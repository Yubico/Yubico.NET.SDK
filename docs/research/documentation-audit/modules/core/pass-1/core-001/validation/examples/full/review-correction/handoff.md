# Full review correction: four local preparation fixes

**Ready for focused independent confirmation.** Original review SHA-256:
`f1c2d367252874dd6cb8b6b895fa4b5726103986a35ca26853c3899b310f03e0`.
The original Full profile remains exact at
`806ce07d050835dbcac7e4ce33ffb2b0006c5c8e9937e4321f3af6f569a4dc5c`.
`preservation.json` and `before/` retain the reviewed preparation/manifest/code/source-layout bytes.
No Full build, compiler capture/replay, test/example/native/device execution or Git mutation ran.
The active separate DocCheck final build and every qualified participant/workspace remain untouched.

## Narrow changes

1. **FULL-R1:** `capture.py` supplies the actual selected project to the proven descriptor algorithm.
   It checks the original pilot source SHA and replaces exactly one in-memory call expression:
   `helper.compiler_file_arguments(lines)` becomes `full_descriptors(helper, lines, PROJECT)`.
   Every other observer statement is unchanged. The adapter creates a private function-global copy
   with only PROJECT bound to that explicit project. It modifies neither the qualified helper module's
   PROJECT/ROOT nor its source, and preserves original raw Csc text/comma/quote semantics.
2. **FULL-R2:** the test import splitter recognizes genuine namespace/static using directives only.
   FULL-211's entire original `using var fakeConnection` resource declaration and subsequent body now
   remain inside CompileOnly. FULL-238 remains a same-real-context alias and points to FULL-211's actual
   canonical path/prefix/suffix/hash. No second alias source file is materialized.
3. **FULL-R3a:** FULL-244 uses `public void CompileOnly(YubiKeyTestState state)` for its synchronous
   original fragment. There is no dummy await or diagnostic suppression.
4. **FULL-R3b:** FULL-027 adds the genuine `System.Text` import for the original `Encoding.UTF8` call.

The generator is corrected as well as the three generated sources. All accepted raw/decoded bodies,
resource scopes, author/review hashes, real project contexts, source membership and all 34 previously
accepted noncompiled qualifications remain unchanged. `manifest-corrected.json` changes only the four
wrapper/alias metadata entries and adds the correction-review reference; original manifests remain exact.

## Project-relative and literal reconstruction proof

`verify.py` here exercises three actual project bases (Full consumer, Core.UnitTests and Management
IntegrationTests) with relative Source Link, relative quoted comma-containing embedded source, absolute
quoted source and quoted resource descriptor inputs. Expected paths are independently constructed from
each selected project's parent. Original raw argument strings remain exact. Negative cases reject a
relative/foreign/non-project selector and wrong-DocCheck/wrong-selected-project membership. The original
qualified helper PROJECT/ROOT remain unchanged across every case.

Positive namespace/static imports and negative `using var`/using-block cases distinguish the resource
declaration from an import. Literal body reconstruction independently checks FULL-027, FULL-211,
FULL-238 and FULL-244. It verifies FULL-211's declaration appears after the method's opening brace,
the alias uses the canonical source, FULL-244 is synchronous and FULL-027 has the real framework import.
The existing Full verifier also reconstructs all 300 selected source/context files from accepted bytes.
No C# compilation success is inferred from these static checks.

## New fixed input profile

`profile-delta.json` accounts for the complete original input population:

- **Five** existing participants change: Full capture adapter/verifier and the three generated wrappers.
- The old manifest participant is replaced by the explicitly corrected new manifest.
- **Two** actual static-proof participants are added: the corrected generator/import splitter and this
  bounded regression verifier.
- **1,707** original participant rows remain byte-exact. No root/product/qualified-tool input is repinned.
- Compile-source membership and the 25-project closure are unchanged.

**Corrected Full profile:** `../reviewed-inputs-corrected.json`

**SHA-256:** `cc9da7b2d9520c7b69fa54471b443744fa95e8e1b42c539f7649cb44ac11ef46`

The separate accepted DocCheck profile `db8347bc…` is unchanged and need not wait for this re-review.

## Bounded confirmation and unchanged caller sequence

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
python3 -B "$F/review-correction/verify.py"
python3 -B "$F/verify.py"
```

After focused acceptance, exclusive checkout release and the current build skill:

```bash
P=cc9da7b2d9520c7b69fa54471b443744fa95e8e1b42c539f7649cb44ac11ef46
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-first \
  --profile "$F/reviewed-inputs-corrected.json" --profile-sha256 "$P"
```

The same canonical clean-build pattern and nine existing test-context commands remain in
`full-example-preparation.md`. Require actual complete zero-warning capture/output/preservation proof.
No injected test or example DLL may run; restore ordinary uninjected test outputs with the final normal
clean solution build before any SDK test lane. Preserve first failures and return literal source errors
to the relevant editor instead of rewriting an accepted body or inventing a helper.
