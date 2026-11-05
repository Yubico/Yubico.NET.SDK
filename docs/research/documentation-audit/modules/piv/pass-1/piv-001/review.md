# PIV independent content and original-citation review

## Verdict

**Required corrections before acceptance: three documentation groups, six product paths.** The other
51 of the 57 edited paths are accepted for content at the identities in
[`evidence/review-identities.md`](evidence/review-identities.md). Acceptance covers documentation
meaning and usefulness, not compilation, device qualification or campaign completion. Re-review the
corrected complete units and freeze their new identities before release.

Reviewer: fresh independent GPT-6.1 Sol, November 5, 2026; primary checkout; baseline
`727fe43afc430c5d8364d868a6436de50626e476`, post-PR745. Read root/module instructions, plan, findings,
investigation, coverage, edit-report, workflow, approved style, SDK house style, source analysis and
the consequential retained originals. Reviewed all 57 edited paths' affected semantic units, complete
examples and their real helpers, result records, resource scopes and meaningful assertions. Scoped
shared PIV material only; cross-module target checks supply no Core/other-module implementation audit.

Only this review and `evidence/review-*` records are owned here. No product edits, builds, tests,
hardware commands, formatter, commits or subagents were run. Python checks are read-only.

## Required factual and usefulness corrections

### PIV-REV-001 — Read and describe the actual F9 result property

**Medium; C004/C009.** In `src/Piv/examples/PivTool/PivExamples/Attestation.cs:91–98`, the second
XML example obtains `result.IntermediateCertificate`, guards it for non-null, and prints its subject.
The actual helper at lines 111–117 calls `AttestationResult.Succeeded(intermediateCert)`. The first
parameter initializes **AttestationCertificate**; **IntermediateCertificate** remains null
(`PivExamples/Results/AttestationResult.cs:47–48`). Thus the example compiles but its useful success
branch never runs. The CLI already compensates at `CliRunner.cs:849` with
`result.IntermediateCertificate ?? result.AttestationCertificate`.

Required documentation-only correction:

1. Make the XML example consume the actual F9 certificate, retaining disposal of every returned
   certificate. Either read `AttestationCertificate` explicitly, with a brief explanation of this
   helper's current result shape, or follow the real CLI fallback. Avoid two disposing owners for
   the same certificate.
2. Qualify `AttestationResult.AttestationCertificate`'s XML summary: this property can also carry the
   retrieved F9 signer certificate from `GetIntermediateCertificateAsync`.
3. In `src/Piv/examples/PivTool/README.md:84–87`, tell JSON consumers that `attest-intermediate`
   currently places F9 metadata in `attestationCertificate`, leaving `intermediateCertificate` null.

Keep the runtime helper/result shape unchanged in Pass 1. This is a missed concrete manifestation
of the existing sample result-contract follow-up, not a new independent implementation-bug count.
Re-review both XML examples, the result property and the CLI/JSON explanation together. Original
F9 baseline/attestation evidence remains unaffected.

### PIV-REV-002 — Describe partial credential rotation precisely

**Medium; C002.** `src/Piv/src/IPivSession.cs:179–182` correctly says SET PIN RETRIES restores both
factory credentials and directs callers to ChangePinAsync/ChangePukAsync. Its final claim that
“a failure between them leaves the defaults in place” overstates the resulting state. If ChangePin
succeeds and ChangePuk fails, the new PIN is installed while the PUK remains the default. Neither
the session nor the separate protocol methods rolls a successful change back.

Required replacement meaning: **“These are three separate commands. A failure during the subsequent
changes can leave one or both credentials at their defaults; inspect state and complete the remaining
changes before deployment.”** Preserve the nearby authentication requirements and unchanged-limit
reset warning. The README's existing recovery-plan wording needs no preference-driven rewrite.
Re-review the complete retry-limit contract and inherited facade presentation. No runtime fix is requested.

### PIV-REV-003 — Align two neighboring crypto-test explanations with their actual oracles

**Medium; C010.** Full-file assertion review found two residual claims in already affected files:

- `PivSigningAlgorithmTests.cs:240–242` describes a P-256/SHA-384 truncation check, but the complete
  method at 246–271 hashes with **SHA256** and verifies that digest. Correct this XML to P-256 with
  SHA-256; remove the invented truncation exercise. Preserve the method body/name/assertions.
- `PivCryptoTests.cs:127–130` says deterministic differences “show the message reaches the key
  unchanged.” The assertions distinguish selected short/padded and long/common-prefix message pairs
  and repeat one message. They catch the specified 32-byte padding/truncation regressions, but a
  deterministic hash-first implementation can also distinguish those pairs. Describe the specific
  regression checks and direct original-message/key binding to the independent host-verification
  oracle. Preserve the actual differential assertions and genuine X25519 TODO.

This local commentary correction neither reopens the fixed Ed25519 runtime bug nor denies the separate
existing `YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages` host-verification test. The coordinator
reports that test passed in the full 83-execution unattended batch. Keep local-method evidence separate
from suite-wide evidence. Expand internal/test clean sampling after these material misses and reconcile
new edited overlaps; the coordinator owns selection and denominator changes.

## Style acceptance and optional feedback

The edited task guide, API contracts and tool guide generally satisfy the mandatory approved voice:
affirmative task/actor/actual behavior first, direct and calm, with useful firmware, ownership, failure
and recovery conditions nearby. The cryptographic, PIN-only, retry and reset qualifications save the
reader consequential inference. Remaining negatives generally distinguish a real limitation and lead
to a supported SDK workflow. **No optional stylistic rewrites are requested.** Preserve useful rationale
and avoid editorial churn while applying the three required groups above.

## Independent original-citation adjudication

The original PDF/HTML/RFC/source capture hashes independently match both source manifests. The PDFs'
archive covers, rather than failed landing-page requests, establish status. Exact line references below
use the retained text's LF line numbering, matching the repository read/grep tools (form-feed page
separators must not be counted as extra lines).

| Original and exact passage | Actor, strength, applicability and verdict |
| --- | --- |
| Combined SP 800-73-4, May 2015, updated February 8, 2016; archive cover lines 1–39 | Withdrawn July 15, 2024; superseded by revision-5 Parts 1/2/3. The captured filename `part1` contains all three parts. The guide preserves revision 4 and explicitly separates deployment adoption. Accepted. |
| SP 800-73-4 Part 2 §2.4.2, 3792–3819 | The card's successful/failed authentication changes security-status indicators; PIV PIN/PUK/admin indicators are application-local. This supports the card-versus-managed-observation distinction, not an OS-handle-disposal guarantee. Accepted. |
| Part 2 §2.4.3, 3825–3856 | Profile PIN **shall** be 6–8 ASCII-decimal bytes, right-padded FF; card enforces minimum/format. PUK **shall** be eight bytes and **may** contain any binary octet. Actual SDK UTF-8/raw-byte and per-operation limits are correctly separated. Accepted. |
| Part 2 §3.2.1, 4202–4245 | Absent-data VERIFY can report 63CX or 9000; P1 FF clears verification without consuming retries; issuer supplies initial/reset count. SDK count-unavailable 9000 and the four-bit count are accurately explained. Accepted. |
| Part 1 §3.3.3 and certificate Tables 15–17/footnote | Key History counts retired management keys; F3 is conditional and its URL format is prescribed. Certificate value length 1856 is **recommended** and can be exceeded. Fixed SDK models and automatic GZip threshold are labeled local implementation choices. Accepted. |
| SP 800-78-4, May 2015, archive cover; §3.1/Table 3-1; §§3.2.1–3.2.2; §5 | Withdrawn July 15, 2024, superseded by revision 5. Card key generation, slot-specific curves, exponent 65537, issuer/CA information signatures and management-system authentication have different actors. “Should process broader exponents” is not an import guarantee. SDK/extension support is distinct from federal profile conformance. Accepted. |
| FIPS 201-2, August 2013, archive cover; §2.9.3 | Superseded by FIPS 201-3 January 24, 2022. Issuer/federal PIN verification-data-reset identity controls differ from destructive Yubico applet RESET. The public guide fixes its cited revision without upgrading adoption. Accepted. |
| FIPS 186-5, February 2023, §6.4.1 steps 1–2, 1325–1338 | Signature generator hashes M then converts the leftmost required bits to integer e. P-256/P-384 byte-aligned truncation/zero-left-padding explains arithmetic; it grants no permission for every hash/key pairing in a federal profile. Accepted. |
| RFC 3279 §2.2.3, 376–383; RFC 8017 §§8.1.1/8.2.1/9.1/9.2; RFC 8032 §5.1.6 | ECDSA r/s ASN.1 sequence, RSA EMSA encoding before RSASP1, and PureEd25519 original-message/64-byte signature descriptions match the raw operation and direct examples. RFC material supplies algorithm/encoding semantics, not device message-size or PIV-profile guarantees. Accepted, subject to local-test wording correction above. |
| RFC 7748 §§5/6.1 | X25519 uses 32-byte little-endian u-coordinate encodings; shared-result use/KDF and protocol-dependent zero checks belong to the application protocol. Raw SDK agreement remains distinct from the existing generation-only method and pending supplemental oracle. Accepted. |
| Yubico reset command remarks 29–63; technical manual 138–176, 440–469; attestation-tool original | Normal PIV reset retains F9 private key/certificate; Bio MPE uses device reset. Slot-policy defaults and independent certificate storage match the guide. AES released origin is 5.4.2, distinct from the SDK's 5.4 gate. F9 replacement is unrecoverable and changes the applicable signer chain. Retrieval/issuer verification/SPKI binding alone are distinct from manufacturer-root trust. Accepted. |
| YSA-2017-01, mitigation paragraph 77 | Vendor PIV interval is 4.2.6–4.3.4; recommended alternatives include ECC generation or host-generated RSA import. Python's broader 4.2 boundary is conservative implementation evidence. Accepted. |
| Pinned Python `4ca60f706af930459138d8dc0f0f953480e1c7a4` source and retained origin patches | Rechecked actual capability/ROCA/FIPS predicates, serial's 5.0.3 introduction, Bio preflight/MATCH capability changes and 5.7 move/delete/25519/large-RSA origins. These establish source history, not normative requirements or physical qualification. Post-5.7 FIPS NFC and Bio profile conditions remain separate. Accepted. |
| Python `ykman/piv.py:209–273` versus C# AdminData complete encoder/decoder | Python's shared Pivman software data uses fixed four-byte big-endian timestamp fields; C# writes trimmed little-endian Unix seconds. Docs call this an interoperability difference in Yubico software state, without inventing a NIST timestamp rule or a migration policy. Accepted; PIVIMP-009 remains. |

Context7 independently resolved .NET and confirmed the .NET 10
`ECDsa.VerifyHash(ReadOnlySpan<byte>, ReadOnlySpan<byte>, DSASignatureFormat)` overload at
`https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdsa?view=net-10.0`.
This is API-signature evidence, not compilation of a fence. The source-pinned vendor and standards
captures remain the authority for their claims; no new publication edition was silently substituted.

## Completeness, examples and preservation

- Independently reconciled **57** edited paths, **48** C# files, **339** XML blocks; original/current
  file hashes match the editor handoff. Every ordered non-comment line in all 48 C# files matches the
  baseline, preserving original declarations/signatures, executable bodies, assertions, imports,
  directives and runtime strings. XML fragments parse with zero errors. This is static evidence.
- Independently re-extracted and read **61 original complete bodies and 60 current bodies**, including
  implicit session/client/helper calls. All 61 original hashes match the editor's record. Current module
  scope is 42 bodies; shared scope adds 18 (six migration, eight raw access, four interaction). Historical
  v1 and other-applet bodies remain explicitly classified. The count change comes from the README/tool
  replacements, not a missing body. Complete file/body identities and classifications are in the manifest.
- README generation/sign/decrypt lifetimes, caller-owned factory/active key inputs, P-256 DER host
  verification, known-plaintext RSA/OAEP comparison, borrowed PIN-only parameters and close/reopen scopes
  are coherent. The active management key is authenticated before protected writes; recovery flags are
  checked alongside observed authentication; partial effects and still-blocked PUK are local.
- Certificates/public crypto objects have disposal scopes in the edited fragments. Owned mutable
  `Memory<byte>` outputs flow from real SDK methods and are cleared through their owned spans. Sample
  read-only plaintext is truthfully qualified; no writable cast is proposed. Typed store callbacks have
  a real application contract and the 24-byte management-key assumption is explicit. Current F9 sample
  usefulness is blocked by PIV-REV-001, despite syntactic plausibility.
- Read full helpers behind CSR/self-signing, generic signing, verification, key generation, slot queries,
  retry information, reset and the 22 CLI commands. Runtime sample bugs stay deferred rather than
  becoming intended SDK contracts. The full actual 36-source PivTool and meaningful snippet hosts still
  need coordinator compiler/analyzer evidence. Source parsing supplies no “compiled fence” claim.
- All 11 accepted C groups have coherent edits. Keep investigator IDs **PIVIMP-001 through PIVIMP-010**
  and **PIVCORE-001 through PIVCORE-003** unchanged. Probe labels using `PIV-IMP` are not new independent
  groups. The three Core questions retain physical PIN release/isolation versus the observed current
  SDK regression, native cancellation cause/serialization, and shared owned-response/connection contracts.
  PR745's fixed retry/admission/authentication/ownership/witness findings remain superseded.

## Hardware checkpoint and remaining coordinator gates

This reviewer ran no device operation. The coordinator's supplied current checkpoint supersedes stale
“running”/“pending” sentences in the earlier status/edit reports: primary 103 is Management 5.8 beta,
secondary 125 is 5.8 alpha, and both directly report PIV version `00 00 01`. Each has an independent
25-slot public baseline, 24 normal slots empty and its own retained F9 identity. Primary F9 is RSA-2048,
Once/Never, with a 793-byte certificate. Metadata/CLI version and raw applet version remain distinct.

The existing full unattended batch is reported **PASS: 83 executed, three multi-key skips, 483.8 seconds,
zero warnings**; the 83 include all selected slow methods, independent Ed25519 host verification and
the PIN-isolation regression. Independent cleanup V2 on 103 reset and matched fresh 25-slot public
state exactly, then authenticated factory PIN/AES-192 and matched state again. V1 cleanup failed
before mutation because its F7/F9 comparison assumed one response instead of handling 61xx chaining;
that failed control is preserved and is not an SDK defect.

The user has now **deferred physical touch and hotplug testing**. Touch printer, touch signing and
hotplug remain not run under that deferral. Six supplemental unattended probes remain pending in the
supplied checkpoint (complete RFC/same-key/vendor X25519, repeated Always signing, key/certificate
independence, issuer-signature/SPKI attestation binding, actual PivTool RSA/Ed25519 expected failures).
The seventh cancellation probe needs physical coordination and is deferred. Three multi-key cases
remain pending secondary cleanup control/two-explicit-key fixture selection. Restore the coordinator's
temporary 103-only allowlist to its original bytes before final gates. Late outcomes must be reconciled
against their actual assertions without silently upgrading profile, transport, OS or manufacturer trust.

Before final acceptance: correct/re-review PIV-REV-001–003; reconcile/expand clean sampling (editor
checkpoint 385 eligible, minimum 39 before these further overlaps); compile current snippets and the
real full tool; perform scoped formatting/final identities and applicable coordinator gates; reconcile
hardware/status and final evidence. No Pass-1 runtime fixes or Core implementation audit are authorized.
