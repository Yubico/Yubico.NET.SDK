# Independent storage relocation and logger v2 review

**PASS — no required changes.** Fresh independent GPT-6.1 Sol review, November 11, 2026.
Astra may resume the separately authorized integration gates using the relocated preservation adapter
and reviewed v2 logger. This is tooling/storage acceptance only, not a successful SDK gate, consumer
compilation, Linux qualification, publication, physical-device acceptance or whole-Core audit.

## Scope and independence

Read the active checkout's root instructions, `coordination.md`, original source acceptance in
`review.md`, recovery handoff/transfer/diagnostic/provenance records, actual partial failure captures,
original and adapted preservation/logger sources, qualification source and retained second-run
receipts, Linux evidence-gate source and adapter, preflight and record checker. The reviewer did not
author the recovery or logger. Wrote only this review. No SDK/product edits, index/ref/worktree changes,
build, logger qualification rerun, native execution, device access, formatter, commit, push or subagent.
Read-only Python ran with `-B`; no process signals were sent.

## Relocation and authoritative preservation

- Fresh `python3 -B recovery/verify-recovery.py` (paths here relative to the integration packet) exited
  **0** before subsequent coordinated mutable-record changes. It verified all **10,558** original
  entries in both copies, including bytes, modes and symlink targets; the inactive OLD copy remains
  unchanged. Independently compared the complete before/copied inventory objects as well.
- Independently compared the captured private-admin before/after entry sets and every current entry:
  **only `gitdir` differs**. The complete private-admin backup was verified at
  `/home/rupicola/Code/y/.core-docs-audit-runtime/storage-recovery-first/private-admin-backup`.
  Index SHA-256 remains `bec87214c74b7d3ac356cf973521837aa269c521efffb85a7ec193e5c6f34251`;
  HEAD `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`, MERGE_HEAD
  `5746e72ed9a848d08b5782ddde025aa4a4b522ef`, empty unmerged index, and all **7,196** product index
  entries remain bound to accepted tree `899dade50586c55efad0031e0a001498a11f5a26`.
- Primary branch/HEAD, all four original user files and every original other-worktree backlink remain
  unchanged. Fresh verification additionally observed `fixture-worktree5`, `yubikit-windows-async`,
  `yubikit-windows-w1-contract`, `yubikit-windows-w1-native`, `yubikit-windows-w1-owner` and
  `yubikit-windows-w3-owner` registrations outside the original population. These are additions, not
  permission to alter other registrations; no such registration was modified by this review.
- Fresh `python3 -B validation/check-preserved-relocated.py` exited **0**: eight manifests, **5,366**
  frozen artifacts and **114** prior PIV integration files in each active/primary checkout; four user
  files and campaign HEAD in primary. The integration report names the **NEW active home checkout**,
  not OLD. Source anchors: `validation/check-preserved-relocated.py:21–45` pins the original algorithm
  and relocation receipt, requires the exact new root and its registered backlink, performs one exact
  routing substitution and checks the reported root. The imported original `check-preserved.py:31–77`
  still pins before-receipt SHA-256
  `8f45a2c984ab0d741152378d5fcadb4f785a478fca9b47f4ba384139cf7558c9` and retains every original
  denominator/hash check. Its optional historical-binary verifier still fails on missing/changed bytes.
- Transfer source `recovery/transfer.py.snapshot:120–163` uses full copied identity verification before
  an fsynced atomic replacement of this worktree's own backlink. The recorded cross-filesystem
  `rename()`/EXDEV reason supports that bounded manual relocation, not a broad repair or deletion.

## Original failure and evidence integrity

Independently compared every retained original failed-toolchain/logger file with its inactive OLD
source. Actual clean output is **20,039 bytes**; build output is **4,096 bytes**.
`recovery/failed-actual-toolchain/02-build.log:15–18` records MSB3026 retries with **Disk quota exceeded**.
The logger's zero-byte receipt and zero-byte retained clean copy remain zero-byte evidence, not repaired
results. The actual clean step is not the build gate. No completed actual `result.*` exists in that
failed run; no original exception stack was reconstructed.

`recovery/environment-diagnosis.json:2–11` records UID 1000 using **13,316,681,728 bytes against the
same hard limit**, zero remaining UID block allowance. Mount-wide free space is not UID headroom.
The recorded home-backed filesystem/storage choice and untouched shared cache are consistent with
this diagnosis. This review did not rerun quota syscalls or change quotas/configuration.

## Logger v2 semantic acceptance

- `validation/run-command-v2.py:175–204` retains canonical command/expectation admission, explicitly
  declares home TMPDIR/TMP/TEMP for runner and child, takes the cooperative lock and publishes a durable
  started journal before spawning. Admission rejects an earlier missing/nonquiescent final journal.
- `:67–122,235–284` establishes a new session/group, captures leader start identity, uses subreaper
  adoption, reconciles and reaps owned descendants, bounds TERM/KILL and drain, and requires the
  session/start-time ownership bound before signalling that group. Escapes are blockers, not authority
  to kill other groups. There is no process-name/global build-server kill. EOF is separately tracked
  from locally closing descriptors; stream write/finalization failures cannot produce success.
- `:50–64,125–158,285–338` preserves failed partial files and existing receipts, retains actual run
  text before accepting RESULTs, and handles primary receipt failure with a failed external final
  receipt. A missing durable final record leaves admission blocked even after lock release. A failed
  but proven-quiescent command may release ownership without claiming command success. Process
  ownership uncertainty remains blocked rather than silently permitting another job.
- Original v1 source is hash-pinned; its strict parser still checks command exit/status, zero warnings,
  target outcomes, exact unique target/project membership, nonzero execution and failed-test guards.
  V2 additionally binds actual run/command identity, retained step logs and RESULT text/JSON counts.

Reviewed qualification is **retained evidence, not newly executed tests**. Independently matched current
source identities, archived/external final receipt bytes and archived stream hashes for all four second
qualification process cases, plus the current positive capture. The orphan receipt
`recovery/logger-v2-qualification-second/records/orphan/receipt.json:33–84` records the actual owned
descendant, TERM then KILL, empty members/escapes and stream completion. Retention and primary-receipt
fault final records explicitly remain failed (`records/journals/{retention,receipt}/final.json:29–51`).
`validation/qualify-logger-v2.py:54–80` asserts exact exit/output behavior, external disposition,
owned-group signals and disappearance of the orphan's `/proc` entry, rather than merely restating
implementation calculations. `:85–132` checks immutable receipt/failed-fsync partial behavior and six
deliberate parser mutations. The **4,124** parser-replay executions are historical PIV counts only.
These are meaningful bounded regressions, not current SDK executions or universal process-supervision
proof. No further logger selftests were run by this reviewer.

## Linux evidence adapter and record tools

Fresh `python3 -B validation/linux-evidence-gates-home.py --check-source` exited **0**. Independently
parsed the original test source and constructed an AST edit limited to the five `TemporaryDirectory`
`dir` keyword values; it equals the adapter-transformed AST. Source/hash binding and execution
namespace/argv/import routing retain the original tests and `unittest.main` discovery. All real runtime
record checks, intended-negative distinctions, stale publish/artifact/source rejection, actual evaluated
import/dependency binding and capture provenance cases remain unchanged. The adapter does not replace
them with a synthetic success or weaken any assertion. This was static verification only; the full
evidence-gate run still requires the separate sealed publish/native evidence transaction.

Independently verified **150** recovery-manifest file entries plus **five** validation-tool/attribute
entries against current bytes. Read-only use of `check-records.py`'s inspection functions found **421**
packet records, with **zero** raw/Git-clean normalization changes, before adding this review. Recovery's
151 current files at that point comprise the 150 listed files plus the manifest itself. Historical
`record-preflight.json`'s 416-record count is not a final current/staged claim. Final population remains
coordinator-owned after all writers; this review supplies no staged-byte assertion.

## Exact reviewed identities

All values below are freshly computed/verified SHA-256:

| File | SHA-256 |
| --- | --- |
| `validation/run-command-v2.py` | `48097a3593e3b71cac83bcc76c6e639608635ece05be2a579f7dc7b0dda3fd15` |
| `validation/qualify-logger-v2.py` | `60040169cb713d1da60d9486ba7af820622af7980371455350807b3e932ab46a` |
| `validation/check-preserved-relocated.py` | `56fcd7721f1e72016100d77c2e05c96eded80a6ab5abdbf144d59362e720b4ba` |
| `validation/linux-evidence-gates-home.py` | `79be7c80e83c6c3c122aa59a0c0bbad02df177f40a10245408dff9472fc8eaf5` |
| `validation/preflight.py` | `d2e5b40fc2890bed032cddcac81bea8edd15c8c5f442e77c195c58bd9b9358e3` |
| `validation/check-records.py` | `e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4` |
| `recovery/verify-recovery.py` | `4161b0fa37612b1ea5ea2f6337d4b32cc254c175d6bfb2fcd7543bbd79f09887` |
| `recovery/relocation-receipt.json` | `0da550fd99ca719dc1de1c91ee32bf5569e824cc27905d9f606830fea74e791c` |
| Original independent source `review.md` | `a39efdd36e08261d571457f42b1144f2522542df65dc40612f3b96348197a36d` |

An initial reviewer-only reconciliation command assumed the positive receipt lived directly under
`logger-v2-positive-second/` and failed with FileNotFoundError. The actual path is
`logger-v2-positive-second/positive/receipt.json`; the corrected full read-only reconciliation exited
0. This was a reviewer path error, not a logger failure; no retained evidence was changed.

## Caller consistency and remaining boundaries

**No caller-command correction is required before gate resumption.** In particular,
`validation/preflight.py:62–70` only prints JSON; it has no `--output` option and does not overwrite the
original `preflight.json`. The bare handoff command is safe. To retain fresh availability output, Astra
can instead wrap it with a fresh logger label, for example:

```bash
python3 -B "$V/run-command-v2.py" --label availability-home-first -- python3 -B "$V/preflight.py"
```

Do not redirect over historical preflight evidence or invent an unsupported `--output` argument.
`recovery/verify-recovery.py` is intentionally an **initial checkpoint**: it compares every original
active copied file and the original index. Later authorized coordination edits, consumer edits to
original files, build output or staging can invalidate that checkpoint by design. Do not run it as a
post-consumer/final gate or normalize OLD to make it pass. Consumer additions outside the original
population alone do not invalidate its original-entry comparison. Use relocated preservation for fresh
protected-byte checks, and final record snapshot/check/staged verification after all packet writers.

The handoff's paused-consumer statement describes its historical checkpoint; the coordinator reports
that the disjoint consumer preparer has since resumed without SDK build permission. This review grants
no additional build or artifact permission to that owner. Astra retains sequencing, current consumer
compiler/provenance proof, fresh SDK and Linux gates, exact staging/publication, and whole-Core audit
dispatch. The user's physical PIV deferral remains intact.
