# Validation tooling preparation

## Prepared scope

Sol prepared the validation sources on 2026-11-04, independently of module investigation.
Ownership covers this record and `validation/`. The preparation uses root instructions, the
approved run plan, campaign workflow/style, current baseline JSON and frozen YubiOTP tooling.
Module source, module documentation and investigation records remain for their assigned owners.
Builds, tests, hardware, formatting, agent dispatch and commits remain coordinator-owned.

[`prepare-tools.py`](validation/prepare-tools.py) reproduces four checker files and four utilities
from the final YubiOTP archive. It verifies the source manifest against this run's baseline JSON
and each selected source against its frozen row before copying. Existing identical targets are
reused; changed targets fail closed. [`checker-provenance.json`](validation/checker-provenance.json)
and [`utility-provenance.json`](validation/utility-provenance.json) retain complete source/target
SHA-256 hashes, literal substitutions and counts. The utility record also pins both current-run
baseline JSON files and the generator.

The checker substitutions are the baseline
`95e5ccaeda33954c21f2a38058d4f66b5f208787` → `a74792facb657413e98223e059876d8b213ffff3`,
YubiOTP → YubiHSM run-evidence path, `/tmp/opencode/yubihsm-001-doccheck` output path and
`Yubico.YubiKit.AuditHsm001.Checker` project/assembly identity. The Roslyn token/directive/API
algorithms stay unchanged: strict mode compares all changed executable C# against the baseline,
normalizing directive-free using-list order, and rejects vacuous sets or missing baseline files.
API mode compares the complete API snapshot path set and bytes. These broad mechanical gates
preserve the diff boundary; their scope adds no Core investigation.

Import-removal approvals start absent. The inherited separate `tokens-import-removals` algorithm
retains its strict captures, hashes, IDE0005 provenance and ordinary-import rules. The coordinator
can adjudicate that mode after an actual scoped formatter transformation and retained strict
pre-format success. Preparation supplies neither exemptions nor standing removal authorization.

The inherited `compile` subcommand retains historical Core/CTAP defaults and its CTAP label.
Full documentation-group and 22-source HsmAuthTool compilation will receive a separate assignment
after editing, with final bodies, genuine reference/analyzer context and actual Csc provenance.
Future hosts use unique `AuditHsm001.*` identities and `/tmp/opencode/yubihsm-001-*` outputs.
This preparation retains only checker/tool sources, configuration, provenance and static evidence.
Old example bodies, example results, hardware probes, binaries and caches stay in their own runs.

## Exact coordinator commands

Run from the primary checkout, serially with every toolchain/device operation. These commands
are prepared for later execution. Each attempt receives a fresh evidence name.

```bash
RUN=docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
CHECKER=/tmp/opencode/yubihsm-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.AuditHsm001.Checker.dll

python3 "$RUN/validation/run-command.py" initial-checker-build dotnet toolchain.cs -- build --project AuditHsm001.Checker --clean
python3 "$RUN/validation/run-command.py" final-tokens dotnet "$CHECKER" tokens "$ROOT"
python3 "$RUN/validation/run-command.py" final-api dotnet "$CHECKER" api "$ROOT"
python3 "$RUN/validation/run-command.py" final-preservation python3 "$RUN/validation/check-preserved.py"
python3 "$RUN/validation/run-command.py" final-record-check python3 "$RUN/validation/check-records.py" --reviewed
```

The checker selector has exactly one recursively discovered project-filename match in the static
inspection. The coordinator confirms the actual build selects that project and reports zero
warnings/errors. Outputs default to `/tmp/opencode/yubihsm-001-doccheck/{obj,bin}/`.

`run-command.py` retains the exact command, combined output and exit status, including launch
errors. It copies UTF-8 toolchain evidence byte-for-byte before five-run rotation, records every
discovered artifact's path/size/hash and any exclusion reason in `retention-index.json`, and uses
exclusive creation for logs, run directories and retained files. Symlinked evidence fails closed.
Binary/cache artifacts receive index entries rather than archive copies. The wrapper receives
execution authority from the coordinator's assignment, independently of its retention function.

`check-preserved.py` reads this run's baseline JSON, verifies the five frozen manifest hashes and
784/402/534/164/433 recorded identities (2,317 total), then checks all four original user files by
path/size/hash. It reads prior archives and user files without modifying them.

`check-records.py` checks top-level run Markdown links/fences and retained text/PDF identities.
External local link targets receive existence checks only. `--reviewed` verifies the distinct
repository-relative `path` / `final_sha256` rows in `reviewed-files.json` under `files`; use it after
independent review. Additional campaign Markdown can be supplied through repeated `--markdown`
arguments. Complex Markdown syntax and external URLs retain their separate review requirements.

## Freeze and verification boundary

[`primary-binary-sources.json`](validation/primary-binary-sources.json) starts as `[]`. To retain a
future primary PDF, register its exact run-relative `path`, byte count (`bytes`) and SHA-256
(`sha256`) after source adjudication. Both record and manifest checkers require the `%PDF-`
signature and matching identity for every registered PDF. All other retained files must be text;
generated binaries/caches and symlinks are rejected.

Freeze after final review and the last in-run log write:

```bash
python3 "$RUN/validation/manifest.py" write
python3 "$RUN/validation/manifest.py" verify
```

The manifest includes every run artifact except itself and verifies the exact path/size/hash/kind
set, totals, baseline and run identity. Exclusive creation preserves an existing manifest.
Capture freeze/verify output externally, such as `/tmp/opencode/yubihsm-001-manifest.log`;
an in-run wrapper log would keep changing while its child freezes the archive.

Static inspection is reproducible with `python3 "$RUN/validation/verify-preparation.py"`.
[`preparation-static.log`](validation/preparation-static.log) records Python syntax, XML parsing,
frozen-source/provenance reconciliation, recursive checker-name uniqueness, the empty initial PDF
allowlist and source-only preparation. These checks read tooling, baseline records, the frozen
source manifest and project filenames. They establish preparation integrity; .NET compilation,
tokens/API, archive/user-file preservation gates, record acceptance, example compilation and all
repository/device gates remain pending. The archive remains open for later evidence.
