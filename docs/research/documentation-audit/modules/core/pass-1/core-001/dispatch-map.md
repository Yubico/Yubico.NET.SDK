# Core investigation dispatch map

**Recommendation:** begin exactly three independent Sol 6.1 investigations: **A**, **B1** and **C**.
The source pin is `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This is a recommended dispatch,
rather than an execution or edit authorization. The coordinator releases each packet and owns phase gates.

## Exact path sets

Each JSON packet lists exact paths and unit IDs. Its `unit_ids` are primary reading/disposition ownership;
`shared_read_unit_ids` refer to exclusively assigned shared-document/caller ranges in
`inventory/shared-sections.json`. `paths` and `disposition_paths` differ for bounded external projects:
the former select files with unread units, while the latter retain the complete candidate-file disposition.
File line counts include comments, tests, fixtures and historical content; they are sizing evidence,
rather than proof of complexity or correctness.

| Initial packet | Exact packet | Files / lines in primary set | Focus |
| --- | --- | ---: | --- |
| A | [A.json](inventory/packets/A.json) | 91 / 16,728 | Fresh scoped discovery, metadata/identity, owned monitor/feed/listener scheduling and tests |
| B1 | [B1-portable-lifetimes.json](inventory/packets/B1-portable-lifetimes.json) | 49 / 9,747 | Physical registry/slots and portable PC/SC/native ownership, sharing, admission and checked release |
| C | [C.json](inventory/packets/C.json) | 104 / 17,050 | Crypto catalog/keys/codecs, credentials/presence contracts, Base32/TLV/buffer utilities, logging and tests |

A also receives 376 shared read units; C receives 176. Initial B1 receives 126; B's remaining shared units are divided by stage in
`shared-sections.json` and the B-stage packets. Shared-document research can cross family mechanisms;
its primary range owner remains unique. The future **SHARED editor** alone integrates shared-file edits,
including Core root guides, root/shared guidance and cross-cutting narrative/configuration. It is an
editing responsibility for a later phase, rather than a fourth initial investigator.

### B is a staged queue

The complete [B family](inventory/packets/B.json) contains 352 files / 62,364 lines, including 311 Core
files and 41 external verification/benchmark files. Its native callbacks, retained release state,
worker/reactor lifetimes, failure paths and secure-channel state warrant smaller investigations.
The following exact path sets are disjoint and cover B's entire primary file population:

| Stage | Exact packet | Files / lines | Suggested release condition |
| --- | --- | ---: | --- |
| B1 | [Portable lifetimes](inventory/packets/B1-portable-lifetimes.json) | 49 / 9,747 | Initial packet alongside A and C |
| B2 | [Linux lifetimes](inventory/packets/B2-linux-lifetimes.json) | 63 / 11,868 | After B1's registry/lease evidence is available |
| B3 | [macOS lifetimes](inventory/packets/B3-macos-lifetimes.json) | 41 / 7,636 | After the shared release-proof vocabulary is settled |
| B4 | [Windows/support](inventory/packets/B4-windows-support.json) | 106 / 14,842 | Use pinned Windows source now; add a separate user-ready delta later |
| B5 | [Session/framing](inventory/packets/B5-session-framing.json) | 43 / 8,830 | Consume B1/platform ownership findings; trace complete raw exchanges |
| B6 | [APDU/SCP](inventory/packets/B6-wire-scp.json) | 50 / 9,441 | Consume session/crypto evidence and shared source register |

Treat B4's 106-file support mix as another checkpointed packet if the first investigation confirms
substantial independent subfamilies. Keep at most three active investigators unless the coordinator
explicitly adjusts the limit. Releasing a later B stage after an initial packet returns keeps that limit
while permitting fresh independent investigators. Parallel investigation never gives concurrent edit
ownership of the same file.

## Packet contract

Every released packet carries these requirements:

1. Read root `AGENTS.md`/`CLAUDE.md`, current Core README/CLAUDE, applicable nested instructions,
   campaign workflow/style, approved plan and SDK house style. Tests also use `src/Core/tests/CLAUDE.md`.
   Read the relevant external project instructions before its caller/support work.
2. Read complete units and their implementation/assertion/lifetime context. Include XML, internal/native
   rationale, test/example comments, code-only implicit contracts, substantive standard claims and genuine
   unfinished-work markers. Whole-file orientation or a keyword hit supplies no clean disposition.
3. Use direct, calm, precise, approachable prose: affirmative actor/task/behavior first. Retain useful
   rationale, conditions, resource scopes and normal word spacing. Reserve negatives for consequential
   limits or likely misconceptions, paired with a supported action.
4. Own only the released packet's new investigation records. Product edits, test assertions, API snapshots,
   runtime help/output and behavior changes wait for their explicit later scopes; implementation remains
   Pass 2. Frozen campaign/integration records and original user files remain read-only.
5. Source dependencies outside the packet may be read narrowly. Their primary owner dispositions their
   units. Promote an excluded candidate only with a recorded dependency and coordinator reconciliation;
   preserve old unit identities and update the denominator explicitly after a true split/addition.
6. Original-source research records exact revision/status/section, actor, strength and applicability.
   Use Context7 for framework/library/API questions. Keep pinned Python code/tests/history as comparator
   evidence. Share one source register; bound uncertainty to an initial pass and one focused follow-up.
7. Return per-unit read/clean/finding/exclusion outcomes, exact source identities, qualified tests/oracles,
   source gaps/decisions, candidate-associated unchanged paths and a claim-to-document reopening map.
   Keep documentation, implementation and hardware/platform state separate.
8. Use the parallel inherited registry's source-run keys. Refer old findings to its owner instead of
   duplicating their detailed re-analysis or allocating a colliding `CORE-001`. Fresh questions use the
   `core-001:CORE-XREF-<number>` namespace after allocation.
9. Builds/tests/formatters/device work require coordinator assignment and serialized repository workflows.
   Inventory itself requires no C# host build. Compiler/consumer-shape checks remain `VALID-NOT-CHECKED`
   for their later validation packet. Route scratch to the approved home-backed runtime directory.
10. Return blockers promptly while continuing independent readable work. Preserve the Windows late-handoff
    checkpoint and PIV physical-test deferral. Leave unfinished worktrees and native/device state alone.

## Inherited registry inputs

[inherited-routing.json](inventory/inherited-routing.json) maps every one of the parallel owner's
70 nonadditive groups and 100 provenance rows to a primary packet, required boundary packets, secondary
read scopes and current file ownership. [inherited-packet-inputs.json](inventory/inherited-packet-inputs.json)
is the reverse packet input list. The original registry hash binds this routing to its authority;
its states and conclusions remain unchanged. The separate 21 `PGP-DEP` documentation labels stay separate.

A receives metadata FIPS, flagged form factor, wider application bits, fixture/cache and discovery
questions. Keep fixed-field length hardening's exact resolved scope distinct from those interpretation
questions. C receives reader length/encoding interpretation and caller buffer/crypto questions. B's
stages receive their actual registry/session/native/framing/SCP mechanisms, with C reading SCP secret
aliasing and A reading discovery/native transfer dependencies. Applet policy/actor questions retain a
boundary primary owner, so initial families avoid a duplicate whole old applet investigation.

Preserve `fixes-wave-001:CORE-001` and its unexplained serial 9681620 failure after the architecture change.
Preserve serial 125's two `6A88` results before EXTERNAL AUTHENTICATE/RESET, the YAML parse failure in
`DOC-002`, proposed accepted-finding `CORE-DOC-*`/`CORE-IMP-*` namespaces, and the Windows readiness gate.
The run-qualified `CORE-XREF` inventory keys remain candidate routing, with zero new accepted allocations.

## Nine mandatory later boundary packets

These are distinct from the initial Core families and from an applet whole-module re-audit. The exact
candidate files, selected units and exclusions are already indexed. Release each after relevant Core
mechanisms and inherited-finding provenance are available.

| Applet | Exact packet | Required emphasis |
| --- | --- | --- |
| OATH | [BOUNDARY-Oath.json](inventory/packets/BOUNDARY-Oath.json) | Connection/session/reset/reopen, Base32 and historical blocked SCP initialization |
| Management | [BOUNDARY-Management.json](inventory/packets/BOUNDARY-Management.json) | Core metadata fields, live versus cached reads, transport selection and scoped/owned-monitor companion tests |
| CTAP2 | [BOUNDARY-Ctap2.json](inventory/packets/BOUNDARY-Ctap2.json) | Shared FIDO framing/channel/cancel, credential units, prompt terminal ownership and crypto/COSE boundaries |
| WebAuthn | [BOUNDARY-WebAuthn.json](inventory/packets/BOUNDARY-WebAuthn.json) | Direct and indirect CTAP2/Core use, caller/result/prompt ownership and recovery dependencies |
| Security Domain | [BOUNDARY-SecurityDomain.json](inventory/packets/BOUNDARY-SecurityDomain.json) | SCP initialization/reset/readiness, keys/certificates, raw/effective firmware, APDU/lease/logging boundaries |
| YubiOTP | [BOUNDARY-YubiOtp.json](inventory/packets/BOUNDARY-YubiOtp.json) | HID status/CRC/programming sequence/reset, SmartCard drain, presence outcomes and effective version |
| YubiHSM Auth | [BOUNDARY-YubiHsm.json](inventory/packets/BOUNDARY-YubiHsm.json) | SmartCard/credential/prompt lifetime, APDU/TLV ownership and applet-owned response/profile uncertainties |
| OpenPGP | [BOUNDARY-OpenPgp.json](inventory/packets/BOUNDARY-OpenPgp.json) | Credential units, SCP/cache/disposal, version provenance, workflow serialization and presence |
| PIV | [BOUNDARY-Piv.json](inventory/packets/BOUNDARY-Piv.json) | Physical PIN/handle state, cancellation/serialization, response/TLV ownership and preserved physical deferrals |

Each return contains actual factory/backend/helper call paths, selected transports, an ownership table,
initialization/failure/cancellation/reset trace, raw/effective version provenance, all inherited dependency
outcomes, exact documentation reopening paths, scoped verification limits and independent review needs.
Complete examples include their implicit calls and enclosing cleanup. Core mechanism findings can be
shared once; each applet still receives its own current call-site disposition.

## Exclusive shared-file editing and final reconciliation

[SHARED.json](inventory/packets/SHARED.json) and [EXTERNAL.json](inventory/packets/EXTERNAL.json)
retain candidate-file dispositions. `shared-sections.json` assigns each selected section one primary
research reader and the future SHARED editor. Other family investigators may read the same dependency,
but contribute proposed corrections through that editor rather than editing the file independently.
Applet-specific active documentation receives a later explicit boundary editor; historical applet
archives remain frozen.

Before final compilation, reconcile every candidate-associated unchanged path as well as edited paths.
Only actually read clean units enter the coordinator's stratified 10%/minimum-three sample denominator.
Keep API, narrative/example, internal/test, native/platform and all nine boundary strata represented;
large enum/accessor populations cannot substitute for lifetime/native review. Recheck the late Windows
handoff point, source identities, compiler-shape questions, original citations and outstanding physical
lanes before requesting first-sweep closeout.
