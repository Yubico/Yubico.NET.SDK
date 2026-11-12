# Independent Sol confirmation: generated context prerequisites

## PASS — consumer fourth attempt ready

Accept `reviewed-inputs-contexts-fixed.json` at SHA-256
`3e40cb440ddd872de8bfba3ae3c0e45166ff5dbcb3a44aad890b968c97885228` for the coordinator's serialized
consumer retry. All observed third-attempt diagnostics have source-backed generated-context corrections.
No additional correction or framework work is required by this bounded review. Actual compiler/body/API
binding and output acceptance remain pending the retry.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. The reserved primary's 361 accepted product paths/map,
root policy and qualified tooling are unchanged. Queued worktree corrections remain outside this profile.

## All 21 diagnostics and the actual prerequisite contracts

Read the retained diagnostic identities/triage, original enclosing documentation, hash-bound owner
prerequisite excerpts, actual SDK declarations and the complete generated-context diffs.

| Body | Observed diagnostics | Independent context decision |
| --- | ---: | --- |
| FULL-036 | 3 CS0104 | Correct to remove only the wrapper-added CTAP2.Extensions directive. The enclosing WebAuthn example constructs WebAuthn registration/authentication extension containers. Their actual PreviewSign properties, GeneratedSigningKey, GenerateKey, SigningParams constructor and CreateSignByCredential contract use the WebAuthn PreviewSign types. No alias, cast, substituted type or body edit is needed; the short names now resolve to that intended layer. |
| FULL-411 | 1 CS1061 | The original PIV source imports Management. Actual `Management.IYubiKeyExtensions` supplies `GetDeviceInfoAsync`; restoring that namespace fixes the wrapper's omitted extension context. |
| FULL-163 | 1 CS0103 | Actual public ChecksumUtils is in `Core.Protocols.Otp.Hid`. Importing that namespace preserves the original response/length/CRC expression. |
| FULL-209 / FULL-232 | 2 CS0246 | Actual IFidoHidConnection is in `Core.Protocols.Fido.Hid`; both original connection/session scopes are retained with the real namespace. |
| FULL-250 | 1 CS0103 | The Management guide explicitly requires caller-supplied `ProtocolConfiguration customProtocolConfig`. Restore it as a typed parameter and import its real Apdu namespace; retain the original SCP/session lifetime and both awaited reads. |
| FULL-263 | 2 CS0103 + 1 CS4033 | Pre-existing owner inventory/preparation require `IManagementSession session` and `CancellationToken cancellationToken`. The unchanged body awaits the real configuration write; the containing wrapper must be async Task. |
| FULL-268 | 1 CS0103 + 2 CS4033 | Pre-existing owner preparation requires idle caller-owned `ISmartCardConnection connection`. The unchanged awaited session scope needs async Task; its logger factory/session/borrowed-connection boundaries remain intact. |
| FULL-279 | 2 CS0103 + 1 CS4033 | Original XML names the initialized session and caller token. Restore `IManagementSession session`, `CancellationToken ct` and async Task, retaining early return, real prompt/write and finally clearing. |
| FULL-281 | 1 CS8604 | Restore the independently documented nonnull acquisition delegate contract, as detailed below. This corrects the wrapper's weakening rather than changing nullable flow inside the accepted body. |
| FULL-329 | 3 CS0103 | README explicitly requires the task-owned six-byte `byte[] accessCode`. Restore that mutable parameter; retain its actual write and original finally zeroing. It is not replaced with read-only memory or a dummy initializer. |

### FULL-281: documented application contract, not a nullability escape

The actual helper's `byte[] newLockCode` parameter alone would not justify inventing a nonnull
acquisition function. Here the original Management owner already supplies the exact application
prerequisite in both:

- `management-001/edit-c.md:212`: `Func<byte[]?> GetCurrentCode` / **`Func<byte[]> GetNewCode`**, with
  caller-owned 16-byte buffers and only the unlocked-key current code nullable.
- `management-001/snippet-preparation.md:138–144`: the same delegates as typed method parameters,
  explicitly without no-op acquisition implementations or artificial hardware initialization.

Those documents are now fixed profile/evidence participants. The corrected wrapper matches them. The
body still declares both nullable cleanup locals, acquires inside try, awaits the genuine
`DeviceConfiguration.SetLockCodeAsync` and conditionally clears both arrays in finally. No null-forgiving
operator, new guard, warning suppression, fake provider or altered operation was added.

The observed diagnostic set contains **3 CS0104, 1 CS1061, 10 CS0103, 2 CS0246, 4 CS4033 and 1 CS8604**.
All 21 match generated-context omissions/conflicts above. No accepted-body defect was found in that
emitted set; a later retry can expose other errors and still owns actual C# binding.

## Fresh read-only checks and exact preservation

Inspected the verifier's imports/entry points and finite lexical checks, then ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**. It checks all **300 source preparations**, prior bounded fixes, current source/authority/body
regions, genuine contexts, original pilot proof and current import/context fault families. It reparses
the retained third-attempt log and matches all 21 path/location/code/message identities, diagnostic lines
in the exact old source snapshot and evidence excerpts against whole-file hashes.

The all-source checks are honestly **finite lexical regressions**, not a universal parser, free-name
resolver, nullability proof or compiler pass. They distinguish actual statement wrappers from original
member/test scopes and detect the watched prerequisite/async/PreviewSign/import families without
fabricating SDK behavior. Historical import-correction results remain historical; the current guard
rechecks current imports instead of rewriting the old oracle to pretend the candidate never changed.

Independent profile/JSON/diff checks also confirmed:

- Every **1,732 current fixed row** matches its length/hash. Source membership (1,330 paths), 25-project
  closure, accepted product map, all ten contexts and host restore configuration are unchanged.
- Exactly **11 consumer source prefixes/signatures** change. Independently reproduced all 11 exact
  unified diffs and source hashes. Original accepted directive bytes/order, every non-prefix byte,
  resource scope and API call remain exact. All **56 test sources** and all other consumer sources
  remain byte-identical to their 300-source predecessor snapshot.
- All **662 authority records** retain accepted raw/decoded bodies, coordinates, document/authority/
  review identities and dispositions; test-context metadata is unchanged. Consumer context changes are
  restricted to generated prefix/mode/typed-parameter/context-key metadata and generated source hashes.
- The profile delta changes 14 existing participants (11 generated sources, contexts.py and two
  verifier selections/wiring), changes current manifest selection, and adds explicit proof/owner inputs.
  The ledger's **1,708 unchanged active roles** exclude the old manifest retained as evidence; raw path
  comparison has **1,709 identical rows**, nine new paths and no physically removed path. This role/
  physical-path distinction is the same preserved-history convention as the predecessor, not input drift.
- All **549 immutable failed-attempt records** and the recorded predecessors/evidence match their
  preservation hashes. Consumer-first, second and third original receipts remain failed/nonzero;
  none has an accepted-compilation record. Profile `72753dce…`, prior reviews and historical sources
  remain exact. No old failure is relabelled, and no existing checker/observer/parser/logger source or
  workspace is changed by this correction.

No added NoWarn, warning disablement, null-forgiving workaround, invented SDK/helper implementation or
accepted-body fix was found. No broader audit or new compilation framework is requested.

## Exact coordinator retry

Keep the reserved primary stable and serialize the cycle. With exclusive ownership and the current
build skill loaded:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=3e40cb440ddd872de8bfba3ae3c0e45166ff5dbcb3a44aad890b968c97885228
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-fourth \
  --profile "$F/reviewed-inputs-contexts-fixed.json" --profile-sha256 "$P"
```

Require the actual complete zero-warning/error canonical build RESULT, original receipt,
Csc/input/output guards and immediate preservation proof before accepting consumer compilation or
continuing serially to the nine existing test contexts. Preserve the first failure under its own label;
return any genuine body error with exact source/body ID/literal diagnostic to the coordinator/editor.
Do not silently edit the accepted body in its host.

Never execute example or injected test assemblies. The final normal uninjected clean solution build
must restore ordinary test outputs before any SDK test lane, including after failed/interrupted
injection attempts. Existing output ownership, historical-copy and pre/post input guarantees stand.

## Reviewed identities and limits

Paths below are relative to `validation/examples/full/`.

| Record | SHA-256 |
| --- | --- |
| `manifest-contexts-fixed.json` | `3041b8961276182c4a6d203bb38f2fdd56fabeeba32c862d78e2d2f6045a9fbe` |
| `contexts.py` | `1283512d95ea473e0ccbc9fdb51a8eacf8c9bf86173be34f7f40fb2230ef1890` |
| `compiler-context-correction/context-evidence.json` | `66813bb8a4e1563ca2421e690fdbe9914206d639763ad78ca0bbf2ee643ea77d` |
| `compiler-context-correction/diagnostic-triage.json` | `a4b29c7437b7edf0c5f607967bef42e70b284626d19f0faf8ba8c09497a0d0fd` |
| `compiler-context-correction/generated-contexts.delta.json` | `10c2b9400bf98d65d03442b52922a2ac30405dbb1d1ebd85dd7d9341443e6bb2` |
| `compiler-context-correction/preservation.json` | `b4922e51f684b0c31a8b70f4b1e8f332435bf14a61d8b5f5c18bcfd751e57c9b` |
| `compiler-context-correction/verification.json` | `e9c6e1dbc0d33be6f260d3891e7a4f4496735160d28294dcf8b0bb34cdc25546` |

Only this review is written. No SDK build/test, actual capture/replay, compiled checker/runtime,
native/device/crypto operation, formatter, product edit, Git mutation or subagent ran. Queued WA/PGP/B2
corrections remain isolated in their owning worktrees. Finish authorized Core Pass-1 gates/publication
and deferred handoff, then stop; platform/physical lanes retain their existing bounds.
