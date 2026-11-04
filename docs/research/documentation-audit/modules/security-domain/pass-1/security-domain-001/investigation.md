# Security Domain Pass 1 — investigation

**Checkpoint:** investigation complete; candidates await coordinator adjudication. **Date:** 2026-11-04. **Investigator:** Sol 6.1 (`github-copilot/gpt-6.1-sol`). **Source revision:** `b73efe3813b429b4b8eb2a7e819a6ad7be699d47`.

**Subsequent coordinator errata:** [corrections](corrections.md) rejects C001's proposed four-level testing
link (the original three-level path is correct), replaces E2's chain-serial §7.5.4 pointer with §7.5.1,
and records draft §4.6 SCP11c command restrictions. Original investigation prose below is checkpoint
history, not authority over these adjudicated corrections.

## Scope and method

Read root `CLAUDE.md`; module `README.md` then `CLAUDE.md`; both test instructions and required `docs/TESTING.md`; campaign workflow/style; run plan/inventory. Read **all 38 inventory paths completely**: 11 shipping C#, 15 test C#, five Markdown, two snapshots, three project files, two runner files. All method bodies, setup, assertions, cleanup, comments and fixture data were included. The unit-test crypto fixture is module-local evidence, not an invitation to inspect Core. No Core or other module implementation/documentation was read or edited. No build, test, example execution, hardware, commit, or further agent was used.

Only this report, `coverage.json`, and focused `evidence/` records are investigator-owned. No product documentation, code, assertions, snapshots, project files or configuration changed. Initial status showed coordinator's modified campaign workflow and untracked run records plus the four preserved user paths; these were not overwritten. Read-only shell scripts were used for inventory/counts and narrow section extraction, not editing.

`coverage.json` reconciles every path, eight public families / **96 unshipped API entries**, explicit generated exclusions, active external references, and **12 eligible clean semantic units**. Sampling requirement is `max(3, ceil(12 × .10)) = 3`, spanning API, narrative/examples, and internal/test comments. These are investigator-clean candidates, not independently reviewed or sampled yet. Findings are grouped by consumer task, not counted as independent bugs per affected file.

## Overall assessment

The README already has a useful read-only entry point, disposable SCP03 parameter scope, zeroed rotation input, explicit firmware fallback and caller-owned certificate guidance. Certificate loading has particularly strong local failure/success lifetime assertions. Keep those strengths.

The largest improvements are explaining **what grants management authority**, **what first import and deletion really affect**, and **what reset completion does not prove**. XML is sparse/inconsistent in several actionable contracts; contributor examples have stale signatures and unsafe/missing scopes. Test comments sometimes overstate authentication, disposal or protocol verification. These need focused corrections, not a rewrite into an audit transcript.

Original-source excerpts, exact status/applicability and failed access attempts are in [evidence/sources.md](evidence/sources.md). Final GP versions were not accessible after bounded follow-up; the accessible SCP11 **1.3.0.13 Public Review** is **not** final v1.3. Protocol claims dependent on final/vendor profile remain explicit questions.

## Candidate documentation changes

### SD-C001 — Refresh contributor guidance and runnable examples

**Disposition proposed:** documentation correction, medium priority. **Paths:** module `CLAUDE.md`, tests `CLAUDE.md` / `README.md`.

Observed: module class/private-constructor XML says it will “eventually” expose operations already implemented. Contributor initialization pseudocode uses removed factory/configuration shapes and `SelectAsync` on a backend whose facade call is `SelectApplicationAsync`. Logging snippet passes nonexistent `loggerFactory:` to `CreateAsync`. Several reset examples put the action before the required leading `resetBeforeUse`, use unbound cancellation variables, and create inline `Scp03KeyParameters.Default` without an owned disposal scope. Module “ALWAYS use ArrayPool” conflicts with root's ≤512-byte synchronous stack rule; claimed `clearArray: true` does not match `EncodeKeyComponent`'s actual `Return(keyBuffer)` after explicit used-range zeroing. Test instructions link `../../../docs/TESTING.md`, one level short from `src/SecurityDomain/tests`. Tests guide claims all module tests with a command lacking integration selection. No toolchain execution was performed to settle that last CLI claim; simply label it unit tests unless coordinator verifies the actual command.

Proposed edit: present-tense facade purpose, current signatures or clearly marked sketches rather than stale copied method bodies, typed complete test patterns in xUnit-v2 context, owned parameter scopes, correct testing link (`../../../../docs/TESTING.md`), root-aligned buffer rule and precise helper description. Do not retain runtime pseudo-implementations as compile-ready examples. Do not change executable tests to satisfy prose.

**Reopen:** factory/backend signature, logging setup, buffer cleanup or test-helper signature changes. Any Core logging configuration example must await the dependency answer below or use an explicit application-dependent sketch rather than guess.

### SD-C002 — Reset is destructive, bounded, and not a verified all-keys success guarantee

**Disposition proposed:** important docs correction plus deferred implementation question. **Local evidence:** `SecurityDomainSession.cs:898–1077`, `SecurityDomainSessionTests.ResetAsync_TransmitsRawBlockingApdusAndReselectsApplication`; E1.

Observed: reset lists keys; skips SCP03 component KIDs 02/03; uses KID/KVN 00/00 for SCP03; sends at most 65 raw attempts **per processed reference**, not blindly 65 per physical component. It accepts two named statuses as stop signals, catches and continues ordinary transmission errors, and silently exits the loop at the limit. It then logs completion and reselects without re-reading/verifying factory contents. Empty key information returns early. Cancellation can happen after persistent mutations and is rethrown, not rollback. Unit assertions cover one queued blocked status per reference and reselection/readiness; they do not prove retry exhaustion, every error route, actual reset or complete device restoration.

Module/test contributor prose hard-codes `0x6988` as “security not satisfied” while source uses the symbol `SWConstants.SecurityStatusNotSatisfied`; a module-local exception fixture identifies 6982 with that meaning. Correct to symbolic terms or the supported 6982 wording; do not infer missing constant behavior by opening Core. Tests `CLAUDE.md` also has SCP11a KID **0x10** instead of local ScpKid usage / README **0x11**.

Proposed edit: explain irreversible loss of custom SD keys/data and restoration of defaults as intended **device** behavior, recommend unauthenticated reset session, state bounded attempts and possible partial mutation on failure/cancellation, avoid claiming SDK validates factory state on return. Do not imply resetting unrelated applet credentials or the entire physical key. Retain `SecureChannelException` on post-reset reauthentication with now-invalid retained parameters.

**Pass 2 question:** should exhaustion/all transmission failures/no-key response throw or return a structured incomplete result, and should successful factory state be verified? Needs fake regression cases and later explicit device matrix, not a code fix here.

**Reopen:** ResetAsync / BlockKeyAsync / TryGetResetParameters / raw reset send behavior, vendor reset profile, or default restoration policy changes → README reset/constraints/cancellation, both reset XML contracts, test helper/base explanations, reset tests and AI-docs-guide example.

### SD-C003 — Delete reference is a filter, with destructive wildcard normalization

**Disposition proposed:** important docs correction; one edge case deferred. **Evidence:** full `SecurityDomainTlvEncoding.cs`; `DeleteKeyAsync`; two exact-wire unit assertions; E1.

Observed: zero KID or KVN omits that filter. Both zero is rejected **before** normalization. SCP03 KIDs 01/02/03 normalize to wildcard KID, deleting the entire matching KVN set, not just an ENC/MAC/DEK component. `(SCP03, KVN=0)` passes initial validation, normalizes to `(0,0)`, and produces **empty filter data**; device outcome is not established locally. `deleteLast` is P2=1; not a host-side safety verification. E1 describes last-key deletion forcing default-state reset.

Proposed edit: document nonzero version requirement for safe SCP03 selection, wildcard semantics, KVN-only SCP03 deletion, possible multiple matches, and last-key consequences in both XML surfaces and adjacent README rotation. Do not normalize the empty-filter edge into an endorsed delete-all API.

**Pass 2:** decide reject-after-normalization policy and test `(01/02/03,0)` plus generic KID-only/KVN-only cases. **Reopen:** delete normalization/filter/P2 or vendor semantics → both DeleteKey XML contracts, README rotate/reset/constraints and lifecycle test commentary.

### SD-C004 — First SCP03 import replaces factory defaults even when replaceKvn is zero

**Disposition proposed:** important docs correction. **Evidence:** E1 / vendor Key Sets; SCP03 integration setup and actual negative assertions.

Observed: README says zero adds a version and then instructs deleting the old one. On factory state the vendor says first import replaces the default set. Integration tests assert default-KVN authentication subsequently fails. Their source is evidence of intended test coverage, not an executed hardware result. README's add-then-delete recipe can try to delete a version no longer present, and does not tell users to verify fresh authentication before removing a retained old key. Key set version FF is reserved; vendor SCP03 range 01–FE and AES-128 apply even though module doesn't locally validate every dimension.

Proposed edit: distinguish **factory transition**, **adding alongside existing custom sets**, and **explicit replaceKvn**; escrow before import; authenticate a new session successfully before deleting any old key that still exists. Do not present recovery of overwritten secrets as possible. Source sends `(byte)replaceKvn`; do not claim range enforcement. Include key/certificate metadata operations in persistent-state coverage instead of current README's incomplete mutator list.

**Reopen:** PutKey wire/replace behavior, firmware default-set rules, or lifecycle assertion changes → README rotate and constraints, both PutKey SCP03 XML contracts, test comments and package summary if scope changes.

### SD-C005 — SCP11b channel establishment is not host authorization for SD key management

**Disposition proposed:** important docs correction; Core trust/lifetime details deferred. **Evidence:** module `Scp11b_EstablishSecureConnection_Succeeds` actually asserts `IsAuthenticated` and expects `ApduException` while attempting generation/deletion; accessible original E2 §4.3/7.9 distinguishes card-only authentication.

Observed: README “every operation that writes key material requires an authenticated channel” is insufficient: SCP11b does not authenticate the host. Module sends many write APDUs without a local `IsAuthenticated` check; public EC import does not even call `EncryptData`, unlike private/SCP03 import. Device authorization rejection must not be documented as guaranteed local `InvalidOperationException`. `GetCertificatesAsync` **loads** certificates in received order but does not validate trust, signatures, validity, chain order, or key association. “leaf last” is expected device/store order, not SDK sorting/validation. `StoreCertificatesAsync` stores an SD-key certificate bundle, not the host's OCE authentication chain; the README conflates those roles.

Proposed edit: explain read-only discovery versus SCP03 / suitably authorized SCP11a/c administration; explicitly qualify SCP11b and inherited `IsAuthenticated` terminology. Distinguish device private-key reference, returned/persisted trusted public key, CA-KLOC **public** reference, CA-KLCC identifier, OCE private key/certificate chain supplied by caller, and certificate store for SD key. Public-key provisioning needs an authenticated trust path; retrieval/loading alone is not trust verification. A concise role table and task order are more useful than adding an incomplete SCP11a/c certificate-generation tutorial.

**Reopen:** management authorization/profile rules, SCP11 key parameter trust/ownership, certificate validation behavior → README protocol/setup/security sections, interface/concrete certificate/PutKey/generate XML, and affected SCP11 test comments. Do not claim final-GP conformance from the draft.

### SD-C006 — Fill public contract gaps and align concrete/interface XML

**Disposition proposed:** docs correction, medium. **Evidence:** all declarations, bodies, snapshots and local tests; no API change needed.

Eight public families are enumerated in coverage. `CaIdentifier`, `KeyInfo`, `SecurityDomainKeyInfoComponent`, interface type and five feature fields need purpose/value explanations. Component `Tag` / `Value` are the returned descriptor pair (existing fixture 88/10); explain key type and length where supported without inventing closed enum coverage. No attestation operation is present despite feature descriptor. `None` is not a valid `GetCaIdentifiersAsync` request; combinations Kloc|Klcc are valid; results concatenate KLOC first and absent groups with DataNotFound are ignored. Method XML omits these useful conditions.

Both CreateAsync and device factory should state borrowed versus owned connection, caller disposal of key parameters, SmartCard restriction and exact firmware fallback/gating. Module only directly stores the parameter reference: inherited deep lifetime semantics stay a dependency. Existing module-local disposal tests establish some borrowed-connection behavior, not all Core lifecycle guarantees. `GenerateKeyAsync` returns an **ECPublicKey**, not raw point bytes; point format belongs in the description of its PublicPoint, not as the return type. Private-key import's caller cleanup is buried internally and names `Clear()`; prefer the already-used `using` pattern without asserting an unverified Core alias. Specify replacement zero/nonzero meanings consistently.

Keep concrete/interface contracts in sync, using appropriate inheritance if project conventions permit; don't duplicate boilerplate for generated record equality/deconstruction/operators. Cancellation is forwarded, not transactional rollback. `SecureChannelException` wrapping is confined locally to ConfigureProtocolAsync's selected exception types when parameters are present; app selection occurs **before** that try/catch and cancellation is rethrown. Avoid “every creation failure is wrapped.” StatusWord is copied only from an immediate ApduException, not recursively extracted.

**Reopen:** any corresponding declaration/body/parameter ownership/feature-gating/wrap-filter change → both API surfaces, DTO XML, factory README constraints, and Security Domain migration exception wording/map entry.

### SD-C007 — Allowlist contracts: malformed hex, CA scope, clear consequences, and SCP11c gap

**Disposition proposed:** important docs correction; profile question deferred. **Evidence:** `StoreAllowListAsync` / `ClearAllowListAsync`, interface, audit repro/wire unit bodies, E2/E3.

Observed: empty collection throws ArgumentException before transmission; malformed odd/nonhex string throws **FormatException** from Convert.FromHexString, not documented ArgumentException. Empty individual string is accepted locally. Clear intentionally emits 70/00 and removes the serial restriction, **not** deny-all. Interface omits these consequential remarks. Wording “only listed certificates” is imprecise with an intermediate chain: the accessible original checks CA-directly-signed OCE or first intermediate, not every arbitrary leaf serial. Current methods have no allowlist counter/tag 92 or counter input; accessible draft §7.9 requires that tag for SCP11c. Vendor applicability and final version remain unsettled.

Proposed edit: fix exception XML, describe hex byte order as caller-supplied sequence (do not assume X509 GetSerialNumber little-endian compatibility), retain no-deny-all warning, qualify certificate-chain scope and authorized channel requirement. Do not advertise unrestricted SCP11c allowlist updates; record the profile gap rather than invent a successful recipe. Existing tests assert exact wire and approved-chain acceptance/clear, not unlisted-chain rejection.

**Pass 2:** decide per-entry validation and SCP11c counter/profile support after final/vendor source available. **Reopen:** encoding/validation/counter/authorization or certificate profile change → README SCP11/allowlist, both store/clear XML, audit repro rationale and allowlist integration comments.

### SD-C008 — Raw data APIs describe the selected SD and exact result, not a generic application service

**Disposition proposed:** docs correction plus bounded pass-2 range question. **Evidence:** `GetDataAsync`, `StoreDataAsync`, recognition parser and exact-wire tests.

Observed: GetData casts low two bytes of int to P1/P2 (no 0..FFFF check), rejects negative expected length, forwards requestData/Le. StoreData sends 00 E2 90 00 with supplied bytes; it does not select another application, parse BER-TLV or implement an application-personalization state machine. XML's “or targeted Application” implies more than local code does; existing no-secure-connection exception promise is not locally enforced. Recognition API returns **value of nested tag 73** extracted from GET DATA 66, not an outer TLV encoding; unit asserts AA BB from 73 02 AA BB.

Proposed edit: document low-level scope and caller payload responsibility; say recognition value bytes and parsed list shapes precisely; narrow unverified generic GP citation to a verified reference or emitted command description rather than retain authoritative-looking §11 blanket proof. **Pass 2:** integer truncation/range decisions belong with SD-C012, not a new redesign. **Reopen:** GET/STORE/recognition encoding/parsing or range checks → interface/concrete XML, raw wire tests, contributor command table.

### SD-C009 — Test explanations must match assertions; remove development-history residue

**Disposition proposed:** docs/comments correction plus deferred assertion deficiencies. **Evidence:** every test body read, especially the following.

* `Scp11b_GenerateEcKeyAsync_GeneratesValidKeyAndAuthenticates`: first callback checks point shape and ECDH construction; second callback **omits scpKeyParams** and only checks registered KID/KVN. “Verify generated key can be used for authentication” is false commentary. Correct comment now; later add real authentication assertion if intended.
* `IYubiKeyExtensions_ListKeyInformationAsync_DisposesSession`: asserts nonnull list and a transmission, **not** connection/session disposal. Correct comment; future test should directly observe disposal/reuse semantics.
* Negative tests use `ThrowsAnyAsync<Exception>`; transport/setup/cancellation could satisfy it. Comments should describe expected failure rather than assert a proved authentication boundary or blocked-key coverage (there is no blocked-key case in that class).
* `Scp03HandshakeFakeConnection` calls itself protocol-conformant/independent, yet MAC scope comments explicitly mirror Core, external-auth handler just advances a MAC chain and returns success (does not verify host cryptogram/MAC), and only **AesCmac** has four independent RFC vectors. Narrow to deterministic fixture/scenario plus CMAC-vector checks, not SCP03 conformance. Reinit status property says step index 5 while code rejects at step 6. No Core inspection needed.
* SCP03 creation XML says 5.7.2 while attribute is 5.4.3; “custom non-default” bytes equal vendor defaults but use a different reference. Explain fixed public test material, not unique secrets.
* Replace “upstream asserted”, “this branch”, “demonstrating RED”, “audit 26m”, unrelated suite symptom narratives and “GenerateEcKeyAsync” residue with current rationale. The good certificate-handle cleanup/order assertions and exact-wire comments remain.
* `ScpCertificates` “self-signed” means issuer/subject name equality in code, not signature validation. Its ordering helper is a test-fixture arranger, not a trust verifier. State that boundary.

**Pass 2:** strengthen broad exception assertions, real authentication/disposal tests, and fixture protocol-check claims if desired. **Reopen:** corresponding setups/assertions/fixture semantics → test class/method XML and comments, test README/CLAUDE coverage descriptions. Renaming methods or changing assertions is not authorized in this docs pass.

## Deferred module-local implementation candidates

These are source observations, not executed reproductions or newly confirmed production vulnerabilities. Coordinator should assign stable SD-IMP IDs after triage; they are not silently fixed in Pass 1.

### SD-C010 — Malformed response strictness

`ListKeyInformationAsync` ignores an odd trailing component byte; `GetCaIdentifiersAsync` pairs TLVs without checking tags, discards odd trailing TLV and indexes reference values [0]/[1] without a length guard. Neither preserves a complete raw listing as an explicit DTO field. Desired policy needs a decision: reject malformed/security-critical encoding while allowing future valid identifiers, consistent with root doctrine. Add meaningful malformed-response tests later. **Affected docs/reopen:** key-info/CA API XML and DTO shape docs, malformed-response test descriptions whenever parser/error/raw-result behavior changes.

### SD-C011 — Reset drops supplied configuration and exact firmware source

Creation passes `options.ProtocolConfiguration` and `FirmwareVersionOverride` to InitializeAsync. Reset reinitializes with **only cancellationToken**; local fields retain key parameters but not these inputs, and InitializeAsync resets `_hasExplicitFirmwareVersion` to false / uses 5.3.0 fallback. This local call-flow loss is established without reading base behavior. Whether Core's effective version also remains stale is **not established**. Desired behavior likely preserve supplied reset policy, but implementation is deferred. Public docs can truthfully limit the override's applicability to creation and tell callers to create a fresh session with their options after reset when they need the supplied policy, rather than call the loss an intended permanent contract. **Reopen:** reset option retention/reinit → README firmware/reset, factory XML, reset XML, contributor initialization and firmware guidance; SD-specific architecture/migration version assertions if effective-version semantics change.

### SD-C012 — Import validation and response identity

All three PutKey overloads cast int replaceKvn to byte without range check; GetData similarly truncates object identifiers. Generate accepts supplied KID without local SCP11-family validation. `ValidateKcv` requires at least ten response bytes and compares three three-byte KCVs in fixed time, but does **not** check byte 0 equals requested KVN and allows extra data. Desired range/open-value/response policy and firmware restrictions need a decision; do not pretend module enforces vendor range or validates complete response identity. **Reopen:** validation or KCV response rule → key import/GetData/Generate contracts, README rotation constraints and relevant test explanations.

### SD-C013 — Sensitive test/temporary lifetimes need later cleanup, not new assurances

Library helper stack KCVs and secret-derived AES intermediate aren't explicitly zeroed; fixture DeriveKey makes static-key copies and stores session keys/MAC arrays while Dispose clears only `_staticKeys`; some fake instances aren't disposed. Integration paths create inline default parameters, Scp11 parameter owners, returned X509 certificates and exported private ECParameters without complete cleanup in all branches; ScpCertificates isn't disposable and does not transfer documented ownership. Exact inherited ownership of ECPrivateKey/Scp11KeyParameters remains dependency, so do not prescribe double-disposal based on guesses. Existing plaintext rented key copy **is** cleared in finally; encrypted payload memory is not automatically equivalent to plaintext exposure. Treat each by semantic sensitivity, not a blanket byte-array finding. **Reopen:** sensitive helper/fixture cleanup or parameter ownership → security notes, contributor buffer/parameter examples, local private-import comment, helper/test cleanup comments. Broader security-audit orchestration is outside this assignment.

## Precise Core dependencies (record only; do not investigate now)

The following are candidate SD dependency questions, not separate boundary-review assignments. Source locations are **references by namespace/type known from SD**, not claims that their implementations were inspected.

| Question | Local anchor / deferred Core family | Documentation to reopen on answer |
| --- | --- | --- |
| What does inherited IsAuthenticated mean for SCP11b versus host-authorized SD operations, and which errors escape preparation vs ConfigureProtocolAsync? | SD InitializeAsync / ApplicationSession and SmartCard Scp initializer/processor | README auth/constraints, both factory XML, SecureChannelException XML, migration exception subsection/map; SD-C005/006 |
| Which key/certificate inputs are borrowed/cloned/owned by StaticKeys, Scp03KeyParameters, Scp11KeyParameters, ECPrivateKey, ECPublicKey? What is supported cleanup API? | SD `_scpKeyParams`, private import `parameters.D`, local test parameter construction / Core.Cryptography + Core.Protocols.SmartCard.Scp parameter types | README security/setup, private import XML/comment, contributor examples, test helper cleanup; SD-C005/006/013 |
| Effective firmware/configuration state after SD reset without forwarded options; feature availability versus descriptor values? | SD ResetAsync/InitializeAsync/EnsureSupports / ApplicationSession and Feature | README version/reset, Generate/XML feature fields, contributor guidance, SD-specific architecture/migration statements; SD-C011 |
| Are borrowed/owned connection, lease, cancellation, data lifetime and Le semantics exactly as SD guidance describes? | SD ConstructInitializedAsync/OwnConnection/TransmitAndGetResponseDataAsync/GetData / Core sessions, APDU response/formatter | factory/extension XML, README ownership/cancellation, raw-data XML; SD-C006/008 |
| Supported current global logging setup and whether logs expose APDU/key-derived data? | SD Logger/YubiKit logging usage and stale loggerFactory example / Core logging configuration | contributor debugging snippet, README no-sensitive-logging guidance; SD-C001 |

Core is the final campaign target. These questions remain precise dependencies until then; no src/Core path was opened. Module-local source/tests may support narrower statements now, but they do not close inherited semantics.

## Active external documentation disposition

Narrow tracked-text search for SecurityDomain / Security Domain / SCP03 / SCP11 outside `src/`, research, archives and agent configuration found the references recorded in coverage. Read complete relevant SD paragraphs, table rows, diagram blocks and mapping entries. No active external **SD C# task example** was found beyond the root's already-read illustrative SCP snippet (a Management example, excluded from editing/investigating that module).

Accepted current references: root package row; SD direct Core project-reference edge; migration member/selector names; SmartCard-only mention; package-list mentions. SD-only firmware description agrees with local creation path but is subject to reset dependency. Native AOT link/runtime claims were read as existing evidence limits, **not** independently revalidated here. Diagram generated SVG/PNG copies are explicitly excluded in favor of their Markdown source; no generation was performed.

Potential follow-through: `docs/AI-DOCS-GUIDE.md` SD example should say custom keys removed rather than just blocked (SD-C002). `docs/migration/v1-to-v2.md` Security Domain paragraph can mention SCP11 alongside SCP03 and qualify selected exception wrapping (SD-C005/006); corresponding `securitydomain-exception` map entry should remain synchronized. Do not edit historical changelog, gap-analysis snapshot, dated implementation plans/ISA, docs-inventory report or dated troubleshooting sessions. Do not repair unrelated sibling statements encountered in shared tables.

## Examples and review handoff

README current C# blocks are **composable fragments** sharing imports/device and, for rotation/generation, the preceding authenticated session; reset is a separate task, not a second same-scope session declaration. Validator should wrap each task with complete connection/session/key lifetime and unique host names. Installation shell blocks are commands, not C#; don't claim executing them. Contributor raw constant lists/error patterns are **sketches**; initialization/logging/reset sample fragments are currently stale and should be replaced or corrected under SD-C001. Test-guide snippets are **test-context fragments**, requiring xUnit-v2/helper/reference/analyzer context. API snippets embedded in protocol prose are not standalone hosts.

Coordinator should adjudicate the important behavior/profile questions before the fresh editor starts, assign stable IMP/DEP IDs, and choose at least three eligible clean units in coverage. Review all affected contract copies, not just README. Preserve executable tokens, assertions, API snapshots, build/project settings. This investigation provides no compilation, passing tests, specification certification, OS portability or hardware qualification claim.

## Record verification

Read-only Python reconciliation parsed `inventory.json` and `coverage.json`: **38/38 unique paths**, no missing/extra paths, **zero baseline SHA-256 mismatches** across all 38 product files, public-family entry sum **96**, clean categories **4 API / 4 narrative-example / 4 internal-test-comment**. Fence counts independently checked: module CLAUDE 11, module README 6, test README 4, test CLAUDE 4. `git diff --stat -- src/SecurityDomain` produced no output. These are inventory/content-preservation checks only, not a build or test run.
