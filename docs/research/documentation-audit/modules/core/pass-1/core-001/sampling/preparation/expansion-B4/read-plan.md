# B4 narrative expansion and separate protected-output quota repair

**Additional proposed reading only.** All earlier selections, population, ledgers and coordinator verdicts remain unchanged.

B4 narrative: original population 38, full original quota **4**, additional next-ranked quota **4**, proposed total **8**. Both SAMPLE-B4-001/002 units have zero final clean credit.
Original A/B1/B2/B3/B4 actual selected-read total is **638**. The other 345 original selected units remain unread according to the coordinator handoff; added/replacement units are separate and still unread.

## Four new narrative reads
- `core-001:unit:f5a358ec89b4c4ce` — `src/Core/CLAUDE.md:6–13`; original rank 5; **UNREAD / zero credit**.
- `core-001:unit:9a180bff18d16922` — `src/Core/PACKAGE_README.md:13–19`; original rank 6; **UNREAD / zero credit**.
- `core-001:unit:7070cc291aec3e7e` — `src/Core/CLAUDE.md:1–5`; original rank 7; **UNREAD / zero credit**.
- `core-001:unit:db781bee61b48612` — `CLAUDE.md:585–596`; original rank 8; **UNREAD / zero credit**.

## Separate protected-output exclusion/replacement
Original 9ea1794bce816ec8 runtime-help unit receives zero final clean-quota credit. The known NoTests-policy discrepancy is retained as existing non-D/protected output; it is not a new runtime finding and runtime text remains unchanged.
Same-stratum working population 105, 10% minimum 11, original valid selected 10; **1 deterministic replacement(s) required**. This repair is separate from the four-unit material-miss expansion.
- `core-001:unit:7dc8f2fc14f0186c` — `nuget.config:1–20`; original rank 12; **UNREAD / zero credit**.

## Exact inputs and remaining gates
The accepted current-products manifest and reviewed focused two-document patch 3c066d... bind edit exclusions. The focused patch is independently PASS but transfer-pending at handoff; its immutable before/after snapshots are mapped back to original coordinates. No whole-file exclusions or live source chasing.
[original-units.md](original-units.md) supplies every new complete pinned unit. JSON selections retain original character/line ranges, source/range hashes and pending verdicts.
Final combined comment mapping, remaining validation/sampling patches, final eligibility and actual Astra reading/verdicts remain separate. Other strata are not reduced; the broad code-only/private/internal/test population is unchanged except already adjudicated specific exclusions.
No content verdict, source audit, product/runtime change, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.
