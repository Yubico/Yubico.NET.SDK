# Security Domain Pass 1 — independent review

2026-11-04; fresh reviewer `github-copilot/gpt-6.1-sol`. Baseline
`b73efe3813b429b4b8eb2a7e819a6ad7be699d47` versus current working-tree content, not a HEAD commit range.

**Verdict: REQUIRED CORRECTIONS — R1 and R2 below.** All 26 authored paths were reviewed. Apart from
those precise factual corrections, the edited content passes this independent documentation review for
the recorded scope. This is not a build/test/example-compilation, hardware or campaign-closeout verdict.

## Required findings

### R1 — Correct the draft subsection for allowlist chain-serial checks

**Priority:** medium; citation accuracy. **Affected product locations:**

- `src/SecurityDomain/README.md:155`: `§§4.3, 7.5.4 and 7.9`.
- `src/SecurityDomain/src/ISecurityDomainSession.cs:249`: the same subsection list in StoreAllowListAsync
  XML, also inherited by the concrete method.

**Evidence:** independently retrieved original August 2023 SCP11 v1.3.0.13 Public Review PDF. The
CA-direct OCE/intermediate serial rules are in **§7.5.1, Definition and Scope**, printed page 50.
**§7.5.4, Processing Rules**, printed page 54, addresses BF20 authorization under ANY_AUTHENTICATED.
The substantive description is supported; the subsection number is not. See
[independent original-source checks](evidence/review-sources.md#r-e2--original-scp11-public-review-pdf).

**Precise correction:** change `7.5.4` to `7.5.1` in those two product locations. Preserve the explicit
Public Review/draft/date/final-profile qualifications. Coordinator should also mark/correct the original
investigation/evidence citation pointers so the incorrect subsection is not propagated; those records
are not reviewer-owned. Do not upgrade this to final-v1.3 conformance or change runtime behavior.

**Re-review:** pending the two corrected files and exact replacement hashes.

### R2 — Name the certificate assertion actually performed

**Priority:** low; test-comment factual accuracy, not a request for a stronger test.
**Location:** `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs:239`.

The new comment says `Check byte identity via thumbprint`. The complete test at lines 223–246 asserts
one returned certificate and equality of `Thumbprint` strings (lines 240–242), not RawData equality.
Thumbprint is a SHA-1 hash, independently checked against the framework's original property page.
By contrast, the module's certificate-bundle unit test really does compare RawData.

**Precise correction:** use `Compare certificate thumbprints, not chain validity or association with the
device key.` Preserve the assertions, setup and cleanup unchanged. This corrects the evidentiary claim;
it does not require byte-comparison assertions or reopen the runtime/test implementation in Pass 1.

**Re-review:** pending the corrected file and exact replacement hash.

## Scope, method and accepted content

Read root `CLAUDE.md`, module README/CLAUDE, test CLAUDE, `docs/TESTING.md`, campaign workflow/style, and
RUN plan/findings/investigation/coverage/source evidence/edit-report. Read all 26 authored product paths:
full module Markdown and C# files; full affected test files including setup, assertions, cleanup and
fixture bodies; complete SD teaching excerpt/migration subsection/map entry. Also read the complete
module-local backend interfaces/implementation, key-material and TLV helpers, CMAC-vector tests, and
integration project context. Compared the explicit baseline-to-working-tree diff, rather than assuming
the edit report was the diff. No affected authored product path is omitted.

- **Task usefulness/style:** read-only start, factory authentication, full key-material import/zeroing
  scope, fresh-session verification before deleting retained keys, generated SCP11b public key retention,
  provisioning-role table and separate destructive reset task are coherent. The five README task fences,
  contributor reference/logging fragments and three full test-context examples retain their prerequisites
  and resource scopes. The inline SCP11 constructor expression is a typed fragment, not a complete
  provisioning tutorial. No optional wording/style churn requested.
- **API agreement:** all 16 concrete operations correspond to the explicit local interface methods,
  including three distinct PutKey overloads, and use inheritdoc. Parameters/results agree, including
  ECPublicKey, nested tag-73 value, descriptor pairs, KLOC-before-KLCC and caller-owned certificates.
  No new public contract is inferred from generated record members. The certificate load-failure and
  successful caller-ownership descriptions agree with the loader and handle/identity/RawData assertions.
- **Local state/error/authorization:** checked preparation and selection before the configuration catch,
  its supplied-parameters/type filter, immediate-only status extraction, IsAuthenticated versus host
  authority, plaintext/encrypted import paths, caller parameters retained locally, cancellation forwarding
  and no rollback. Inherited behavior remains a dependency, not a newly reviewed Core guarantee.
- **Destructive operations:** checked zero-filter encoding and SCP03 normalization, replacement-byte casts,
  KCV-only response comparison, firmware override/fallback and explicit-only generation gating. Reset
  enumerates, skips component 02/03, uses reference 00/00 for SCP03, performs at most 65 raw attempts per
  processed reference, retries ordinary failures, stops at the named statuses, can exhaust silently,
  skips empty listings, and reinitializes without original options. Prose does not close these defects
  or recast them as new lifecycle policy. SD-IMP-001–008 remain engineering/verification work.
- **Test commentary:** verified broad-exception caveats, later unauthenticated generated-key listing,
  convenience-listing non-disposal assertion, tracked-device cleanup and independent cleanup token,
  fixture non-verification of host cryptogram/MAC, four CMAC vectors versus protocol conformance, name
  ordering versus certificate trust, factory public bytes and firmware attributes. R2 is the remaining
  assertion-overclaim; no executable test modification is requested.
- **Original citations:** independently fetched Yubico manual, original SCP11 draft, GP catalogue,
  framework originals and RFC, not merely investigator excerpts. Yubico's SCP03 first-import/last-set
  behavior and 5.3.0/5.7.2 support match the qualified prose. The draft's §§4.3, 7.5.1, 7.8–7.10 support
  the described roles, clearing consequences and counter gap within its explicitly limited status.
  R1 is the subsection correction. Final versions remain inaccessible; no final-profile claim accepted.

## Independent link/hash checks

Read-only Python and git checks completed with exit 0:

- Parsed all 26 edit-report path/hash rows, recomputed SHA-256 from current bytes: **26/26 exact matches**.
- `git diff --name-only b73efe3813b429b4b8eb2a7e819a6ad7be699d47 --` the module and three authored shared
  documents returned exactly those 26 product paths.
- Resolved **19 module relative Markdown link occurrences**, zero missing paths. The sole module
  heading link resolves to `understand-scp11ac-provisioning-roles`. Tests' original
  `../../../docs/TESTING.md` resolves correctly; the investigator's proposed four-parent traversal is
  wrong and must not be reintroduced. Vendor/draft/catalogue links were independently fetched.
- The broad shared-document link scan additionally found a pre-existing, untouched
  `../CLAUDE.md#memory-management-hierarchy` anchor in AI-DOCS-GUIDE without a matching heading. It is
  outside the edited SD excerpt, not a new SD correction or a whole-shared-document link pass.

**Scope-limit disclosure:** the first generic link-anchor script incidentally read heading text from
`src/Core/README.md` (and other pre-existing shared-document link targets). That was unnecessary for this
assignment. It did not investigate Core behavior or trigger edits; no Core source/test implementation,
dedicated boundary review or other-module audit was performed. Remaining checks were limited to SD links.
Do not describe this review as having opened no Core document at all.

## Exact reviewed/accepted hashes

`PASS` accepts documentation content at that exact SHA-256 for this scope. `R1`/`R2` identifies a reviewed
hash requiring the correction above, **not** final acceptance. The other 23 paths are accepted. Any
subsequent content/formatter change needs hash reconciliation; the table does not accept unseen bytes.

| Product path | SHA-256 | Verdict |
| --- | --- | --- |
| `docs/AI-DOCS-GUIDE.md` | `54ff0bf0a5c57143e73094f0b7f2bd67d4175e358c4ae2e554bdaa05616b9a93` | PASS |
| `docs/migration/v1-to-v2-map.yml` | `eea28b28c26fbcff358b04214e7fb660cc32226c5e5e7fa2d97bd5fbc9729c41` | PASS, SD scalar only |
| `docs/migration/v1-to-v2.md` | `606f8822d3dc0f6360a709c7adf33acb164dffe1d559acaf8ce2c95971dd2588` | PASS, SD subsection |
| `src/SecurityDomain/CLAUDE.md` | `6d5e5c55f89c2c0dfa53200afe4f2d35732c09994cfe94b1ea1db4e924a14a12` | PASS |
| `src/SecurityDomain/PACKAGE_README.md` | `854d204ed4b648c436209aba6b87f055a0133b92a458a0b5f597bb8375386b9b` | PASS |
| `src/SecurityDomain/README.md` | `0a290632f4eb8db0eb6e051e69ad555e567009f8df4b888d93cefd922f9508f5` | R1 |
| `src/SecurityDomain/src/CaIdentifier.cs` | `6b3ce2f017aa678dffa796366b2850ecf56c53fd974848ce818651e3cec2ecf8` | PASS |
| `src/SecurityDomain/src/ISecurityDomainSession.cs` | `7862421d402c5bbeaefcaa523b5f65aa62968f69545cb5b716c4498a06009861` | R1 |
| `src/SecurityDomain/src/IYubiKeyExtensions.cs` | `40aeb7d45b6717b92a83caa7306ad6c204674b190ee12605e9413826de58183c` | PASS |
| `src/SecurityDomain/src/KeyInfo.cs` | `10d445ef6df239e4518a700f968208c8ea16c5af64a3a5eab31e744e4ed7a1e2` | PASS |
| `src/SecurityDomain/src/SecureChannelException.cs` | `7227829b83651cf905969fd52fdbd14d01f7301c27281ff249769407616b2f82` | PASS |
| `src/SecurityDomain/src/SecurityDomainSession.cs` | `5a67ccf399c1bd60c53485fba0976eb5dc492bb110fad2415f44463d4ab3f2e0` | PASS |
| `src/SecurityDomain/tests/CLAUDE.md` | `fa5db89cf8e780481a88245a5d4f52d716e13cb6f63b9fd2766de1bb788530c5` | PASS |
| `src/SecurityDomain/tests/README.md` | `658b60cae8657c4e0fe2d44fe5cbc7c174b606fde246b97bb4ad0c2ecd796394` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/ScpCertificates.cs` | `60fed66b3ad1d08e1a6bef9a70ae217c5d29960a2ccb755f9e6657e1d62a025f` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_NegativeTests.cs` | `fc3459f453a3be25d129a222d31e29ba879f8c62dd214df6115b5abacc9a88aa` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs` | `62b28bb2b347f795da553fbc8ca3234c30b7df2558191f18aa7ea503d129da3a` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs` | `8295999e0895306b014de9e3f6d28513d7e61a53fe9d46b23d765b8dc357dd7e` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs` | `f4b5cf41174b82a25d6b1c8e3a23a00cd02cd3062fb4a196506d6ebbb3a1d285` | R2 |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/SecurityDomainStateRestoringTests.cs` | `1ca525a8e82b2b690d41952b15d69cb67d2fc98b4e88e407fa433d97766f4bcc` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs` | `0af1b49de21ffee8cb937379b98869bd20404a0a99798e880d135a0a7d360778` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs` | `779c5caa812bb132918302fa016fd5fb8264483e312da3e8048ef24c9e188207` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs` | `28facb769793997f44eefa40418436e4b58dc085ad803cd09cd0137dd06539c2` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecureChannelExceptionTests.cs` | `f8edd5cb5a3f1240aef6a65b5257f1aa285d12db2f3d4212b3b4c5700bc45f04` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainCertificateBundleTests.cs` | `eb150df5f8103ddf02a1ca8cc6305a329a8a10f3bb8e576941bbb1d911c70f09` | PASS |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs` | `493adcd9ce5bcd8f30ec01512d61141d6542401d65402beec660959eded03969` | PASS |

## Remaining coverage and limits

No authored-path review gap. Package README was reviewed despite its initial clean inventory status;
coordinator still owns sampling reconciliation. The original clean-unit campaign manifest is not
silently marked sampled by this report. Final GP/Card/Amendment-D/ISO originals and actual device/profile
conditions remain evidence gaps; inherited ownership/authorization/configuration/transport/logging remain
SD-DEP-001–005 for Core's final target. No new lifecycle policy, implementation fix or all-platform/security
certification is accepted. Unrelated map YAML defect DOC-002 remains untouched and out of verdict scope.

No product edits, formatter, build/test execution, example-host execution, hardware, commit or further
agents. Did not inspect or poll the coordinator's validation/checker outputs and use none as proof.
Only `review.md` and `evidence/review-sources.md` were authored. Required corrections need a fresh follow-up
content/hash review; compilation and repository gates remain the coordinator's separate evidence.
