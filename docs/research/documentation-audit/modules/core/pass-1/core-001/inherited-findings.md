# Initial inherited-finding rebaseline

**The inherited registry is prepared for Core family review.** It is an initial source/history
rebaseline, with all nine dedicated applet/Core boundary packets still pending. Source baseline:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. A later metadata-only campaign HEAD changes the record
checkpoint, while the implementation evidence remains pinned to this source baseline.

## Reading map and counts

- [Registry](inherited-findings/registry.json): run-qualified identities, original definitions and
  authority paths, aliases/relationships, current mapped files and exact Git/SHA-256 identities,
  relevant commit ancestry, bounded source/assertion observations, status, actor boundary and
  documentation reopening map.
- [Source register](inherited-findings/sources.md): authority order, exact original-source limits,
  current anchors, retired-architecture map and tooling observations.
- [Nine boundary seeds](inherited-findings/boundary-seeds.md): per-module questions, current starting
  call sites, affected XML/guides/examples/tests and physical/Windows reopening conditions.
- [One-run preparer](inherited-findings/prepare.py): reproducible source-pinned record assembly.
  It performs no SDK/toolchain/device execution. Run with `python3 -B` from the repository.
- [Records-only verification](inherited-findings/verification.json): counts, authority/target paths,
  125 unique pinned/current source hashes, six historical architecture mappings and 21 preserved PGP
  labels. It retains the initial stale-reference failure and its record-only correction.

The registry contains **100 run-qualified provenance rows: 95 original numbered identities and five
explicitly labeled historical unkeyed observations**. They map to **70 nonadditive mechanism/question
groups**. Neither figure is a confirmed-bug count: a group can join dependency occurrences or hold
distinct subquestions, and an original ID can already be documentation-resolved. All **21 OpenPGP
PGP-DEP documentation-impact labels** are preserved separately with their full original sections and
reopening descriptions; they contribute zero additional Core bugs and are outside the 100-row total.

| Initial disposition | Provenance rows | Meaning |
| --- | ---: | --- |
| Resolved in source for the exact old scope | 15 | Source statements support the identified correction/removal. Existing named assertions are distinguished from new execution. Family reviewers still check full affected units and consequential sources. |
| OATH terminal reset resolved in source; physical SCP gap retained | 1 | Plain/SCP terminal-session behavior is implemented. Historical plain reset passed; SCP RESET was never reached. |
| Documentation-resolved, boundary recheck pending | 2 | CTAP-XREF-001 and WA-XREF-001 are two source-run occurrences of the same accepted documentation dependency. WA recovery and native selected-user decisions remain separate. |
| Reproducible from pinned source | 5 | Includes repeated provenance for FIPS mapping, flagged form factor, reader units and wider application bits. These are source-local observations, with no new hardware claim. |
| Reproducible records-only parse | 1 | Existing DOC-002 YAML ScannerError remains at the pinned map. No YAML fix was made. |
| Changed native/discovery or scoped PC/SC policy; physical uncertainty retained | 5 | Three abandoned/stall observations plus two PIN/isolation-policy occurrences. New ownership/policy code leaves old unproven causes and platform dimensions explicit. |
| Changed source or reopened documentation, current review pending | 7 | Six changed-source observations and the previously documentation-resolved Management fixture/cache dependency. Current tracked Core selector evaluation matches only its two canonical test projects; the retained substring algorithm leaves a future/untracked-host collision risk. |
| Needs decision, known policy with missing rationale, or missing evidence | 10 | Six decisions, one policy/rationale gap and three evidence gaps. |
| Candidate applet/boundary findings retained | 38 | Fifteen selected boundary candidates, two applet/profile candidates and all 21 WA engineering IDs. Exact current applet behavior still belongs to the assigned boundary/family reviewer. |
| Shared-source mapping prepared, boundary review pending | 16 | Initial local Core facts and actual current path/history identities; full applet-to-native trace remains assigned. |

The rows preserve both report identity and relationship. In particular, OATH `DEC-002` is the raw
access-key contract, while CTAP `CTAP-DEC-002` is the native selected-user/serializer actor decision.
Historical `CORE-001` keeps its `fixes-wave-001` key. No new Core finding ID has been allocated.
`CORE-DOC-*` and `CORE-IMP-*` are proposed namespaces for Astra's later collision check and triage.

## Important current conclusions

**Discovery changed substantially; the old physical failure remains unexplained.** Fresh scoped Find
calls, last-known `CachedDeviceInfo`, owned monitors, `DeviceChangeFeed` and the bounded discovery/native
owner split replace several old assumptions. The registry maps six old implementation paths through
their actual historical blobs and transition commits, including deleted repository/event-hub/service/
worker paths. `DiscoveryIdentityReader` itself remains and delegates bounded work. Linux checked release,
quiescence and retained claims answer ownership questions more precisely, while the old two-key serial
9681620 failure still needs matching conditions and actual native-release evidence.

**The metadata hardening has a precise boundary.** Current fixed-field length guards and their
malformed/optional-field assertions support the old #21 correction. They leave three distinct current
source questions: FIPS tags still route through application-bit decoding, flagged form factor still
casts the entire byte, and wider capability input still reads its first Int16. Zero-filled wide-input
fixtures establish acceptance only. Management's previously resolved fixture/cache prose is explicitly
reopened for the new metadata/discovery boundary, rather than recounted as an independent bug.

**Shared ownership facts need operation-specific use.** `ApplicationSession` publishes readiness after
the full initializer, borrows a direct connection and the prompt, owns its protocol, and transfers hidden
connection ownership only through convenience creation. Its comments identify the absence of a full
in-flight-operation lifetime lease; a per-exchange guard leaves multi-command workflow questions open.
`ApduResponse.Clear` clears its actual copied owner, including status, while TLV, formatter, transport
and returned applet buffers retain separate lifetimes. SCP03/11 parameter object aliases and disposal
must be reflected in complete examples and reset paths. This preparation supplies local facts rather
than an end-to-end cleanup or serialization guarantee.

**Applet and actor boundaries stay explicit.** CTAP owns PIN-peer/profile checks and response DTOs;
generic Core crypto/catalog metadata supplies no automatic profile conformance. The HSM supplied-
cryptogram 56-versus-48-byte mismatch remains an applet/profile finding, with the three-key crypto
oracle relationship preserved rather than a duplicate Core bug. Native WebAuthn selected-user privacy
remains an actor/serializer decision, with no automatic stripping policy. OpenPGP KDF/cache, raw applet
version and PIN profile questions stay distinct from generic reader and session mechanics.

## Next family and boundary work

1. **Discovery/metadata/monitoring:** rebaseline old abandoned-read, event/publication/freshness and
   fixture assumptions against complete current producer and native-owner units. Investigate the three
   metadata interpretation questions with independent fixtures and primary provenance.
2. **Connections/sessions/native/protocols:** trace actual admission, cancellation request, terminal
   completion, drain, release and recovery separately; verify borrowed/owned teardown and workflow
   exclusion for the nine applets. Keep Windows/custom/macOS/Linux contracts distinct.
3. **Crypto/credentials/utilities:** inspect SCP key/certificate aliases, response/TLV/encoding copies,
   credential UTF-16/UTF-8/profile units, old private-key/runtime residuals and crypto-helper replacement
   scope. Reverify original normative passages, including the bounded unavailable final GP sources.
4. **Nine applet packets:** use the seed table after the relevant family work. Read actual factories,
   backends and assertions, record each inherited dependency's current outcome, and independently review
   exact documentation reopening paths before accepting closure.

## Verification scope and retained limitations

This worker changed only this handoff and its `inherited-findings/` records. Source reads and Git
history/hash checks are pinned to the integrated baseline; record preparation and a read-only YAML parse
are the only executed Python work. No products, old records, user work, index, refs or worktrees were
changed. No SDK build, unit/integration test, hardware probe, formatter, commit, push or subagent ran.

The integration's recorded **4,442 unit and 197 resilience executions** remain separate overlapping
populations; they supply their original source/test scope and zero physical-mode execution. Earlier
serial125 INITIALIZE UPDATE responses were both 6A88 before EXTERNAL AUTHENTICATE/RESET. Existing
plain-reset or PIV Linux USB evidence retains its precise firmware/transport/actor qualifications.
The user's PIV physical deferral and pending Windows concurrency handoff remain in force.

Original global sources, failed attempts, unavailable captures and unresolved policy questions remain
available through their exact source-run paths. Family reviewers can narrow, refine or supersede an
initial disposition with new evidence; no original unknown was forced closed to complete this registry.
