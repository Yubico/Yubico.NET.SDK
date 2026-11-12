# Consumer-third: remaining generated contexts

**Ready for focused review, not a compiler pass.** The retained 050059 build log has 21 unique
diagnostics across 11 bodies. Restore and previous import defects cleared; consumer-third still failed.
All three failed attempts remain failed. This worker ran only static Python preparation/verification.

## Every emitted diagnostic: generated context versus accepted body

`actual-diagnostics.json` preserves the full diagnostic identities; `diagnostic-triage.json` classifies
each against original raw body/document coordinates and the hash-bound excerpts in `context-evidence.json`.

| Body | Diagnostics | Classification / exact generated correction |
| --- | ---: | --- |
| FULL-036 | 3 CS0104 | Added CTAP2.Extensions conflicts with WebAuthn PreviewSign types. Remove that one generated directive; the enclosing guide uses WebAuthn extension input containers and WebAuthn GeneratedSigningKey. No aliases needed. |
| FULL-411 | 1 CS1061 | Restore Management namespace from original PIV source imports; the actual IYubiKey extension lives there. |
| FULL-163 | 1 CS0103 | Import actual public ChecksumUtils namespace `Core.Protocols.Otp.Hid`. |
| FULL-209 / FULL-232 | 2 CS0246 | Import actual IFidoHidConnection namespace `Core.Protocols.Fido.Hid`. |
| FULL-250 | 1 CS0103 | Restore guide's explicit `ProtocolConfiguration customProtocolConfig` borrowed parameter and its real Apdu namespace. |
| FULL-263 | 2 CS0103 + 1 CS4033 | Restore owner-specified `IManagementSession session`, `CancellationToken cancellationToken`; original await requires async Task wrapper. |
| FULL-268 | 1 CS0103 + 2 CS4033 | Restore owner-specified caller-owned `ISmartCardConnection connection`; async Task wrapper retains logger factory and session scopes. |
| FULL-279 | 2 CS0103 + 1 CS4033 | Original XML explicitly identifies `IManagementSession session`, `CancellationToken ct`; restore both and async Task, retaining early return and finally cleanup. |
| FULL-281 | 1 CS8604 | Full wrapper weakened existing owner prerequisite `GetNewCode` from `Func<byte[]>` to nullable. Restore the exact documented contract; preserve nullable current-code provider. |
| FULL-329 | 3 CS0103 | README explicitly requires task-owned six-byte `byte[] accessCode`; restore mutable buffer parameter, retaining actual write and original finally zeroing. |

**All 21 observed diagnostics are generated-context defects; no true accepted-body defect was found.**
No accepted body was edited. Later compiler errors remain possible; return any real body defect with
document/body ID/literal diagnostic to the coordinator/editor rather than silently repairing it.

### FULL-281: contract evidence, not a nullable-flow workaround

`DeviceConfiguration.cs:214–228` contains the exact cleanup sketch and does **not** declare acquisition
helper implementations. Its actual API requires `byte[] newLockCode` (lines 230–234), but that alone
would not authorize inventing a nonnull acquisition contract. The pre-existing Management owner explicitly
documents it in:

- `management-001/edit-c.md:212`: `Func<byte[]?> GetCurrentCode` / **`Func<byte[]> GetNewCode`**, with
  caller-owned 16-byte buffers and only the unlocked-key current code nullable.
- `management-001/snippet-preparation.md:138–144`: the same exact typed application delegates, without
  provider implementations, fake APIs or hardware initializers.

The corrected Full parameter matches that documented prerequisite. The body still assigns inside try,
awaits the real helper and clears both nullable cleanup locals in finally. There is no `!`, added guard,
NoWarn, invented acquisition implementation or substituted call. These two owner documents are explicit
fresh-profile inputs; exact excerpts and whole-file hashes are retained for focused review.

## Whole-population verification and preservation

- Exactly **11 consumer prefixes/signatures** changed; all **56 test preparations** and the remaining
  consumer sources stay byte-exact. `generated-contexts.delta.json` contains exact hashes and unified diffs.
- All **300 selected sources** retain original accepted directives/order/content and every non-prefix
  byte. Raw/decoded bodies, authority/review fields, source coordinates, aliases and context membership are unchanged.
- `all-source-classification.json` classifies the same async, watched free-prerequisite, nullable
  acquisition and PreviewSign ambiguity families for all 300 sources: 159 async statement wrappers,
  47 synchronous statement wrappers and 94 original-member/specialized contexts. No sync statement
  wrapper with await, unresolved watched statement prerequisite, or competing PreviewSign short-type
  imports remains. FULL-281 is the sole byte-array acquisition-delegate case. All duplicate/nonexistent
  repository namespace import guards also pass.
- These are deliberately **finite lexical checks, not a general C# parser or proof of arbitrary
  free-name/nullability/type resolution**. Exact original members retain their actual enclosing declarations;
  the compiler still owns binding. No framework or new execution mechanism was added.
- Static context oracles failed on all 11 original cases before the fix and pass after it; recorded in
  `regression-stages.json`. Full raw-source/wrapper/pilot/prior bounded-review guards also pass.
- `preservation.json` binds **549 immutable records across all three failed attempts**, all current
  **300 sources** in one text snapshot, six preparation predecessors, profile `72753…` and prior review/
  correction records. Old manifests/profiles/seals/checker workspaces remain intact. Historical import
  verification remains historical; the current verifier rechecks imports on current sources, rather than
  rewriting or rerunning the old prefix-only oracle against a changed candidate.

## Fresh candidate and coordinator retry

`../reviewed-inputs-contexts-fixed.json`

**SHA-256:** `3e40cb440ddd872de8bfba3ae3c0e45166ff5dbcb3a44aad890b968c97885228`

Derived additively from unchanged profile
`72753dce86098c486a9098bb79c1498efb2127fcc0192a1e6999ef300a8ed9c5`:
**14 changed existing rows** (11 generated sources, `contexts.py`, two verifier selectors/wiring),
**one manifest replacement**, **nine explicit additions**, **1,708 predecessor rows unchanged**,
**1,732 final fixed rows**. New inputs are the finite verifier, old manifest, five preservation/triage/
context-evidence files and the two existing owner prerequisite documents. Historical evidence is
transitively hash-bound by preservation. `profile-delta.json` enumerates every input change.

The **1,330 compile-source paths**, **25 projects**, **361 accepted product paths**, accepted product map,
all ten contexts, root/product/qualified tooling, observer/parser/logger and host restore fix remain unchanged.
`verification.json` records final static profile/context checks. Focused acceptance must precede retry:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=3e40cb440ddd872de8bfba3ae3c0e45166ff5dbcb3a44aad890b968c97885228
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-fourth \
  --profile "$F/reviewed-inputs-contexts-fixed.json" --profile-sha256 "$P"
```

Use the unchanged qualified logger/capture and canonical clean build pattern. Accept actual complete
zero-warning/error Csc/output/preservation proof before continuing the nine test contexts serially.
Preserve any failure with its fresh label. Never execute injected assemblies; restore ordinary uninjected
test outputs with the final normal clean solution build before any SDK test lane. No SDK build/test,
capture replay, native/device/provider execution, formatter, Git mutation or subagent occurred here.
Primary remains reserved. Finish Core Pass 1 and its deferred-finding handoff, then stop.
