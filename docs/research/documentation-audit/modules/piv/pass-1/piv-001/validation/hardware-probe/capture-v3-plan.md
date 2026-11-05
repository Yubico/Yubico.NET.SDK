# Exact transitive project-reference capture

## Diagnosed boundary

The coordinator's second P01 attempt clean-built with AllEnabled and zero warnings. Its
v2 capture reconciled actual Csc sources/references/analyzers/configs and passed the four
direct-project bindings, then failed by comparing the late `ProjectReference` item set to
only those four declarations. No P01 test or pre-cleanup operation ran in that attempt.

The late item set contains twelve projects: declared Core, PIV, Tests.Shared and PivTool,
plus Cli.Commands, Cli.Shared, Ctap2, Management, Oath, OpenPgp, YubiHsm and YubiOtp.
Installed SDK 10.0.403's `Microsoft.PackageDependencyResolution.targets:489–494` explicitly
adds `_TransitiveProjectReferences` to `ProjectReference` before reference resolution.
PivTool's five direct references and Cli.Commands' applet references explain this closure.
The mismatch was a capture assumption, not an observed SDK runtime defect.

## Narrow versioned change

`capture-v3.py` preserves every v2 Csc membership, direct-four, analyzer-package, language,
warning/no-skip and source-retention guard. It replaces the late direct-only equality with
an exact graph proof:

1. Keep the four host XML declarations fixed and require the restored net10.0 direct
   references to equal them.
2. Read project XML recursively, normalizing relative paths, backslashes and the known
   `$(RepositoryRoot)` expression. For the current unconditional graph, every XML edge
   must exactly match the restored project's net10.0 dependencies.
3. Use restored project identities, selected framework and dependency edges as the
   evaluated authority for conditional, metadata or unresolved-property reference items.
   Preserve their XML descriptions rather than guessing condition/property values.
   Unconditional literal edges still have to appear in that restored graph.
4. Require the recursive closure, all restored project nodes and the late item set to equal
   exactly the twelve known repository project paths. Extra, omitted, unreachable,
   duplicate or mismatched identities fail capture.
5. Bind every one of the twelve restored project IDs/compile assembly names to its unique
   actual Csc ref DLL under that project's own obj directory. Also require the resolved
   source-project item set to equal the closure and bind each resolved DLL to that
   project's bin directory. Record both DLL identities.
6. Retain every closure project XML file, restored graph/assets, SDK transitive-reference
   source and graph/edge/binding proof alongside the existing source/config/compiler text.
   Reading project XML expands no Core implementation-body review.

The helper is bounded to this twelve-project net10.0 host graph. A changed graph requires
an explicit new adjudication; it does not widen acceptance to arbitrary dependencies.
No build hook, props, product/probe body, fixture, catalog, device helper or analyzer
selection policy changes in this correction.

`run-probe-v4.py` differs from the preserved v3 wrapper by exactly two `capture-v2.py` →
`capture-v3.py` path substitutions. It retains all serial/config, one-executed/zero-skip,
runtime assembly, raw-failure, fixture and independent-cleanup boundaries. Physical P07
remains blocked.

## Coordinator ordering and command

The coordinator reported serial-125 cleanup control PASS at 07:08 UTC and a successful
fresh serial-103 pre-multi capture. While P01 compiler proof was blocked, the coordinator
reordered the three existing unattended multi-key cases ahead of P01, temporarily
allowlisting `[103,125]`. This is within the existing unattended dedicated-key scope;
physical touch/hotplug/P07 remains deferred. No worker-side hardware operation occurred.

Await the multi-key runner's terminal completion, independent restoration of both keys,
and the coordinator's return of the shared allowlist to `[103]`. The currently changed
two-key config must not be captured as the earlier P01 build's input. Preserve the old
v3 failure as historical evidence; perform no manual recapture of that current workspace.

After the coordinator confirms source/config stability for the fresh build/capture
window, run:

```bash
/usr/bin/python3 docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/hardware-probe/run-probe-v4.py P01_X25519_Rfc7748AliceAndBobAgree --name p01-x25519-v4
```

This wrapper checks `[103]` before a new build, clean-builds the unique audit host, runs
v3 capture immediately and starts pre-cleanup/test execution only after capture succeeds.
Subsequent P02–P06 cases use the same new wrapper and their unchanged case names with
fresh evidence stems. The source review/formatting checkpoint is coordinator-reported;
hold its accepted product sources stable until each actual capture completes.

## Preservation and readiness

Original v2 tool, v3 wrapper and the exact raw twelve-item list are preserved byte-for-byte
under `capture-v2-preserved/`. Original `evidence/p01-x25519-v3/` failure/build/compiler
artifacts remain intact. `capture-v3-provenance.json` records those identities, full source
diff, exact wrapper transformation, SDK source identity and preparatory graph checks.

This worker performs static source/XML/data inspection only. It executes no new capture,
build, test, hardware command, formatter, commit or subagent. The next coordinator fresh
capture establishes current source/config/binary time binding; this preparation makes
no new P01 runtime or SDK-bug claim. Serial-125 cleanup preparation is already complete
and receives no additional changes here.
