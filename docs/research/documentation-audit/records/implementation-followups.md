# OATH pilot implementation follow-ups

## Working agreement

**Latest user direction:** Documentation is the main campaign scope. Current OATH fixes are an explicit exception; do not generalize their authorization. Small bounded bug fixes may proceed, but meaningful behavior/API/lifecycle changes are deferred to a later implementation pass, then followed by re-review of affected documentation. Cross-application reset/reopen behavior is deferred. CORE-001 remains recorded, not assigned for a fix in this cycle.

Implementation concerns discovered by the documentation audit are recorded here and linked to their evidence. The user requested deferring implementation work until the end-of-pilot review, when we will decide whether future findings should be fixed immediately by an independent agent or collected for a later batch.

This document tracks work; it does not authorize implementation changes or establish new API contracts. No external issues have been created. Owners below are proposed roles, not assigned people.

**Implementation authorization:** The user subsequently approved autonomous follow-up work using `docs/SDK-HOUSE-STYLE.md`, the local Python SDK's firmware policies, and the two attached allow-listed test keys. Wave1 assignments and ownership are recorded in [the implementation plan](implementation-plan.md) and [dispatch record](runs/fixes-wave-001/dispatch.md). Historical per-entry deferral notes below describe the pilot stage; entries assigned in the active wave are now in progress, not resolved.

Baseline: `ddf3cdc24984f598678b48f2a0f98d6be1a58bf9`.
Evidence ledger: [OATH-001 findings](runs/oath-001/findings.json).
Review point: end of the OATH pilot, before declaring its consequential findings resolved.

**Upstream update:** Documentation was committed as `7c4153f8`; `origin/yubikit` at `089b2393` was imported in merge `361567eb`. [Reconciliation evidence](runs/upstream-001/reconciliation.md) closes IMP-003 and records IMP-004's code correction with a remaining regression-verification gap. All other entries retain their dispositions. Original observations below describe the audit baseline unless a resolution is stated.

**Wave1 resolution:** CLI commit `618adbec` and protocol commit `c79e4c1e` were integrated in `c90dced1`. IMP-001/002/008/009/011/012/013/014 are closed within their stated scope after TDD, independent re-review, full gates, and applicable hardware checks. [Completion evidence](runs/fixes-wave-001/completion.json) records 3,828 passing unit tests and separate corrected-source runs on both firmware versions. Historical observations/needed-work bullets below are retained as the issue definition; resolution states supersede them.

## IMP-001 — Clear the raw VALIDATE response

- **Source:** OATH-001-F07 / E3.
- **State:** closed in `c79e4c1e` / integration `c90dced1`; actual session-owned raw response cleanup is covered across success/failure paths. Lower-level transport copies remain outside this claim.
- **Priority:** consequential sensitive-buffer cleanup.
- **Proposed owner:** OATH implementation maintainer.
- **Location:** `OathSession.ValidateAsync`; `ApduResponse` ownership is supporting evidence.
- **Observation:** the raw response array contains the device authentication HMAC. The session clears extracted/computed HMAC arrays but does not explicitly clear this raw copy.
- **Needed work:** define cleanup ownership and clear the session response on success and all relevant failure paths without erasing caller-owned input or data still needed by the protocol.
- **Verification:** meaningful regression evidence for success, missing/malformed response, and device-HMAC verification failure; run relevant prescribed tests. No runtime retention/exploitability claim has been established by the audit.
- **Documentation dependency:** do not promise complete authentication-response clearing before correction. Re-review authentication ownership/cleanup statements after the implementation changes.
- **Scope boundary:** lower-level transport copies were not comprehensively audited; this finding does not certify their handling.

## IMP-002 — Preserve OathTool cancellation during password flows

- **Source:** OATH-001-F06.
- **State:** closed in `618adbec` / integration `c90dced1`; token propagation, prompt cancellation, cleanup and CLI cancellation outcomes are verified. Redirected synchronous input remains explicitly non-interruptible until the read returns; admitted device exchanges keep their existing boundary.
- **Priority:** ordinary correctness.
- **Proposed owner:** OathTool maintainer.
- **Locations:** `OathSessionHelper.CreateUnlockedSessionAsync`, `UnlockSessionAsync`, and `AccessCommand.ChangeAsync`.
- **Observation:** supplied cancellation tokens are dropped for unlock, interactive password input, and confirmation. Broad exception handlers may also convert cancellation into an ordinary authentication failure.
- **Needed work:** define intended cancellation outcome and propagate the caller token through the supported input/authentication path, with correct session/buffer cleanup. Preserve the protocol's existing cancellation-admission boundary rather than promising interruption of an admitted device exchange.
- **Verification:** meaningful checks for cancellation before/during supported interactive input and before validation, plus cleanup on cancellation. Redirected synchronous input has separate limitations to investigate if included.
- **Documentation dependency:** re-review sample cancellation comments and any task-level cancellation guidance after the fix. Do not describe the current omission as a deliberate limitation.

OATH-003-F10 extends the same follow-up to delete, rename, single-code, and reset handlers: broad exception catches swallow cancellation while the bulk-code path propagates it. Reconcile CLI exit/message policy across these operations and account for reset's partial-success boundary (IMP-010).

## DEC-001 — Authentication-failure provenance and session state

- **Source:** OATH-001-F08 / E2.
- **State:** provenance correction implemented in `d74cdd44` / `57e0fa4d`, with documentation reconciliation in `76a8ae52`. Device-HMAC mismatch is `BadResponseException`; device-reported rejection retains status and neutral wording. Broader post-verification-failure session policy remains deferred; this is not a complete closure of DEC-001.
- **Priority:** consequential error-contract accuracy.
- **Proposed owner:** SDK maintainer with protocol/firmware evidence.
- **Locations:** `OathFailureReason.WrongPassword`, `OathException.GetMessage`, `OathSession.ValidateAsync`.
- **Observation:** device-HMAC mismatch and status words `0x6984`/`0x6A80` map to an error asserting the user's password is incorrect. The published VALIDATE table describes those status words as authentication not enabled and wrong syntax. Source/test inspection alone does not establish actual mappings across firmware.
- **Decisions:** distinguish confirmed user-key rejection, failed device authentication, and malformed/protocol responses; decide session usability after failed device verification.
- **Needed evidence:** authoritative vendor clarification or applicable firmware observations, existing rationale if any, and targeted failure-path tests. Do not treat fakes encoding the current assumption as independent firmware proof.
- **Documentation dependency:** the existing narrow WrongPassword explanation remains an open release-documentation issue. After deciding behavior, update exception documentation/messages, authentication method contracts, examples, and relevant migration guidance together.
- **Scope boundary:** the audit has not established that the current behavior is a deliberate deviation or that changing the mapping alone is sufficient.

## DEC-002 — Accepted raw access-key contract

- **Source:** OATH-001-F01 / E1.
- **State:** unresolved public-contract question; decision deferred to pilot closeout.
- **Priority:** ordinary API precision.
- **Proposed owner:** SDK maintainer.
- **Locations:** `IOathSession.ValidateAsync` and `SetKeyAsync`.
- **Observation:** implementation checks only nonempty keys, while the normal password workflow derives a 16-byte key. Lack of a length check does not prove broad device support.
- **Decision:** whether the public contract promises only standard password-derived keys or intentionally supports a broader raw-key interface, and under which device constraints.
- **Documentation dependency:** the current docs batch can explain the normal DeriveKey workflow and ownership now. Avoid unsupported arbitrary-length promises or new input restrictions. Re-review these members after the decision.

## IMP-003 — Make credential-secret ownership consistent across PUT

- **Source:** [OATH-002-F02 and partial F01](runs/oath-002/findings.json).
- **State:** closed by upstream #726 (`6ca266db`), imported in `361567eb`; verified by the passing post-merge OATH unit suite.
- **Proposed owner:** OATH credential implementation maintainer.
- **Observation:** shortening/padding returns the original Secret array for lengths 14 through the algorithm block size. PUT clears that result in finally, so caller-visible data is consumed only for those lengths. Other lengths use a temporary array. Reusing the creation object can consequently send cleared key bytes.
- **Resolution:** initialization clones caller input; Secret returns a borrowed read-only view; processed-secret storage is independent and PUT preserves the owned seed for retry. Disposal clears owned storage/views, not caller input or independent copies. Post-disposal/concurrent use is unsupported, not guarded by a new exception guarantee.
- **Verification:** new input-isolation/disposal/boundary tests and PUT→Locked→VALIDATE→PUT regression coverage executed in the 167-test OATH suite. See the upstream reconciliation report for named evidence and limits.
- **Documentation:** invalidated direct-array-aliasing prose was reconciled in the README and XML contracts, preserving separate caller-input cleanup and disposal responsibilities.

## IMP-004 — Clear decoded secret arrays on parsing failure

- **Source:** OATH-002-F03.
- **State:** closed within the identified paths by upstream correction plus `8503fe2d`, integrated in `eba72a81`. New shared decoder tests observe actual destination clearing on failures, including a sensitivity RED. Private abandoned array/ParseUri storage is supported by delegation/finally inspection rather than direct storage observation.
- **Proposed owner:** OATH credential/parser maintainer.
- **Observation:** URI parsing decodes before later algorithm/integer/period validation; exceptions leave the decoded array without explicit cleanup. Base32 can also throw after partially writing its result.
- **Resolution in source:** non-secret validation runs before decoding; decoded temporary output is cleared after cloning into the object; partial output is cleared when SDK/OathTool/Cli.Commands decoding throws.
- **Verification:** meaningful partial-output clearing regressions and unchanged-overlap-admission checks passed; separate decoder admission and failure-cleanup ownership is documented. No claim that private abandoned URI arrays were directly observed.
- **Documentation:** current cleanup descriptions match source. This status distinguishes code correction from observed failure-buffer cleanup; it does not imply callers can dispose an instance that parsing never returned.

## IMP-005 — Validate Base32 encoding shape in all decoder paths

- **Source:** OATH-002-F05.
- **State:** closed in `8503fe2d` / `eba72a81`; shared Core decoder validates terminal shapes across all three callers, with explicit overlap/cleanup contracts and independent re-review. Permissive unused pad bits are preserved.
- **Proposed owner:** OATH and OathTool parser maintainer.
- **Observation:** SDK, OathTool, and `src/Cli.Commands/src/Oath/OathHelpers.cs` decoders omit terminal-quantum validation; input `A` reaches an empty decoded result, and subsequent SDK padding can produce an all-zero processed key. This is a source-derived path, not a hardware observation. Upstream cleanup changes do not reject this input.
- **Needed work:** reject invalid encoding shapes consistently across all three paths; decide optional canonical pad-bit rejection separately from invalid-length rejection. Avoid separate fixes drifting between SDK and CLI decoders.
- **Verification:** exact RFC vectors, padded/unpadded cases, impossible terminal lengths, embedded invalid characters, and failure cleanup coordinated with IMP-004.
- **Documentation dependency:** Base32 support/conformance statements and URI examples. The narrow claim about accepting literal spaces is corrected in the docs batch without blessing malformed input.

## IMP-006 — Define valid creation identifiers and preserve metadata round trips

- **Source:** OATH-002-F06.
- **State:** closed for outgoing SDK identity validation in `8503fe2d` / `eba72a81`; exact64-byte UTF-8 budget and metadata round trips enforced before PUT/rename. Incoming parsing preserved. Fake-wire tests verify no rejected command transmission; no new device-range matrix is claimed.
- **Proposed owner:** OATH credential identity maintainer.
- **Observation:** names such as `60/demo` or names with colons can be reinterpreted as period/issuer metadata. Empty issuer and direct zero-period construction also produce problematic encodings; responses with recognized zero-period prefixes are omitted by the existing parser.
- **Needed work:** define valid components and creation-boundary checks, distinguish raw device IDs from the host convention, and decide treatment of ambiguous names and empty issuer.
- **Verification:** round-trip and pre-transmission behavior for ambiguous delimiters, empty issuer, invalid periods, and ordinary IDs; include applicable protocol name-length constraints when defining the boundary.
- **Documentation dependency:** Name/Issuer/Period, GetId, ID format guidance, and creation examples. Do not change permissive response handling merely to hide invalid outgoing requests.

## IMP-007 — Validate numeric creation values before wire encoding

- **Source:** OATH-002-F11.
- **State:** closed for identified narrowing/omission in `8503fe2d` / `eba72a81`; SDK request bounds6..8digits, positive TOTPperiod, nonnegative Int32HOTPcounter enforced with exact-wire boundaries. Wider counter API and URI policy remain deferred; no exhaustive hardware range characterization is claimed.
- **Proposed owner:** OATH credential encoding maintainer.
- **Observation:** Digits narrows from int to byte without validation, and negative HOTP counters omit IMF rather than representing or rejecting the requested value.
- **Needed work:** establish device-supported ranges and reject unsupported values before encoding; distinguish Google URI syntax from device capabilities. Resolve IMF range without assuming the protocol's four-byte field automatically settles signedness or the public API shape.
- **Verification:** boundary digit/counter values, negatives, overflow/narrowing, and exact outgoing bytes.
- **Documentation dependency:** numeric property contracts, parsing exceptions, and URI conformance policy in DEC-003.

## DEC-003 — Define provisioning-URI validation policy

- **Source:** OATH-002-F04.
- **State:** contract question deferred to pilot closeout.
- **Proposed owner:** SDK maintainer.
- **Observation:** parser accepts missing HOTP counter, differing issuer representations, extra component colons, and digit values beyond the cited Google's 6/8 syntax. Tests explicitly expect some permissive cases.
- **Decision:** which are supported extensions, invalid inputs, or inherited behavior to remove in v2. Distinguish REQUIRED format conditions from SHOULD guidance. Merely referencing a format does not automatically promise strict rejection of every nonconforming input; settle the intended parser contract before calling these deliberate deviations.
- **Documentation dependency:** ParseUri format/exception contract and migration guidance, coordinated with IMP-006/IMP-007. Do not simply weaken the spec reference to legitimize unchecked behavior.

## DEC-004 — Establish the provenance of fourteen-byte storage padding

- **Source:** OATH-002-F09.
- **State:** rationale unresolved; terminology correction proceeding; decision deferred to closeout.
- **Proposed owner:** protocol/firmware maintainer.
- **Observation:** SDK pads stored secrets to 14 bytes; another SDK corroborates the practice, but the consulted YKOATH PUT page gives no minimum and RFC 2104 does not define a 14-byte HMAC minimum.
- **Decision/evidence:** locate historical/device rationale and applicability; decide whether internal constant naming should change. Do not remove padding or claim zero-padding increases entropy based on this documentation review.
- **Documentation dependency:** key-processing rationale. Current docs can accurately describe an SDK padding threshold without asserting an unsupported device or cryptographic requirement.

## IMP-008 — Validate and return the intended full-HMAC response

- **Source:** [OATH-003-F02](runs/oath-003/findings.json).
- **State:** closed in `c79e4c1e` / integration `c90dced1`; digit header excluded, required structure checked before completion, caller ownership documented. Exact SHA1/SHA256/SHA512 vectors passed on both available firmware versions separately; no fixed/future digest-length guarantee is made.
- **Proposed owner:** OATH calculation maintainer.
- **Observation:** full CALCULATE TLV values include a digit-count byte before HMAC bytes. CalculateAsync returns that entire value and resolves completion after tag presence without semantic length validation, conflicting with its HMAC-only description. Empty values can reach successful resolution.
- **Needed work:** define a compatible full-response parser, validate required structure before successful notification, return intended HMAC data, and document caller ownership. Credential currently carries no hash algorithm, so do not hard-code a single digest length without a supported contract.
- **Verification:** realistic full-response vectors and malformed/missing values; replace four-/one-byte fixtures that encode unrealistic success behavior; cover callback outcomes and sensitive output cleanup.
- **Documentation dependency:** CalculateAsync public return contract and contributor validation guarantees. The current public HMAC-only claim remains a release-documentation dependency until implementation is reconciled; do not redefine metadata-inclusive bytes as an intended API merely to close the audit.

## IMP-009 — Validate required LIST and aggregate response structure

- **Source:** OATH-003-F03.
- **State:** closed for identified required-structure gaps in `c79e4c1e` / `c90dced1`. Known malformed data is rejected before host-name omission; valid tested extensions remain supported. Existing unknown named-result and duplicate semantics are retained, not certified as a new normative policy.
- **Proposed owner:** OATH response-parser maintainer.
- **Observation:** LIST assumes a required algorithm byte; aggregate handling silently drops incomplete pairs, skips wrong name tags, maps unknown tags to null, and recalculates some custom-period entries before validating their initial values.
- **Needed work:** distinguish malformed required structure from supported extensions; define duplicate/unknown-field policy consistent with repository forward-compatibility guidance. Preserve malformed recognized-period isolation separately from generic structural failures.
- **Verification:** well-encoded empty TLVs, odd pairing, missing/wrong required tags, default/custom periods, unknown extensions, and duplicate identifiers.
- **Documentation dependency:** completeness/null/exception contracts and code examples. Passing generic TLV parsing is not proof of OATH semantic validity.

## IMP-010 — Handle reset's post-command refresh failure

- **Source:** OATH-003-F07.
- **State:** closed by the user-approved terminal-reset contract in `d74cdd44`, integrated in `57e0fa4d`. Successful RESET ends plain/SCP sessions without SELECT or recovery; caller closes/reopens. Required caller/helper migrations and documentation passed fresh independent reviews. Live plain-channel terminal reset passed on125; live SCP remains unverified because initialization returns6A88 beforeRESET.
- **Proposed owner:** OATH session maintainer.
- **Observation:** destructive RESET can succeed before a separate SELECT is cancelled or fails, leaving device data erased while session salt/identity/state remain stale.
- **Needed work:** decide session invalidation/recovery and error reporting after refresh failure. Investigate reset under SCP from primary/vendor/device evidence; another SDK's re-establishment is a lead, not proof of failure here.
- **Verification:** cancellation between exchanges, SELECT failure/malformed initialization after reset, subsequent session use, and supported secure-channel reset behavior.
- **Documentation dependency:** reset failure is not rollback; a short factual partial-success note is permissible now. Re-review reset/session recovery guidance and CLI failure output after implementation.

## IMP-011 — Clear the raw aggregate-calculation response

- **Source:** OATH-003-F09; related to IMP-001 but a distinct operation.
- **State:** closed in `c79e4c1e` / `c90dced1`; raw aggregate cleanup encloses parsing and asynchronous followups, with success/failure/cancellation regression evidence. Returned immutable strings and lower-level copies have separate lifetimes.
- **Proposed owner:** OATH calculation maintainer.
- **Observation:** CalculateAllAsync disposes TLV copies but does not clear the original response bytes containing calculated outputs, unlike individual calculation siblings.
- **Needed work:** failure-safe original-buffer cleanup throughout parsing and custom-period followups, without corrupting returned values.
- **Verification:** success, malformed responses, cancellation during followup admission, and recalculation failures. Immutable returned strings have a separate lifetime and must not be described as zeroable buffers.
- **Documentation dependency:** transient-output clearing/security promises; coordinated with IMP-008/IMP-009.

## DEC-005 — Define supported calculation timestamp bounds

- **Source:** OATH-003-F08.
- **State:** resolved in `d74cdd44` / `57e0fa4d`: nonnegative Int64 Unix seconds; TOTP exclusive-end windows must fit Int64; no calendar-year cap; HOTP timestamp remains metadata. Exact boundary/future tests and live RFC vectors on125 passed. Documentation re-reviewed in `76a8ae52`.
- **Proposed owner:** SDK maintainer.
- **Observation:** signed division truncates negative timestamps instead of RFC 6238 floor; next-window bounds can overflow near Int64 maximum. No domain is documented or validated.
- **Decision:** supported timestamp range and consistent validation/computation in single, bulk, and formatting paths. Negative timestamps need not be supported, but should not silently be claimed RFC-conforming without a policy.
- **Documentation dependency:** timestamp units/default can be clarified now; range/exception guarantees and exact-window claims for extremes depend on this decision.

## DEC-006 — Establish HOTP operation and marker details

- **Source:** OATH-003-F01/F06 and investigation gaps.
- **State:** device-specific evidence incomplete; deferred to closeout.
- **Proposed owner:** protocol/firmware maintainer.
- **Needed evidence:** counter advancement for full versus truncated individual CALCULATE, failure-time consumption, and aggregate marker precedence for credentials that are both HOTP and touch-required.
- **Current support:** RFC 4226 describes HOTP counter advancement and integration-test source expects successive outputs; the consulted YKOATH page does not settle all of these operation/failure details.
- **Documentation dependency:** make HOTP's state-changing nature explicit once scoped evidence is established; do not imply failed calls cannot advance a counter or that a touch marker universally proves TOTP. Timestamp metadata and the bulk null-result contract can be documented now.

## IMP-012 — Make help dispatch non-mutating

- **Source:** [OATH-004-F07](runs/oath-004/findings.json).
- **State:** closed in `618adbec` / `c90dced1`; help/argument validation runs before bootstrap, respecting option values and `--`. Fake-boundary regressions prove no discovery/mutation callback; the defective physical reset-help path was never executed.
- **Proposed owner:** OathTool command-dispatch maintainer.
- **Observation:** reset dispatch ignores help flags; `reset --help` reaches discovery/confirmation and `reset --help --force` can reach destructive reset without confirmation, despite advertised command-help syntax.
- **Needed work:** define help precedence/unknown-flag handling and recognize help before discovery or command execution.
- **Verification:** no-device fake-dispatch tests establishing that supported help forms cannot invoke mutation, regardless of force or option order. Do not use a physical reset to reproduce this.
- **Documentation dependency:** runtime usage strings and invocation guidance must be corrected with the dispatch behavior. The advertised universal help form remains an open release-documentation issue, not an accepted contract.

## IMP-013 — Distinguish boolean and value-taking CLI options

- **Source:** OATH-004-F08.
- **State:** closed in `618adbec` / `c90dced1`; arity, ordering, escaped positionals, sanitized diagnostics and raw seven-digit forwarding are verified. No URI-specific range policy was imposed on raw add.
- **Proposed owner:** OathTool argument-parser maintainer.
- **Observation:** GetPositionalArgs consumes the next non-dash token after any flag, so boolean options can swallow credential names and shift positional arguments.
- **Needed work:** model option arity and define supported ordering; coordinate with help/cancellation behavior rather than adding undocumented ordering restrictions to prose.
- **Verification:** boolean/value options before, between and after positionals; missing/unknown options; examples for add/delete/rename/URI commands. Preserve command behavior intentionally rather than mirroring the current parser.
- **Documentation dependency:** parser comment, usage strings, and operation examples.

## IMP-014 — Require valid SELECT initialization metadata

- **Source:** OATH-004-F11; related semantic-validation principle to IMP-009.
- **State:** closed in `c79e4c1e` / `c90dced1`; required version/device ID and conditional challenge structure checked before state publication, with failure-binding cleanup tests. Both available keys supplied the expected metadata in separate live runs; no complete firmware matrix is claimed.
- **Proposed owner:** OATH backend/session maintainer.
- **Observation:** missing version/name tags leave default version and empty salt; short version data is indexed without validation. Empty successful SELECT can publish initialized state and a shared empty-salt-derived identifier.
- **Needed work:** define mandatory fields/lengths and deliberate malformed-response errors, including construction cleanup; preserve unknown extension tags. Resolve duplicate-tag policy and authentication-algorithm metadata separately if needed.
- **Verification:** absent/short/duplicate mandatory fields, conditional challenge handling, extension tags, identity/state publication, and cleanup after failed construction.
- **Documentation dependency:** initialization, device identity, effective version, and failure contracts. Do not advertise missing required metadata defaults as supported device behavior.

## DEC-007 — Establish OATH secure-channel gate provenance

- **Source:** OATH-004-F12.
- **State:** actual source gate confirmed; hardware/policy rationale unresolved; deferred to closeout.
- **Proposed owner:** SDK maintainer with vendor/firmware input.
- **Observation:** OATH enforces effective firmware5.6.3 for any SCP parameters. Core gates SCP03at5.3.0 and SCP11at5.7.2. Published vendor material gives generic5.3.0 SCP03 support without explaining an OATH-specific5.6.3 floor.
- **Evidence:** [technical manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-scp.html), [legacy SDK SCP guide](https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/secure-channel-protocol.html), OathSession/ScpInitializer source. Legacy example syntax is not v2 API authority.
- **Decision:** hardware minimum, workaround, or inherited SDK policy; define supported compatibility before changing the gate. Do not lower it based on generic support prose alone.
- **Documentation dependency:** distinguish device requirements from SDK/effective-version gates now; update factory/version guidance if the policy later changes. Remove unnecessary NEO physical-firmware shorthand rather than inventing a supported NEO contract.

**New direction/evidence:** The user authorizes Python sibling-SDK firmware checks as the policy reference. At local revision `4ca60f706af930459138d8dc0f0f953480e1c7a4`, `yubikit/oath.py:279–282` rejects SCP for `5.0.0 <= version < 5.6.3`, and rename requires5.3.1 at438. This establishes the reference policy for supported YubiKey5 devices. The shallow clone does not establish historical hardware rationale, and this policy is not proof of earliest physical support. Implementation review will preserve the distinction and Core's separate SCP gates.

## DOC-001 — Supply a verified Linux PC/SC setup reference

- **Source:** OATH-004-F04.
- **State:** closed by reviewed documentation commit `244df5b1`. PC/SC runtime package names, socket activation and reader prerequisites are documented and linked from OATH. Package/activation sources were verified; existing Arch packages and listening socket were checked read-only. Ubuntu/Fedora installation execution is not claimed.
- **Proposed owner:** platform documentation maintainer.
- **Needed work:** identify supported distributions and verify their PC/SC service/driver packages and activation instructions. The current Linux guide only covers HID-oriented permissions.
- **Documentation dependency:** OATH can accurately state its PC/SC prerequisite now; add a concise verified setup link when available. Do not invent package/service commands during the audit.

**Closure evidence:** independent review `ses_e617a8c6effeN3ia5UHGECkfV3` passed; `dotnet toolchain.cs -- docs-qa` passed in run `20261103-000656-299-docs-qa`. No package or service configuration was changed.

## End-of-pilot review

### Additional verification blocker: CORE-001

The first live implementation run excluded healthy key9681620 during two-key discovery after an abandoned HID identity read. Its OATH lifecycle succeeds with125 unplugged. This is tracked separately in [discovery-blocker.md](runs/fixes-wave-001/discovery-blocker.md); root cause remains unconfirmed and Core's ownership guard was not bypassed. It is not a reason to label5.4.3 OATH unsupported or to count skipped hardware rows as verified.

For each entry, assign an owner, decide implementation timing, and identify the documentation units invalidated by the eventual change. Record implementation/review/test references and the resulting revision when resolved.

Use the pilot's actual results to choose the future scheduling policy:

- Which findings blocked truthful documentation?
- Which fixes could have run independently without conflicting file ownership?
- How much documentation re-review did deferral create?
- Did immediate fixing risk turning a bounded docs audit into a broad implementation project?

Until that review, continue verified documentation work and keep unresolved consequential claims visible in the ledger. A deferred entry is not a resolved finding or release sign-off.
