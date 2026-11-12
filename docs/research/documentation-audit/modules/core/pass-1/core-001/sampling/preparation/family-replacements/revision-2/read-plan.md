# Non-SW successor for B6 qualification replacements

**Proposed reading only.** The original eight-unit proposal and original gap/status overlays remain unchanged at their original paths; `predecessor-history.json` binds those bytes as history.

The clean-quota replacement pool skips 48 SWConstants IDs from the original 104-unit XML population. This is a replacement-pool restriction only: broad denominator **104**, minimum **11**, all original statuses and exact ISO gaps remain unchanged.
Available next-ranked non-SW/unselected/unedited pool: **49**. Requested **4**, filled **4**, shortfall **0**. The previously selected ScpProcessor XML unit and the three B5/B6 test-role units are retained byte-for-byte; only the four provisional SW choices are superseded.

## Four new original units
- `core-001:unit:d41ecdd3d18d0e91` — `src/Core/src/Protocols/SmartCard/Apdu/ApduCommand.cs:75–79`; original rank 19; replaces `core-001:unit:3ad8b9f956a4e672`; **UNREAD / zero credit**.
- `core-001:unit:035ae50978ce88d7` — `src/Core/src/Protocols/SmartCard/Scp/SessionKeys.cs:51–56`; original rank 21; replaces `core-001:unit:aa9d6d49701b178d`; **UNREAD / zero credit**.
- `core-001:unit:9cd17279b86d5878` — `src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs:79–86`; original rank 22; replaces `core-001:unit:a3d37dc1aed22b2f`; **UNREAD / zero credit**.
- `core-001:unit:62ede839d51aa516` — `src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:183–189`; original rank 26; replaces `core-001:unit:f39183b2d438d21d`; **UNREAD / zero credit**.

[original-units.md](original-units.md) supplies the full successor proposal, including its four unchanged rows. `replacement-pool-exclusions.json` records every exact SW replacement-pool exclusion and its scope.
No unknown ISO clause is declared verified; no original source gap or status is erased. Non-SW selection supplies checkable proposed units, not an automatic verdict on other normative/provider claims.
Security Domain narrative expansion is separate in `sampling/preparation/expansion-SecurityDomain/`; it changes no B6 quota. Final accepted correction overlap, canonical mappings and actual Astra reads remain pending.
No source research/audit, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.
