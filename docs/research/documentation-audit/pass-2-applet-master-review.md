# Pass 2 applet master — independent bounded Sol review

**2026-11-13. Important correction requested; full semantic acceptance withheld.**

The master is usable for applet-by-applet review, and its coverage ledgers preserve the major
resolved, retracted and shared-owner distinctions. One sampled severity rationale needs correction.
This review does **not** certify every compound facet, proposed AFTER or exact reopening condition in
all 179 card displays. The remaining bounded review groups are listed below; their absence of a finding
here is not acceptance.

## Important correction

### R1 — CTAP-IMP-007: support the High impact rating or reduce it

**Location:** `pass-2-applet-master.md:2558–2564`, especially 2562–2564.
**Original aliases:** `ctap2-001:CTAP-IMP-007`, `core-001:CORE-CTAP-X006`.

The card establishes a real required-label validation gap, but its High rationale stops at
“mislabeled peer material enters token/key agreement.” It does not establish a high-impact consequence
of that gap under the implementation actually inspected. At the pinned C# baseline,
`src/Ctap2/src/Pin/PinUvAuthHelpers.cs:74–96` enforces 32-byte coordinates and imports them with
`ECCurve.NamedCurves.nistP256`; the peer's unchecked kty/crv/alg labels do not select another curve or
derivation algorithm. Provider point validation is retained. The frozen X006 definition explicitly
preserves those same limitations.

For the same accepted X/Y coordinates, changing only the ignored labels leaves the selected P-256
operation unchanged. Missing schema checks therefore do not, by themselves, demonstrate weaker
cryptography, off-curve acceptance, token authorization bypass or high-impact secret compromise.
The card correctly disclaims an exploit and off-curve evidence, but those disclaimers do not supply
the missing impact rationale. This is a prioritization/evidence correction, not rejection of the bug.

**Required correction:** retain the source-confirmed conformance gap and the narrowly scoped
no-brainer label check. Either rate its demonstrated schema/interoperability consequence accordingly,
or state a concrete, bounded high-impact failure path supported by the original evidence. Do not
invent a vulnerability, remove provider validation or start new research/implementation to justify
the existing rating. Preserve the exception and optional-label decisions as unresolved.

**Confidence:** high in the inspected mechanism and missing causal explanation; no new runtime,
provider or attack result. No Critical correction was found in the reviewed scope.

## Scope and evidence boundaries

- Reviewed only the completed master and companion, original module deferred registers, nine inherited
  registers, relevant frozen Core boundary definitions/reconciliation and the additive Windows successor.
  Root `CLAUDE.md` and `records/style.md` supplied the local review rules and voice.
- Parsed headings and ledgers before targeted reads. The complete master was not loaded into one
  context. Reused the editor's static fence/body/link receipt rather than replaying all 377 fences,
  179 bodies, 170 unique named IDs and 337 links as purported independent semantic evidence.
- C# HEAD matched `6ce355468047e8004fd51cf1425808f6e8242ed3`. Python HEAD matched
  `4ca60f706af930459138d8dc0f0f953480e1c7a4` in
  `/home/rupicola/Code/y/yubikey-manager`; its `git status --short` was empty.
  Sample source was read from the named Git objects, not an assumed moving working tree.
- No product/master/companion/frozen/index edits, builds, tests, hardware operations, provider/crypto
  execution, commits, pushes or subagents. This new report is the sole authored file.
- Literal AFTER proposals may remain unapplied and uncompiled. This review does not make compilation
  or implementation tests a prerequisite to accepting a clearly labeled proposal. Schematic alternatives
  are evaluated as intent, not as existing APIs.

### Coverage conclusions

| Check | Result and limit |
| --- | --- |
| Original deferred inventory | Compared original deferred-register rows with all nine local coverage ledgers. No missing original numbered group was identified at this ledger level. Compressed ranges, OpenPGP's intentional IMP gaps and PIV's split groups were not treated as omissions. |
| Core boundary inventory | All 82 original applet boundary groups have local routing: OATH 6, WebAuthn 12, Management 3, CTAP2 23, SD 8, OTP 9, HSM 7, OpenPGP 8, PIV 6. Compared the original boundary definitions' remaining mechanisms with those routes; this is not acceptance of every destination card's complete prose/snippets. |
| Inherited routing | Parsed all nine inherited registers: 13/31/9/26/16/14/15/15/11 rows, 150 nonadditive occurrences. Original source-run identity, local aliases and canonical owners were considered together; missing literal fully qualified strings were not reported as missing identities. |
| Shared-owner closure | No ledger-level orphan was identified. The final index retains mechanism owners; local occurrences supply applet policy. SD-DEP-004, HSM-DEP-002/003, OTP-DEP-001/003, PGP-CORE-002/004 and PIVCORE-001/003 have appropriate canonical destinations, not cross-applet closure. |
| Compound facets and exact reopening | Local ledgers preserve the important named facets and incorporate original path/trigger maps by reference. Selected boundary/register comparisons found no lost trigger. A complete path-by-path semantic join of every inherited reopening map and every destination card was **not** finished; see remaining groups. Links and identity presence alone are not that proof. |
| Required review fields | Read all nine context/review maps with the selected bodies. OATH, Management, SD and OpenPGP legitimately place some judgments in the map; no body-only missing-field finding is made. CTAP/HSM/PIV body fields and shared section conventions are valid field locations. Unsampled body completeness remains unaccepted. |
| Counts/state | The 176 local / 179 displayed distinction, three referrals, all-unstarted user review and all-deferred/uncompiled implementation are consistent with the tracker and companion. No bug total, user acceptance or execution result is inferred. |

The following protected dispositions survive the ledger/reconciliation comparison:

- OATH's fourteen scoped IMP resolutions, resolved DEC-005, and the wider-counter/URI remainder of
  IMP-007. The latter is a residual policy group under DEC-003, not reopening of fixed narrowing.
  Terminal reset remains settled; Python reselect/rehandshake is not its proposed replacement.
- Rejected CTAP-IMP-017 ordering inference; resolved descriptor/User prose; rejected borrowed-CM-token
  leak and wrong legacy forced-change-status subclaims. Native User disclosure remains a decision,
  not a proven vulnerability or automatic field-stripping request.
- HSM's retracted retry-message mismatch, source-functional final-response clearing and unresolved
  historical 56-versus-48 response/real-HSM handshake. No arbitrary-prefix parser fix is endorsed.
- Resolved SD production certificate-bundle partial cleanup. Helper/test/contained-object owners
  remain separate; exact draft/final/vendor authority is not promoted by Python agreement.
- PIV's post-PR745 fixes and response-only pool rollback. No returned-pool-memory assertion or
  replacement pre-return runtime oracle is restored. PivTool framing remains separate from fixed raw
  SDK behavior; SAMPLE-A004 retains distinct PIV/OpenPGP caller anchors.
- Windows supersession is limited to named old built-in HID mechanisms. Managed owner/staging results
  are not PC/SC/native/other-OS/physical closure for SmartCard-only applets. Serial 9681620's two-key
  failure and serial 125's pre-EXTERNAL-AUTHENTICATE/RESET `6A88` failures remain qualified.

### Independent semantic/source sample

OATH's eight card narratives and HSM's thirteen card narratives were read, including their original
policy/qualification distinctions. For the other applets, bodies were sampled as listed below, with
all local maps and coverage ledgers read. This narrative scope is deliberately distinct from exact
source-window and AFTER verification.

| Card/body sample | Actual pinned source inspected | Independent conclusion |
| --- | --- | --- |
| OATH-DEC-001/002; remaining OATH narrative/qualification | C# `OathSession.cs:702–750`; Python `oath.py:333–376,400–411` | Failed device-HMAC verification precedes unlock; raw input is forwarded without a fixed-width Python guard. State/width choices remain decisions. Selected literal BEFORE matches. |
| WA-IMP-011/012/013 | C# `WebAuthnClient.Authentication.cs:97–125`, `RegistrationExtensionSession.cs:45–75`, `PrfCodec.cs:58–90`, `PrfInput.cs:77–103`; Python `ykman/_cli/fido.py:35–44` | Sampled UV forwarding, support filtering and independent PRF copies/concatenation match the claims. Proposed ownership/UV/enforcement flows preserve important boundaries. External fido2 implementation is explicitly uninspected, not attributed to this Python pin. Frozen normative citations were not independently re-researched. |
| MGMT-IMP-010 | C# `ManagementSession.cs:201–218`; Python `management.py:750–767`, `ykman/_cli/config.py:118–152` | SDK firmware/transport dispatch and Python UI family clues are accurately distinguished. Proposed terminal/expert alternatives do not claim unobserved Management-lock erasure. |
| CTAP-IMP-001/007 | C# `PinUvAuthHelpers.cs:41–119`; GetInfo card was reviewed narratively, not source-reverified | Peer-label BEFORE is accurate and the proposed required-label check preserves width/provider validation. R1 concerns severity, not the check. GetInfo's proposed missing/empty/unknown boundaries are clear but not independently source-validated here. |
| SD-IMP-001/002/003 | C# `SecurityDomainTlvEncoding.cs:15–51`, `SecurityDomainSession.cs:844–886`; Python `securitydomain.py:185–221,280–304` | Normalization can produce the empty filter; the literal AFTER reorders the guard safely and leaves legitimate wildcards intact. Reset retry versus status/profile/reauthentication claims are qualified. Allowlist final/vendor/counter questions remain decisions; its other source windows were not reverified. |
| OTP-IMP-001 | C# `HotpSlotConfiguration.cs:37–67`; Python `yubiotp.py:448–464` | BEFORE and Python divisor/range claim match. Proposed range/divisor arithmetic maps 0x30000 to 0x3000 instead of 0x0003, consistent with the stated steps-of-16 contract. No device output was generated. |
| HSM-IMP-001/002/003/007/008 plus remaining HSM narrative | C# `ChangeManagementKey.cs:17–40`; Python `hsmauth.py:210–220,250–263,606–626,695–718` | Acquisition under the literal cleanup scope is plausible and preserves output/borrowed-input limits. Python generic challenge errors, strict label decode, enum/touch differences and nonstrict key slicing match. The HSM response layout and most C# HSM windows remain author evidence, not independently revalidated source. |
| PGP-IMP-001/002 | C# `OpenPgpSession.Pin.cs:20–95,284–326`; Python `openpgp.py:1205–1240` | Identity-only AFTER separates P2=82 from logical User derivation/retry identity correctly. Python does the same. Transition remains a policy/procedure, not a claimed atomic helper. Its other source/KDF windows were not reverified. |
| PIVIMP-005 | C# `Signing.cs:56–114`; Python `piv.py:546–576,1135–1160` | Tool hashes every message before raw sign; Python performs algorithm-specific framing and Ed25519 passthrough. Selected BEFORE matches. Schematic AFTER does not invent a PSS overload or infer X25519 signing from Python passthrough. Historical device confirmations remain inherited evidence. |

No sampled BEFORE mismatch, inaccurate sampled Python attribution or unsafe sampled literal AFTER
was found. That sentence applies only to these windows/proposals, not all author receipts or external
framework/provider behavior.

## Human-review usability and remaining bounded work

The size is substantial, but the applet contexts, maps, local ledgers, anchored cards and shared index
support the requested one-applet-at-a-time workflow. The master explains actors, persistent side
effects, owned versus borrowed data and evidence limits. N/A code for qualification has concrete future
procedures. No preference-only rewrite, arbitrary shortening or whole-document reorganization is needed.
R1 is a material impact-evidence issue, not a voice preference.

**Do not mark the entire master independently complete from this review.** To finish semantic coverage,
the parent can request fresh bounded applet reviews using the following groups. These are review limits,
not newly discovered backlog items, and require no builds, implementation tests, device runs or SDK reaudit.

| Remaining group | Exact bounded objective |
| --- | --- |
| OATH finishing slice | Reconcile all original reopening paths against DEC-003/004/006/007 and X005's remaining test sites. Check their unsampled BEFORE windows and the test-owner proposal. Narrative and protected-exclusion review above need not be repeated. |
| WebAuthn | Review WA-IMP-001–010 and 014–021, whole-ceremony/native-user referral/qualification bodies, and all 31 inherited rows' exact destination/reopening facets. Retain the 011–013 source sample; finish their registration-side/normative coverage only where needed. |
| Management | Review all bodies other than the sampled MGMT-IMP-010, including 17 IMP groups' residual facets, two metadata referrals, X003 and qualification; join the nine inherited keys and exact triggers. Do not reintroduce removed selector or host-string mechanisms. |
| CTAP2 | Finish IMP-001–048 except rejected 017, DEC-002/003 and X020 at card/facet level; R1 and the 007 source window are already identified. Prioritize bio wire, token/parser boundaries, storage/DER/ARKG proposals and destructive reset continuity. Join all 26 inherited rows, not just their ID set. |
| Security Domain | Review remaining IMP-004–008, DEP-001/002/005, X003/X007 and device/producer bodies; finish omitted reset/allowlist windows and all 16 inherited rows' exact reopening join. Preserve resolved production certificate cleanup and final/vendor authority gaps. |
| YubiOTP | Review all bodies beyond IMP-001, including X004 output-through-teardown, DEP-005 and qualification; join 14 inherited rows and exact triggers. Inspect configuration/secret acquisition and code-retention decisions without proposing a read-back patch API. |
| YubiHSM Auth finishing slice | Narrative is reviewed. Check unsampled C# BEFORE and schematic AFTER details in IMP-001/003–008 and X001–004, and join all 15 inherited reopening rows. The actual 56-byte regions/production/HSM claims remain evidence gaps; do not solve them by truncation or new execution. |
| OpenPGP | Review remaining bodies beyond IMP-001/002, especially KDF aliases/copies, mutation/reset, ATT/NEO wire and tool ownership; join 15 inherited keys and all 21 documentation labels' exact original triggers. Do not infer new engineering IDs from numeric gaps. |
| PIV | Review remaining 16 displays, particularly split 006/007, reset/coherence, timestamp/raw-ID choices, whole-workflow and qualification; join all 11 routed keys and original exact definitions. Preserve PR745 exclusions, response-only rollback and F9/physical restrictions. |

## Input identities

All paths below are relative to `docs/research/documentation-audit/`. Hashes identify reviewed inputs,
not successful build/test artifacts. Some large JSON inputs were parsed and read by selected fields;
their hash does not imply every field was semantically reviewed.

| Input | SHA-256 |
| --- | --- |
| `pass-2-applet-master.md` — 543,910 bytes / 8,136 lines | `066994f1841b189ea411e66d288791ace6d45779b4d9fa7bd3fbe6dd195e4db9` |
| `pass-2-applet-master-coverage.md` | `65a3afc9e662ea7cc1c6ba7f06e8b6437ff5471f1c55fd112d34c6e05bc2ddc7` |
| `records/style.md` | `2b8882adb75b18d67adcd71d38a6c70fb4aa7c44ec3211c0d45455ab94e9ca15` |
| `records/implementation-followups.md` | `e71056672c0cce567a4bd901786794d12e3812edcf99e2212cf448b757a5a263` |
| `records/runs/webauthn-001/triage.md` | `43ff8bdfa918b968ea9552d2e5a7984160bb86f34be731fc3ccb42d561231e8a` |
| `modules/management/pass-1/management-001/triage.md` | `44ce21e000e94044c332c14c9a2efb7e02fa63f35acb04a78778a044cc1e3915` |
| `modules/ctap2/pass-1/ctap2-001/triage.md` | `0b82744aff3cb26c397b00a770eebdef016b0a4dc0bc34e962799a57261e22d6` |
| `modules/ctap2/pass-1/ctap2-001/triage-addendum.md` | `359113bcb6e038645dcc48747be1b1b87a46d3cba304ab6b091d61d90df26655` |
| `modules/security-domain/pass-1/security-domain-001/findings.md` | `d29d4c6614eb1896f4189f623d1804477b8fa81e4a49e0e9ed4eacf9d5a7f9a6` |
| `modules/yubiotp/pass-1/yubiotp-001/findings.md` | `227577316f213f4b7ff7a72cff7b1563d8dc551db46a07644125adb7b77fba53` |
| `modules/yubihsm/pass-1/yubihsm-001/findings.md` | `634cec0e60644d5d287da17a31a6298000fdfdd5ebf99a50888801b3e3e758fc` |
| `modules/openpgp/pass-1/openpgp-001/findings.md` | `76b85dd1b3248326c451ad3884a4fa16ecdc64c595bf95a9d6913897b3b2dac0` |
| `modules/piv/pass-1/piv-001/findings.md` | `2002bfc27a36d22dd9a96228a515ce5d92a202435e5c8828746d483175c0da52` |
| `modules/core/pass-1/core-001/deferred-findings.md` | `b450b5bb1363af7227fe7100f7f050571289b67af87769956562bd871ef1915a` |
| `modules/core/pass-1/core-001/triage/final/candidate-reconciliation.json` | `1dffa7804eb6c87140cc17ffcb909bdf0eec2d51080d3b5b43ed93e174ca30c9` |
| `modules/core/pass-1/core-001/triage/final/inherited-reconciliation.json` | `0f0b6ad92303f65ce56cd0f3c253e4b7a7a3db371ad30c66e21ea408192f6756` |
| `modules/core/pass-1/core-001/triage/final/reconciliation-4/deferred-finding-continuity.json` | `6177a7a9553c633af9908bd97c1f5196f7ea9899160ce6d017d0af7b4c1a9406` |
| `integrations/windows-upstream-20261113/deferred-findings-successor.md` | `7af060c11d92a253a9bc12e98845a3942d382cf89ddb77a8e5c5620068948a59` |

The inherited-register prefix is `modules/core/pass-1/core-001/boundaries/`:

| Register | SHA-256 |
| --- | --- |
| `Oath/inherited-dispositions.json` | `358ab35f2caaf0c4b15d4706fe69785b054f442461b521c12bc5580153f693f9` |
| `WebAuthn/inherited-dispositions.json` | `3af8b94c5517e08f873f6b1ab91cb51e18c676ba6c7b026f23487c2215fd5c2f` |
| `Management/inherited-dispositions.json` | `ab5c9818d8e9e4ee3d0b190f5876a9eb98e50a9f3801bb89877ff3d1ab13a65f` |
| `Ctap2/inherited-dispositions.json` | `1cdb2d23eee6ca5c7a659251c18d5f6b9a7aac2d6fb8137cbdecdfbb6201bfe4` |
| `SecurityDomain/inherited-dispositions.json` | `8b526c6ffb37e393adc234a321e0c8fa8323f6ac3deccffbdd70b59545323ce0` |
| `YubiOtp/inherited-dispositions.json` | `95ac1acab618ffa455eb83cdf5b3fb63c60fa1756f0e29b34b9db121feff4a6e` |
| `YubiHsm/inherited-dispositions.json` | `7d596fc91f57f88eaa90aeae6e91783ad99b0c6bb215a38327511941f2094d07` |
| `OpenPgp/inherited-dispositions.json` | `e8620822e827edab1cbdd698f4e628604aaf14bd09dc9ce3ed3feae8c3e9f60c` |
| `Piv/inherited-dispositions.json` | `723e1cb10296c0c0be5b12b1dae1277414bf31a2ad5f3c1087cded7787c246e2` |

**Disposition:** request R1's bounded evidence/severity correction. Retain this report's remaining
semantic-review limits rather than recording blanket “Ready for user review” acceptance on behalf of
unreviewed cards. Actual user review and every implementation remain unstarted/deferred.
