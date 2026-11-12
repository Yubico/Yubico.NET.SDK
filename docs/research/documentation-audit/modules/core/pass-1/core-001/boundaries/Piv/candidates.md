# Local PIV/Core proposals

These six IDs are local proposals, `core-001:CORE-PIV-X001` through X006. They allocate no
accepted Core IDs and add no duplicate count to inherited/family mechanisms. D means a
source-supported documentation facet for coordinator triage; M implementation, P policy and
Q qualification remain separate. Every product path stays unchanged. `file-dispositions.json`
includes all candidate-associated retained paths with explicit reasons, including existing
truthful passages that need no new edit. The original PIV archive remains frozen.

## CORE-PIV-X001 — Explain card state, selected application and connection scope

**Observed:** PIV has no managed verified-PIN property or PIN-clear operation on session disposal.
`PinPolicy.Once` correctly names card-controlled verification lifetime. `Piv/CLAUDE.md:230`
instead says state may remain “until failure or disposal,” leaving disposal scope ambiguous.
The direct PIV factory borrows its connection. Convenience disposal closes its hidden connection
through Core, whose default is now strict Exclusive with explicit sharing opt-ins.
`AuditV2/PivAuditHardwareReproTests.cs:54–57` still says “Today this signs successfully,” a
historical outcome rather than a guaranteed result at this pin. The test uses sequential handles
in one process, not concurrent shared handles in separate processes.

- **D:** Localize direct/hidden connection ownership, selected-application state and strict-sharing
  migration where users encounter PIN policy and fresh sessions. Retain the useful public
  `PinPolicy.Once` wording. Correct historical assertions' present-tense inference.
- **M:** No new runtime fix proposed by this packet. Preserve #22 / YESDK-1630 mitigation and
  checked native release. This is B1-C001's caller-specific occurrence.
- **P:** Cross-process coexistence/isolation and custom/direct-connection responsibility remain
  explicit application/maintainer choices; shared mode is no isolation policy.
- **Q:** Requested UNPOWER, successful native release, observed device power and observed PIN
  status are four different facts. Preserve PIV physical deferrals and original same-process
  hardware outcomes with their exact OS/device/firmware scope.

**Reopen:** `src/Piv/src/IYubiKeyExtensions.cs:31–63`, `PivSession.cs:66–112`,
`IPivSession.cs:42–49,101–114`, `PinPolicy.cs:28–29`, PIV README “Common operations / Authenticate,”
“User interaction,” “Constraints”; CLAUDE “Known Gotchas”; the AuditV2 test's type/method comments.
Route shared changes to `src/Core/src/Transports/SmartCard/{SmartCardSharing.cs,
PcscNativeResources.cs,SmartCardConnectionFactory.cs}`, Core README “PC/SC sharing policy and
migration,” Core `SmartCardSharingTests.cs:23–28,276–288`,
`docs/architecture/physical-device-model.md`, `docs/architecture/raw-access-tiers.md` and
`docs/migration/v1-to-v2.md`. Re-review after changed sharing/disposition policy, connection
ownership, observed application selection/PIN state, or an exact independent two-process result.

## CORE-PIV-X002 — Put whole workflows outside the per-exchange guarantee

**Observed:** `AuthenticateAsync` has two sends; enable/recovery/disable/reset have several sends
and local state transitions. Explicit sign/agreement may first read touch metadata; auto-sign/
decrypt read required metadata. Request callback, parsed-response resolution and local RSA
unpadding lie outside the APDU exchange guard. Disposal drains an admitted exchange without a
lease over that complete public workflow. This is `piv-001:PIVCORE-002` / B5-C006 localized.

- **D:** Tell callers to await and serialize complete workflows and callbacks before teardown or
  input/key/prompt disposal. Preserve existing no-rollback/PIN-only backup instructions. Explain
  that success can follow token cancellation after exchange admission and that a token is no
  native deadline. No PIV TimedOut result is established.
- **M:** Whole-workflow admission/lifetime coverage and richer failure-state invalidation are
  future implementation questions. Do not silently add an operation lock or transaction here.
- **P:** Maintainer decides whether complete workflows refuse overlap or are caller-serialized,
  and how disposal/reentrant callbacks interact. PIV's callbacks are outside the exchange;
  OTP's callback-inside-guard self-wait mechanism must not be copied as a PIV conclusion.
- **Q:** Existing request-callback cancellation test is pre-wire. Physical touch-dependent
  cancellation, terminal recovery and hotplug remain DEFERRED.

**Reopen:** `PivSession.cs:175–211,293–304,446–568,734–813,834–877,986–1119`,
`AuthenticationProtocol.cs:101–213`, `IPivSession.cs` Authenticate/Reset/PIN-only/crypto XML,
PIV README “Enable, reopen, recover and disable,” “Reset,” “User interaction,” CLAUDE “Session
Shape,” PivTool README callback/terminal-work guidance, and
`TouchNotificationTests.cs:243–268`, `PinOnlyWorkflowTests.cs`,
`PivPinOnlyModeSessionTests.cs`, `PivPinOnlyWorkflowTests.cs` explanations. Shared dependencies:
`ApplicationSession.cs`, `ExchangeGuard.cs`, user-interaction/raw-tiers/applet-public-API guides
and current PIV migration examples. Re-review when admission, callback timing, reset/SCP lifetime
or failure-state policy changes; the code review and documentation review remain distinct gates.

## CORE-PIV-X003 — Preserve each actual response and key-material owner

**Observed:** Crypto/auth/raw GET/Bio helpers clear their actual `ApduResponse` and owned encoded
buffers; parsed results transfer independently. PRINTED decoder clears its input and returns a
separate management-key copy; import clears predecessor encodings. B6-C002's transport/chained/
decrypted copies remain lower-layer owners. `PrivateKeyOperations.cs:283` calls the Curve25519
agreement result a “point,” while the public API correctly says raw 32-byte X25519 result.
`DataObjectProtocolTests.cs:123–124` describes only required Data clearing, whereas actual Clear
also clears SW. Its assertions intentionally inspect Data only.

- **D:** Correct local return terminology and distinguish asserted Data-only scope from full-owner
  source behavior. Retain caller-owned `Memory<byte>` and constructor-source independence.
  Existing public/private key, certificate and peer lifetimes remain complete in examples.
- **M:** Preserve existing PIVIMP-007: response tags are checked without algorithm-specific
  size admission; empty/one-byte successful crypto results are accepted. BigInteger/provider
  hidden copies and GZip/CertInfo admission remain applet/runtime work. No new Core defect count.
- **P:** Decide validation depth and secret-result ownership requirements separately from generic
  public metadata copying and the federal/vendor profile choice.
- **Q:** Actual-array unit observers establish managed owner cleanup, not transport staging,
  native erasure or all-provider cleanup. PIVIMP-009/010 remain separate interoperability/input
  findings rather than reasons to rewrite Core TLV behavior.

**Reopen:** `PrivateKeyOperations.cs:119–158,190–274,278–289,336–374,420–455`,
`DataObjectProtocol.cs:40–112`, `PinOnlyProtocol.cs:71–151`, authentication parsing/finally,
`BioProtocol.cs:79–133`, `KeyProtocol.cs:145–328`,
`DataObjectProtocolTests.cs:104–128`, `PrivateKeyOperationsTests.cs`, PIV README “Security notes,”
`IPivSession` crypto/raw-object/Bio output XML, tool DecryptionResult/README ownership, Core
`ApduResponse.cs`, `Tlv.cs`, `TlvHelper.cs`, `docs/usage/tlv-processing.md` and raw-tiers guide.
Re-review after actual output shape, copy/clear ownership, transfer/resolution ordering or parser
policy changes; retain clean neighboring public metadata fields with their nonsecret rationale.

## CORE-PIV-X004 — Name effective version and source/profile provenance

**Observed:** PIV FD precedes Configure/SCP; override controls effective compatibility, while
detected raw version drives default management-type fallback. Metadata takes precedence.
`PivSessionTests.cs:195–197,548–549` infer physical >=5.8 from a major-zero fixture. Core
implements optimistic compatibility; raw 0.0.1 supplies no physical version floor.
`KeyProtocol.cs:395` calls the no-data attestation command Case1/no-Le despite actual trailing Le.

- **D:** Correct these exact test/internal rationales; preserve actual SDK gate tables and
  existing AES5.4.0/released5.4.2, serial5.0.0/Python5.0.3 distinctions. Name selected applet,
  Management metadata, effective override and deployed firmware separately.
- **M:** No new gate/profile implementation requested. Existing PIVIMP-003 remains open.
  Core catalog support for Brainpool/secp256k1 remains outside PIV's strict wire table.
- **P:** Compliance target, default-on-development policy, modern FIPS/NFC/Bio support and any
  override trust requirements remain maintainer/product decisions.
- **Q:** B4-C006/Q001's exact installed native producer/header/ABI gap remains. B6's final
  SCP03/review SCP11/exact ISO/YubiKey profile gaps remain. No source comparison certifies hardware.

**Reopen:** `PivSession.cs:129–155,213–240,943–984`, `SmartCardBackend.cs:32–60`,
`Features.cs`, `PivSessionTests.cs` cited remarks, `SessionConsistencyTests.cs:327–338`,
`KeyProtocol.cs:393–410`, PIV README Requirements/preview support matrix, CLAUDE firmware section,
`IPivSession.ResetAsync`, tool device-info prose and `docs/migration/v1-to-v2.md` current PIV
version/key-type sections. B4 native source and B6 protocol clauses retain their original owners.
Re-review on a changed gate/default/override/source revision or separately supplied native/profile evidence.

## CORE-PIV-X005 — Align test promises with current owners and oracles

**Observed:** `PivConnectionConcurrencyTests.cs:24–27` says a session refuses concurrent calls;
its body aims to overlap only one large GET exchange. Failure of Throws can bypass the readTask
await. `PivHotplugContentionTests.cs:27–38,71,103–108` assumes unconditional release/no-finalizer
and treats a cancellation token as a bound on native I/O. Checked release can quarantine; token
expiry is observed only at exchange admission. Final loop time is checked between awaits.
`PivSessionTests.ResetAsync_BlocksPinAndPukThenResets` uses Contains, not a full sequence equality.

- **D:** Update those exact explanations. Retain accurate existing qualifications in crypto,
  ECDH, Always, X25519, attestation, reset9A and cross-key tests.
- **M:** Keep complete pending-task cleanup, failure-primary preservation, fixture floors,
  secret/private/certificate disposal, and stronger independent oracles under PIVIMP-008.
- **P:** Decide the desired release-failure oracle separately; “must never ConnectionInUse” is
  not an automatic correctness condition when release is genuinely unproven.
- **Q:** Test source is unexecuted here. Historical86/89 canonical methods and supplemental
  P01–P06 retain their original scope; no new run is credited to those bodies.

**Reopen:** the three exact files/ranges above; `PivMultiKeyContentionTests.cs:271–309`,
`PivDiscoveryContentionTests.cs:50–99`, `PivPinOnlyWorkflowTests.cs:48–63`, decrypt fixture
attributes, import/private-key/certificate cleanup scopes, PIV README “Maintainer verification,”
CLAUDE “Test Infrastructure,” shared `docs/TESTING.md`/Tests.Shared instructions via their owner,
and Core PC/SC/session-concurrency descriptions. Re-review complete methods/helpers after any
oracle/cleanup change; a passing name/timeout wrapper never substitutes for terminal/drain evidence.

## CORE-PIV-X006 — Keep sample policy, buffer and result boundaries visible

**Observed:** PivTool uses Core ConsoleCredentialReader with local digit-filter/length options;
its generated/KDF source arrays remain after copying into disposable owners. CLI parsing and
discovery/file reads precede credential finally blocks. Certificate signature generators borrow
session/key objects and block without the outer operation token. Broad catches convert cancellation
to result errors, sometimes “touch timeout.” Read-only plaintext/result certificates have independent
ownership. Existing PIVIMP-006 owns these sample defects; C owns the generic reader/provider issues.

- **D:** Retain existing truthful PivTool limitations and direct SDK alternative. Keep explicit
  caller-owned prompt/PIN/key/certificate scopes in every example and separate selected metadata
  from live reads. No helper, task host or experimental API is invented.
- **M:** Preserve PIVIMP-005 as confirmed/unfixed: actual tool RSA failed original-message
  verification; Ed25519 verified SHA256(message); direct SDK controls passed. Preserve PIVIMP-006
  cleanup/workflow/cancellation repairs. These are applet/tool findings, not generic Core defects.
- **P:** Digit-only PIN/PUK menu policy and fixed-salt example passphrase derivation do not impose
  a universal Core UTF-8/federal profile. Choose production acquisition/escrow separately.
- **Q:** Existing compiler evidence is historical and not a new execution. Any later validation
  uses current exact project/analyzer inputs and nonzero executed counts; no new tooling project.

**Reopen:** PivTool README “Choose the supported crypto path,” “Credentials, recovery and reset,”
“Find the SDK examples”; `Signing.cs`, `Decryption.cs`, `PinManagement.cs`, `PinPrompt.cs`,
`Certificates.cs` generators and failed-store ownership, `Verification.cs`, `SlotInfo.cs`, all
calling CLI/menu/result scopes listed in file dispositions. Shared dependencies include Core
credential XML/reader options, current user-interaction and applet-public-API guides, and PIV
migration fragments. Re-review after actual algorithm framing, credential acquisition, cancellation
mapping, host certificate/secret ownership or result representation changes.
