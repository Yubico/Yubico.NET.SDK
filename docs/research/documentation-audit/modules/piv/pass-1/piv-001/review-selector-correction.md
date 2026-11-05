# PIV maintainer-selector correction review

**PASS — accept the scoped selector correction and all 57 current post-format product identities in
`evidence/review-accepted-v3.json`.** No further content correction or optional rewrite is requested.
Classify this as a C010 documentation/task guard with a tooling dependency, not a new SDK runtime bug.

Independent reviewer: GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476`. Current `src/Piv/README.md` SHA-256:
`1725cae8c82139750743613680a256160f5691955473bef2bee02a49a54599b8`.

## Source-backed selection semantics

Read `scripts/toolchain/Discovery.cs` discovery/matching methods, the narrow test-project construction
in `Context.cs`, test-target selection in `TestTargets.cs`, and relevant `Conventions.cs` constants.
This is tooling dependency inspection only; no Core or other-module implementation audit was opened.

`DiscoverProjects` recursively enumerates csproj paths, requiring the `/tests/` path component,
`.UnitTests` or `.IntegrationTests` path substring as appropriate, and the `Yubico.YubiKit.` prefix
somewhere in the path; it excludes `/.claude/`. `Context.cs` supplies units alone by default and
units plus integration projects when `--integration` is present. `MatchProjects` then matches the
**filename without extension** using case-insensitive substring containment. The project selector
is neither an exact module identifier nor a directory restriction.

A read-only Python enumeration reproduced those structural filters over the current checkout and
then the exact filename matching rule. Results:

| Test lane | Old `Piv` selector | Qualified `Yubico.YubiKit.Piv` selector |
| --- | --- | --- |
| Default unit lane | Canonical PIV unit project only | Canonical PIV unit project only |
| `--integration` lane | Canonical unit + canonical integration + retained AuditPiv001 hardware probe | Canonical unit + canonical integration only |

Exact qualified selections:

```text
src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Yubico.YubiKit.Piv.UnitTests.csproj
src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/Yubico.YubiKit.Piv.IntegrationTests.csproj
```

The first is the sole default-unit selection. Both are selected for each documented integration
command; `--integration` adds integration tests rather than selecting them exclusively. The old
integration selector additionally includes:

```text
docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/hardware-probe/tests/Yubico.YubiKit.AuditPiv001.Hardware.IntegrationTests/src/Yubico.YubiKit.AuditPiv001.Hardware.IntegrationTests.csproj
```

That probe contains the preserved expected-failure signing cases. Annotation/checker filenames
containing `Piv` alone do not establish inclusion in these test commands: they also need the structural
test-discovery filters above. The current enumeration establishes the actual additional hardware-probe
project, without extending this conclusion to every retained host or to build-target selection.

Smoke additionally excludes Slow and RequiresUserPresence categories; the full unattended command
excludes RequiresUserPresence. Those test-level filters do not replace correct project selection.
The qualified selector correctly targets the canonical module test projects in this checkout and
avoids the retained audit project. Matching remains substring-based, so future project inventories
can require rechecking this dependency.

## Complete task and exact change

Read the complete Maintainer verification section. All three commands now use the qualified selector,
and the added sentence explains its purpose. The nearby dedicated-key authorization, persistent
mutation, touch/hotplug coordination and two-key prerequisites remain intact. The wording leads with
the supported maintainer action and preserves local conditions without emphatic contrast or prose churn.

Independently checked every v2 accepted path against actual current bytes: only README changed.
Reversing exactly the three selector replacements and removing the one explanatory sentence restores
the prior accepted README hash exactly. All other 56 product paths, including the corrected CLAUDE
annotation example, match `review-accepted-v2.json` exactly. The new v3 manifest records all 57 actual
current post-format hashes. V2 manifest SHA-256 remains
`2e7e4f5981cf6a7a8159e9b3210abae2097fb1071810248074229fd5d91bcb97`.

No shipping C# bodies, executable test assertions, API snapshots, runtime toolchain behavior or
archived hosts changed. Prior source/API preservation and hardware-impact acceptance remain intact.
This review supplies source-backed project selection and content acceptance, not test execution or
new compilation evidence. Coordinator extraction/current compilation and release evidence should bind
to the v3 hashes; existing failed-host evidence remains preserved. Physical deferral is unchanged.

Only this record and `evidence/review-accepted-v3.json` were authored. No product edits, builds, tests,
hardware operations, formatting, commits or subagents were run.
