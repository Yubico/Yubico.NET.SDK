# CTAP2/Core boundary investigation

**Outcome:** the assigned static boundary investigation is complete at
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. The return dispositions all **590 assigned units** across
**180 selected files**, plus the complete **181-path candidate-file population**. The packet's
`file_count: 181` counts that disposition population; the explicit `paths` list has 180 entries.
These are selected
Core-facing ranges with implementation, caller, helper and assertion context. They are a distinct
CTAP2/Core call-site review, rather than a whole CTAP2 audit or a repeat of CTAP/WebAuthn's earlier review.

The investigator proposes 23 local concern groups, `CORE-CTAP-X001`–`X023`, with separate documentation,
implementation, policy and qualification facets. Most retain existing CTAP/Core-family mechanisms;
the count represents neither 23 independent defects nor accepted Core IDs. Fresh independent review,
coordinator triage/editing/sampling and mandatory B4/B6/WebAuthn follow-ups remain open. No product file,
assertion, API snapshot, central ledger, frozen archive, index or ref was changed. No SDK build, test,
formatter, crypto/native/device execution, commit or subagent ran.

## Record map

| Record | Purpose |
| --- | --- |
| [Unit dispositions](boundaries/Ctap2/unit-dispositions.json) | Every assigned ID, exact source/range hashes, boundary read disposition, candidate associations and review state |
| [File dispositions](boundaries/Ctap2/file-dispositions.json) | All 181 candidate paths, selected ranges versus contextual reads, pinned Git/current identities and unchanged-path decision |
| [Candidates and reopening map](boundaries/Ctap2/candidates.md) | Local concerns, original aliases, unchanged-path actions, explicit B4/B6/WebAuthn owners |
| [Ownership and operation traces](boundaries/Ctap2/traces.md) | Actual factory/backend/helper graphs, initialization/failure/cancel/reset tables, buffer and version provenance |
| [Inherited outcomes](boundaries/Ctap2/inherited-dispositions.json) | All 26 routed source-run keys, preserved registry states and scoped current outcomes |
| [Sources](boundaries/Ctap2/sources.md) / [identities](boundaries/Ctap2/source-identities.json) | Original edition/status/section/actor/strength/application and retained bytes; bounded source gaps |
| [Verification recommendations](boundaries/Ctap2/verification.md) | Assertion-accurate existing coverage, meaningful unexecuted vectors, clean nominees and Windows delta map |
| [Static reconciliation](boundaries/Ctap2/verification.json) | Exact ID/path/hash checks and zero new SDK executions; separate from independent content review |
| [Separate checker receipt](boundaries/Ctap2/verification-receipt.json) | Fresh coverage/source/link reconciliation with zero failures; no independent content or runtime acceptance |

Final investigator dispositions are **6 read-candidate, 492 read-related-candidate context, 91 read-clean
and 1 read-exclusion**, with zero unread IDs. Related context preserves an accurate surrounding contract
or a dependent implementation question; it is not a finding that every line needs editing. The exclusion
is the complete unshipped API analyzer snapshot, read as declaration support rather than prose-clean
coverage. The directive-only shipped snapshot has its candidate-file disposition. Nine substantive clean
units are nominated for later sampling; their exact IDs and hashes remain in the packet.

The final read-only status check also showed other campaign workers' records and the original untracked
user files. Their contents and lifecycle were left with their owners. This packet's writes remain confined
to `boundaries/Ctap2/*` and this report.

## Consumer task and actual boundary

The caller opens one session, awaits complete operations and keeps borrowed inputs and prompts usable
through their last real continuation. `device.CreateCtap2SessionAsync` selects HID FIDO first, otherwise
SmartCard; SCP without an explicit preference selects SmartCard. An explicit supported preference selects
one concrete transport. Neither connection nor initialization failure triggers another transport attempt.
The convenience session owns the connection it opened. `Ctap2Session.CreateAsync(connection)` borrows
the supplied connection and releases its session attachment on failure/disposal; the caller retains
connection disposal and recovery responsibility.

Core binds the session before device I/O, registers the protocol for cleanup, rejects SCP on HID, and
prepares a FIDO HID channel. CTAP2 selects the FIDO AID only on SmartCard, obtains GetInfo, applies its
detected-firmware USB gate, and asks Core to configure the effective firmware and optional SCP channel.
It rebinds its backend after SCP wrapping. Core publishes readiness after that entire initializer returns.

The HID backend sends CTAPHID CBOR `0x10`, whose payload begins with a separate CTAP command byte.
Core owns framing, CID/sequence correlation, keepalive observation and cooperative CANCEL/drain. The
backend owns terminal CTAP status and presence outcome; typed decoding happens afterward. SmartCard's
backend emits policy notification before its APDU and classifies CTAP status after Core's APDU status
handling. Presence Completed means a successful CTAP exchange, with RP verification still required.

## Findings that matter to the current integration

1. **Connection exclusion needs the same membership bound as Core.** Two CTAP guide passages still
   imply all physical interfaces. Core coordinates process-local opens with overlapping known member
   IDs. Disjoint scopes, direct factories and external processes retain their own coordination needs.
2. **Per-exchange drain leaves whole workflows with the caller.** Credential begin/next, ClientPin's
   GetInfo/key-agreement/token calls and blob read-modify-write each span multiple exchanges. Session
   disposal can begin between them or before post-exchange parsing/notification completes. Existing
   single-exchange held tests establish narrower ordering, not a workflow lease.
3. **Successful response ownership remains layered.** Core's accumulated HID array, copied APDU owner,
   CTAP typed arrays/borrowed slices and WebAuthn results have independent lifetimes. Stored credential
   disposal clears its private clone, leaving the original transport response unchanged. Existing
   documentation already discloses much of this; implementation decisions retain their old aliases.
4. **PIN peer and Unicode enforcement belong to explicit profile/input owners.** The Core reader counts
   UTF-16 code units; CTAP's preset accurately discloses that. ClientPin checks only byte length, while
   its peer helper checks coordinates and forces P-256 import without all required COSE labels. Generic
   Core catalog applicability supplies neither CTAP profile validation nor device support.
5. **The selector has a localized stale reset claim.** Its remarks say reset callers retain the confirmed
   physical identity, while both actual reset flows select again. Their README and caller comments
   already explain this risk. Correct that statement without changing reset behavior in Pass 1.

The candidates retain separate exceptional cleanup sites, unsigned-output support, DER/profile/range
questions and the conditional logger-failure path around backend cleanup. Sharing a helper never merges
independent defects. Current accurate qualifications remain candidate-associated unchanged paths for
the future editor's explicit retain-or-correct decision.

## Dependencies and completion limits

Completed A/B1/B2/B3/C/B5 reports were consumed with their bounded source and execution scopes. B3's
published NativeShims source supplies source evidence, while the installed binary producer remains
unestablished. Integrated Linux native tests and compiled Mac source retain their own recorded scope;
they qualify neither Windows/macOS runtime behavior generally nor CTAP ceremonies on physical devices.

**B4** must supply the pinned Windows HID/CCID admission, drain, disposal/reopen and access-policy answer.
The user's future concurrency handoff gets a separate source/evidence delta when announced; no unfinished
checkout was inspected. **B6** must supply the full APDU/SCP chain/status/intermediate cleanup, major-zero
short-APDU option and post-handshake applet-state answer. Their active work is not guessed here.

`ctap2-001:CTAP-XREF-003` remains the precise credential-units dependency. Documentation-resolved
`webauthn-001:WA-XREF-001` and `ctap2-001:CTAP-XREF-001` remain resolved for their User/descriptor prose.
`webauthn-001:WA-IMP-010` recovery and `ctap2-001:CTAP-DEC-002` selected-native-result privacy/serializer
choice remain distinct, explicitly assigned to BOUNDARY-WebAuthn and maintainer/coordinator adjudication.
This review authorizes no field stripping or parser repair.

Original draft/final/ISO/GP, vendor support/firmware and provider provenance gaps remain explicit.
The shared Python YubiKey Manager pin provides general Core/history context; that tree has no
`yubikit/fido.py`, so it supplies no invented CTAP implementation oracle. Earlier CTAP original passages
and B5's retained originals govern scoped protocol statements. Initial reuse and one focused follow-up
completed this packet's bounded source work; the precise remaining questions go to their named owners.

Static investigation completion is separate from independent acceptance, required mechanism follow-ups,
consumer compilation, managed verification and physical/platform qualification. PIV physical deferrals,
historical serial-9681620 discovery uncertainty and all frozen evidence remain preserved.
