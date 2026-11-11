# Home-backed storage recovery and validation handoff

## Active location and phase

Astra authorized this scoped environment recovery after the first main integration gate failed on
November 11, 2026. Sol copied and verified the complete integration checkout, then atomically changed
only its private administrative `gitdir` backlink. The active checkout is:

`/home/rupicola/Code/y/yubikit-core-docs-integration`

The original `/tmp/opencode/core-upstream-20261111` remains an **inactive, unchanged evidence copy**.
It still contains the original `.git` pointer; treat it as a frozen filesystem copy and keep all
commands that can write Git or working files in the new checkout. Recovery removed no original file,
freed no tmpfs space and changed no quota, service, package installation or system configuration.

Scratch, temporary files, process journals and private-admin backup remain outside versioned records:

`/home/rupicola/Code/y/.core-docs-audit-runtime`

The pending merge remains HEAD `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`, MERGE_HEAD
`5746e72ed9a848d08b5782ddde025aa4a4b522ef`, with an empty unmerged index. Its index SHA-256 remains
`bec87214c74b7d3ac356cf973521837aa269c521efffb85a7ec193e5c6f34251`. The 7,196 staged product
entries still match independently accepted tree `899dade50586c55efad0031e0a001498a11f5a26` exactly.
The source acceptance remains bounded to the independent `review.md`; this recovery adds no SDK review.

## Root cause and original failure record

The `/tmp` tmpfs has user quotas. Read-only `quotactl_fd(Q_GETQUOTA, USRQUOTA, 1000)` established
**13,316,681,728 bytes used against the same hard limit: zero UID block headroom**. Its UID inode
hard limit is unlimited. Mount-wide free blocks and inodes therefore did not make further allocation
available to this UID. `/home` is a different Btrfs filesystem with ample reported space. The .NET
file-app cache already lives on `/home`; recovery leaves that shared cache alone.

The original actual toolchain directory is retained under `failed-actual-toolchain/`:

| Original file | Captured bytes | Interpretation |
| --- | ---: | --- |
| `01-clean.log` | 20,039 | Clean completed with zero warnings/errors. This is a clean step, not the build gate. |
| `02-build.log` | 4,096 | Partial build output records MSB3026 copy retries with “Disk quota exceeded”. |
| `run.log` | 1,607 | Partial toolchain chronology ending after build startup. |

The separate `failed-logger-attempt/` preserves the invocation and failed retention exactly, including
the **zero-byte receipt and copied clean log**. Neither source contains a completed RESULT. The
coordinator-reported Python errno 122 is recorded as reported; the original first exception stack is
unavailable and has not been reconstructed. Main build/unit/docs/architecture/complexity/resilience/
pack acceptance remains pending. Native and device lanes had not started.

Original build PID 2898163 and toolchain PID 2897864 had exited before recovery. The pre-transfer
process/descriptor check found no live .NET/logger writer for this checkout. `active-run.txt` still
names exited PID 2897864; its original bytes remain in both checkout copies and this packet. A stale
marker is historical evidence, rather than proof of a current job or lease. Recovery sent signals
only to the deliberately spawned Python qualification descendant group. Shared MSBuild/compiler
servers and unrelated jobs were left alone.

## Transfer proof and private Git state

`relocation-receipt.json` has SHA-256
`0da550fd99ca719dc1de1c91ee32bf5569e824cc27905d9f606830fea74e791c`.
It binds the original/new roots, private Git directory, backup location and parent/index identities.
`checkout-before.json` and `checkout-copied.json` compare all **10,558** relative entries, including
file bytes/hashes, regular/directory modes and symlink targets, before any new tooling or records were
added. The inactive original is rechecked against that full population.

The private directory remains at
`/home/rupicola/Code/y/Yubico.Yubikit.NET/.git/worktrees/core-upstream-20261111`.
Its complete pre-relocation backup is
`/home/rupicola/Code/y/.core-docs-audit-runtime/storage-recovery-first/private-admin-backup`.
`admin-before.json` maps every private-admin backup entry to its original relative path, byte hash and
mode without copying the binary index into versioned records. `admin-after.json` proves that only
`gitdir` changed; HEAD, index, MERGE_HEAD, MERGE_MSG, MERGE_MODE, AUTO_MERGE and ORIG_HEAD remain intact.

Git 2.56.0's versioned `move_worktree` implementation uses `rename()` directly; tmpfs and `/home`
have different device IDs. Recovery therefore used an authorized `cp -a`, full identity comparison,
and the documented manual backlink update instead of a cross-filesystem `git worktree move` or broad
repair. The published primary branch/HEAD, all four original user files and all original other-worktree
backlink hashes remain unchanged. A new unrelated `fixture-worktree5` registration appeared during
later verification; its hash is recorded separately and its files/registration were left untouched.

`relocation-verified-first/stdout.log` records a successful replay, including the accepted product-tree
binding. Two original verification failures remain under `verification-order-failure/` and
`verification-registration-failure/`: the first compared component-sorted Path objects with
string-sorted names despite identical unique membership; the second treated an unrelated newly added
registration as a changed original registration. Their exact checker sources and failures precede the
bounded corrections. Full node identities and every original backlink remain checked.

`verify-recovery.py` is an **initial recovery checkpoint**: it also binds original partial build-output
bytes and the unstaged-record index. A later authorized build or staging action intentionally changes
those dimensions. Preserve this checkpoint rather than relabeling it as a post-build check.

## Authoritative tooling after relocation

| Entry point | Current use |
| --- | --- |
| `validation/check-preserved-relocated.py` | Authoritative preservation: original before-receipt hash and algorithm, with one source-pinned in-memory root-routing transformation to the receipt's new checkout. It explicitly verifies that it checked the new root and its registered backlink. |
| `validation/run-command-v2.py` | Authoritative command capture after independent tooling review. Imports only the source-hash-bound v1 helpers, preserving strict target/project/count/warning checks. |
| `validation/linux-evidence-gates-home.py` | Minimal execution adapter for the incoming evidence-gate script's five explicit `/tmp/opencode` TemporaryDirectory arguments. Original source/assertions remain unchanged. |
| `validation/preflight.py` | Root-derived read-only availability/filename inventory. Save a fresh output under a new name; its original `preflight.json` remains historical. |
| `validation/check-records.py` | Root-derived packet/raw-normalized/index checker. Final snapshot and staging verification remain coordinator work after all packet writers finish. |

Original `check-preserved.py` still follows the before receipt's old integration root and remains
historical routing. Original `run-command.py`, preparation reports, capture files, manifests and all
frozen applet evidence retain their bytes. Use the relocated adapter and v2 runner for fresh gates;
presence of the old copy must not substitute for checking the active tree. `inspect-resolution.py`
writes existing resolver records despite its read-only description; avoid replaying it over preserved
captures. `prepare-examples.py` and its 25 candidate bodies remain with the independent consumer owner.

Relocated preservation passed: **eight manifest hashes, all 5,366 artifacts and 114 PIV integration
files in each active/primary checkout**, plus the primary's four original user files and unchanged
campaign HEAD. See `preservation-relocated-first/`. The adapter retains the exact original
`preservation-before.json` SHA-256 `8f45a2c984ab0d741152378d5fcadb4f785a478fca9b47f4ba384139cf7558c9`.
Its optional historical-binary check still requires every registered original external byte and fails
when a required binary is absent or changed.

### Logger v2 qualification and lease behavior

V2 starts one new Linux session/process group, records its PID/SID/start-time ownership and acts as a
process-local child subreaper. After direct-child completion or failure, it reconciles the proven owned
group, bounds TERM/KILL and pipe drain, reaps adopted descendants, and records the postcondition. It
signals only members of that original group whose session and start-time bound still match. A detected
escape outside the group produces a blocked record, rather than permission to signal unrelated groups.
The runner records actual EOF separately from closing a local descriptor.

Before process admission, an fsynced external started journal is published under the home runtime.
Primary receipts and external final receipts use new-file atomic publication. Failed writes leave
diagnosable partials and preserve existing receipts. A primary receipt failure falls back to the
external final record and returns failure. A missing or nonquiescent external final journal blocks the
next admission, even after the cooperative lock is released. Astra reviews such a blocker explicitly;
the runner does not silently clear it.

The current source qualification is `logger-v2-qualification-second/qualification.json`: nonzero
child output on both streams, a parent exiting while its owned child ignores TERM, injected retention
failure, injected primary receipt failure, immutable-receipt overwrite rejection and an injected
fsync failure. Every process case ends with no surviving group/adopted child. The orphan case records
TERM then KILL against only its proved group. Six strict historical RESULT rejection checks cover
exit, target, warning, project membership, zero execution and failed-test guards. The 4,124 historical
executions printed by that parser replay remain **old PIV result data**, not new SDK test executions.
The positive supplemental Python capture also passes at the current logger identity.

The first v2 sources and qualification remain preserved. A narrow second qualification made the
receipt's stream-completeness flag include actual pipe EOF/finalization, rather than only write-loop
errors. Original v1 source SHA-256 remains
`61bd7b3f218da8051c44ade033887ddbe598d9d9b2f95cc3fa7a8b8696838640`.

## Coordinator commands after fresh independent tooling review

Run from the new root. Load current `domain-build` and `domain-test` skills before SDK gates. Use new
attempt labels for corrections and retain the original failed attempt. Each invocation serializes its
own job, preserves actual run text before rotation and checks actual RESULTs rather than assumed targets.

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
export TMPDIR=/home/rupicola/Code/y/.core-docs-audit-runtime/tmp
export TMP="$TMPDIR"
export TEMP="$TMPDIR"
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
python3 -B "$V/check-preserved-relocated.py"
python3 -B "$V/preflight.py"

python3 -B "$V/run-command-v2.py" --label docs-patch-guards-home-first -- \
  python3 -B scripts/docs/test-docs-patches.py

python3 -B "$V/run-command-v2.py" --label main-gates-home-first --toolchain \
  --expect-target build --expect-target test --expect-target docs-qa \
  --expect-target docs-architecture --expect-target complexity --expect-target resilience \
  --expect-target pack --expect-projects-file "$V/expected-unit-projects.json" \
  --expect-project resilience:Core.UnitTests -- \
  dotnet toolchain.cs -- build test docs-qa docs-architecture complexity resilience pack \
  --clean --fast --complexity-args "--base f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa" \
  --package-version 2.0.0-alpha.2
```

V2 sets and records the dedicated TMPDIR/TMP/TEMP for its own process and every child. Root-relative
solution outputs now resolve on `/home`. Normal build/unit/resilience/pack membership remains the
13 canonical unit rows plus a distinct resilience Core row; fresh execution/skips and complexity
dispositions still require review. The expected-project file is unchanged. Real compiler/analyzer
membership and effective per-project policy remain the consumer-capture owner's proof task.

Separate Linux lane, after main-gate disposition and with no intervening clean:

```bash
python3 -B "$V/run-command-v2.py" --label linux-publish-home-first --toolchain \
  --expect-target linux-hid-verification-publish \
  --retain-text artifacts/linux-hid-verification/before-publish.json \
  --retain-text artifacts/linux-hid-verification/publish-manifest.json -- \
  dotnet toolchain.cs -- linux-hid-verification-publish

python3 -B "$V/run-command-v2.py" --label linux-native-home-first \
  --retain-text artifacts/linux-hid-verification/evidence -- \
  python3 -B verification/LinuxHidRouteVerification/capture.py --native-only

# Set CAPTURE to the exact Evidence directory emitted by that capture.
python3 -B "$V/run-command-v2.py" --label linux-evidence-gates-home-first -- \
  python3 -B "$V/linux-evidence-gates-home.py" "$CAPTURE"
```

Publish/capture already derive their artifacts and source root from the active checkout. The incoming
evidence tests explicitly prefer `/tmp/opencode` at five sites, overriding TMPDIR. The hash-bound home
adapter changes only those arguments in memory; its `--check-source` receipt verifies five exact sites
and syntax without executing tests. That static check is retained under `linux-home-adapter-static-first/`.
Retain the sealed publish binding and raw native evidence separately; storage recovery qualifies no
native behavior, platform interoperability or physical device operation.

### Consumer-owner handoff

The consumer preparer is paused and authored no files. The resolver's original 25 raw/generated
candidates remain unchanged. `validation/Directory.Build.props` still defaults
`IntegrationValidationWorkspace` to `/tmp/opencode/core-upstream-20261111-validation`; the independent
owner must supply a new home-backed derivative/output context before compilation. Preserve original
raw bodies and fix only wrapper-local naming collisions in migration groups 02/25 (`device`) and 22
(`int` versus nullable serial binding). These are preparation collisions, not proven product failures.
Rebind current source roots, exact SDK references, analyzers/policies, actual Csc sets and the complete
unique direct/transitive graph as already required. Recovery builds no consumer host.

### Record/staging handoff

After all writers, current consumer evidence and reviews finish:

```bash
python3 -B "$V/check-preserved-relocated.py"
python3 -B "$V/check-records.py" snapshot
python3 -B "$V/check-records.py" check
# Astra stages only exact reviewed paths, including both new byte-preserving attribute files.
python3 -B "$V/check-records.py" staged
```

`recovery/.gitattributes` and `validation/commands-v2/.gitattributes` preserve raw text bytes. Private
Git backups, compiler/runtime/test binaries and scratch directories stay external; versioned manifests
record identities only. Current record preflight is read-only and supplies no final staged-byte claim.
Independent review of relocation/v2 tooling precedes further gates. Integration publication and the
whole-Core semantic audit remain subsequent coordinator phases.
