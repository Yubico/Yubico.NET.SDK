# B — fresh independent PIN/UV documentation review

2026-11-03; reviewer `github-copilot/gpt-6.1-sol`, fresh independent session.

**Verdict: required clarification before acceptance (one narrow XML correction).** The other
reviewed changes are factually supported and useful. One optional addition is recorded separately.
This is documentation review, not implementation repair, cryptographic conformance or hardware qualification.

## Scope and independence

Reviewed the uncommitted B patch in `/tmp/opencode/ctap2-001-edit-b`, HEAD/baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Read all 13 changed files completely, including
unchanged executable bodies, assertions, fixtures and private helpers, rather than accepting the
investigator/editor conclusions or reviewing only diff hunks. Read the complete diff against that
baseline. All affected XML/comment units have a disposition below.

Read root `CLAUDE.md`, available skill descriptions, module/test `CLAUDE.md`, `docs/TESTING.md`,
`docs/SDK-HOUSE-STYLE.md`, campaign workflow/style, run `plan.md`, `triage.md`,
`investigation-b.md`, `edit-b.md` and `evidence/b/original-clauses.md`. The request's compact
`plan/triage/investigation-b/edit-b` refers to those sibling Markdown records, not a nested directory.
Loaded secure-credential-prompt and security-guidelines for ownership/security review; no PRD was produced.

Independently fetched both exact original CTAP publications below. Read their complete consequential
PIN/UV passages and focused 2.2 persistent-token passages, including the PIN-change reset of persistent
permissions. Context7 independently checked the asynchronous Memory lease and ECDH import APIs;
official framework/RFC originals provided the focused follow-up. No source was edited. This report is
the only authored file, in the primary checkout. No builds, tests, hardware/PIN operations, commits,
delegation, example compilation or mechanical token/directive/API gate were performed.

Narrow dependency evidence reads (not whole-file documentation acceptance):

- `src/Ctap2/src/Ctap2Session.cs`: complete `GetInfoAsync`, `GetInfoCoreAsync` and generic
  `SendCborRequestAsync` units, including initialization and notification dispatch.
- `src/Ctap2/src/Cbor/CtapRequestBuilder.cs`: both complete `WithBytes` overloads.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs` and
  `TestExtensions/Ctap2TestStateExtensions.cs`: complete files, to check normalization,
  token discarding and callback/cleanup boundaries against actual calling tests.

## Required correction

### RB-R1 — name the authenticator implementation obligation and the caller's UV prerequisites

**Required; precision/usefulness, not a new implementation defect.**
`src/Ctap2/src/Pin/ClientPin.cs:466–469`, `GetPinUvAuthTokenUsingUvAsync` remarks currently say:

> CTAP 2.1 requires this subcommand when both uv and pinUvAuthToken are true.

The exact 2.1 §6.5.5.7.3 statement is an **authenticator MUST implement** obligation, not a
platform MUST invoke obligation. The current compressed sentence omits its actor and can be read as
requiring invocation. This is also the location where a direct caller needs an explicit instruction
to check token capability as well as configured UV; unlike the PIN method, this method queries no
GetInfo capability before sending 0x06 (`ClientPin.cs:483–525`). Keep the existing permission-specific
checks, single-request/no-automatic-fallback distinction and interaction qualifications.

Suggested documentation-only replacement for the opening of that paragraph:

> Use this method when the authenticator reports both uv=true and pinUvAuthToken=true, with
> built-in UV configured. CTAP 2.1 requires the authenticator to implement this subcommand under
> those conditions.

This names both actors without inventing helper preflight, negotiation or mandatory caller fallback.
It is within CTAP-DOC-005 / B-05 and the existing CTAP-IMP-010 dependency; do not add a new bug count.
Re-review the complete changed paragraph after correction. No executable change is requested.

## Optional improvement

### RB-O1 — make forced PIN change discoverable from token API documentation

**Optional; useful local context, not an acceptance blocker.** Consider one short class-level or
token-method sentence explaining that a configured PIN is not sufficient for issuance when
`forcePINChange=true`: change the PIN before requesting a token. The exact 2.1 legacy 0x05 flow
returns PIN_INVALID for that state; 0x09 returns PIN_POLICY_VIOLATION. The helper does not check it
locally. Do not describe every PIN_INVALID as forced-change evidence or add a retry policy.
Source: `ClientPin.cs:312–453` forwards requests/errors without that preflight; original 2.1
§6.5.5.7.1–2 explicitly handles the state. Existing generic additional-device-policy qualifications
are truthful, so this is an optional navigation improvement rather than a factual rejection.

## Accepted semantic dispositions

| Changed file / full affected units | Independent disposition |
| --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` — class, constructor, Protocol, retry methods, all PIN/token methods, validation/padding/parser comments, Dispose | Accept except RB-R1. Caller selects protocol; session borrowed; helper disposes supplied protocol, not merely borrows it. Inputs stay unchanged through task terminal state. Returned decrypted arrays are separately caller-owned, with absent length/scope metadata clearly disclosed. Exact concrete-session fresh-GetInfo fallback is not negotiation and does not retry failed 0x09. Unicode obligations and byte-only validation are separated. Ordinary scope/lifetime and uv-key omission match originals. Named finally cleanup is not promoted to end-to-end erasure. |
| `src/Ctap2/src/Pin/ClientPinSubCommand.cs` — parameter/response constants, permission type and lbw/pcmr members | Accept. General retry query does not require protocol field; optional PIN-only powerCycleState is not a UV recovery signal. RP scope and lbw write role are correct. pcmr requires perCredMgmtRO=true, alone, ignores RP, returns persistent token and does not imply all metadata share its key. |
| `src/Ctap2/src/Pin/IPinUvAuthProtocol.cs` — class and every crypto-member contract | Accept. V1 SHA256 versus V2 separate HKDF, lengths/key portions, no implicit Kdf in Encrypt/Decrypt, ownership and no integrity claim are accurate. Local Verify is not device authorization. Required COSE structure remains a requirement despite disclosed missing checks. Import exception possibility is not collapsed into the derivation wrapper. |
| `src/Ctap2/src/Pin/PinRetryStatus.cs` — class/properties | Accept. Parsed/defaulted snapshot; omission versus explicit false is not recovered by the model. Temporary and exhausted blocks are distinguished. |
| `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` — ECDH contract/check/import comments | Accept. Coordinates alone are explicitly checked; kty/alg/crv and optional restrictions are missing checks, not intended compatibility policy. Provider import precedes catch. Dictionary allocation after deriving Z lacks the clearing guard; no provider-erasure claim. |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` — algorithm/lifecycle/cleanup remarks and Dispose | Accept. Shared AES/HMAC key, zero IV, no padding and truncated tag are actual behavior. Disposal marks state, keeps scalar properties readable and cannot clear direct returned buffers. Cleanup wording expressly excludes all-exception-path guarantees. |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` — algorithm/salt/layout/lifecycle remarks, label comment, Dispose | Accept. Two independent 32-byte expansions, correct labels and key order. Empty/zero salt equivalence is mathematical HMAC reasoning, not executed provider/vector evidence. Layout code fence is a representation diagram, not an executable example. |
| `src/Ctap2/src/Pin/UserVerificationRetryStatus.cs` — class/properties | Accept. Field 4 parsing is disclosed as nonstandard for CTAP 2.1 UV; neither Boolean establishes standardized power-cycle recovery. Correct configured-PIN token recovery/reset alternatives. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnhancedPinTests.cs` — class/gate comment | Accept. Complete bodies verify the actual conditional scalar/range assertions, clientPin=true after SetOrVerify and early return. Default normalization may mutate before callback. No complexity enforcement or zeroing proof is asserted. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PinManagementTests.cs` — class/method/normalization/blocked/UV/error/restore explanations | Accept. Actual assertions are nonempty token/credential and response presence, not old PIN/token invalidation or returned UV. uv is absent in supplied options. Broad caught statuses do not identify policy. Restoration/cleanup is callback-limited; source strings remain frozen and their incorrect reset/UV claims are not adopted as documentation truth. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/ClientPinTests.cs` — class, fixtures/send/decrypt comments and fake summary | Accept. False fixtures omit field 4. Send-count cases do not decode fields; separate canonical cases actually assert command/map keys/protocol/subcommand. Fake crypto copies inputs/always verifies, so non-null arrays do not establish permissions, fallback, crypto or clearing. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV1Tests.cs` — class/peer-fixture explanation | Accept. Independent SHA256/HMAC expected computations exist; AES tests are shapes/round trips. Missing required alg in accepted peer fixture is accurately identified. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs` — class/peer fixture/public-coordinate explanation | Accept. KDF tests assert length/determinism, not independent HKDF vector. Fresh-call test compares public coordinates only. No remote key-agreement, token lifetime, erasure or device conformance assertion. |

The prose is generally direct, calm and precise. Public limitations stay beside the relevant
contracts; they are not an investigation chronology. Internal/test qualifications are useful because
the frozen assertions/diagnostics otherwise invite false inference. Do not remove those distinctions
as editorial churn, or normalize missing security checks into intended policy.

## Independently checked original clauses

Access date **2026-11-03**. Exact publications, not search-result dates or automatically upgraded versions:

- **S21:** *Client to Authenticator Protocol (CTAP)*, **Proposed Standard, June 21, 2022**,
  [original CTAP 2.1 errata](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html).
- **S22:** same title, **Proposed Standard, July 14, 2025**,
  [original CTAP 2.2](https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html).

Both documents' conformance rule excludes notes/examples from normative status. Original fetched
content was read independently, including focused paragraphs below; investigator excerpts alone
were not used as the review authority.

| Clause / anchor | Focused original quotation / actor, strength and scope |
| --- | --- |
| S21 §6.5.1 `#authnrClientPin-pin-composition`, §6.5.5.5 `#settingNewPin` | “Platforms MUST enforce” baseline “Minimum PIN Length: 4 Unicode characters”; “PIN are in Unicode normalization form C”; “PIN MUST NOT end in a 0x00 byte”; UTF-8 MUST NOT exceed 63 bytes. Authenticator MUST enforce four code points. Setting flow separately uses advertised minPINLength, default four, and authenticator MAY additional constraints. SDK's actual four-byte floor proves none of the Unicode requirements. |
| S21 §6.5.5.5–6 `#settingNewPin`, `#changingExistingPin` | “padded on the right with 0x00 bytes to make it 64 bytes long”; `LEFT(SHA-256(curPin), 16)` and `newPinEnc || pinHashEnc`. Normative platform encoding; no PKCS#7. |
| S21 §6.5.5 `#authnrClientPin-cmd-dfn`, §6.5.6–7 `#pinProto1`, `#pinProto2` | keyAgreement “MUST contain the optional 'alg' parameter and MUST NOT contain any other optional parameters.” getPublicKey specifies kty=2/alg=-25/crv=1/x,y32. ECDH must return error if parse fails or point is not on curve. V1: “Return SHA-256(Z)” where Z is 32-byte big-endian x coordinate; zero-IV AES with no padding; first16 HMAC. V2: `HKDF-SHA-256(salt = 32 zero bytes, IKM = Z, L = 32, info = "CTAP2 HMAC key") || HKDF-SHA-256(salt = 32 zero bytes, IKM = Z, L = 32, info = "CTAP2 AES key")`. Normative algorithms; following two-invocation note is non-normative explanation. V1 token MUST16/32; V2 MUST32. |
| S21 §6.5.5.4 `#gettingSharedSecret` | Platform “selects a mutually supported PIN/UV auth protocol”; if no preference, SHOULD select first-listed. Supported value MUST match GetInfo. Fresh shared secret each transaction. This supports caller selection, not an SDK negotiation promise. |
| S21 §6.5.5.7 `#gettingPinUvAuthToken`, `#getPinToken` | mc/ga RP Required, cm Optional, be/lbw/acfg Ignored. “remove all permissions except lbw” after operation testing UP. Legacy defaults mc/ga; first-use RP association is explained in a note and performed by Make/Get algorithms; noMcGaPermissionsWithClientPin=true suppresses defaults. Legacy rejects explicit permissions/RP. No administrative permission inferred from fallback. |
| S21 §6.5.5.7.2–3 `#getPinUvAuthTokenUsingPinWithPermissions`, `#getPinUvAuthTokenUsingUvWithPermissions` | “This subCommand MUST be implemented if the authenticator returns both uv and pinUvAuthToken option IDs set to true” — **authenticator implementation**, RB-R1. PIN equivalent uses clientPin/token. UV be/acfg require uvBioEnroll/uvAcfg; unconfigured UV returns NOT_ALLOWED. UV interaction can establish UP; PIN issuance calls beginUsingPinUvAuthToken(false). Platform recovery note permits conditional PIN fallback; it is not helper behavior. |
| S21 §6.5.2.1–2 `#authnrClientPin-globalState-puat`, `#authnrClientPin-globalState-retries`; §6.5.5 response table | Retry maximum MUST no more than eight, MAY lower. At zero PIN retries, reset only; at zero UV retries, reset or correct configured ClientPIN via token subcommands. Three mismatches produce PIN_AUTH_BLOCKED “indicating that power cycling is needed”; zero produces PIN_BLOCKED. powerCycleState: “If the field is omitted, no information is given” and “only valid in response to a getRetries request.” Ordinary timers/optional rolling period are authenticator state, not host-array lifetime. Change and ordinary issuance invalidate existing ordinary tokens across protocols. |
| S21 §6.1/§6.2, uv option definition | “Platforms MUST NOT include both the 'uv' option key and the pinUvAuthParam parameter in the same request.” Platform actor; omission applies even false. Returned UV is established by the authenticator algorithm, not the test's method name. |
| S22 §6.4 `#getinfo-percredmgmtro`, §6.5.5.7 `#pcmr` and issuance flows | pcmr “Persistent Credential Management Read Only”, 0x40, RP “Ignored”; only read CM. Unauthorized condition: “perCredMgmtRO is false or absent, or any other pinUvAuthToken permission is requested.” Normative authenticator checks, alongside configured PIN/UV/token capability requirements. pcmr branch returns encrypted persistentPinUvAuthToken, not an ordinary timed token. SDK raw array models no persistent identity. |
| S22 §6.5.2.2 `#authnrClientPin-globalState-persistent-puat`, §6.5.5.6 | Persistent state contains a permission set, not ordinary UP/UV/timer state. PIN change calls resetPersistentPinUvAuthToken, “all persistent permissions are cleared on pin change”. Thus no permanent-authority promise follows from the word persistent. |
| S22 §6.4 `#getinfo-encidentifier` | encIdentifier key: `HKDF-SHA-256(salt = 32 zero bytes, IKM = persistentPinUvAuthToken, L = 16, info = "encIdentifier")`; AES128 CBC iv||ct, regenerated IV each GetInfo. Establishes this field only, not encCredStoreState or every encrypted field. |

Framework follow-up: Context7 `/dotnet/docs`, Memory usage Rule4:
“terminal state” means “completed, faulted, or canceled”, applying also to ValueTask.
[Original guideline](https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md).
This supports the PIN-buffer lease wording; requesting cancellation alone does not end the lease.
Context7 ECDH lookup was insufficient on exact exceptions; a focused independent
[official Create(ECParameters) read](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdiffiehellman.create?view=net-10.0)
documents CryptographicException when parameters do not validate. Combined with the actual import
outside the SDK catch, this supports **possible** unwrapped errors, not a universal provider exception map.

Independently read [RFC5869 §2.2](https://www.rfc-editor.org/rfc/rfc5869):
`PRK = HMAC-Hash(salt, IKM)`; and [RFC2104 §2](https://www.rfc-editor.org/rfc/rfc2104):
“append zeros to the end of K to create a B byte string.” Both are Informational RFCs.
Empty and 32-zero-byte SHA256 HMAC salt keys pad to the same block key; unchanged info/IKM yields
the same HKDF output. This is algorithm reasoning, not an executed .NET vector or certification claim.

## Exact reviewed file/hash manifest

SHA256 over exact file bytes; baseline column is the Git blob at the full baseline above, edited
column is the reviewed worktree file. All 13 files were read completely. These identify the review
target, not executable-token preservation. No files outside this list were changed in the B source diff.

| Exact path | Baseline SHA256 | Reviewed SHA256 |
| --- | --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` | `0ed9d4b0ca55c9d09590ffaee2a8c10e9458ec8858c0e1a31810dd867222dc5d` | `57790e29a23941dae2fca1773b79728a2b4427361b0d11df8f4c3545d9f1b7e6` |
| `src/Ctap2/src/Pin/ClientPinSubCommand.cs` | `da314d9c8487dec1cf9277c53b20fee3d020d435433fe9800903027be62d05bc` | `b00e83166447955f42179ea19eac0135a05511ea101e4d142859666b20b4b3ca` |
| `src/Ctap2/src/Pin/IPinUvAuthProtocol.cs` | `c5c4e2ef0239738f26136d9d428646e6d2b3595e089fa792af4a695769742207` | `207d521fbbcbbb073fc2cf1bc84427d6ad52b4ccb270c59cefcfe3d8abc88fd0` |
| `src/Ctap2/src/Pin/PinRetryStatus.cs` | `561e8b40417a218d9438ac65f2797c09e7b7fa1499f19cd1798324fa330615e9` | `c30d9bff8dbdfcb3bdef6e9572345744f41f3d5d7028ef4a9775c2b86aefea8d` |
| `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` | `3ff46427d8b3a75b49f882184efdfe48d6d8809fe8a7043eed5bda9c8cc78e97` | `a09c413760f942fab7b091f371adc6846a93137c793b9bf3a50bdfe6525c9179` |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` | `bc9e6abe4e4faabe1cd5ec271e1a6f916d0ff919d8a5b707a30d0f10308e8559` | `77c2a8c7a00f94089d88ec392c6f313a74fe2be98da5e0d40ff0528091ca5c41` |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` | `067914b51ccaa81da3c7cc3f4c6c1c01b6542b95e278a6f64ed0f5784ec27f7d` | `fb8a03ce6864b15138e56d91ff401bced6d51dc4aed18afa2e20430b21b752d7` |
| `src/Ctap2/src/Pin/UserVerificationRetryStatus.cs` | `90f8f7db1743d39765a9dbf94e446b28f06c75b13c7f3d44b5323e64ee36aeb0` | `60d7250eb4d67522b8c131c18c23b195f89188a53a410143774e2c83db3ef461` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnhancedPinTests.cs` | `35ce8220403999bcf5b879cf62b64f1e8f9374ea5867e83ad742d249685c2e89` | `3a310e73455ca375f8352afc8dc6a0ddb4a53d5924b6305a029245db886cd26e` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PinManagementTests.cs` | `4dde07bd8e981a153c439f2f7fb0b7882f95695b281a75b2aecef4f30ea4f580` | `39602d50a59b4ed42d5bd798c4fbc780c5bccb730390997f179276c61d68d5b2` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/ClientPinTests.cs` | `fec34f5af3cf11c85a85e2019f79901cf6ddca0a69a8e925683827c02783f1d1` | `83d96f8add7ede53fb09f33de8d7f29f39d6e5a364d85cfafc524064fa688f22` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV1Tests.cs` | `5f8c1246987c011555ddd584dd538ce23cc4e26fc6acb688034d05149d8db77e` | `32d3b4edad5ad112979cc086b5130e72c1be92937dcccf54c27e2d3cc0e380f1` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs` | `d94efb8fe3e2c6e2bb96383812fc7e3906c8d5bb0e03c6844d30d2b076a44cdb` | `b1ec8a777138dfdae86eb80487488879ec32ac3c3124495b5f6874a14a676042` |

Read-only hash commands: Python hashlib over `git show BASE:path` / file bytes, `git rev-parse HEAD`
and `git diff --name-only BASE`. `git diff --binary | sha256sum` independently matches editor's
`b8fc9814c9d5754dd72436b9315127cc30818f7306418de5ee5cabf7b3ce13e0`.
Explicit `git diff --binary BASE` hashes to
`986901a6937cd68d136e2e384dea01e0bfcec07d5b22b55d779a899dd9c5241d`;
these are different diff-command representations, not an unexplained source-file mutation.

## Limits and re-review handoff

RB-R1 remains required/pending; RB-O1 is optional. No runtime/API/assertion/string change is requested.
Coordinator owns the later mechanical preservation, build, generated-cref and meaningful-example gates.
Inspected tests were not run and prove no hardware/UV/permission/retry/expiry sequence occurred.
No fault injection, provider-internal clearing, cross-platform invalid-point experiment or independent
HKDF vector was executed. Protocol references describe obligations; the SDK's missing peer/response/
Unicode/capability checks remain deferred, not fixed or endorsed by truthful qualifications.

Cross-owner guides, examples, administrative/extension consumers and WebAuthn were not independently
accepted here. Reconcile F/A/C/D/E/G dependencies from `edit-b.md` after RB-R1 and final integration,
especially uv omission, shared-protocol ownership, fresh versus cached GetInfo and persistent scope.
The F-owned unedited PKCS#7 pitfall and frozen diagnostics remain dependencies, not B acceptance.
Any changed file hash after this manifest reopens its affected semantic review.
