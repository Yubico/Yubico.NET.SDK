# Independent B5/B6 content and citation review

**Required corrections; not PASS.** The reviewed patch is identity-consistent and the main
ownership, exchange-lifetime, cancellation, and implementation/profile qualifications are supported.
Four localized documentation corrections remain. No executable change or new investigation is required
to address them.

This review covers the editor return at base `da66f5d01ba594993974ddd26723e8c300c0a454`, product
source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, and complete product diff SHA-256
`4a89837083fd34826639a55d1bd988574565bc8a4da508975820e0acfa3755d1`.
The hashes identify the reviewed input; they are **not approval of the remaining incorrect passages**.
Line numbers below refer to that worktree, before any correction.

## Required corrections

### R1 — Finish relocating the consequential GlobalPlatform citations

**Candidates:** `CORE-B6-C006/C009/C010/C013`, `FPT-072/075/076/079`.

- `src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs:130` cites
  `§7.6.2.3/Table 7-18` for the response. In the retained **1.3.0.13 Public Review**, Table 7-18 is
  **§7.6.3.1**, printed page 58, and covers **a/c**. The **b** response is **§7.7.3.1/Table 7-22**,
  printed page 62. Cite both for the common parser. The static c versus ephemeral a/b distinction
  itself is correct.
- In the same file, `:36` still puts INTERNAL AUTHENTICATE in §7.6; it is **§7.7** in this review.
  `:42` labels §7.1.1 as the handshake; that section is **SCP Identifier and Parameters**. Use the
  actual establishment/command sections, or remove that misleading local locator. At `:103`, identify
  **§7.6.2.3 for a/c and §7.7.2.3 for b** when citing the common request shape. The b section explicitly
  incorporates the former processing with its own key-agreement modifications; do not erase those roles.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:124` retains the old
  **§7.1.1.3** locator and an unqualified 29-byte response description. The inspected SCP03
  **1.1.2 Public Release** puts the response in **§7.1.1.6/Table 7-3**, printed page 34: 29 mandatory
  bytes plus a conditional three-byte counter for pseudo-random challenge generation. Describe this
  test as rejection below the implementation's minimum prefix, not exact universal response size.
  `:141` also retains **Amendment F §7.1.3** for the b response; use the identified review edition and
  **§7.7.3.1/Table 7-22**. Its test supplies SCP11b inputs, so no a/c claim is needed there.
- `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs:34` attributes the SCP03 initial zero chain to
  §6.2.4. The explicit EXTERNAL AUTHENTICATE initial-chain rule is **§6.2.3**, printed page 24;
  §6.2.4 describes command-MAC generation. Cite the explicit rule.
- Update `editing/protocols/citations.md:16` to distinguish the request and response sections above.
  Preserve the original investigation records, whose combined `§7.6.2.3/Table 7-18` shorthand is an
  input limitation, not authority to repeat the wrong locator in newly edited documentation.

**Original evidence:** `investigation/B6/sources/scp11-public-review-1.3.0.13.pdf`, printed pages
45, 55–58, 60–62; corresponding retained text `:1993–2016,2417–2609,2647–2743`.
`scp03-1.1.2.pdf`, printed pages 24 and 33–34; retained text `:939–961,1359–1435`.
The response tables and SCP03 passages were also extracted directly from the retained PDF bytes during
this review. No final 1.4 or uninspected 1.1.2.6 clause is substituted.

### R2 — Correct the retained unknown-firmware test explanation

**Candidates:** `CORE-B6-C013` / `FPT-079`, with C007's parameter-lifetime referral unchanged.

`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/ScpExtensionsTests.cs:163–181`
retains “Should proceed to initialization (will fail, but not due to firmware check).” Its corresponding
SCP11 unknown-firmware case is at `:201–221`. Both leave firmware null. In
`ScpInitializer.cs:57–59`, the initialization processor must be a `ChainedResponseReceiver` with
**non-null** firmware; the null-firmware cases are rejected with `ArgumentException` before the
protocol-specific support branch. Their broad exception assertions do not prove that the branch or
handshake was reached.

Replace the misleading comment with the exact current assertion/admission scope; qualify the second
case alongside it. Keep test names, exception assertions, and runtime strings unchanged. Revise this
path's retain rationale in `path-decisions.json` and the `FPT-079` retained-path record accordingly:
the existing “firmware-routing assertions” rationale is insufficient for these two cases. This is a
missed documentation correction on a candidate-associated retained path, not authorization to repair
the tests or change null-version policy.

### R3 — Retain the selected-security-level conditions on normative SCP03 claims

**Candidates:** `CORE-B6-C005/C010/C011/C013`, `FPT-071/076/077/079`.

`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/ScpEmptyResponseTests.cs:9–13`
replaces the original explicit “with R-MAC in the security level” condition with an unqualified
requirement to protect 9000/62xx/63xx. Restore that condition. Apply the same qualification to the
normative summary/comment in `ScpProcessor.cs:46,166–169` and `editing/protocols/citations.md:15`.
The inspected SCP03 §6.2.5, printed page 28, explicitly says R-MAC is optional and may be switched
on/off. Core's SCP03 initializer selects `0x33`, which includes R-MAC; that makes the rule applicable
to this path, not to every possible SCP03 security level. Keep the error-status-only rule and genuine
EXTERNAL AUTHENTICATE exception, while retaining the instruction-only local exemption qualification.

Likewise, make the applicability in `citations.md:14` explicit: §6.2.6 begins “This section applies
when both command confidentiality (C-DECRYPTION) and integrity (C-MAC) are required.” Counter advance
plus no encryption for absent data is supported under that condition. The current encryption-path
comments and empty-padding discrepancy remain valid; no implementation change follows.

**Original evidence:** `scp03-1.1.2.pdf`, printed pages 26, 28–30 and 35; retained text
`:1023–1030,1131–1166,1217–1224,1490–1504`. Direct PDF extraction confirmed these passages.
SCP11's inspected review §5.3.2 separately defines its permitted selected levels and prohibits treating
its establishment as blanket applet authorization.

### R4 — Distinguish shared-secret width from KDF output width

**Candidates:** `CORE-B6-C009/C010`, `FPT-075/076`.

`src/Core/src/Protocols/SmartCard/Scp/Scp11X963Kdf.cs:21–23` says “ka1 || ka2 yields 80 bytes”.
`GetSharedSecret` checks **32 bytes for each agreement** (`:154–155`) and returns their **64-byte**
concatenation. `DeriveSessionKeys` then requests **80 bytes from X9.63** (`:47–58`), split into five
16-byte values. State those two steps separately so the new explanation cannot be read as an 80-byte
P-256 agreement concatenation. This is a numeric/wire-derivation clarification, not a style preference.

## Supported content and retained limits

- Session/raw factory readiness and version prose matches the concrete factories and base publication:
  raw HID attachment is not wire initialization; inherited raw firmware stays 0.0.0; Security Domain
  uses its override or 5.3.0 assumption. `IsSupported` remains callable after disposal. The authentication
  flag reflects completed optional setup, not PIN/UV/management authority or channel liveness. The
  inspected SCP11 review §4.3 really does require the first correct-MAC post-establishment a/c APDU;
  §5.3.2 explicitly seeds command chaining with the receipt. These phase corrections are supported.
- The exchange guard, `ApplicationSession` disposal, FIDO's self-exchange detector, and OTP callbacks
  support the separation of per-exchange drain, whole-workflow completion, callback self-wait, protocol
  CANCEL, terminal wake, native completion, and physical release. The narrowed FIDO cancellation tests
  assert scripted packet count/drain/reuse, not a physical re-plug requirement or normative vendor CANCEL.
- CTAPHID INIT/version fields, same-CID INIT versus another channel's busy response, no-response CANCEL,
  and the indefinite pending-read qualification match the retained CTAP 2.3 passage and actual control
  flow. Encoded-CBOR normalization, reserved-CID/control admission, and failed-tail recovery stay held.
- OTP's padded outbound CRC coverage, seven-byte report chunks, status offsets, command-specific inbound
  data/CRC, broad NEO refresh catch, tight polling, local deadlines, and dummy-report cleanup are correctly
  distinguished. A local deadline does not bound a native GET. CRC is not authentication.
- APDU formatters always emit Le. Complete-command ceilings and short data-chunk sizes are distinguished.
  Follow-ups use fixed-zero header fields and do not implement a 0x6C retry. Final status throwing is
  exactly non-0x9000; diagnostic fallback text is not promoted into success classification. First plain
  command-fragment uncertainty and plaintext SELECT retaining the local SCP graph are correctly qualified.
- Response.Clear reaches the response's private copy and its aliases/status, not transport staging,
  earlier fragments, the response writer on every failure, or the separately allocated decrypted temporary.
  Inspection of allocations and protected scopes found no new all-buffer-erasure claim to accept here.
  In particular, `new ApduResponse(State.Decrypt(...), ...)` makes another copy; the finally clears
  `unmacdData`, not that returned plaintext temporary. SCP03 pre-state-return cleanup remains narrower
  than the initializer's post-return guard; neither state nor SessionKeys has a finalizer.
- Contained key/certificate owners, shared record clones, independent caller source buffers, optional DEK,
  default/init-modified KeyReference encoding, fixed P-256/AES-128 processing, SD trust selection, and PSO
  chain-verification actors are accurately separated. Constructor admission is not generic profile support.
- Counter mutation before `stateAdvanced`, empty-command padding versus the inspected release, later raw
  INS82 admission, signed synthetic 0x61xx/0x6A82 responses, and split protected-response interoperability
  remain findings/qualification gaps. Nothing reviewed fixes or endorses them as intended guarantees.

The complete example helper in `ApduCommand.cs:31–55` uses actual raw-session APIs and scopes session,
payload, and returned response cleanup usefully. It clears the payload even if factory creation fails,
uses `throwOnError: false` to retain final-status ownership, and reads status before clearing. Its explicit
borrowed connection/header/payload prerequisites make **complete composable helper** an honest
classification, not a complete discovery/selection/authentication program. Its body hash is
`3e0457b3fedca0f971d59cfbff209e3cd61308f507e8b9283a066f79c5a201aa`.
Compilation/binding remains unrun. The removed provider prototype is genuinely commented-out code;
the original body and fragment classification are retained. Existing test names, skipped bodies, fixture
strings, and current assertions supply no extra trust, erasure, device, or conformance evidence.

## Population and outside-owner disposition

All **91** path identities were freshly checked against base and current raw SHA-256 values:
**56 edited, 23 retained, 12 held**. Every directly indexed B5/B6 candidate-associated path has either a
primary path decision or a direct outside-owner entry; none is mechanically missing. R2 is the semantic
retention omission. R1 identifies missed passages inside edited units. Keep all original B5 C001–C009,
B6 C001–C016 and FPT-058–082 identities; do not allocate replacement finding IDs. ATR C016/FPT-082
has no released D facet and remains held. Other harness/implementation holds do not authorize stronger
prose or changing executable bodies.

The exact outside-owner referrals remain the hash-bound `outside-owner-handoffs.json`, including its
**21 direct paths, 15 original B5 source-run records, and eight B6 records**. In particular:

- **Shared guide owner:** root `CLAUDE.md`; `src/Core/README.md`, `src/Core/CLAUDE.md`,
  `src/Core/tests/CLAUDE.md`; `docs/SDK-HOUSE-STYLE.md`, `docs/MEMORY-MANAGEMENT.md`,
  `docs/architecture/raw-access-tiers.md`, `docs/usage/user-interaction.md`; migration
  `v1-to-v2.md`, `v1-to-v2-gaps.md`, `v1-to-v2-map.yml`, `v1-to-v2-changelog.md`;
  `docs/v1-to-v2-comparison.md` and `docs/v2-highlights.md`. Reconcile mapped ownership, initialization,
  exchange/workflow, effective-version, response-copy and complete-example claims; keep dated history
  historical. The stale fake API names and `ApduException.SW` examples still need that owner, not a fake
  API or product change by this reviewer.
- **Transport owners:** `src/Core/src/Transports/Hid/IOtpHidConnection.cs` for B5 C004/C005; native
  producer, terminal/drain/release and deployed-binary claims remain B1/B2/B3/B4 responsibilities.
- **PublicApi owner:** `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs`
  and `AsyncSurfaceConventionTests.cs` for B5 C009's asserted factory grammar/allowlist scope.
- **CTAP2/WebAuthn:** `src/Ctap2/src/Backend/HidBackend.cs` and
  `src/WebAuthn/src/Client/CeremonyDeadline.cs` for original-request result/status, cancellation, incoming
  report/output ownership and complete ceremony lifetime. Lower-layer drain is not ceremony rollback.
- **Management:** `src/Management/src/ManagementSession.cs`, `management-001:MGMT-IMP-004` for
  detected/effective metadata and parameter/example scopes. **Security Domain:**
  `src/SecurityDomain/src/SecurityDomainSession.cs`, `security-domain-001:SD-DEP-001` through
  `SD-DEP-004` for assumed/explicit firmware, retained reset parameters, omitted reset config/version,
  trust/authority, first-protected-command phase and response ownership.
- **YubiOTP:** `src/YubiOtp/src/Backend/HidBackend.cs`, `yubiotp-001:OTP-DEP-001` through `004`
  for command-specific CRC, polling/NEO, result ownership and whole-operation lifetime. **OATH:**
  `oath-pilot-and-fixes-wave-002:IMP-010/DEC-007`; **OpenPGP:** `openpgp-001:PGP-CORE-002/003/004`;
  **PIV:** `piv-001:PIVCORE-002/003`; **YubiHSM Auth:** `yubihsm-001:HSM-DEP-001/002/003` retain
  their exact public XML, session/extension, README/CLAUDE and example/test reopen paths in the JSON.
  These are caller-specific authorization/cache/reset/output tasks, not automatically closed Core findings.

The inherited audit `#23`, `N2`, `consideration-3/5` scopes remain nonadditive. Serial **1256A88**
remains two 0x6A88 setup failures **before EXTERNAL AUTHENTICATE and RESET**; plaintext reset success
does not validate secure reset. Serial **9681620** remains unexplained. Final SCP11 1.4/vendor-profile,
code-cited SCP03 1.1.2.6, ISO normative clauses, original NIST sample bytes, capture identity, actual
provider/native producer and physical release/reopen gaps stay explicit. PIV physical deferrals and the
future Windows ready delta remain unchanged; no unfinished Windows worktree was inspected.

## Verification and return boundary

The live full-index product diff equals the returned full diff byte-for-byte. All 91 current/base file
hashes agree with the path decisions; direct index-path coverage has zero omissions; product
`git diff --check` exits 0. The independent reading identifies the four corrections above, not a runtime
pass. Visual diff review found documentation/newline edits only, but the coordinator's stronger
compiler-bound token/directive/import/signature/default/attribute/assertion/runtime-string/API-snapshot
preservation check is still required. Do not substitute the editor's limited noncomment comparison.

No SDK build/test, formatter, cryptographic computation, provider/native/device execution, Git index/ref
mutation, commit, push, product/editor-record edit, or subagent ran in this review. Only this review and
`review-identities.json` are written. After localized corrections, regenerate the editor identities and
review the corrected delta, then use the coordinator's existing compilation/token/API/example gates.
Finish Core Pass 1 publication and the explicit deferred handoff, then stop; no Pass 2 or new tooling
project is requested.
