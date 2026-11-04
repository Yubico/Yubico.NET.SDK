# D — fresh independent administrative documentation review

**Date:** 2026-11-04. **Reviewer:** `github-copilot/gpt-6.1-sol`, fresh independent session.
**Verdict: one narrow required test-comment correction before acceptance.** The remaining
reviewed documentation is accepted for its stated scope. One optional clarity improvement is
separate below. This is not implementation completion, device qualification or a passing test run.

## Scope and independence

Reviewed all **20 changed D paths** in `/tmp/opencode/ctap2-001-edit-d` against
`5b09a9b6942ded3913b5c9340c3e126617e61a82`; HEAD independently matches that baseline.
Read every file completely, including unchanged methods, fixture factories, fake protocols,
assertions and explanatory units, and the entire 1,814-line baseline-relative diff.
Source outside D is dependency evidence only, not acceptance of its stale worktree prose.

Read root `CLAUDE.md`, available skills, module/test `CLAUDE.md`, `docs/TESTING.md`,
`docs/SDK-HOUSE-STYLE.md`, canonical campaign workflow/approved style, run `plan.md`,
`triage.md`, `investigation-d.md` including its leading erratum, `edit-d.md`,
`framework-adjudication.md`, `review-b.md`, `correction-b.md` and D's retained primary quotes.
Loaded security-guidelines for bounded ownership review, applying root semantic classification
rather than the skill's coarse firmware table; loaded verify for report/scope checks only.
No build, test, hardware operation, compiler probe, formatting, commit or delegation occurred.
This report is the only authored file; no source, assertion, API, token or executable string changed.

Narrow dependency reads checked actual bodies, not inherited documentation conclusions:

- `Ctap2Session.cs`: complete fresh GetInfo, generic SendCborRequestAsync/GetInfoCoreAsync,
  SendCborAsync and initialization-check units; generic requests supply `UserPresenceNotification.None`.
- `Backend/HidBackend.cs` and `Backend/SmartCardBackend.cs`: exchange/status/response-transfer
  units; Core HID SendCommandAsync and PC/SC TransmitAndReceiveAsync guard individual exchanges.
  No guard surrounds the administrative begin/next loop.
- `Pin/ClientPin.cs:263–304`: complete GetPinTokenAsync explicitly selects legacy subcommand 0x05.
- Integration `Ctap2TestHelpers.cs` and `TestExtensions/Ctap2TestStateExtensions.cs`: complete
  files, confirming default normalization before callbacks, discarded tokens, callback-limited
  cleanup, broad shared-RP deletion and partial two-change recovery.
- Retained pinned `CborWriter.Map-v10.0.2.cs:117–175`: map-completion sorting mechanism;
  framework-adjudication's actual 10.0.2 binary probe remains the authoritative bounded runtime
  evidence. No duplicate probe was run.

## Required correction

### RD-R1 — replace the residual unproved nondecreasing-count comment

**Required; low severity, test-evidence precision.**
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs:205–227`,
`EnrollCaptureNextSampleAsync_WithPoorQuality_ReturnsStatus`, still labels its single supplied
count at line 213:

> `3); // Remaining doesn't decrease`

There is no preceding begin/sample count in this test, and its sole result assertion at
line 226 checks `LastSampleStatus == TooFast`, not RemainingSamples or a before/after delta.
The general class qualification does not make this local claim true. The analogous comment
in `BioEnrollmentModelsTests.cs:196` was correctly made fixture-specific by the editor.

Replace only that comment, for example:

> `3); // Supplied count; this test asserts only the decoded TooFast status.`

Do not change the fixture, method name, assertion or executable tokens. This is within
CTAP-DOC-010/012 and existing D-15 / CTAP-IMP-022 proof-scope work, not a new implementation
bug. Re-read this complete test after correction and update its hash; all other reviewed hashes
can remain accepted if unchanged. **Overall PASS remains pending this correction/re-review.**

## Optional improvement

### RD-O1 — clarify which enumeration the extended tests perform

**Optional, not an acceptance blocker.**
`Ctap2CredentialManagementExtendedTests.cs:29` says “presence of two created IDs in RP enumeration”.
The body invokes `EnumerateCredentialsAsync` for one RP, not `EnumerateRelyingPartiesAsync`.
“presence of two created IDs in credential enumeration for one RP” would remove that ambiguity.
The detailed comments and actual two-ID assertions already establish the narrow intended meaning.

## Accepted semantic dispositions

Paths below expand under `src/Ctap2/`; the exact byte manifest follows. Each row covers the
complete affected documentation/comment units in that file, not only individual changed lines.

| File | Independent disposition |
| --- | --- |
| `src/BioEnrollment/BioEnrollmentModels.cs` | Accept all enum/model/property/decoder explanations. 0x0B/0x0C/0x0D are explicitly local mismappings, standard 0x0E is identified; no storage-full or invented vendor profile. Missing Touch/Good/zero/empty defaults, key-8 name maximum, flat-versus-array schema and independent begin ID are explicit. IsComplete is not persistence or validated completion. |
| `src/BioEnrollment/BioEnrollmentSubCommand.cs` | Accept all constants' explanations. Correct standard 1–7 values are distinguished from unchanged shifted local constants and unused 8; no preview/vendor workaround asserted. |
| `src/BioEnrollment/FingerprintBioEnrollment.cs` | Accept class, constructor, every operation and encoder comment. Current shifted values, missing modality authentication prefix, sensor subcommand omission, wrong timeout field and wrong inventory shape/status remain candid limitations. Null timeout has no helper ten-second default. Explicit enrollment cancellation is distinct from .NET cancellation and not automatic. Retain begin ID through captures; no fabricated sample budget, transaction policy or notification guarantee. Session/protocol/token are borrowed; cleanup limits remain. |
| `src/Config/AuthenticatorConfig.cs` | Accept full helper/constructor/operation/transcript units. Always-auth helper differs from standard unprotected-device exceptions; capability/state/authorization are distinct. Enable is not an EA ceremony/certificate; toggle is not setting a desired value and unknown outcomes need reconciliation. Unicode-code-point minimum, local 4–63 validation, separate byte constraints, list replacement/omission and force policy are precise. Legacy 0x05 PinInvalid versus actual 0x09 PinPolicyViolation is correct for the named revision. No automatic verification/rollback/erasure is implied. |
| `src/Config/ConfigSubCommand.cs` | Accept Section 6.11 correction, active 1–3 meanings and unused local 4/5 qualifications. Standard vendorPrototype is 0xFF; recipients are subcommand-3 parameters. |
| `src/CredentialManagement/CredManagementSubCommand.cs` | Accept all seven identifiers/values and exact Section 6.8 reference. |
| `src/CredentialManagement/CredentialEnumerationResult.cs` | Accept ownership, first-response total and disposal units. Collection remains accessible; owned entries' raw/key properties are revoked and captured views cleared; independent public data survives. Null total proves no completeness. |
| `src/CredentialManagement/CredentialManagement.cs` | Accept helper/lifetime, metadata, both eager enumerations, delete/update and all changed internal explanations. Missing or nonpositive totals are not normalized/validated into conformance; continuation failures return no partial result and credential failures dispose decoded entries. No operation-wide guard or prototype fallback. Raw original-RP hash routing, global unbound cm versus matching-RP administration, name replacement with preserved handle, no blob/account cleanup and disposal-marker limitations match source. Final request only is cleared. Automatic canonical writer explanation is correct; no broken-update inference from insertion order. |
| `src/CredentialManagement/CredentialManagementModels.cs` | Accept estimate/default/raw-copy/total/hash/truncated-display distinctions. No required-count/nonnegative/hash-length validation or capacity reservation invented. |
| `src/CredentialManagement/RelyingPartyEnumerationResult.cs` | Accept order/read-only wrapper/copied total distinctions; no inferred completeness or secret-owner disposal requirement. |
| `src/CredentialManagement/StoredCredentialInfo.cs` | Accept all changed copy/total/lifetime units. One complete response clone is owned; LargeBlobKey is its view. Borrowed input/transport/caller clones are outside disposal. Independent user/descriptor/encoded COSE public map survives; no private-key claim. Retained secret copies require caller clearing. |
| `tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AuthenticatorConfigTests.cs` | Accept complete changed summary/test/recovery comments. Default normalization precedes option checks; minimum target can equal existing minimum and only >=6 is asserted. Force-state assertion uses correct legacy 0x05 PinInvalid, not permission 0x09. Toggle restoration is success-path-only and not re-read; recovery and token/PIN cleanup are partial. No executed lifecycle qualification. |
| `tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2BioEnrollmentTests.cs` | Accept sensor-only evidence and provisioned-state versus support distinction. Positive decoded sample hint/kind is not enrollment, persistence, real standard exchange or sensor qualification. Explicit token finally does not clear discarded setup tokens. |
| `tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs` | Accept, with optional RD-O1. Actual update checks matching ID/display name only; multiple-user test checks count >=2 and both IDs. Narrow version gate is not negotiation. Broad shared-RP cleanup and omitted/success-only token clearing are explicit. |
| `tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementTests.cs` | Accept all changed units. Normal early returns are not explicit skips. Enumeration ID presence and metadata nonnegative checks are narrow. Successful GetAssertion does not fail deletion test; accepted NoCredentials alone is not proof. Cleanup can fail or bypass clearing. |
| `tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/BioEnrollmentModelsTests.cs` | Accept supplied status/count/zero/raw-copy and local-schema explanations. No missing-field rejection or real completion asserted; no unproved vendor extensions. |
| `tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs` | Accept except RD-R1. Modality-only/timeout fixtures preserve known limitations, InvalidCommand is not empty inventory evidence, begin-only Authenticate observation is not all-operation transcript proof. Cancel/rename/remove count sends only. Repeating-ID loop leaves omitted-next-ID case untested; fake zero tags/identity encryption are not crypto or sensor evidence. |
| `tests/Yubico.YubiKit.Ctap2.UnitTests/Config/AuthenticatorConfigTests.cs` | Accept all changed units. Actual enterprise/toggle prefix assertions cover 32 ff bytes, 0x0D and subcommand; artificial version-2/16-byte tags are not real MAC proof. RP/force tests inspect map sizes only; mocked numeric acceptance is not device policy. |
| `tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementModelsTests.cs` | Accept automatic sorting, incomplete opaque COSE fixtures, policy-2 optional-with-ID-list correction and exact owned-view proof limits. Failure test preserves source but cannot observe its inaccessible clone clearing. |
| `tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementWireTests.cs` | Accept selected metadata/delete request fields, copied totals/order/wrappers, disposed-view and failure-propagation evidence. No continuation-byte/global-sequence/update-transcript/recovery or real protocol-2 proof. |

The prose generally follows the approved direct, calm, precise voice. Necessary limits stay
beside actionable contracts rather than hiding defects or requiring reconstruction from the ledger.
There are no consumer XML code/example blocks added in these units. Existing test bodies remain
test-dependent evidence, not runnable consumer recipes or claims of a ceremony performed here.

## Independently fetched original evidence

Access date **2026-11-04**. Fetched the exact original over HTTPS (200), not a search snippet or
investigator excerpt: *Client to Authenticator Protocol (CTAP)*, **Proposed Standard, June 21, 2022**,
[CTAP 2.1 2022-06-21 errata](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html).
Read the complete original Sections 6.7/6.8/6.11, focused 6.4/6.12, stateful-command conditions,
6.5.5.7.1–2 and 7.1.3/7.2.3. No automatic upgrade to a newer CTAP publication.

| Original clause | Consequential evidence / actor and scope |
| --- | --- |
| §6.7 tables, §6.7.3–4 | Standard begin/next/cancel/enumerate/rename/remove/sensor are 1/2/3/4/5/6/7. Platform sends sensor modality=1 AND subcommand=7. Begin/next authenticate `fingerprint(0x01) || subcommand || subCommandParams`; timeout is unsigned parameter-map key 3, not top-level Boolean getModality key 6. Sensor supplies maxTemplateFriendlyName key 8. These original algorithm instructions are not weakened to fit current code. |
| §6.7.4–8 | Begin response supplies ID/status/remaining; next supplies status/remaining and need not repeat ID. Platform sends the begin ID until zero/unrecoverable error/cancel. Cancel is a separate unauthenticated modality/subcommand request. “If there are no enrollments existing on authenticator, it returns CTAP2_ERR_INVALID_OPTION.” Inventory success is key-7 array of all nested templates, required ID key 1/name key 2 optional. Friendly-name maximum SHOULD be lesser of 64 bytes/advertised maximum. |
| §6.7 feedback table | 0x0B=FP_EXISTS; 0x0C unused; 0x0D=NO_USER_ACTIVITY; 0x0E=NO_USER_PRESENCE_TRANSITION. Completion-token nonexpiry text is a Note, not a guaranteed token lifetime. |
| §6 stateful commands | Authenticator MAY assume globally exclusive adjacency and <=30-second gaps, MAY return NOT_ALLOWED if violated; MUST discard sequence state when initializing token expires. State SHOULD NOT survive power cycles. Not a guaranteed 30-second lease or SDK whole-sequence guard. |
| §6.8.2–7 | Platform MUST NOT associate RP with cm token for metadata/global RP enumeration; authenticator accepts unbound or matching-RP token for credential operations. Platform uses first total-1 continuations; standard next lists subcommand only. Estimate and platform large-blob cleanup are explanatory Notes, not fixed storage/device cleanup promises. Different user handle returns INVALID_PARAMETER; absent/empty names are removed. Returned RP text may be truncated, not comparison/hash identity. |
| §6.11, §6.11.1–4 | Standard 1/2/3/0xFF; authenticator MAY implement none/some/all. Authentication transcript is `32×0xff || 0x0d || uint8(subCommand) || parameters`. Normative algorithm permits specified unprotected states to skip verification. Opening authnrCfg platform instruction is explicitly a Note. Enable is idempotent; disable alwaysUv can return OPERATION_DENIED. Minimum is Unicode code points; all three policy parameters optional; minimum cannot decrease; nonempty mutable recipients overwrite previous additions plus immutable preconfigured list. Omission/empty is not list clearing. |
| §6.5.5.7.1 versus .2 | After correct-PIN verification, forcePINChange=true: **legacy getPinToken 0x05 returns PIN_INVALID**; **permission getPinUvAuthTokenUsingPinWithPermissions 0x09 returns PIN_POLICY_VIOLATION**, platform SHOULD direct PIN change. API name/protocol version or earlier permission acquisition does not change the owned test's later explicit 0x05 call. D-14's original wrong-status subclaim is rejected, not all its genuine cleanup/setup findings. |
| §6.4/6.12, §7.1.3/7.2.3 | bioEnroll=false is supported/unprovisioned; ep=false is supported/disabled, unlike false/absent credMgmt/authnrCfg. Prototype use forbidden when bioEnroll present. Reset MUST disable EA; alwaysUv targets make/get operations, not all metadata. No new rollback/reset advice inferred. |

Independently fetched both exact official Yubico manual URLs (200):
[5.7 firmware specifics](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html)
and [FIDO application specifics](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-fido.html).
The former explicitly says “credential management introduced in 5.2.1” before standard approval;
that is not qualification of this helper's standard 0x0A path on that firmware. The latter's
alwaysUV defaults table says YubiKey 5 FIPS cannot disable it, while Bio can toggle; EA requires
custom provisioning/platform/RP support, and FIDO reset disables EA. Its feature table and
5.7.4 prose give coarse/differing chronology, not exact earliest standard-wire support.
No original vendor definition obtained for local bio key 4, sample-count key 8, subcommand 8
or config local 4/5. The edited source correctly refuses to invent those profiles.

Context7 independently resolved .NET and queried `/dotnet/docs` for Memory usage Rule 4:
terminal means **completed, faulted or canceled**, and constructor-retained memory is consumed
by later methods (Rule 5). This supports borrowed-token lifetime through awaited use, not lifetime
ending when cancellation is merely requested. Writer query against
`/websites/learn_microsoft_en-us_dotnet_api` returned **no match**, not evidence of writer behavior.
The pinned-source/binary framework adjudication settles automatic sorting: closing the actual
Ctap2Canonical map sorts `displayName,id,name` into `id,name,displayName`. Both CM signed/final
parameter maps use the same closed helper. **D-08 wire-order defect is rejected; only its old
alphabetical explanation was wrong.** This is not complete update interoperability proof.

## Exact reviewed SHA-256 manifest

SHA-256 over exact bytes: baseline Git blob versus current reviewed worktree file. All 20 files
were read completely. This identifies the review target, not a mechanical executable/API gate.

| Exact path | Baseline SHA256 | Reviewed SHA256 |
| --- | --- | --- |
| `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs` | `df1758f48df228818fe6b02ff0a1989f70b541f2d64f63c08d8972862c9a30fe` | `eb94f6b2cf50b3f8b5d0dca00a2557fbff7cb27c37fe0b34258ac4d0c74fec8c` |
| `src/Ctap2/src/BioEnrollment/BioEnrollmentSubCommand.cs` | `9fce2fc0afa8aa42702032609531ea717017e9646b4053332591e9b2a75c0e36` | `d84e59d47a7a3ed9b620d35421af57281455d8609f166ed3b363d2c1820d0845` |
| `src/Ctap2/src/BioEnrollment/FingerprintBioEnrollment.cs` | `dd4feafc985ac8fe3ba6c8c1f2fc936f17590d60a1ca045d0a78fd7133a6777b` | `83c6d85d068f428eedd44b647f412dfe423018cf83c6cd906ad13cf9133c342e` |
| `src/Ctap2/src/Config/AuthenticatorConfig.cs` | `2e08ef1fe9febc7c006f7413df6d078b2a313c2cfb9839aff481049fc4fd781a` | `1a2d87b21bbd3c44662a7440eb24dff811116e7f4927a0dc6387ee31c770514c` |
| `src/Ctap2/src/Config/ConfigSubCommand.cs` | `5d791b1bba0856f372faa8883464c0fd8433316c3e48865811a7b77f4ee86098` | `02ca8401707ec71f6f8e7a8155bb76d08269e9bed2a93a6aafddb87d271497a3` |
| `src/Ctap2/src/CredentialManagement/CredManagementSubCommand.cs` | `0eabfefab902c24b2abf37277a8a39212a718e97c76a15a9cf3cef29f1106ddb` | `3a371e877214c6e91958288782b5e90092a8fb7a605f3df9d90bbd5267bfe1c2` |
| `src/Ctap2/src/CredentialManagement/CredentialEnumerationResult.cs` | `1e25e0271556e9bf3edb9a34eaa67b7fb0a011cf88fe552d715ce5c52e26c62e` | `ca0ac675b05e69f0f0aa4f06fb838c48f3db366104519f9e06767fc910cb993e` |
| `src/Ctap2/src/CredentialManagement/CredentialManagement.cs` | `6737c0b1a5948f6479810c6336e7ef2d4978738f5041516bef7e3009f29243f1` | `469dd9c89df16bb08f180ed15b461544bf3641c59e2715b0c047796697771fc4` |
| `src/Ctap2/src/CredentialManagement/CredentialManagementModels.cs` | `54e05826700f1b961e3af7bffd4b89e0c1d34870bae1c6702331eeca077372fb` | `9dcb8d733224c937d22d32952286510af00c422113096dcae4a03ec37dae7262` |
| `src/Ctap2/src/CredentialManagement/RelyingPartyEnumerationResult.cs` | `e663e11f562591197a2fd6e0042390394ca2b4b34192a3c291b0e86056bd763a` | `d6163edb683e41dd2d4ae6e307bec75d00cb871f2b3992d911d016997d2e7540` |
| `src/Ctap2/src/CredentialManagement/StoredCredentialInfo.cs` | `a7d8f547b46f81ccc6c2d35013dea7125083f359ee6cab92568d81322dd847c4` | `f8c192db143e2a81f1a48ef130388cc6f101bf7cf8e4808597c3a0444edf8447` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AuthenticatorConfigTests.cs` | `e135154ee80d4bb533ce350424f1d43567efe210bf774e20d5f1afcd4ddd3562` | `3ed0e895bdbf21c8a6fce12a79f50fb7dcb8826fc0817e22877c8d664a170751` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2BioEnrollmentTests.cs` | `f897183aab6c00a579ec6c2b95c6ef910f5d60e3f88c3a72affe7427e013c804` | `6b6929d7dca268d8631dddc9b5a87c2ee03466796f34070799eeb9042c9c79cc` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs` | `799f0b13acdfa06a5e7989f6609fb62ce083b20fc79b0e2141d267e7f60d521d` | `a0dc31337bc40de9a268ef4aa6e7ed5352588de1deb67d9f623df73c2589c8b0` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementTests.cs` | `11cec719d989b3d52033c4f0a2ab85c28a7817276eff1e595a1291e3d08373f8` | `bf6200e9519e184ffdea0e7ad9f9f1e213ab5849c9609213626867dfe875e16c` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/BioEnrollmentModelsTests.cs` | `fbd364f760d1858e7846b46c90ddb7812ad403bc639240527b978b79f464f54c` | `48c1302cf6d46ec5b21d1e01bfd1e45cca4058fa241c6690ff5bf667028c8eb4` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs` | `de2725041e6ab1377cbd5fbe37c88211f158b464530834e128544b612918b706` | `0c8aa16392483fc9b4bf59ed0a5685964cacd74783a9dfcf088e6e2d932ca8a4` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Config/AuthenticatorConfigTests.cs` | `06d2332a6946c7651718012f7cd4b7c4cd4a7eae30f0629aa6460dae8c8af93b` | `80ab3a142170db310cf7864d257c28cf4b5b133d98586f34123732b3de8e363f` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementModelsTests.cs` | `feb36053a496a44799e9fb90460adb25daff2ecc7b42bb7ba3b201e20c0984d8` | `ff90f7ff128488ece2a2246161b5650a19b4f782e8662fde5eb6d1ab06661279` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementWireTests.cs` | `533d1d9dd904619f2351cb468a7edc37e88e6bc56a643cddf5d93a7afd26d391` | `ee336f7200f11c5315a6f79f61e5de4acd17cf89f45724f6b43176c9c94cf697` |

Read-only Python used `git diff --name-only BASE`, `git show BASE:path`, Path.read_bytes and
hashlib; exact 20 changed paths and baseline HEAD were confirmed. Exact editor-command diff
`git diff --no-ext-diff --no-color` SHA-256 independently matches `edit-d.md`:
`795ecb234cb9a18b7992f467c6cef7aa37968d9b8d506af36dbe7b398883912d`.

## Limits and handoff

RD-R1 is pending; RD-O1 is optional. Coordinator owns mechanical compile/API/token/directive/XML-cref
checks and later integration gates. No source-preservation lexer, SDK build/test or hardware gate
was run by this reviewer, and other sessions' previous results are not this review's passing gates.
No exact earliest SKU/standard/prototype firmware matrix, original vendor profile, certification,
sensor persistence, timeout/cancellation recovery, force-policy hardware lifecycle or provider-internal
erasure was established. D-08 and D-14 status errata do not close genuine remaining work.

Retain CTAP-IMP-012–016, 018–022, 024 and CTAP-DEC-003 dependencies with their existing scope;
IMP-017 is rejected ordering-runtime inference, not a required encoder fix. Reconcile A/B/C/E/F/G
guides/examples/boundaries at integration, especially generic presence silence, individual-exchange
guards, borrowed versus ClientPin-owned protocol, begin-ID retention, totals/raw hash routing,
configuration versus minPinLength reporting, legacy versus permission force status and known-PIN
setup/cleanup. Those owners' stale source prose is not within this final review's acceptance scope.
Any changed hash reopens the affected semantic units rather than inheriting this verdict blindly.
