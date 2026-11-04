# CTAP2 validation-artifact discovery isolation

**2026-11-04; metadata-only follow-up.** This isolates this run's validation projects from ordinary
module-scoped builds. No root toolchain, SDK source, executable example, test, probe C# or original
framework source snapshot was changed. No build, restore, test, probe execution, hardware, commit
or delegation occurred. The revised helper/probe have not been compiled by this follow-up.

The coordinator reports that the original `Ctap2.DocCheck` helper build passed with zero warnings at
`20261104-002723-607`. That result belongs to the original project filename, not the renamed project.
The original project bytes are retained at
`validation/checker-source/original/Yubico.YubiKit.Ctap2.DocCheck.csproj.snapshot` (SHA-256
`58aadd80552103e2dec45899c64cb8fd47ab863204de66d8c4163a4b4ec5574b`, 445 bytes).

## Actual discovery logic and collision

Source inspected directly, without invoking the toolchain:

- `scripts/toolchain/Context.cs:45–47`: build candidates use
  `DiscoverProjects(repoRoot, "src", Conventions.ProjectPrefix)`; tests separately use `/tests/`
  and `.UnitTests`/`.IntegrationTests` with that prefix.
- `scripts/toolchain/Discovery.cs:10–18`: recursively glob **all `*.csproj`** under the repository,
  require `/src/` anywhere in the full path and the `Yubico.YubiKit.` prefix anywhere in that path,
  and exclude `.claude`. There is no `docs/research` build-project exclusion.
- `Discovery.cs:98–104`: `MatchProjects` checks
  `Path.GetFileNameWithoutExtension(p).Contains(filter, StringComparison.OrdinalIgnoreCase)`.
  Parent directory names, assembly names and output DLL names are **not** consulted by this filter.
- `BuildTargets.cs:82–90`: a scoped build loops over **every** matching candidate.
- `Discovery.cs:20–26`/`Context.cs:72–74`: `IsPackable=false` removes packaging candidates, **not**
  build candidates. `BuildTargets.cs:72–79` builds the solution when there is no project filter;
  discovery eligibility alone does not add these artifacts to the solution.

Therefore the earlier helper and framework probe basenames match ordinary `--project Ctap2`, despite
being nonpackable and not runner-discoverable. The former generated-context name would match too and
could run a frozen context's input-hash guards during an unrelated module build. This is an artifact
selection problem, not SDK behavior or analyzer policy. Only this run's filenames/generator metadata
were corrected; root discovery logic remains unchanged.

## Old → current names

Paths are relative to this run:

| Role | Original project basename | Current runnable project |
| --- | --- | --- |
| Checker | `Yubico.YubiKit.Ctap2.DocCheck.csproj` | `validation/checker-source/src/Yubico.YubiKit.AuditCt001.Checker.csproj` |
| Framework probe | `Yubico.YubiKit.Ctap2.FrameworkProbe.csproj` | `evidence/framework/src/Yubico.YubiKit.AuditCt001.FrameworkProbe.csproj` |
| Future snippet host | `Yubico.YubiKit.Ctap2.DocExamples.<id>.csproj` | `validation/checker-source/context/<id>/src/Yubico.YubiKit.AuditCt001.Examples.<id>.csproj` |

No snippet host exists yet. The generator now emits `AuditCt001.Examples.<id>` and rejects IDs
containing `ctap2`, `unittests` or `integrationtests`, case-insensitively. No membership was invented.
All current basenames retain the generic discovery prefix and exclude `Ctap2` and runner suffixes;
they remain explicitly buildable with `AuditCt001` scopes and explicitly nonpackable.

The checker and probe add only explicit `AssemblyName` to preserve existing DLL paths:

- `/tmp/opencode/ctap2-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Ctap2.DocCheck.dll`
- `/tmp/opencode/ctap2-001-framework-probe/bin/Release/net10.0/Yubico.YubiKit.Ctap2.FrameworkProbe.dll`

Current checker project SHA-256: `415c775f585ea0ddf3f96a1d57481516989ab9f678acc015490aef24b546e4c6`
(508 bytes). Its source/configuration/preservation baseline was not changed. Future host assembly names
default to their new project names; no historical host DLL name needs preserving.

## Original framework execution remains separate and verifiable

Read [framework-adjudication.md](framework-adjudication.md), original build RESULT/step/probe logs,
assets and original hashes before changing metadata. The original result remains
`20261103-234138-628-build`; its actual command was `--project Ctap2.FrameworkProbe`.
Those result/command/asset bytes were not rewritten to make the new name appear historically executed.

The original report is deliberately byte-identical. **Its old project/script/hash links describe
execution-time inputs.** Resolve them through the mapping below or the current
[hash manifest](evidence/framework/hashes.json); do not hash the replacement project/script against
the old manifest or run an old snapshot expecting its retired project basename to remain discoverable.
The original report's project link is now a retired location; the explicit retained link below is its
replacement for evidence lookup. This records the historical-link relocation rather than silently
leaving stale hash/path claims.

| Original path under `evidence/framework/` | Byte-identical retained snapshot |
| --- | --- |
| `src/Yubico.YubiKit.Ctap2.FrameworkProbe.csproj` | [original project](evidence/framework/original/Yubico.YubiKit.Ctap2.FrameworkProbe.csproj.snapshot) |
| `run-probe.sh` | [original reproduction script](evidence/framework/original/run-probe.sh.snapshot) |
| `hashes.json` | [original execution manifest](evidence/framework/original/hashes.json.snapshot) |

Snapshots end in `.snapshot`, not `.csproj`; they do not enter the project glob. The original script
bytes preserve the old commands and original log paths; the revised runnable `run-probe.sh` clearly
labels itself unexecuted revised metadata and uses fresh `-isolated-` logs with overwrite refusal.

| Input | Original/retained SHA-256 | Current SHA-256 |
| --- | --- | --- |
| Probe project | `5a0c6813e7b50471f7e9ef4f3fd937eabb30345ad5fc2c9064a3d1d0a8156651` | `868bc62205935a7a563787a24369b2b9041c1324a4721b97ede17e2236a33422` |
| Reproduction script | `91fb909ffcdd27620948b794d0c3c7b5c8aefb92a8418bf40ee68901940c97d1` | `958f8865239bdc30514a4082f2e4976a624f8cdcb2df032b3d3688914387addf` |
| Original hash manifest | `c0d6b732a6e7f40d01b9c98392fea63149df27c8089743d7d53379c78ef144a3` | Replaced by schema-2 mapping/current-metadata manifest; not a new execution record |
| Probe C# source, unchanged | `867427cf77d2c69eb12e072431c62a4690400a23b9ed8f24e38db25078d8c180` | Same |
| Historical adjudication report, unchanged | `3401f31c7bb26faa84ffa5ff0e9b59c343107ebbcadc67a3ce79a36264347cdf` | Same |

Every original execution-manifest entry was checked against the still-current immutable bytes or
the two mapped project/script snapshots, including the pinned framework sources, original logs/assets
and available original runtime/DLL files. All matched original sizes and hashes. This proves source/input
preservation, not that the renamed project has compiled. The original DLL hash in that manifest is
historical evidence; a future rebuild may replace temporary outputs and must retain fresh evidence.

## Exact reproduction/build commands — prepared, not executed here

From repository root, in the coordinator's reserved toolchain lane:

```bash
# Revised checker only; DLL path stays unchanged.
dotnet toolchain.cs -- build --project AuditCt001.Checker --clean

# Revised framework probe only (build, not runtime execution).
dotnet toolchain.cs -- build --project AuditCt001.FrameworkProbe --clean

# Later, after explicit extractor/context classification and preparation:
dotnet toolchain.cs -- build --project "AuditCt001.Examples.$CTX" --clean
```

For an independently authorized **framework-only** reproduction, the updated script is:

```bash
bash docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/evidence/framework/run-probe.sh
```

It builds with `--project AuditCt001.FrameworkProbe`, then invokes the unchanged probe DLL path;
it does not execute SDK or hardware operations. It writes
`/tmp/opencode/ctap2-001-framework-isolated-build.log` and
`/tmp/opencode/ctap2-001-framework-isolated-probe.log`, refusing to overwrite existing files.
This runtime reproduction is **not requested or performed by this metadata follow-up**.
Original executed commands remain in the snapshot script/report and are not current reproduction commands.

See [validation-tooling.md](validation-tooling.md) for updated preparation/check/collection commands.
Existing Python scripts, host analyzer policy, source hash guards and compile-only boundaries are retained;
only emitted project filenames, scope text and context-ID collision guards changed.

## Static verification and remaining historical follow-up

Static verification performed without running the toolchain or probe:

- Parsed both Python scripts with `ast.parse`; parsed current project XML and compared each with its
  retained original. The only XML metadata addition in either renamed project is `AssemblyName`.
- `bash -n` accepted the revised reproduction script (syntax-only, no execution).
- Reproduced the actual discovery/matching predicates against repository project paths: this run has
  **zero `Ctap2` basename matches**, exactly one match for `AuditCt001.Checker` and one for
  `AuditCt001.FrameworkProbe`, explicit nonpackability, and no runner-shaped project path/name.
- Verified all **26 original execution hash entries**, original-manifest identity and every current/mapped
  hash-manifest record. Original probe C#, framework sources, report and logs/assets remain byte-identical.
- Scoped tracked diff checks found no changes to root toolchain/discovery or immutable Management history.

These checks passed; they do not replace the next fresh helper build. All artifact writes used the patch
tool. One initial patch failed context verification without applying changes; the corrected patch applied.

Older immutable Management audit helpers/hosts still contain `Management` in their project basenames
and remain subject to that module-filter collision. They were **not renamed, excluded, rebuilt or fixed**.
The actual matched historical basenames are `Yubico.YubiKit.Management.DocCheck.csproj` and
`Yubico.YubiKit.Management.DocExamples.Xunit2.csproj` under that run's `validation/checker-source/`.
That is a separate historical-tooling follow-up requiring explicit ownership/authorization. Do not infer
repository-wide isolation from this CTAP run's filename change or alter historical evidence to obtain it.
