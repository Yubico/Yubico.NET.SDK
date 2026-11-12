# Provisional boundary candidates and reopening map

All `CORE-HSM-X001`–`X007` identifiers are local routing candidates. They allocate no accepted Core IDs
and count no inherited applet/family alias as another independent bug. Product bytes remain unchanged.
Fresh editing, independent source/citation review and runtime qualification are separate assignments.
The machine-readable index localizes original assigned IDs; full file dispositions include caller-only
files that have no new selected unit. The source pin is the one in `analysis.md`.

## CORE-HSM-X001 — Explain reset, readiness and channel history locally

**Observed:** `HsmAuthSession.ResetAsync:507–519` preserves its backend, version and public ready/auth
flags, sends protected RESET when SCP is active, then performs an unprotected re-SELECT through the same
secure graph. It retains no SCP parameters and starts no fresh handshake. Its comment claims that a
PC/SC transaction remains owned, although this protocol path creates none. `IsAuthenticated` is inherited
Core channel-setup history, unchanged by management/credential operations, recovery latch or re-SELECT.

**Documentation:** propose a concise local auth distinction and reset failure/secure-channel qualification
in `src/YubiHsm/src/IHsmAuthSession.cs` type and Reset remarks, `HsmAuthSession.cs` initializer/reset comment,
`README.md` Constraints/Requirements and `CLAUDE.md` Module Context/version/reset explanation. Keep the
existing version-retention statement. Remove the unsupported transaction rationale in a fresh prose edit.
`IYubiKeyExtensions.cs` connection ownership remains useful and needs consistency review, not a rewrite.

**Runtime/decision:** decide HSM reset's desired terminal/reinitialize policy, full-workflow admission and
auth-state meaning separately. This report does not select OATH's different terminal-reset behavior or
claim measured device breakage. Aliases: B5-C001/C006; B6-C011/C012/C015; `HSM-DEP-001/002`.

**Qualification:** an independently traced fake and an authorized identified SCP-capable YubiKey must
separate RESET success, SELECT result, retained/retired host state, next protected operation, teardown and
fresh handshake. Existing override-only reset test establishes none of these secure steps. Preserve
serial 125's two 6A88 setup failures before EXTERNAL AUTHENTICATE/RESET.

**Reopen:** channel/reset implementation or policy changes → the XML/guides above,
`tests/...UnitTests/HsmAuthSessionByteLevelTests.cs` reset commentary,
`tests/...IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs`, `src/YubiHsm/tests/CLAUDE.md`,
tool `ResetCommand.cs`, `ResetMenu.cs`, `HsmAuthExamples/ResetHsmAuth.cs`; shared Core session/raw-tier
guidance and `docs/migration/v1-to-v2.md`, `v1-to-v2-map.yml`, `v1-to-v2-gaps.md`,
`docs/v1-to-v2-comparison.md`, `docs/v2-highlights.md` HSM/reset/SCP entries. Shared editor owns those
files. Migration YAML DOC-002 remains separate.

## CORE-HSM-X002 — State reachable buffer cleanup, with explicit response ownership

**Observed:** successful CALCULATE owns a final response, copies three keys, resolves success, and clears
that response even when Parse or success-resolution fails. The current write-through helper reaches
the real array; the sealed Core response now offers `Clear()`. Raw transport results, fragments, failed
writer staging and SCP decrypted temporary copies remain independent. Retry/backend errors can throw
before the successful-response clearing scope. Error data must be classified by actual semantics rather
than presumed secret or harmless. TLVs are disposed after entry into the encoder; caller input remains
borrowed and inline construction failure has a narrower cleanup scope.

**Documentation:** retain the already precise owned-copy language, add the lower-layer scope only where
an end-to-end guarantee might otherwise be inferred. Route owner API commentary through Core's explicit
`ApduResponse.Clear` contract. Scope standalone Parse/source/additional-copy responsibilities precisely.
Affected: `HsmAuthSession.cs` calculate/ZeroApduResponse internal rationale, `SessionKeys.cs` XML,
module README Security notes and CLAUDE buffer handling; `SessionKeysTests.cs` helper/zeroing assertions.

**Runtime/decision:** a future implementation can replace the legacy helper with the explicit owner API
and address actual remaining copies/exceptional allocations with identity-based cleanup oracles. No
write-through consumer sample or blanket "zero everything returned by a key" policy is proposed.
Aliases: `HSM-DEP-003`, `HSM-IMP-002`; C-C005–C007/C020; B6-C002/C006/C007. Current local helper is
source-functional, so this is not a new demonstrated failure to clear its response.

**Qualification/reopen:** changed owner/encoder/formatter/transport/secure-response cleanup → all those
surfaces, `src/Core/src/Utilities/Tlv*.cs`, APDU response/transmitter/chaining/SCP XML, Core README/CLAUDE,
`docs/usage/tlv-processing.md`, raw-access tiers, migration ownership entries and samples that copy keys.
Existing SessionKeys disposal test captures all three actual arrays; ZeroApduResponse test observes final
raw owner only. Neither asserts transport/decrypt/fragment cleanup or failed allocation cleanup.

## CORE-HSM-X003 — Keep full workflow, prompt and actual task lifetimes together

**Observed:** LIST policy lookup and CALCULATE are separately guarded exchanges. Prompt callbacks,
result parsing and outer finally are outside the exchange. A prompt request exception prevents CALCULATE
and resolution; a successful request is resolved exactly once, with token None. Keys are disposed if
successful resolution fails. LIST cancellation propagates; ordinary LIST failure logs and requests
conservatively. A held callback can outlive session disposal because no full-operation lease is held.

**Documentation:** existing README User interaction already asks callers to await terminal results,
retain prompts and avoid reentrancy. Add the precise whole-public-workflow/resource rule locally in the
interface/factory remarks and Constraints, in agreement with B5's shared correction. State that Core SCP
and native release lifetimes do not extend an application's detached callback work. Keep cancellation
outcomes tied to actual terminal exceptions, not merely token state.

**Runtime/decision:** select whole-method exclusion/disposal admission policy separately. Proposed tests
can hold request/resolution or the LIST→CALCULATE gap, attempt another call/disposal, then release/drain
in finally. No new lifetime lease or callback policy is implemented here. Aliases: `HSM-DEP-002`,
B5-C006; C's caller-owned presence/logging contracts. Callback exceptions can reach log providers;
do not promote ordinary APDU header-only logs into universal redaction.

**Qualification/reopen:** workflow/notification/exception mapping changes → HSM interface, initializer,
CALCULATE/policy helpers, README/CLAUDE, `TouchNotificationTests.cs`, test guide, sample SessionKeyMenu
and calculation operation; `docs/usage/user-interaction.md`, shared session XML, migration presence/cancel
entries. Historical 6982/Failed plus fresh recovery remains a measured result without timeout causality.

## CORE-HSM-X004 — Preserve raw, effective and selected-metadata versions

**Observed:** HSM SELECT tag 79/length 3, fallback 5.4.3, explicit override, major-zero permissive gates and
discarded reset-version result are distinct. Malformed TLV framing can throw before fallback. The
factory never substitutes `CachedDeviceInfo`; integration attributes use fixture metadata instead.
`InfoCommand` prints session policy under an applet-version label; the runtime string remains unchanged.

**Documentation:** retain accurate HSM gate table and obsolete alias explanation. Add a useful local
source note where version is shown, and ensure factory remarks do not imply overrides skip SELECT or
unsupported-feature errors prove physical firmware. No public guide needs duplicate hardware history.

**Runtime/decision:** preserve Core's existing policy pending maintainer selection; no inferred earliest
firmware claim. Aliases: `HSM-DEP-001`, A-C015, B5-C001. Python's exact 0.0.1 patch is a comparator, not
equivalent to C#'s major-zero condition. A Core prose overclaim remains A-owned.

**Qualification/reopen:** parser/fallback/gate/override/reset policy changes → backend and factory XML,
Requirements/version tables, Info command comments, byte-level challenge/reset tests, integration
metadata/gate comments and helper; shared firmware/Feature XML and migration entries. Older-firmware,
NFC and production-profile qualification remain separate lanes.

## CORE-HSM-X005 — Keep applet response format and live HSM authentication open

**Observed:** a supplied nullable symmetric cryptogram, including a present empty memory, emits tag 78.
C# forwards the full successful CALCULATE response to strict 48-byte Parse. A historical valid laboratory
cryptogram yielded 56 bytes and failed. Current pinned Python slices 0:16/16:32/32:48; vendor C suppresses an
8-byte symmetric cryptogram and expects 48 bytes. The historical Python tail field was named `card_crypto`, while
the C comment says host crypto. These choices do not identify the observed tail.

**Documentation:** current XML, SessionKeys remarks, guide/sketch, contributor instructions and diagnostic
calculation comments already preserve the consequential limitation. Retain them. This is a read-context
inherited concern rather than another required prose fix. Make no automatic truncation/receipt API proposal
look like an implemented contract. Keep host adapter interfaces explicitly application-defined.

**Runtime/decision:** exact inherited `yubihsm-001:HSM-IMP-008` remains deferred, with related
`HSM-IMP-007`. Require actual region identification, expected accepted/rejected variant grammar,
firmware applicability and result/receipt ownership before fixing. Core crypto agreement cannot close it.

**Qualification/reopen:** verified response contract/API changes → `IHsmAuthSession` symmetric parameter/
result/exceptions, `SessionKeys.ExpectedResponseLength` and Parse XML, CALCULATE helper, README complete
exchange sketch, CLAUDE, HsmAuthTool calculation comments, SessionKeys/byte-level/hardware oracle tests,
shared HSM migration/comparison/support entries. Genuine HSM/connector enrollment, peer verification,
host authentication and protected command/response remain a separate qualification task even after a
YubiKey-only raw-response comparison succeeds. Frozen probe records remain immutable.

## CORE-HSM-X006 — Preserve actual profile, metadata and retry validation actors

**Observed:** C# checks symmetric/asymmetric contexts of 16/130 bytes and imported scalars of 32 bytes. It performs no local
P-256 point/trust check in HSM CALCULATE; generic Core EC constructors are not called. GET_PUBLIC_KEY
checks 65-byte width; GET_CHALLENGE returns unchecked shape. LIST skips short recognized records,
decodes labels permissively and preserves unknown algorithm/touch metadata. GET_CHALLENGE uses general
APDU errors even with password; nine other paths use typed retry translation. No authentication retry
loop exists; protocol 61xx continuation is a separate mechanism.

**Documentation:** retain current source-accurate profile/delegation/error qualifications. Frame any
new consumer check as application/profile validation, not a hidden SDK helper or generic catalog
capability. Existing byte-level/recording tests do not establish mathematical validation or HSM trust.

**Runtime/decision:** original `HSM-IMP-001/003/007` remain deferred for consistency/parser/profile choices.
An optional parameter being present-empty versus absent is also a deliberate wire-admission question,
not evidence that Python truthiness is the correct C# contract.

**Qualification/reopen:** validation/unknown-response/raw-data/error policy changes → interface/algorithm/
credential/retry XML, session validation and retry helpers, README actor/handshake/constraints text,
CLAUDE, relevant byte-level/retry/password/LIST/point tests, HSM adapters and migration exception map.
Require independent valid/invalid profile vectors and actor-specific status observations before broader
assurance. Versioned normative applet-response specification remains GAP-SPEC-1 after bounded research.

## CORE-HSM-X007 — Retain diagnostic sample and test-oracle limits

**Observed:** all sample source was read with its enclosing menus/commands/shared helpers. CLI/visible
strings and key output remain diagnostic exposure. Several parses/second allocations precede finally;
wrong-length arrays/early selection exits can strand owned input copies. The calculation menu lists all
algorithms but always invokes symmetric calculation and supplies no prompt. Existing guides/comments
already explain these limits. Tests retain input/CTS lifetimes and persistent-cleanup weaknesses.

**Documentation:** preserve the diagnostic warning and useful scope commentary. Retain method-specific
assertion descriptions, replacing broad assurances only where a later changed oracle warrants it. The
retry exception type XML at 43–45 says every protocol failure is caught by `ApduException`; narrow that
claim to APDU-status failures, because malformed response/Parse failures and transport exceptions can
take other types. This is useful local correction without changing exceptions.

**Runtime/decision:** preserve `HSM-IMP-002/004/005/006` aliases without new independent bug counts.
Safe-default tool output, asymmetric dispatch, secure input/acquisition scopes, test harness cleanup and
stronger independent KATs require explicit implementation scope. Existing same-BCL PBKDF2 recomputation
is a parameter check, not an external known-answer oracle. Returned-owner zeroing is stronger than a
disposed-property-only assertion, but still does not verify end-to-end transport cleanup.

The asymmetric retry test's `Invalid credential password` substring matches the actual credential
helper at `HsmAuthSession.cs:986`. The first record checker caught an investigator misreading of this
message; `corrections.md` preserves the retraction. This matching assertion adds no implementation finding.

**Qualification/reopen:** sample input/output/dispatch/helper/assertion changes → Program and full
HsmAuthTool Commands/Menus/HsmAuthExamples, README Example and complete task sketches, CLAUDE/test guide,
`HsmAuthRetryException.cs` remarks, all relevant test comments, shared CLI/credential guidance and HSM
migration/sample entries. Preserve original failed parser/cancel probes and explicit final applet readback.
Compilation proposals stay UNCOMPILED in this boundary; no fake SDK connector APIs are introduced.
