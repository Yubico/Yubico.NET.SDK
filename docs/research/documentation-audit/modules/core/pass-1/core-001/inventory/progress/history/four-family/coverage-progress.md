# Core structural coverage progress

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Completed investigator returns only; independent review, editing and SDK/runtime verification remain **PENDING**.

| Completed family | Primary | Shared | Read-clean | Candidate-linked units | Exclusions |
| --- | ---: | ---: | ---: | ---: | ---: |
| [A](../investigation-A.md) | 1630 | 376 | 1855 | 151 | 0 |
| [B1](../investigation-B1.md) | 1078 | 126 | 1104 | 99 | 1 |
| [B2](../investigation-B2.md) | 1148 | 71 | 1187 | 31 | 1 |
| [B3](../investigation-B3.md) | 1139 | 5 | 1087 | 57 | 0 |

**Total: 5,573 distinct assigned IDs = 4,995 primary + 578 shared; 5,233 read-clean, 338 candidate-linked, two exclusions.**
The 48 local candidate groups are nonadditive investigation groups, not accepted bugs.

Core: **4,652 / 9,020** substantive units dispositioned; **4,368 pending**. External: **921 / 5,024**; **4,103 pending**.
**8,471 units remain pending completed returns.** C: 1,562; B4: 2,350; B5: 932 (three active). B6: 684 queued; nine unexecuted boundary packets: 2,943.
C partial records were not read or credited. Every completed/remaining assignment ID reconciles once to its original primary/shared owner.

B1 platform notes `9fe6da15d252bcd2` retain a partial-scope exclusion: B3 supplies macOS source review; Windows remains B4 pending. No duplicate count or whole-platform closure.
B2 `a9c5253178fb280d` retains its inactive historical-scan exclusion and original source. Both secondary B3 handoffs add zero primary/shared units.
Read-clean counts include type/native/context/historical/rendering strata: **no final clean-prose or sampling denominator is claimed**. The 4,544 administrative and 7,140 outside-scope records remain separate.
Candidate-associated unchanged-path maps still require later editor/reviewer disposition. Windows late work and PIV physical deferrals remain pending.

[Keyed totals, category/status breakdowns, input hashes and exact ledger references](../coverage-progress.json) preserve source/range identities and reference the four full family ledgers without duplicating all unit records.
The original 38 inventory files, original coverage, packet IDs and investigator records remain unchanged. No source investigation, SDK build/test, formatter, device command, index/ref write, commit or subagent ran.
