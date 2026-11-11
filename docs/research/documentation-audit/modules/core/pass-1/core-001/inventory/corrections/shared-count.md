# Shared packet count correction

**Disposition: stale prose corrected; released packet inputs remain unchanged.**
Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Published inventory/metadata checkpoint: `01a114d4e251e0f52b92f73393fbe1dca5d5c24e`.
This bounded check reviews inventory membership and generation history, rather than Core behavior or
the active family investigators' semantic work.

## Published snapshot and exact populations

The original published `dispatch-map.md` is preserved by Git at the checkpoint above, SHA-256
`b5f204ab57c07434ed96c25d5d9e990dc323be918173cf2500712e60dc508501`.
Its line 22 said: “A also receives 374 shared read units; C receives 174.” Those numbers came from an
earlier generation summary. The published and currently released JSON already contain the final sets:

| Packet | Primary paths / recorded lines | Shared IDs | Unique IDs | Membership against shared-sections owner/stage |
| --- | ---: | ---: | ---: | --- |
| A | 91 / 16,728 | 376 | 376 | Exact A-owner set |
| B1 portable lifetimes | 49 / 9,747 | 126 | 126 | Exact B1-stage set |
| C | 104 / 17,050 | 176 | 176 | Exact C-owner set |

All three packet schemas use `shared_read_unit_ids`; `unit_ids` separately identify their primary
reading units. A/C also retain candidate-file `disposition_paths`. B1 is a staged primary path set.
The count correction changes neither schema nor either kind of unit ID.

## Why the earlier counts differ

During inventory preparation, the caller-selection expression was expanded to retain implicit Core-facing
exchange/cleanup calls, including `.Dispose` and `.Clear`. Existing declaration ranges and their IDs
remained stable. Replaying the earlier predicate against the final stored ranges identifies precisely
these two extra C shared units:

| Existing unit ID | Source range | Declaration group | Newly recognized call |
| --- | --- | --- | --- |
| `core-001:unit:fe8c78eb256acf38` | `src/Cli.Commands/src/Oath/OathHelpers.cs:21–72` | `OathHelpers.UnlockIfNeededAsync` | `.Dispose` |
| `core-001:unit:8ea97b73005fa724` | `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs:167–193` | `FakeOathSession.AuthenticateAndRetryAsync<T>` | `.Clear` |

The same bounded predicate replay accounts for A's two additional shared units:

- `core-001:unit:4abe07d44880460b` — `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Infrastructure/CommandMetadataTests.cs:251–256`, `CommandOutput.Dispose`, recognized `.Dispose` call.
- `core-001:unit:9d486ffba3f7bb7f` — `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/DeviceSelectorTests.cs:355–356`, `SelectionFlow.Dispose`, recognized `.Dispose` call.

It also identifies one additional B-owned cleanup unit, `core-001:unit:23aec184fc694751`, in
`src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs:260–268`
(`ConsoleCapture.Dispose`, `.Dispose`). That unit belongs to B4, leaving B1's final set at 126.
The separately included `nuget.config` support unit also belongs to B4. This explains the final global
shared population of 1,703 units: A 376, B 1,151 and C 176. These are structural candidate selections;
their substantive dispositions remain with the assigned investigators.

The discrepancy arose when the final JSON was regenerated after the caller-filter refinement while
the earlier A/C prose counts were retained. It reflects caller-selection refinement, rather than a
source change, new inline comment, newly split range or reassignment during active investigation.

## Membership and preservation checks

The bounded check verifies:

- Each of the 1,703 shared section IDs is unique and resolves to an existing unread semantic unit.
- The shared-section set exactly matches the unread SHARED/EXTERNAL unit population.
- A and C packet shared IDs exactly match their assigned investigation owners.
- All six B-stage shared sets are unique, pairwise disjoint and together equal B's 1,151-unit set;
  B1 exactly matches its 126-unit stage set.
- A, B1 and C shared inputs are pairwise disjoint. The existing source path sets and their recorded
  file/line counts match their packet summaries.
- The released packet files, shared ownership ledger, semantic-unit population and generator source
  are byte-identical to their published `01a114d4` versions. Git reports no `src/Core/` change between
  the source pin and that metadata checkpoint.

Published/current unchanged input SHA-256 values:

| Path relative to this run's `inventory/` | SHA-256 |
| --- | --- |
| `packets/A.json` | `427fd5833a9a57057a01b556277caf71f5a043820adbe63b0c37485f52712a0c` |
| `packets/B1-portable-lifetimes.json` | `55656e7b59892bf30d766322209b62360d65049eabd258a651661f4367139621` |
| `packets/C.json` | `9ba1809d37186d462f6c554f85766284702e1382fe1b8e9e1cbd4db88d0860d4` |
| `shared-sections.json` | `148a865f7883cd0bde059226553c6cb31b7ea80506bd9669d3e60da0ffabdd0e` |
| `semantic-units.json` | `f358fb02b124714e92f45a5d7d65886ebd2d2f864e3a8214bb38dbd25a72d1cf` |
| `build_inventory.py` | `342220483b020ef35abe0752fddef25417d4677eff8c586a2dcdb425fef5646e` |

`dispatch-map.md` now states A 376, C 176 and initial B1 126. The original structural verification
remains applicable because its checked inputs, source/range identities, ownership and populations are
unchanged. This correction adds no clean semantic dispositions or compiler/runtime qualification.
The full inventory generator and preservation verifier were left unrun to keep active workers' inputs
stable; a bounded read-only membership/hash check independently verifies the correction.

Only `dispatch-map.md` and this new correction record are edited. `inventory.md`, `coverage.json`,
all JSON inputs and all other workers' records retain their bytes. No SDK build, test, formatter,
hardware command, index/ref write, commit or subagent ran.
