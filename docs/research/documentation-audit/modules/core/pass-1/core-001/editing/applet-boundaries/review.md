# Independent applet-boundary content and citation review

**BOUNDED MANDATORY CORRECTIONS — not PASS.** Correct the two items below, regenerate the
editor's affected identities/decisions, and obtain a bounded re-review. No implementation change,
additional applet audit, device run or new research campaign is requested.

Reviewed worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries`.
Baseline HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Runtime/product pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Reviewed product diff SHA-256:
`6c3797572fc9dcc7cb7fff70f4f1122f2736101462a43289ff95f7085a71a568`.
This identifies the reviewed bytes; it is not acceptance of the batch for publication.

## Mandatory corrections

### R1 — Repair the exact retained-original citation locators

**File/section:** `citations.md`, GlobalPlatform SCP03 and SCP11 rows (lines 9 and 11).
**Candidates:** CORE-MGMT-X002; CORE-SD-X006/X007; CORE-OATH-X003; shared B6-C011/C012.

The named retained extractions do support the consequential statements, but the stated line ranges
do not locate all of those statements:

- `investigation/B6/sources/scp03-1.1.2.txt:1149–1173` ends before §6.2.6. In the actual retained
  derivative, the heading is at **1177**, the C-DECRYPTION **and** C-MAC condition at **1179–1183**,
  and the counter/absent-command-data requirements at **1184–1194**. Cite that actual range,
  page 29/§6.2.6, rather than the earlier figure/footer range.
- `investigation/B6/sources/scp11-public-review-1.3.0.13.txt:1085–1119` starts in the preceding
  table and stops before the b-only authentication statement. Cite **1108–1134**, page 24/§4.3:
  a/c's certificate plus first correct-MAC APDU is at **1111–1115**, SD receipt authentication at
  **1123–1126**, and the mutual-versus-b-only distinction at **1127–1128**.

Keep the existing edition/status/actor qualifications: SCP03 1.1.2 Public Release is not the
uninspected 1.1.2.6 Public Review; SCP11 1.3.0.13 Public Review is not final 1.4 or a complete YubiKey
profile. Do not copy the old registers' broader unqualified R-MAC summaries while fixing these
locators. R-MAC selection matters (§6.2.5/preceding security-level context); §6.2.6 expressly requires
both command confidentiality and integrity. No change to a product claim or protocol implementation
is necessary for R1. Historical registers remain immutable; this review supplies corrected locators.

### R2 — Finish the already-proposed retained reset-comment correction

**Files/sections:**

- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs:112–118`
  (`PutKeyAsync_WithStaticKeys_ImportsAndAuthenticates`, finally).
- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs:209–214`
  (`PutKeyAsync_WithReplaceKvn_RotatesKey`, finally).

**Candidate/source anchors:** CORE-SD-X008, also X004; primary
`boundaries/SecurityDomain/candidates.md:69–75` explicitly proposes correcting lifecycle comments
that promise restoration; `test-oracles.md:39,42–43,52–54` distinguishes the request from readback.

Both retained comments say to “Restore factory defaults so later tests and suites can still
authenticate”. The actual finally only awaits `ResetSecurityDomainAsync` with the existing test
token. Neither method verifies factory contents after that request. The rotation comment also says
the rotated set replaced defaults, whereas Session 2 replaces the original **custom** KVN imported
in Session 1. These are local reset/provenance/oracle statements, not optional style changes.

Describe an **attempt/request** to restore the default-key precondition, preserve why later tests
need it, and state that this cleanup does not assert restored factory contents. Correct the
original-custom-set provenance in the rotation comment. Preserve all code, tokens, assertions,
test names and runtime output. The independently timed Track teardown remains another attempt,
not a verified restoration. Source: `SecurityDomainSession.ResetAsync:768–804` skips empty listings,
finishes raw attempts and reinitializes without factory-content verification; `BlockKeyAsync:861–903`
can exhaust its limit without throwing. Vendor last-SCP03-set restoration guidance does not prove
every mixed-key/reset outcome. Update these two paths' retain decisions and CORE-SD-X008's
documentation disposition; do not allocate another runtime finding or repair cleanup behavior here.

## Content conclusions and candidate disposition

All 33 local groups have an explicit documentation/hold/referral disposition in
`decisions-and-referrals.md`; their engineering, policy and qualification facets are not closed by
this review. R2 is the identified exception to the blanket “no additional correction” retain reason.

| Groups | Review conclusion within this boundary |
| --- | --- |
| CORE-MGMT-X001–X003 | Fixture initialization is a separate live read, not discovery metadata. Captured decoder default, qualifier precedence, override policy and timeout aliases are distinguished. The direct ProbeSession constructor does not bind a guard. Reset/rediscovery and SCP setup-versus-lock authority remain bounded. |
| CORE-SD-X001–X008 | The default 5.3.0 fails Core's SCP11 setup gate; the new typed method supplies known firmware without claiming detection. Reset drops configuration/version while retaining parameters. Contained key/certificate identities and record aliases are described, not deep-cloned ownership. GET DATA's hidden response owner, actual Le bounds and logical STORE fragmentation remain distinct. a/c's first-MAC phase does not establish administrative authority from a bool/public listing. R1/R2 remain required. |
| CORE-OATH-X001–X006 | Cached observations and cleared borrowed Salt views match source. Whole-task lifetimes cover aggregate followups, provider/VALIDATE/retry and callbacks. The console preset counts UTF-16 code units, not PIN/profile or firmware byte enforcement. Synthetic reset comments now identify their cleanup oracle. Successful RESET stays terminal for plain/SCP sessions; borrowed connections remain caller-owned. None of the fourteen historical product IMP resolutions is reopened. |
| CORE-OTP-X001–X009 | First-report protocol firmware, subsequent cached status and effective overrides remain separate. Polling thresholds are not native call deadlines. CRC/prefix ownership and the outer one-shot HMAC teardown-failure result gap are accurately scoped. Layout support, named modern Management relocation and absent typed legacy/FIPS operations do not assert transport parity or device qualification. Configuration/source/GetConfig ownership and sample pre-try defects remain held. |
| CORE-HSM-X001–X007 | RESET reuses the existing graph; re-SELECT is unprotected and creates no fresh handshake/PC/SC transaction. Version fallback/override and historical auth flags are distinct from live support/authority. Final CALCULATE response cleanup reaches the real owner only after entry into its try; three parsed key arrays and earlier transport/fragment/decrypt copies remain separate. Strict 48 versus observed supplied-cryptogram 56 and external HSM authentication stay unresolved. The retracted retry-message mismatch remains retracted: helper and asymmetric assertion agree. |

The review read the complete diff, narrative/example contexts, all 107 inventoried bodies,
candidate/referral/source records, and the affected caller/implementation/assertion units, including
candidate-associated retained reset, ownership, backend and test-oracle context. This is not a new
whole-module redisposition of all retained files or an independent re-execution of historical results.
No source-comparator agreement is credited as hardware, profile or connector qualification. Generic
Core algorithm/credential catalogs establish no additional applet PIN or HSM point/trust enforcement.

## Fresh static evidence

Independent read-only checks, without running the editor's record-writing collector, established:

- All **233** before/after path identities match `path-decisions.json`; **37 edited / 196 retained**.
- All **27 edited C# paths** have exactly the same non-full-line-comment lines, including imports,
  directives, declarations, defaults, attributes, assertions, literals, runtime help and bodies.
  Inspection of the actual diff confirms these changed comment lines are not inside executable
  multiline literals. Recorded final-newline additions do not alter executable tokens.
- The actual diff with explicit `c/` and `w/` prefixes matches the exact SHA-256 above. A default-prefix
  diff has different bytes solely because the prefixes differ; it is not a different product revision.
- All **107** example bodies, UTF-8 body hashes and inclusive source ranges match actual files.
  Only OATH getting-started and the added SD SCP11b method are new/changed bodies. Classifications
  distinguish composable/test-member/prompt/method/implementation/adapter bodies and non-C# material.
- Edited XML comment blocks parse as XML fragments. `git diff --check` returns zero.
- All **29** excluded paths match their recorded raw identities and normalized Git baselines; no
  project/API-snapshot edit was smuggled into this batch.
- All **105** primary input identities still match `input-identities.json`. Current primary A/C
  product comments were not substituted for this worktree's original base source.

The identity manifest binds these statements to the supplied editor records and exact retained
standards derivatives. These checks do **not** resolve C# cref binding, compiler/analyzer diagnostics,
consumer/example compilation, Markdown anchors/external links, native release or runtime behavior.

## Outside-owner needs and remaining gates

The coordinator must reconcile the existing SHARED referrals before publication: Core/root session,
response/key/prompt and cancellation guidance; `docs/usage/user-interaction.md`; raw-access and
connection-ownership guides; Tests.Shared reset/options/fixture explanations; and active migration
prose/map/gaps/comparison/highlights. In particular retain the named OTP modern/legacy/FIPS split,
SD known-version/reset/actor conditions, OATH retry-provider and terminal-reset ownership, and HSM
reset/response-profile distinctions. Existing DOC-002 remains separate. These are dependencies, not
permission for this reviewer to edit outside its two files or to repair runtime code.

After R1/R2, review the successor diff/ledgers and affected example identities. The coordinator still
owns compiler/analyzer/cref and meaningful consumer/example compilation gates. No build, test,
formatter, native/device/cryptographic/connector operation, index/ref/commit/push or subagent ran in
this review. No fresh framework/API lookup was needed: the source/API questions here were resolved
from current SDK code and the already-retained original language/API references, without new claims
about a changed external library API.

Keep final/review/vendor/ISO gaps and independent crypto/profile oracles separate: in particular no
64-byte agreement input is promoted into an 80-byte KDF-output claim, and no selected-R-MAC rule is
made universal. New Windows delta work is unready/unreviewed, PIV physical deferrals stand, serial
9681620's cause remains unexplained, serial 125's two 6A88 setup failures prove no live secure reset,
and HSM-IMP-008 plus host/peer/connector qualification remain open. Preserve inherited IDs, original
failed attempts and policy questions. Publish Core Pass 1 and its deferred handoff, then stop.
