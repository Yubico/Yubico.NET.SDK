# Validation tooling preparation

## Scope and readiness

Prepared by Sol on 2026-11-04, independently of module investigation. This is a tooling
handoff, not validation of YubiOTP documentation or behavior. Ownership is limited to this
record and `validation/`. No module source/docs, investigator output or Core content was
read. No builds, tests, hardware operations, formatting, commits or agent dispatch occurred.

The checker sources are generated from the frozen Security Domain run with only four
explicit substitutions: published baseline, run-evidence path, temporary output path and
checker assembly identity. `validation/checker-provenance.json` retains exact source/target
SHA-256 hashes and replacement counts; `validation/prepare-tools.py` reproduces the copies
and refuses changed existing targets. The checker project and assembly are uniquely named
`Yubico.YubiKit.AuditOtp001.Checker`; intermediates and binaries default to
`/tmp/opencode/yubiotp-001-doccheck/{obj,bin}/`, outside the retained archive.

The strict token/directive algorithm is unchanged. It compares all changed executable
shipping/test/example C# against `95e5ccaeda33954c21f2a38058d4f66b5f208787`, normalizing only
directive-free using-list order. Missing baseline files, deleted files and vacuous source
sets fail closed. API mode compares every tracked/untracked public API snapshot path and
byte content against that same baseline; its broad mechanical comparison is not a Core audit.

No `import-removals.json`, source captures, formatter evidence or removal approval exists
in this preparation. The inherited separate `tokens-import-removals` algorithm remains
available unchanged, but is not a default gate or standing authorization. If a later scoped
formatter actually removes imports, retain strict pre-format success/full captures and actual
IDE0005/command/transform evidence; obtain explicit adjudication before using that mode.
Its required schema and conservative rejection rules remain in `checker/src/Program.cs`.

The inherited `compile` subcommand still has historical Core/CTAP reference defaults and a
CTAP compilation label. It is intentionally not used as OTP example evidence. No example
extractor, example host, example body or snippet classification was copied. Prepare those
only after editing, using complete final bodies, explicit prerequisites and real consumer,
OtpTool or test reference/analyzer contexts. Their future projects must use unique
`AuditOtp001.*` names and `/tmp/opencode/yubiotp-001-*` outputs. Retain originals, wrappers,
Csc membership/provenance, dependency identities and raw failures; compile-only is not execution.

## Exact coordinator commands

Run from `/home/rupicola/Code/y/Yubico.Yubikit.NET`, serially with every other toolchain or
device operation. These commands are supplied for the coordinator and were **not executed**
during preparation. Choose a new evidence name rather than overwriting any earlier attempt.

```bash
RUN=docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
CHECKER=/tmp/opencode/yubiotp-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.AuditOtp001.Checker.dll

python3 "$RUN/validation/run-command.py" initial-checker-build dotnet toolchain.cs -- build --project AuditOtp001.Checker --clean
python3 "$RUN/validation/run-command.py" final-tokens dotnet "$CHECKER" tokens "$ROOT"
python3 "$RUN/validation/run-command.py" final-api dotnet "$CHECKER" api "$ROOT"
python3 "$RUN/validation/run-command.py" final-preservation python3 "$RUN/validation/check-preserved.py"
python3 "$RUN/validation/run-command.py" final-record-check python3 "$RUN/validation/check-records.py" --reviewed
git diff --check
```

The build command follows the frozen Security Domain run's successful toolchain invocation,
substituting only the unique selector. It must build exactly the intended checker and report
zero warnings/errors. The wrapper retains the actual RESULT block, step logs and result JSON
before five-run rotation. It stores unique command logs including nonzero exits/launch errors,
copies UTF-8 text only, and records the source path, size/hash and exclusion reason for skipped
binary/cache artifacts in each copied run's `retention-index.json`. It does not authorize or
sanitize arbitrary commands. Build logs may reference temporary outputs without archiving them.

`check-preserved.py` uses the new run's `prior-archive-baseline.json` and
`user-work-baseline.json`, not Security Domain's user snapshot. It first verifies each old
manifest's baseline SHA-256 and expected count (784/402/534/164), then every listed file's
size/hash, and then all four original user files. Prior archives are never modified. The
new baseline files' own hashes are recorded in `validation/utility-provenance.json`.

`check-records.py` checks only top-level run Markdown by default, with local target existence,
balanced fences and text-only/symlink/cache checks over retained run artifacts. It does not
open other-module link targets. `--reviewed` requires a nonempty `reviewed-files.json` with
distinct repository-relative `path` / `final_sha256` rows under `files`; use it only after final
independent review. Without that option the script explicitly reports the hash check not run.
Additional campaign record/handoff Markdown can be supplied with repeated `--markdown` paths.
Complex Markdown links require manual review; the bounded scanner is not a complete Markdown
parser, an anchor checker, an external URL check or a content-review verdict.

## Freeze after final review

Do not freeze while any record, review, evidence or log is still being written. The manifest
includes every run file except itself, including preparation sources and original failed
attempts. It rejects binary/cache artifacts and symlinks, and verifies exact path/size/hash/kind
sets and totals. It refuses to overwrite an existing manifest.

```bash
python3 "$RUN/validation/manifest.py" write
python3 "$RUN/validation/manifest.py" verify
```

Do **not** wrap these commands in `run-command.py` with an in-run log: a wrapper log still
changes after the child returns and would invalidate the frozen manifest. Capture terminal
output externally, for example below `/tmp/opencode/yubiotp-001-*`, or record it in the external
checkpoint. Reverification after staging/publication is read-only.

## Preparation verification boundary

Preparation checks are limited to Python syntax parsing without cache generation, XML parsing,
exact transformed-copy/provenance reconciliation and project-name path discovery. They are
not checker compilation, token/API validation, archive preservation verification, record
acceptance or example validation. Those remain coordinator-owned and pending. No final
manifest has been created; no binaries/cache were retained.

The reproducible static inspection command is
`python3 "$RUN/validation/verify-preparation.py"`; its retained output is
`validation/preparation-static.log`. It reads only validation tool sources, the two new
baseline records and project filenames. It does not execute the prepared validation gates.
