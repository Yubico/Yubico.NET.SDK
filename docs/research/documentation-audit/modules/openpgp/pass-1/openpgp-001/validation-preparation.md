# Validation tooling preparation

## Ready for coordinator execution

Sol prepared these sources on 2026-11-04, independently of module investigation. Ownership covers
this record and `validation/`. Preparation follows root instructions, the run plan, campaign workflow,
approved style and SDK house style. Module investigation, product edits, builds, tests, formatting,
hardware, commits and delegation remain with their assigned owners.

[`prepare-tools.py`](validation/prepare-tools.py) copies the final frozen YubiHSM checker, four
utilities and static verifier after verifying its manifest and each selected frozen source row.
The source manifest SHA-256 is
`9a32ac8f9d2443a65864dbe44432c6d4b1dd40ad7f00b676a6c0ecae950631dc` (477 entries).
Existing identical targets are reused; changed targets fail closed. Complete source/target paths,
SHA-256 hashes, literal substitutions and counts are retained in
[`checker-provenance.json`](validation/checker-provenance.json),
[`utility-provenance.json`](validation/utility-provenance.json) and
[`verifier-provenance.json`](validation/verifier-provenance.json). Utility provenance also pins both
current-run baseline records and the generator. Each provenance file describes newly prepared tools;
historical results and source bodies remain in their original archives.

Checker substitutions are exactly:

| Source | Target |
| --- | --- |
| `a74792facb657413e98223e059876d8b213ffff3` | `fe180282a2d77b15eaaa5868d6c8b00e251d1f05` |
| `modules/yubihsm/pass-1/yubihsm-001/` | `modules/openpgp/pass-1/openpgp-001/` |
| `/tmp/opencode/yubihsm-001-doccheck` | `/tmp/opencode/openpgp-001-doccheck` |
| `Yubico.YubiKit.AuditHsm001.Checker` | `Yubico.YubiKit.AuditPgp001.Checker` |

The Roslyn token/directive/API algorithms remain unchanged. Strict token mode compares every changed
executable C# file against the baseline, normalizing only directive-free using-list order. Missing
baseline files and empty changed sets fail closed. API mode compares the complete snapshot path set
and bytes. These broad mechanical gates preserve the diff boundary without adding module investigation.

Import-removal approvals start absent. The inherited `tokens-import-removals` algorithm accepts exact
ordinary-import removals only after strict pre-format captures, actual scoped formatter/IDE0005
evidence, hashes and a matching post-format transform. The coordinator adjudicates actual removals
after editing; preparation grants no standing unused-import exemption.

## Exact coordinator commands

Run from the primary checkout, serializing toolchain and device operations. Use a fresh lowercase
evidence name for every attempt. The log wrapper is ready for coordinator tests immediately.

```bash
RUN=docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001
ROOT=/home/rupicola/Code/y/Yubico.Yubikit.NET
CHECKER=/tmp/opencode/openpgp-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.AuditPgp001.Checker.dll

python3 "$RUN/validation/run-command.py" initial-checker-build dotnet toolchain.cs -- build --project AuditPgp001.Checker --clean
python3 "$RUN/validation/run-command.py" unit-1 dotnet toolchain.cs -- test --project OpenPgp
python3 "$RUN/validation/run-command.py" final-tokens dotnet "$CHECKER" tokens "$ROOT"
python3 "$RUN/validation/run-command.py" final-api dotnet "$CHECKER" api "$ROOT"
python3 "$RUN/validation/run-command.py" final-preservation python3 "$RUN/validation/check-preserved.py"
python3 "$RUN/validation/run-command.py" final-record-check python3 "$RUN/validation/check-records.py" --reviewed
```

The static verifier recursively discovers project **filenames** and requires exactly one
`AuditPgp001.Checker` match, including nested archive paths. The coordinator confirms the actual build
selects it and reports zero warnings/errors. Checker outputs use
`/tmp/opencode/openpgp-001-doccheck/{bin,obj}/`.

`run-command.py` exclusively creates the full combined-output log with the actual command and exit
status, including launch errors. Immediately after each command, it retains UTF-8 toolchain evidence
byte-for-byte before five-run rotation. Every discovered artifact receives a path/size/hash and
retention/exclusion decision in `retention-index.json`. Binaries and caches are indexed rather than
copied; symlinked evidence fails closed. Command authority comes from the coordinator's assignment.

`check-preserved.py` verifies all six frozen manifest hashes and their 784/402/534/164/433/477 entries
(2,794 prior artifacts), then the four original user files by exact path/size/hash. It reads these
files without modifying them.

`check-records.py` uses the final YubiHSM schema: `reviewed-files.json` contains an object with a
`files` array; each row supplies repository-relative `path` and `final_sha256`. The final record also
supports `baseline`, `reviewed_sha256` and `scope` metadata. Generate current review identities after
editor/reviewer completion. The checker verifies distinct final hashes with `--reviewed`; its ordinary
mode checks top-level run Markdown links/fences and retained text/PDF identities. Additional campaign
handoffs use repeated `--markdown` arguments. External local targets receive existence checks only;
complex Markdown and external URLs retain separate review requirements.

## Later example and hardware preparation

The inherited `compile` subcommand retains its historical Core/CTAP defaults and CTAP label. Complete
documentation groups and the entire 19-source OpenPgpTool receive a separate preparation assignment
after editing. Future hosts use unique `AuditPgp001.*` project filenames and
`/tmp/opencode/openpgp-001-*` outputs. Discover each selector recursively before execution.

Use final YubiHSM capture/verification tooling and its post-capture lessons for that assignment:
retain actual Csc sources, references, analyzers, configuration and compiler evidence immediately
after real compilation, including late capture-hook identity and restore assets. Select exact
context/attempt identities for each active group and the complete native shipping tool. Keep ordinary
sample bin/obj behavior. Derive new preparation and result schemas from final tools and final captures;
the initial failed schema and old result rows remain historical evidence. This assignment copies no
example bodies, compiler captures, hardware probes, binaries or caches. Future hardware-probe
preparation is separate from both tooling readiness and example compilation.

## Static evidence and freeze boundary

`python3 "$RUN/validation/verify-preparation.py"` performs read-only Python syntax, XML and provenance
checks, recursive project-filename uniqueness, baseline/generator hashes and source-only inspection.
[`preparation-static.log`](validation/preparation-static.log) records this preparation check.
[`verifier-static.log`](validation/verifier-static.log) separately reconciles the copied verifier's
frozen-source row, hashes and exact substitutions. These establish tooling integrity; actual .NET
compilation, token/API checks, archive/user-file preservation, record acceptance, examples and
repository/device gates remain coordinator work.

[`primary-binary-sources.json`](validation/primary-binary-sources.json) starts as `[]`. After source
adjudication, a retained primary PDF requires its run-relative `path`, exact `bytes` and `sha256`.
Record and manifest tools check the `%PDF-` signature and registered identity; other artifacts remain
UTF-8 text. Generated binaries/caches and symlinks are rejected.

Freeze after final review and the final in-run log write:

```bash
python3 "$RUN/validation/manifest.py" write
python3 "$RUN/validation/manifest.py" verify
```

The manifest exclusively creates the run identity/baseline and exact path/size/hash/kind set for every
artifact except itself. Capture freeze/verify output externally, for example
`/tmp/opencode/openpgp-001-manifest.log`; an in-run wrapper would change its own log during freeze.
The archive remains open for later evidence.
