# Pass 2 applet master — R1 correction receipt

**2026-11-13. R1 edited; independent bounded reviewer closure pending.**

[CTAP-IMP-007](pass-2-applet-master.md#ctap-imp-007--security-critical-peer-cose-labels) is now **Low**, not
High. A schema-invalid peer map can proceed only with 32-byte X/Y arrays accepted by the provider as
a P-256 point. The confirmed consequence is missing required-label rejection. The ignored labels do
not select another curve or derivation algorithm; no operational interoperability failure, off-curve
acceptance, authorization bypass or secret compromise is established. Required-label checks remain a
protocol-correctness and security-defense fix. Exception provenance and optional-label policy remain
unresolved; the BEFORE, proposed AFTER and acceptance/dependency text are unchanged.

## Exact edited scope and identities

All paths here are relative to `docs/research/documentation-audit/`. SHA-256 identifies file bytes,
not execution evidence.

| File | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| `pass-2-applet-master.md` | `066994f1841b189ea411e66d288791ace6d45779b4d9fa7bd3fbe6dd195e4db9` | `960873b4bd2baba78719f902d1f98e0e75c8d388863eb3753e137da2324549ed` |
| `pass-2-applet-master-coverage.md` | `65a3afc9e662ea7cc1c6ba7f06e8b6437ff5471f1c55fd112d34c6e05bc2ddc7` | `8cdd003fd0cd0c8a67b7177e964438bdd16cc54af8db0c35992f37f8b2bb2be4` |

- Master: original lines 3–4 and 14–15 (review status/navigation); 2562–2564 (R1 severity/impact);
  8117–8118 (review workflow limits); 8131–8135 (integration boundary/next owner). The corrected card
  occupies lines 2562–2595. No other CTAP-IMP-007 summary rating exists in the master or companion.
- Companion: original line 3 (status); additions after lines 8 and 51 (bounded review and lazy followups);
  addition after line 89 (distinguish the original integration manifest from this correction).
- This receipt is the only new authored file. The [original review](pass-2-applet-master-review.md)
  is byte-unchanged: `d7725778b617eb67a6f719e4dd0c60d91b44bbfdbd6678c4c786321c131afadb`.

## Evidence and checks

- C# HEAD remains `6ce355468047e8004fd51cf1425808f6e8242ed3`. Read
  `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` from that Git object: required values are documented at line 43;
  coordinate checks, fixed P-256 import and retained provider validation are at lines 63–96.
  Its working-tree bytes match the pin; SHA-256 is
  `47d199377344b2505fa593fbe3ad4f31fb0eeb937b5f63d572ae5f702a617643`.
- Read only the relevant original CTAP-IMP-007 triage row and frozen
  [X006 definition](modules/core/pass-1/core-001/boundaries/Ctap2/candidates.md#x006--pin-peer-labels-are-a-ctap-profile-boundary)
  at lines 83–92. They retain the conformance gap and import/derivation exception distinction; neither
  supplies the missing high-impact failure path. No frozen input was edited.
- Static comparison: the entire nine-applet region is byte-identical after excluding CTAP-IMP-007.
  All other card bodies, maps, coverage ledgers and 377 fenced code/procedure blocks are unchanged.
  Markdown fence pairing and local link/heading targets were checked for master, companion and receipt.
  Link presence is not semantic verification or external URL reachability.

The independent report remains a **ledger-level comparison plus targeted semantic/source sample**, not
full semantic certification. Its remaining per-applet groups are lazy human-review followups, loaded
only when that applet is selected, not missing known finding IDs or a demand for all-nine-app deep review.
This correction adds no provider/crypto execution, attack result, new research, code fix, build, SDK test,
hardware operation, commit, push or subagent. User review remains unstarted; implementation is deferred;
proposals remain unapplied and uncompiled. The parent owns bounded reviewer closure of R1.
