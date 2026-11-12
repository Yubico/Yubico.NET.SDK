# SAMPLE-WA002 / FULL032: WebAuthn logging example correction

Corrected only logging prose and its fence in `src/WebAuthn/CLAUDE.md`, including the repeated logging
route in Known Gotchas. The pre-edit guide matched accepted PRIMARY byte-for-byte. The root's internal
helper preference is conditional on accessibility; it cannot make an internal Core member callable
from an assembly outside Core's friend list.

## Actual source and API evidence

- Complete isolated `src/Core/src/YubiKitLogging.cs:1–69`: the public LoggerFactory property at 24–36
  exposes ILoggerFactory; CreateLogger<T> at 38 is internal. It delegates to the same public generic
  factory extension now used by the corrected example. Configure and temporary replacement change
  the factory reference; the module's static readonly logger retains the instance created earlier.
- Complete current PRIMARY version of that file, read-only, `1–82`: public property at 32–44,
  internal helper at 46. Its remarks at 20–26 explain application factory ownership, retained static
  loggers and no automatic redaction. Source line differences from the isolated tree are recorded
  with distinct full-file hashes and raw snapshots, not silently treated as identical revisions.
- Complete Core project file `1–63`, in both trees: the InternalsVisibleTo lists omit WebAuthn and
  WebAuthn.IntegrationTests. WebAuthn's own project at `1–25` names its assembly and references CTAP2;
  friendship is not inherited transitively. No friend declaration or accessibility is changed here.
- `PreviewSignProcessor.cs:15–28` identifies the internal processor in
  `Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors`. The documented code is an implementation
  fragment inside this class, not a standalone consumer program or an integration-assembly entitlement
  to use an internal type. The field belongs at class scope; the LogDebug statement belongs in a method
  with selectedAlgorithm supplied.
- Focused Context7 `/dotnet/docs` API clarification returned the generic
  `LoggerFactoryExtensions.CreateLogger<TCategoryName>(this ILoggerFactory)` signature and the rule
  that extension methods require their defining namespace imported. The targeted .NET runtime
  **v10.0.0** original at
  `https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LoggerFactoryExtensions.cs`
  was read in full and retained as `LoggerFactoryExtensions-v10.0.0.cs.snapshot`. It declares the public
  generic extension in `Microsoft.Extensions.Logging`, creating a typed logger category. This is
  source/API evidence, not execution of the example or proof of the installed package's identity.
  The isolated central package file pins Logging/Abstractions 10.0.2; no restore or package code ran.

The guide now uses `YubiKitLogging.LoggerFactory.CreateLogger<PreviewSignProcessor>()` and explicitly
requires file-scope imports of `Microsoft.Extensions.Logging` and `Yubico.YubiKit.Core`. It retains a
static readonly logger, initialization/category rationale and application factory/provider lifetime.
Replacing the factory does not update a cached static logger. Existing sensitive PIN, key, derived
secret, tbs/argument, correlation and redaction rules remain byte-identical.

## Changed example and preserved history

This changes **one current fence**, not zero. `before-body.cs.fragment` and `after-body.cs.fragment`
contain the complete raw bodies; `verification.json` records their hashes, positions and classification.
The fragment is not promoted to a complete executable example. Of the 47 current FIDO fences, 46 are
unchanged and this logging body is explicitly superseded. All 47 historical bodies and their earlier
classifications remain untouched in the original receipts.

`focused.patch` applies to the accepted pre-edit guide. Full before/after guide snapshots, source
snapshots/hashes and preservation receipts are local to this directory. The accepted full 15-file patch,
reviews, revision-2 records and queued WA selection-oracle patch
`d70a50aaf297cc62fd6c7fa93f13e8cfb1acb44fe975279dc9fb60fc30e5b05e` remain byte-exact.

FULL032 is an actual compiler-body accessibility failure, not a wrapper failure. Its previous failed
attempt stays retained with the coordinator; no PRIMARY or validation artifact was edited, regenerated
or relabeled passing. This source-supported correction has **not** been compiled here.

## Focused review and coordinator queue

Reopen selected unit `4886f819fa316d28`, previously credited supported, as **SAMPLE-WA002 documentation
correction pending independent review and fresh compile**. This is a declared handoff to the sampler/
coordinator, not a change to central sampling or finding ledgers and not a new runtime finding.

The independent reviewer should check the complete logging section/fence and repeated Known Gotchas
route against actual Core visibility, friend-list scope, extension import, static logger lifetime and
sensitive-data rules. After acceptance, Astra transfers the exact correction and runs the fresh actual
compile profile. Existing FULL032 failure evidence must remain alongside the new attempt.

Only static preservation/fence/diff checks ran. No SDK build/test, formatter, cryptographic/native/device
call, Git mutation, subagent, runtime/API/IVT change or PRIMARY write occurred. Finish Core Pass 1 through
the existing review/compile/publication gates, deliver deferred findings, then stop.
