# Independent Sol follow-up: R1 checker provenance

## Decision

**PASS — the prepared correction addresses R1.** Accept the exact profile below for the separately
assigned first bound checker build. This accepts the provenance design and reviewed inputs, not an
unperformed compilation, an as-yet nonexistent seal or a checker result. No further R1 code correction
is required by this static review.

This follow-up preserves [the original review](validation-review.md) unchanged. Scope is R1 only;
it does not reopen whole-Core investigation or qualify product behavior. Source baseline remains
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; profile and observed metadata HEAD are
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`. Metadata may advance independently of the exact participating
file identities. No equality-to-HEAD requirement has been introduced for the full source population.

## Accepted chain and limits

Run-relative paths use `validation/checker-binding/` unless stated otherwise.

1. **Reviewed profile.** `reviewed-inputs-final.json`, SHA-256
   `65a53f340bd1496b259c98e4e15e20254cab61b394fb39e8f8f128b94a503e80`, binds 242 original inputs and 30
   exact text snapshots. It includes the unchanged checker source/project/props, root policy/configuration,
   the actual fixed toolchain include list, binder/hook, selected SDK/compiler/task inputs, canonical
   dotnet host and runtime. Later unrelated helpers do not silently enter its participant population.
   Both earlier profiles contain the obsolete binder identity and remain unaccepted historical attempts.
2. **Actual compiler evidence.** The before capture records compiler file inputs and snapshots, evaluated
   policy, restore/package identities and preprocessed import closure. The compile capture requires
   unchanged inputs and exact source/reference/analyzer/config item-to-argument membership; hidden
   response inputs, missing canonical analyzer participation and weakened execution prerequisites fail
   admission. The installed, profile-pinned `Microsoft.CSharp.Core.targets` actually emits
   `CscCommandLineArgs` from Csc and calls `TargetsTriggeredByCompilation` after that task inside
   `CoreCompile`. This supports the hook placement; actual hook execution still awaits the build.
3. **Output and build seal.** The finish/admit/build path requires complete successful zero-warning
   original v2 build evidence and actual Csc completion, identical before/compile/finish input identities,
   final implementation DLL/PDB equality to compiler outputs, reference assembly identity, runtime
   metadata and copied Roslyn equality to the selected SDK owners. The seal binds the original build
   receipt and capture records, original outputs, profile, runtime and sole executable DLL path. An
   incomplete build or a merely successful unrelated command supplies no acceptable seal.
4. **Bound invocation.** `run` requires the accepted profile hash and the coordinator-accepted actual
   seal hash. `bound` verifies current original inputs, source/configuration, snapshots/capture records,
   build receipt, generated sources, original outputs and exact runtime output membership. The DLL is
   derived from the seal; it is not supplied as an arbitrary command argument. The same checks repeat
   after the unchanged v2 logger's invocation, including exact actual command equality. Acceptance
   requires **both** its successful original command receipt **and** its passed binding sidecar.
   Replaced/missing original bytes, stale binaries or a failed sidecar remain unaccepted evidence.
5. **Ownership and runtime.** The hook imports the root targets at the correct nine-level depth and
   applies its controls only to `Audit20261111.DocCheck`. Restore/build share the named hook/profile/temp
   properties. Preprocess is evaluation-only, not a second compilation. Binary outputs stay in the
   Core-specific home jobs directory; new textual captures/sidecars stay in this Core run. Runtime
   selection uses the sealed canonical host with `exec --roll-forward Disable --fx-version 10.0.15`.
   Injection/policy environment overrides are rejected rather than dumped. The SIGTERM entry adapter
   raises the cleanup exception expected by the existing supervisor; this review sent no signal.

The immutable chain now covers the new semantic guard through its reviewed source, actual compiler
inputs, actual original outputs and accepted invocation. This is additive: the foundation, logger,
qualifier, checker source and original receipt fields are unchanged. Missing historical/live bytes are
not repaired by a bookkeeping rebuild. New participant bytes require a fresh profile/build review;
new product documentation or metadata alone need not invalidate an unchanged checker profile.

## Read-only verification performed

Independent Python byte reads/hashes, JSON/XML inspection and Python AST parsing verified:

- The exact final profile hash and **all 242 inputs / 30 snapshots** against their current original bytes.
- **All 102** `static-verification-final.json` record raw hashes against their current files. Their
  supplied classification report retains textual records and byte-preserving snapshots; no binary
  runtime payload has been added to the record population.
- The six original source/report identities listed in that report, plus the original review identity.
- The actual installed SDK target's Csc output and compilation-trigger hook location, by reading it.
- The 22 negative-case definitions and their independent copied fixture state, standard SHA-256 oracle,
  actual argument-membership checks and signal-exception adapter. These are meaningful provenance
  boundary checks. Their supplied execution report is retained; this reviewer did **not** rerun tests,
  selfcheck entry points or signal handling.
- The retained qualification's **38 raw text identities**, four quiescent Python cases, atomic/fallback
  claims and six historical RESULT-parser rejection labels. This was retained evidence inspection,
  not new process qualification.
- The already successful `validation/commands-v2/sdk-info-first/receipt.json` and both retained stream
  identities. They report SDK **10.0.404** and runtime **10.0.15**. No SDK command was replayed.

No binder prepare/build/capture/run operation, compiled checker, process qualification, SDK test,
native/device operation or formatter ran. No source, index, ref, original record or frozen archive was
changed by this review. The previous whole-foundation preservation results remain separately scoped;
this follow-up does not claim another full historical archive scan.

## Exact next commands for the coordinator

**Do not rerun `sdk-info-first`.** `checker-binding/handoff.md:64` still displays that already occupied
label. The following sequence supersedes that line and uses the retained successful SDK observation.
Likewise, do not repeat logger qualification. Keep its original participant hashes and receipts.

After exclusive execution release and loading the current build skill, run from the primary root:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
B="$V/checker-binding"
P=65a53f340bd1496b259c98e4e15e20254cab61b394fb39e8f8f128b94a503e80
python3 -B "$B/bind.py" build --label checker-bound-build-first \
  --profile "$B/reviewed-inputs-final.json" --profile-sha256 "$P"
```

That wrapper executes the canonical clean toolchain build through the accepted logger. Inspect the
actual RESULT, zero warnings/errors, all three compiler/build captures, original command receipt and
emitted seal. **Accept the actual emitted seal SHA-256 before any checker invocation.** No seal hash is
predicted or approved in this static review. Then substitute that actual accepted value:

```bash
S=<accepted-actual-seal-sha256>
SEAL="$B/attempts/checker-bound-build-first/sealed.json"
python3 -B "$B/bind.py" run --label checker-selfcheck-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" selfcheck
python3 -B "$B/bind.py" run --label source-tokens-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" tokens
python3 -B "$B/bind.py" run --label api-first \
  --profile-sha256 "$P" --seal "$SEAL" --seal-sha256 "$S" api
```

Require each original command receipt and `checker-binding/invocation-<label>.json` sidecar to pass.
Read actual output and preserve first failures. If a label becomes occupied, use a fresh attempt label
and its actual seal path rather than overwriting evidence. Further compiler/profile changes require
their own review. This sequence is not repository test/native/device authorization or semantic coverage
completion. Native C/C++ remains not applicable; native-facing C# remains in the source guard. Family
investigations, final examples, the user's PIV physical deferral and later Windows handoff are unaffected.

## Reviewed identities

| File | SHA-256 |
| --- | --- |
| `checker-binding/bind.py` | `f84745dd4b649ded891a9c0870dbd03e5163a765cb893b736d520fffb148633e` |
| `checker-binding/hook.targets` | `389b76a955734aae953da450767a2f139fae26f84d076499567023483558bf64` |
| `checker-binding/reviewed-inputs-final.json` | `65a53f340bd1496b259c98e4e15e20254cab61b394fb39e8f8f128b94a503e80` |
| `checker-binding/handoff.md` | `a41124cc0c99a02f0fe2e8db1d39a40cf13747594928a5cc9af583a9074f71ba` |
| `checker-binding/provenance.json` | `a47fd047beb6881a64d38f226776e99e7c091d095d4b1ba65f3980a4b0503a89` |
| `checker-binding/static-verification-final.json` | `866472d38278eadee1ca26881f59a45371b9bead2febff60b9e2396590ac9415` |
| `commands-v2/sdk-info-first/receipt.json` | `3ba578884d76fec9e3319d3f09da16c9a79b5b55f675391c64a8907931ba9bd8` |
| `logger-qualification-first/retained/qualification.json` | `8d12e538ed587cdbb4d63f767f4d5b8dcb97bf8f21c85f83e65cd881748b37ac` |

Table paths are relative to `validation/`. The preserved original review SHA-256 is
`b788cb19aa760041590b14d254a1a2e6d234b92b8b158f3e835d9fab8f61d371`.
Only `validation-review-followup.md` is written by this follow-up reviewer.
