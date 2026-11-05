# PIV documentation edit handoff

## Outcome and authority

The documentation edit batch is ready for independent content/original-citation review and coordinator
validation. It changes 57 product documentation/comment paths: 48 C# files, eight Markdown files and
one YAML map. All 11 accepted documentation groups have corresponding edits. This is an editing
checkpoint, not a compilation, hardware qualification or campaign closeout claim.

Editor: fresh GPT-6.1 Sol, November 5, 2026. Baseline:
`727fe43afc430c5d8364d868a6436de50626e476` in the primary checkout. Read root/module instructions,
the run plan, findings, investigation, coverage, source analysis, workflow, records/style and SDK house
style. Loaded the module-documentation skill before README/CLAUDE edits and the verification skill
before handoff. The approved voice governed the edit: affirmative task/actor/behavior first, with
conditions, rationale and caller responsibilities nearby. Consequential limitations point to supported
actions. Full resource scopes take priority over minimum word count.

Only this run's `edit-report.md` was authored as a record. Other run records, the campaign README,
four original user files, `mise.toml` and coordinator-owned `src/Tests.Shared/appsettings.json` were
left with their owners. No builds, tests, hardware operations, formatting, commits or agents were run.
Executable code, assertions, imports, directives, runtime help/output strings and public API snapshots
remain unchanged. Descriptive project metadata needed no change. Root README, PIV package README,
highlights, Native AOT evidence and `applet-public-api.md` remain clean scope inventory rather than
receiving manufactured feature claims.

## Accepted groups and edits

| Group | Consumer/reference change and dependent commentary |
| --- | --- |
| PIV-C001 | README reset task, IPivSession reset contract, PivTool reset XML, maintainer gotchas, migration notes and the reset test's 9A-only assertion scope. Normal reset retains F9; retry consumption and post-success metadata failure are adjacent to the task. Bio preflight remains unfinished. |
| PIV-C002 | Credential byte limits/profile distinction, bounded retry decisions, PUK unblock and reset choice, retry-limit credential resets, exception semantics and corrected PIN/PUK example parameter units. AuthenticationProtocol and PinUtilities explain actual encoding/count mechanisms. |
| PIV-C003 | Algorithm-specific raw formats and owned mutable output in XML/README; complete host DER-ECDSA verification and known-plaintext RSA/OAEP comparison, both with finally cleanup. Extractor examples guard nullable metadata and dispose public-only host objects. Tool signing/decryption qualifications direct unsupported framing/cleanup tasks to the SDK. |
| PIV-C004 | Separate key/certificate storage and caller enrollment binding, compression defaults/threshold, raw metadata fields and unknown types, fixed CHUID/CCC shapes and borrowing/copying, retired-key history scope, ADMIN software-state/timestamp representation and raw-object route. Result-property ownership is local to the example types. |
| PIV-C005 | Complete protected setup/close/reopen/recover/disable method, already-active key prerequisite, backup retention, partial writes, remaining blocked PUK, ADMIN-only detection and legacy-derived recovery semantics. Migration recipe/map uses current names and None disable. |
| PIV-C006 | PIV management versus protocol/SCP versus card PIN state, observed authentication transitions, prompt sites and completion-before-unpadding, callback failure and cooperative cancellation/drain. Card policy descriptions use actual verification frequency and fingerprint MATCH semantics. |
| PIV-C007 | SDK gate versus released AES 5.4.2 origin, old-family subset/profile/transport qualification, ROCA predicate rather than a blanket safety claim, fixed superseded standards revisions and separate deployment compliance. Existing Android/Python pinned boundary distinctions remain. |
| PIV-C008 | Present factory/owned-array/response-clearing mechanisms, absent-data VERIFY versus padded empty credentials, Yubico factory-key convention, boolean SET touch choices, truthful prompt copying/CLI routing/JSON comments and preserved Bio/X25519 follow-ups. |
| PIV-C009 | Replaced the tool overview with task flow and all 22 command contracts, exact parser selectors and JSON/text fields, per-command sessions, reset/delete confirmation behavior, command-line secret exposure and current sample limitations. XML uses current named optional arguments and certificate disposal; the contention example uses a real SDK operation/exception. |
| PIV-C010 | Test commentary follows actual assertions: X25519 generation only, Ed25519 nonempty output/import metadata, one Always-policy signature, workflow ECDH shape rather than equality, move/certificate independence, attestation fields rather than chain trust, cross-key liveness versus binding, metadata before decrypt validation and incomplete timeout drainage. PSS host verification is described accurately. |
| PIV-C011 | Added interface inheritance to 39 previously undocumented facade methods; positional metadata fields describe roles/count units/encoding. Corrected shared PIV named arguments, current map paths/types/accessors, compression enum and source-present versus qualified capability language. |

The changed `CertificateCompression`, `KeyCreationOptions`, `InvalidPinException`, result-property and
reset-test regions refine already accepted groups. They are additional edited overlaps in the clean
pool, rather than newly discovered independent runtime findings. Self-review removed an erroneous
draft mention of a force-uncompressed enum value: the actual API has **Automatic and Always only**.
No additional material engineering miss was established beyond the accepted investigation ledger.

## Deferred engineering and re-opening

Preserve **PIVIMP-001 through PIVIMP-010** and **PIVCORE-001 through PIVCORE-003** exactly as recorded
in `investigation.md` and `findings.md`; this batch neither renames those labels nor closes them.
Their accepted path/reopening map remains authoritative. Reset Bio routing, partial PIN-only coherence,
firmware/profile qualification, typed-object generality, generic tool signing, sample ownership/UI/
cancellation gaps, crypto-response/padding/decompression handling, test oracles/drainage/cleanup,
ADMIN timestamp interoperability and raw object-ID range remain deferred.

The source-pinned timestamp difference is documented as an observed interoperability limitation;
choosing a desired encoding/migration policy remains Pass 2. Earlier PR745 retry fallback, authentication
invalidation, supplied-key validation, stale-derived restoration, raw unknown-algorithm preservation,
response clearing and mutable-output fixes are described as current behavior, not unfixed defects.
The three Core dependencies remain PIV-local boundary notes for the final Core cycle.

No new hardware outcome is asserted here. The coordinator's serial 103/125 baselines, independent
reset control and running unattended lane have their own records. Bio/NFC/other OS and broader device
profile qualification remain separate even if Linux USB execution succeeds later.

## Original-source checks

Read the retained originals rather than relying solely on source-analysis conclusions:

- Combined `evidence/nist-sp800-73-4-part1.txt`: archive cover/title/update status; Part 2 §2.4.3,
  lines 3825–3856 (card PIN ASCII decimal requirement, 6–8 bytes, FF padding; eight-byte binary PUK);
  Part 2 §2.4.2, lines 3792–3819 (card-owned security indicators); Part 1 §3.3.3 and Table 19,
  lines 1143 onward and 2209–2229 (retired keys and conditional URL); certificate tables/footnote
  at 2190–2222 (1856 is recommended and can be exceeded). These profile requirements belong to
  the card/issuer/application layers identified in the original, rather than unrestricted SDK byte validation.
- `nist-sp800-78-4.txt`: original title/date/archive supersession and §3.1/Table 3-1, lines 414–479,
  plus §§3.2.1–3.2.2, lines 481–598. Federal slot/algorithm/exponent and issuer-signature requirements
  are distinct from generic raw operations and demo certificates.
- `nist-fips201-2.txt`: exact title/date/supersession cover and §2.9.3 context beginning line 1317.
  The guide fixes the referenced revision; Yubico destructive applet RESET is not the issuer's federal
  verification-data-reset identity procedure. No latest-profile adoption is implied.
- `nist-fips186-5.txt`, §6.4.1 lines 1325–1338: hash-to-integer processing; `rfc3279.txt` lines
  378–383: ASN.1 ECDSA r/s structure; `rfc8032.txt` §5.1.6 beginning 719: original-message signing;
  `rfc8017.txt` signature-generation EMSA passages beginning 1888 and 2072. These explain arithmetic/
  formats without claiming each hash/algorithm pairing conforms to a federal deployment profile.
- `vendor-reset-command.txt` lines 29–63 and `vendor-technical-manual.txt` lines 132–179 and
  440–469: F9 retention, Bio MPE reset route, slot PIN-policy defaults, key/certificate independence,
  AES released origin 5.4.2 and 5.7 AES-192 default. The guide links captured successful source URLs,
  rather than the earlier guessed reset URL that returned 404.
- `vendor-ysa2017-01.txt` line 77: PIV 4.2.6–4.3.4 and ECC/import alternatives. The existing pinned
  Python/Android comparison remains implementation provenance; it supplies no new profile promise.
- Pinned `python-ykman-piv.py` lines 209–273: fixed big-endian timestamp versus the actual C#
  `AdminData` trimmed little-endian encoder. This remains PIVIMP-009.

Original URL/hash/access-time manifests remain `evidence/source-manifest.json` and
`evidence/followup-manifest.json`. Context7 additionally resolved .NET and queried the .NET 10
ECDsa.VerifyHash span/DSASignatureFormat overload, confirming the host verification call's signature.
No new external capture record was written outside this report's ownership.

## Static preservation and sampling handoff

Fresh static checks inspected all 57 owned changed paths against the baseline. For all 48 changed C#
files, removing full-line `//` documentation/comment lines left an identical ordered sequence of every
other line. This establishes preservation of executable bodies, assertions, imports, directives and
runtime strings for this diff. PublicAPI files and projects are untouched. XML fragments from every
changed C# file parsed with Python ElementTree: **zero parse errors**. `git diff --check` over the owned
scope returned exit 0. Targeted stale-pattern search found no positional `CreatePivSessionAsync(ct)`,
invented CertificateCompression.Never, obsolete SignAsync ellipsis or character-count contracts in
module documentation. These are static editing checks, not compiler/analyzer results.

A final report-consistency check re-read all 57 baseline/current path-hash pairs, all 48 C# noncomment
sequences, 339 XML documentation blocks and all 61 original complete-body identities: zero mismatches
or XML parse errors. Local-link target inspection covered 50 relative targets with zero missing paths;
heading-anchor/rendered-link validation remains pending. These checks used read-only Python/subprocess
inspection and `git diff --check`, with no SDK build/test runner or device access.

Clean-unit reconciliation uses baseline line regions and SequenceMatcher opcodes, with insertion points
assigned to the containing half-open baseline region. Of the original 396 eligible regions, **11 edited
overlaps are removed**, leaving **385** eligible clean nominees: API 78, examples 89, internal/test 209,
narrative 9. The updated minimum is **39 distinct regions** (ceil(10%)), spanning all categories.
Sampling/review remains pending; the coordinator owns coverage.json updates and category expansion.

Removed baseline region IDs:

```text
src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:22-78
src/Piv/examples/PivTool/PivExamples/KeyGeneration.cs:35-94
src/Piv/examples/PivTool/PivExamples/SlotInfo.cs:38-81
src/Piv/examples/PivTool/PivExamples/Verification.cs:33-121
src/Piv/src/CertificateCompression.cs:20-22
src/Piv/src/InvalidPinException.cs:17-21
src/Piv/src/InvalidPinException.cs:22-26
src/Piv/src/KeyCreationOptions.cs:20-22
src/Piv/src/KeyCreationOptions.cs:23-25
src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivResetTests.cs:76-95
docs/migration/v1-to-v2.md:313-389
```

## Current example classes and compilation scope

The module contains 42 fenced/XML code bodies after editing: 12 C# Markdown, 23 XML code, five shell
and two text diagrams. The baseline had 43 (including the old tool directory/menu displays and generic
signing fragment). Original-body hashes below preserve every module body, including the unchanged
package shell body. Each numbered body means its source-order fence or XML code ordinal within its file.

| Current bodies | Class and actual prerequisites/resources |
| --- | --- |
| src/Piv/README.md #2 | Complete current discovery/read example, with explicit SDK/crypto/X509/Text imports. Requires a PIV SmartCard device and platform setup. Session owns the hidden connection. |
| README #3 | Composable authorized generation task using a 24-byte factory key actually installed as Triple-DES/AES-192. Clears its owned management-key array; session scope closes before another connection is opened. |
| README #4 | Composable certificate task: open authenticated session and matching issuer certificate file. Both loaded and retrieved certificates have using scopes. Enrollment binding remains caller responsibility. |
| README #5 | Composable P-256 sign/independent host verification task after generation; metadata requires 5.3+. PIN/digest/signature cleanup is in finally, host verifier/session are disposed, explicit DER format is checked. |
| README #6 | Composable RSA known-plaintext/OAEP task with a matching certificate/slot and current PIN, firmware 5.3+. Certificate/RSA/session lifetimes are complete; owned expected/plaintext/PIN buffers clear in finally. |
| README #7 | Composable complete async protected-mode method. Typed borrowed IYubiKey/PIN/active-key/token inputs; caller securely backs up and finally clears secrets. Setup closes before reopen; recovered authentication is checked before disable. Persistent effects/partial recovery/remaining blocked PUK are explained locally. |
| README #8–9 | Composable callback declaration plus session creation, compiled together with real IUserPresencePrompt/Core session references. The application owns the retained prompt; session disposal leaves it with that owner. |
| CLAUDE #1–2 | Composable factory fragments with typed IYubiKey/token; direct creation additionally disposes its caller-owned ISmartCardConnection. |
| CLAUDE #4 | Application-dependent credential-store method with actual Action<Memory<byte>> callback contract, IPivSession/token inputs and a bounded 24-byte store contract. Callback must populate the full slice; method clears/returns pooled storage in finally. It is not a fake SDK helper. |
| CLAUDE #6 | Composable test annotation/method fragment requiring actual Tests.Shared/xUnit integration context, YubiKeyTestState and an authorized device; compile with the genuine annotation host. |
| SlotMetadataExtensions #1–2 | Composable metadata/extractor tasks with open session and firmware 5.3+ RSA or P-256/P-384 slot. Nullable metadata is guarded, public host objects disposed. RSA plaintext source is cleared in finally; device signing requires an external generator. |
| PivExamples/Attestation #1–2 | Composable retrieval fragments with device/session/token and generated slot. Returned certificates have using scopes; trust/slot binding is a separate application task. |
| PivExamples/Certificates #1–6 | Composable retrieval/import/export/delete/self-sign/CSR fragments. Supply typed device/token and borrowed caller-owned managementKey/pin; caller finally clears secrets. Each session and returned Certificate has a disposal scope. CSR now verifies PIN and guards CsrPem. Real helper's compress argument is boolean (false=Automatic). |
| PivExamples/PinManagement #1–5 | Composable credential tasks. Locally allocated PIN/PUK/default-key arrays clear in finally; rotation takes borrowed backed-up currentKey/newKey and an open authenticated-compatible session. Caller owns that surrounding finally. Authorization and current values are prerequisites. |
| PivExamples/DeviceInfo #1; KeyGeneration #1; Reset #1 | Composable tasks using device/token, plus borrowed active managementKey or an application-approved reset boolean where required. Generation/reset mutate authorized devices. Reset F9/partial-state limits are local. |
| PivExamples/Signing #1 | Composable ECDSA-only helper task with configured prompt, provisioned P-256/P-384 slot, borrowed PIN/data and token. Generic helper's RSA/Ed25519 framing limitation is explicit. |
| PivExamples/Decryption #1 | Composable result-inspection fragment with provisioned RSA slot/ciphertext, prompt/PIN/token and metadata support. Read-only result clearing remains a sample defect; direct README SDK example is the supported mutable cleanup task. No read-only-to-writable cast is introduced. |
| PivExamples/SlotInfo #1 | Composable query; every returned slot certificate is disposed in finally, including unsuccessful result processing. |
| PivExamples/Verification #1 | Composable host verification using stored certificate, original data/signature/hash and token. Result Success and IsValid are separate. It requires a certificate already bound to the intended key. |
| PivSessionContentionTests #1 | Composable current ownership-guard example with real ConnectionInUseException, P-256 digest/slot, device/PIN/token, Management reference and finally-owned caller inputs. Session/output scopes are explicit. It is not a nonexistent SignAsync call. |
| README #1/#10; PACKAGE_README #1; PivTool README #1–2 | Non-C# command examples. Tool commands require authorized device provisioning and existing input files. Public factory values are evaluation-only; shell/process/history exposure remains consequential. |
| CLAUDE #3/#5 | Non-executable protocol-flow diagrams. |

Shared examples: migration fences #9/#11/#21 are historical v1 and stay unchanged. Current migration
#10/#12/#22 are composable current factory/generation/protected methods. #12 uses an explicitly labeled
factory evaluation key instead of an undefined SDK-looking acquisition helper; #22 uses actual typed
borrowed inputs. Raw-access fences #1–3/#7/#8 are composable shared guidance; only #3's PIV named-token
argument changed. Raw-access #4–6 belong to other applets and remain outside the PIV compilation claim.
User-interaction #1–4 are composable session/callback/intent fragments, compiled with their actual
session/prompt lifetimes rather than keyword-filtered in isolation. Its only product edit is the new
PIV behavior paragraph; callback bodies remain unchanged.

Compiler hosts should use the real PIV project and actual full PivTool traversal prepared in
`snippet-preparation.md`. Imports for consumer wrappers include System, Threading/Tasks,
Security.Cryptography, Security.Cryptography.X509Certificates, Text, Buffers (pool method),
Core.Abstractions/Credentials/Cryptography/Devices/Sessions and Piv as required. Tool fragments bind
`Yubico.YubiKit.Piv.Examples.PivTool.PivExamples` from the real project; Certificates helpers must be
disambiguated from the X509 namespace if imported. Contention needs Core.Devices and Management;
test annotation fragments use actual Tests.Shared/Infrastructure and xUnit. Imported IPrivateKey
prerequisites remain alive through await and are disposed by their creator. No fake SDK types,
empty credential implementations or casts from read-only to writable memory are suitable wrappers.

Compilation, analyzer execution, final formatter receipts, final XML renderer/link checks, independent
review and device outcomes remain with the coordinator. This file's hashes identify the editor inputs;
formatter/reviewer corrections require updated final identities before example release.

## Original complete-body hashes

SHA-256 uses the literal Markdown fence body with its LF newline. For XML code, remove the line's
indentation plus `/// ` prefix, decode XML entities and append LF to the joined body. Source ordinals
and baseline opening lines identify complete bodies, including surrounding declarations within a fence.
Raw original text remains reproducible with `git show BASELINE:path`; current source/file hashes below
bind the edited bodies. Added README #6/#7 have no baseline body; original README #6/#7/#8 map to
current #8/#9/#10. The tool README replaces its five old display/example bodies with two command groups.

```text
src/Piv/CLAUDE.md
1@39 0af477e01e3a221a1fa69d8a02ab34dbd010dddad8d0f8aa3e5b51587c01ee5c
2@45 e700a69044060c5758652c2fa9760bd10c840c2f3f2f23c5365078fe7cb47d14
3@52 7edf983d15f3e18d72c798ae92b24b900c16a476643d4c8615e9031c58059c93
4@91 36be32c03907b979d6daea547bf29c58a9f05167e09ceaf8160f72726f0df687
5@115 4c9bd11db4b25743b64fd4c3279d947c67699584f1e6c9c78b558b5b34919036
6@164 9d130e74836e29004f6c427ef935ce40cd88d276d10837180b765902660c4e67
src/Piv/PACKAGE_README.md
1@14 74716490a0a242972fe228090ba3a51c426652f62de606c37d62f470687fc2f9
src/Piv/README.md
1@25 74716490a0a242972fe228090ba3a51c426652f62de606c37d62f470687fc2f9
2@34 207b4faf9f255923887b5bd96189d155c974fb304bbf836a2de307d6754e942d
3@61 05a32803214a6b9a1043bed8c336942d8555e46ba9757e91ba243f8bac506fc3
4@83 8a19109945481c91fa6bdcff30ddb1fa12f774a6a21aee809d55b991d646ff39
5@91 a7c309676e67bd85c7dd8fcf141aac1bc45fb41f021db3ad71aeeb3bf5499bcf
6@119 ca8fe727d322c778aabf304fd3260fb21de2c9619067f783b11892536be2b6ae
7@127 f0255a5d67fbf9956f2e67a1a9b16be868f0b2bb985edd5e8c0f231bba31d2f1
8@201 11e682c0686ab1fa8be7904fe81d6fb2ff213b0c5ecea7b4796cd12b820b1f91
src/Piv/examples/PivTool/PivExamples/Attestation.cs
1@42 df512e7108b4b0e06898f98cef3652a37c77573c6b4e74faa8355d1a23b3a8b2
2@88 47932bd6408dce1ad3b494c8196995d6e89b2a0da406e89c37dd2a9c0169e9f5
src/Piv/examples/PivTool/PivExamples/Certificates.cs
1@47 e57235c01e15c1fdddc406d688a1c111699fa62b76b3036d1844438d1d009099
2@87 d90b6b7d19455d9705f4f7debdbdb3cee657bfd6772f6edfedfd512bcd121785
3@137 3790fb2c237b39e9d76ab637dbf2b9e7c741913fba9315416fefb8ab5f471712
4@179 0fef4dcb5f4b3688b22c9cf24cf1c7a1aab73bb11a015d2f2fb930bf3770c87f
5@214 8a8091d202ae5da1df459195da45fd5dea309f432ae507377543bef80479067e
6@324 2249384de683fad2f7c19100ecd76a4fde40a21171c4a1945c7c06fbfa74ec69
src/Piv/examples/PivTool/PivExamples/Decryption.cs
1@42 57c9c29cf98e9bbbb035f056e2a1aa573680b8d3309443487fb237be9577e326
src/Piv/examples/PivTool/PivExamples/DeviceInfo.cs
1@37 c6db0fdceafbbd89a7e5c1e79c153c381500e2d1eb56f2594ef5cfa01c50b88f
src/Piv/examples/PivTool/PivExamples/KeyGeneration.cs
1@46 a3033d3847bd704a576365ac5e44e196abcb8a70c3091d39506a6623bc1f3003
src/Piv/examples/PivTool/PivExamples/PinManagement.cs
1@44 bb2c92c79589db7da894bff2c9cc83c108f3aac91843c0014b2b75e5de542597
2@95 dc238ed1a73070f01d4c5f56691d32f65c3c5ce26f20651928aaf7847d744f36
3@176 5edfc4189a762d3890fc1c218a9fa827095ad88fa7c7f6cc99cac79b5fc27333
4@246 e666c396c881515c16b21e15f0421dd1c567cfec119752925a8ee3b5f3ecd809
5@286 19af3851854ce2c3295df104a04e8b9a03fbc2c17be1f4f0ed2f6d72a3c5e2a5
src/Piv/examples/PivTool/PivExamples/Reset.cs
1@41 2fc29efde0e51219fd96960d1e9b975344e47d00cf59a77d0207492317df899f
src/Piv/examples/PivTool/PivExamples/Signing.cs
1@46 81e8957ea960306c5537e1a35097421ca1a3c12eeaac925ede5268f9d43d7dc9
src/Piv/examples/PivTool/PivExamples/SlotInfo.cs
1@45 e0656770e7714e6fbadf0371868cfe936709c666478af85819c295ea17916e2e
src/Piv/examples/PivTool/PivExamples/Verification.cs
1@44 588869dc8404fa9dc81e0f35c6800e04e5fde8bff70db6118073282eb0baa501
src/Piv/examples/PivTool/README.md
1@9 c76db116efb58487e48fe5b2e91cd1a737d79251e03b4882da62d403c80ff1b0
2@42 604a06efff7da950e482b957ca0b0fb84c868dcd6058a9f6dc1fccd80869ca18
3@78 622ca79d8f2830758a2481e13a12a931b966c39bb6351db47cad08103fdbb841
4@85 07738755f8ced758be7d91d5324dd1c5cfb784fe772a3f67c181d82ecb9c224c
5@94 d3c62bf1137c874f156cfcd47c115001f9cd3cdb63a512ba9d0cba32e649558f
src/Piv/src/SlotMetadataExtensions.cs
1@44 267965a0b9e0636a83a64b3021ecab2c8fc0864479303aa4b42ef31230e49bfc
2@104 6ec3ffe9c26773f3a4e392e308c57d729e8c06846357cbca8782c7ef9f0dffac
src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSessionContentionTests.cs
1@35 2d2a016c43bb37fff40045d71e2a213bd9d238378abb67e1667457f4d4a47616
docs/migration/v1-to-v2.md
9@317 450f1dc005293d383aab52e5ad4ac271d2db98b6748d92a758f87edb4226b1c0
10@325 a5be9db52856b7c16ce8eb9cd283599340950c059bc3170f667d67b29fe47f4b
11@342 c5cfe5e3e7fa3372b28eb71fed84bfd32ea87aa216af034acb0eea77699b4351
12@357 3825872d2aae8b79bf254cf64e3d35e2fd865c84b475787e3df1e0af3842efb7
21@624 a8cf26edd87802d6241acf5d10b69f67ca7e9daf8d294a38a638057e30db9619
22@635 820a4e1d7dc2a20e5a16cbaadab764b25ecc8916f51c67bba0d4f27643ff1d28
docs/architecture/raw-access-tiers.md
1@11 d70cb2ba21c45b2bce6196d1fc064592c1bf991890afd532333032f6da860f4e
2@38 318e1d600007d9682a330cd9ee0ab8fb02fa9031bc36b3b37edbc4c4732ccc36
3@64 c89973710405dc9355bbeb345e86ef0d6fc52a79b76b33bfbb75cb50b5f84340
4@82 b2298d166d6a76d130750ebed9ab3775d271b2679426f79fe2252c924a8ca415
5@98 d3f9d4e02bf88ccf779784dcd47f1599056155e59afb8bb6529198cd729764e1
6@112 d1ebf13f8409ed7cf58cd2a24008902092e1d5d5403af8744b51332f33877f0f
7@131 3bd0b61590590b2f50200473ccaf72d9ea010b73bf81ac18d4e1577ca04fc4f2
8@264 048e4a43422e260a9df70e5e85b7e1576cb644c625850265b5811e1ca4e311d5
docs/usage/user-interaction.md
1@15 a59f59abcbcf3f6a92fea0b7a1fd9de3755c1c256c92ac35bb0ea67ab389f951
2@75 aadb7f3aa7711e6babbfbb6f67170eae06bec69826decf184b495644af78529a
3@99 109cd113261d954faf24c614590f0ffd85ec337e78394d0ef3a1c046f050d5a3
4@106 cdb35996f0077d9974a37ad4ecf16805b34fd470e34c9cd7208f081d168728af
```

## Changed-path SHA-256 map

These are complete byte identities, baseline then editor handoff. This report is excluded from its own
hash map to avoid recursive identity; the coordinator pins it in the later review/release record.

| Path | Baseline SHA-256 | Editor SHA-256 |
| --- | --- | --- |
| `docs/architecture/raw-access-tiers.md` | `0d773df215380efdd7972d093759b3392d7d74172c181bf145ad275d022bbb1d` | `9950b492af9334a5f7d37bfe01a0f6d9360b25f384ba27240bacbcd4b2af5a4d` |
| `docs/migration/v1-to-v2-gaps.md` | `1697f22182c1d0dbe2e04c22398a0f2a98420d12f6ba1a1c7ca19c45e7456799` | `cbf6f91795c7cc3125a9bff28e89105a5f29bca5940f03843d056db0bb2ecdd9` |
| `docs/migration/v1-to-v2-map.yml` | `ec496ba7d7eb86c88b9c2ef176c11cc67771c6a49007b5342498eb1f074b5d73` | `8b04e58ce32954a35c4a2873be095361e1cd27c3e35049e3d254c51d1c432566` |
| `docs/migration/v1-to-v2.md` | `fc7db013f1988eb339783e8eb379b6f10f3b6ac5f995c200e8163b33f96ed9f0` | `820fff12aff06ed3d509204136b5c926663aa5ab2409d10d4df0a733f2374014` |
| `docs/usage/user-interaction.md` | `5cc0a1d43d7c6373a1528912b5073ab8c5904a4fb2ac731fc5bf6fb32d5597a0` | `80e7367c865514f47d1348ba58b42de3bf722fbb1bd91108c2aabce69cf37831` |
| `docs/v1-to-v2-comparison.md` | `938e441358b1ae3475f1a68919421c13f50e0b8baab9c4f5f22a46af9b718057` | `fd293b29198664e52aa1fd46cbe660689469083aa1fd52ae354bff3c52891f4c` |
| `src/Piv/CLAUDE.md` | `54facf850d3ce72617a3a7a26f020df1b600f27866c5d09f44311ec8aab4c45a` | `8d486c8a309ed7fcf821463d8a505320470e4b25e345c83e5c73bbf860de8695` |
| `src/Piv/README.md` | `dd9d4cf813eb6a0f654137ace7e270409238b730e084572704534a85768448c4` | `01c9b2dea26c3a27988fce87d629708eea8f580aaf44a1d8488ad6c280fe45f1` |
| `src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs` | `613ac6123584ef210e32b363b10c046724c25d85d2725fc0bc7461af7772fd0d` | `080baf3140c9213a81940c44330614cd2dfa128c78c80fdb1a7c1d5cb432d260` |
| `src/Piv/examples/PivTool/Cli/Commands/JsonOutput.cs` | `65d67b1508f0bfc5dffca1470439292bfb06420f041c9f7d640b513a7fac7f34` | `0c7f23d049d6fefb6498cee25f57df73bd062276486171cc6d4d6610e85b99aa` |
| `src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs` | `b305142a84fa75a94c26851c7bbb23c1044ea4f4de51383aa04f25bbb7b5334c` | `ff35c5ede31f42e9184a249513aadced5b7bedc3c234297967300d33883e4452` |
| `src/Piv/examples/PivTool/PivExamples/Attestation.cs` | `fd49d1dfe38aeb871d94ffbe8a210157397050cc45c5c46ae89a36555c42c920` | `92f56dcdd52388c152d294317baad1a01313725412a72fc63be31084dfe492b8` |
| `src/Piv/examples/PivTool/PivExamples/Certificates.cs` | `2119ce6a7037de15960c9fc065413079e7826c6c1ac4c7ab65547d5096126167` | `1281897e05222d624f70a532e42694e86daccdf5a44a263e5b95023dd8a75e1a` |
| `src/Piv/examples/PivTool/PivExamples/Decryption.cs` | `1f5258641332c7cba7eeff8fc4394255bda6c964439440ba839f28fe14744267` | `e32d5bf9b2ed2ff55109dce84fcb68481f6c1e36fefa2aaa21f3bc8a03a6f037` |
| `src/Piv/examples/PivTool/PivExamples/DeviceInfo.cs` | `a5cd87909367239dc5f105a4d8069e1c8e89956c04f631eb1fe187485d1d3270` | `727569b664964b68fe46ef7154c8d76a09f3fb9aa3eac420675adddc4acdfa9a` |
| `src/Piv/examples/PivTool/PivExamples/KeyGeneration.cs` | `6bc519c769e42100a8c4792a8b6a818bdc77766b57bec2b9bb6296bfb8ecb0a2` | `c99434ed791a0b832fb06ead79e12941bd31a0ac2f63e824842c99291bf8c674` |
| `src/Piv/examples/PivTool/PivExamples/PinManagement.cs` | `7ae3fb71691d2871e314b6deee61c8099ec58bf68a7e8222709ed757da07bf36` | `a2bfa1e430c5f3cd0de41c35ac3f7da2d57d71b9b5ac2671f341edef2d62bf8b` |
| `src/Piv/examples/PivTool/PivExamples/Reset.cs` | `2bcb34bd8a6b083632973f04858b9d5b2a2928b29eab10210a67b57f2a38ff53` | `329013ebd2cac9f8e83d6f42acc2cc112881a0465438f84cdfe9bec5db2f48f3` |
| `src/Piv/examples/PivTool/PivExamples/Results/AttestationResult.cs` | `18fc216edb27ca2b3de9b28febdc70c539932f5517b0cbd99695c1ef36a4230a` | `ca85a55a84155f04e6fba8d0cb83fd513d41963fb2698433535fced3f66bb6e8` |
| `src/Piv/examples/PivTool/PivExamples/Results/CertificateResult.cs` | `dfa09d1e155198482df0e2038cdba70d64115ac5a18756ad3c6dbc46a66049b0` | `0072f5ad3d635fc2580ab5f79da813a0d8658d6ef6d89f646265e4dd5351abac` |
| `src/Piv/examples/PivTool/PivExamples/Results/DecryptionResult.cs` | `64ddd6e0b23b5bd031c1e4451e29867b3abb8873b7c736146d9b83e7e30f8f56` | `492044b615cbe5e4bcae7718e244201716af60ffe068028cf8bf0f59ce560ec0` |
| `src/Piv/examples/PivTool/PivExamples/Results/SlotInfoResult.cs` | `e38436edc8dc72814c7660b72978c30afc4ebd51f3608cb49b85c63a5cb17191` | `c3679d97cd30633b24ce07053ff0b774725e00826192efb154c5a02e0fbe5840` |
| `src/Piv/examples/PivTool/PivExamples/Signing.cs` | `f6f45d3f299d7185249fc886d92e80e2611191e0f793bf450d35e98c66bbf56b` | `099d9baffcd8f0a445349448c9c02a71e10c1892e5f78f7e799a4b2929b45346` |
| `src/Piv/examples/PivTool/PivExamples/SlotInfo.cs` | `f73707efc43a5036168bf25526e30dbb961662b4dec9c59a67bc36618d3ab84e` | `a03437410dd4e817987c35dd2c2d7e556235fadd05ea23036ce5fe7af81ae3cd` |
| `src/Piv/examples/PivTool/PivExamples/Verification.cs` | `2b2ba8d0011172ce82612d7bee488cfae5ffcf3ee4c4f63be02d23ffa8fea633` | `3c298d917b2e90cc3136d9a54eb0a4c75aa2d28e595a68dfa1d92d45f89de177` |
| `src/Piv/examples/PivTool/Program.cs` | `1bc96176eb28dde25304d397aff701b2716c4240e8465ece49981a79ce26543f` | `c2957ae15caad9f85b00916d650d597727a9a194b4e2f5532559369fbd5b4970` |
| `src/Piv/examples/PivTool/README.md` | `1bbeaec112d19887cefff9e3c398b86f8f1e2d20912e08378db91fb6fbeee8ad` | `e44b2ea2df953a049694f5f7cf6a835328f23389ede05bd87e9759428d9fccb3` |
| `src/Piv/src/Authentication/AuthenticationProtocol.cs` | `54445e7059fedd59adc0ab6bc896d744e81a99f489139a6c8dd413ea8c666adb` | `aef3d82252c547103cb873518ca042f1b7cc6b9ba1bc79e3cbfae1c798bc83f8` |
| `src/Piv/src/CertificateCompression.cs` | `b9f352c82f34722dc373f3d978789e9c48b973f084e4ba867f20359fa3eaf039` | `9ce4a9088b0adc92564b577ed454729d8cbbea1edb6696600af673f58495842b` |
| `src/Piv/src/DataObjects/AdminData.cs` | `0206004e72ecf574234f205d896d88db39d52e8000b6957e28bf0012eca27c5c` | `c0946bbd2c706c6be18acf82af67663a6044f52811e17dfd746295bdfc0983ae` |
| `src/Piv/src/DataObjects/CardCapabilityContainer.cs` | `eb49821f969913839d4d2145cdb879ac4662a1c5c758912725d722a72c64fb06` | `6668b64c2a984227831ec98f8d876571facce00e608232d390e43c77fe5df5ac` |
| `src/Piv/src/DataObjects/CardholderUniqueId.cs` | `6aae5af5bc5b6f46e14d08f5bce436fd9cdb0904e939a74591e5be4e2e803d32` | `3c079eb23911a14bfab29839f1f765f6b77d57b34def3c692bc45dd5c842d169` |
| `src/Piv/src/DataObjects/KeyHistory.cs` | `d0deffe97b7f37984ca58f5e15b440884e1ac67b3619484b4df8118041145de9` | `5320449c85e6059fee5a8b8dc25d29c30d2f5da55819844bfb9970158bc25a6b` |
| `src/Piv/src/Features.cs` | `89bf3668a088d6e078135e9a8e5478714f03e94c6ab09bd970173b729066c5fd` | `17259917860e900c84b0ba0b52809e3b130697156e15bf3898e4eae7ab3e5d9e` |
| `src/Piv/src/IPivSession.cs` | `39aa9ec417249fb3424475c316ccb7a1c47116419650df8babec0137e4f3bd0b` | `4638725e045f4e43c8214e835695a01fb5d00c258979a4a5468171809ea69c08` |
| `src/Piv/src/InvalidPinException.cs` | `64f759109fda3fcea54fdec54699038152cb3ee9435d7b7941c62bbd578a9fd8` | `23b5b025ff35a38e28f78aa4e04e71e8850531338c8dceb12141a869c63e899b` |
| `src/Piv/src/KeyCreationOptions.cs` | `1d3c39499744e45b541efdaa4c6d1868ec47f3c7048b5840e89efb85bf42351a` | `acaaece4112b01b42b51c4f90006e8c23679b31761b6a9122be83931af12d8a9` |
| `src/Piv/src/Metadata.cs` | `cb3018581e2f2b37601d75d5e4299483629a755bcdeb0fac9a8304571c7dd7ce` | `f41fbcfbf579c7f9e9095e57f3143b32dcee332fd3b3103b1ae0cd3567ef3521` |
| `src/Piv/src/PinOnlyMode.cs` | `71fd5aff90994bcd90235f814009f5530bc97d12da38710a8ff52d57328dee89` | `2808b8bb1cfba5254d7cd1fe24e3dab9932ea46cf656e628a34acefb5f1cd797` |
| `src/Piv/src/PinPolicy.cs` | `31415a9c4446c7697401b7d9c5d5d875ed69acccf2d3164bcf5f7b38b2a5a6b5` | `8bddf163172b80b306ff3639ec1752b8af767bee0555227d4b653efe560bc362` |
| `src/Piv/src/PinUtilities.cs` | `d1baf0c84255809a1d456707c9b587320777f08e1e92f499b973562486473949` | `41010a5c40e0f7b468d1a2e1416725e3663eb19afe6d13207bcbbcfd047a21c2` |
| `src/Piv/src/PivSession.cs` | `19bcecc6bec14e6f4f6390388b02d3f44beba5e4956764bd033c1017e12e17fd` | `6bc077507bd14dd118e98d96d5fb63c16f5f3dbd66b0152bf0f6e69f709b3954` |
| `src/Piv/src/SlotMetadataExtensions.cs` | `221422cf63fce503eccfc358edfa7afa563beb9836cd01fd2507349427665070` | `d36a4ba2a91fcfcb7cea037f2a278864dfda0b32c56927259092ccede51937ba` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs` | `562004d28414bf6592590bc6422437f239fbf0dfbdb865b59bbffe14e18f10ce` | `822f4ff418c328551bad9527fb9399dd6bb5410bd9165d18155ff347391e472b` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivDecryptTests.cs` | `2f10d880a4979f87f72b478ae575110949e5421e31050d3504b95fde3d989226` | `ab62ddead80c206670e147fe33a096953d4fad8869d09d3f9577d61f59973749` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivDiscoveryContentionTests.cs` | `cebbc317888e7e58ae44849cc12e5c7b5fb5f30f9134dc7e0e13965830ec5093` | `ff0c0c69ded5e38a7a5f95bb2b7f9986157b5bedbe96e76b12ef2f6adb1bc0e0` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs` | `57b73a58d2f4f3a659f396b27b0ad7b653cf0866c7439d07b8c3c4612a1b0c60` | `264957570c6348523845862a7c181feb1ae68a07f096212d5e8d708f63509346` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivImportTests.cs` | `49841dbdc390d4a459ad2f297945d85e213fb5bf955c8d4b12e3bed52baf0f62` | `7b11785824b1c93c73a460a3a4ac0817b79264bffc14b2275da7dbf341c83342` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMultiKeyContentionTests.cs` | `6c01e7f59ae3e2cf47de071464e20879060c90a983150c3d062a4ab263807059` | `6b204d130d80ec67d2c95d8e1420314900918bae15f6b01a7d95eeeeefe9e2bc` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPolicyTests.cs` | `6f358fe7a59ccc800696ea974f1716f788b8b64ed7a4e1e69398b3bc322ba595` | `b2881c697b567a79454f705224664ba4a42cb80de413f6d5b8e8126156b73680` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivResetTests.cs` | `f145c354f1276cf7794ffe4d7dae3058ea7f97aa60440d5bacc4dad9f965298a` | `de8e0361a96f77b1159ae99b50a220c8882546577c4f802b143943c21a9b396f` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSessionContentionTests.cs` | `ab12817af53c4645dc52a748968b49cd9a330b7318b59e9e05708d0d9e6a3604` | `c2676ee812b7640e9cf94bc308b916427d6cf0380af51ec8f7e2e7ad15cf41de` |
| `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs` | `ee048090a022c638b6e29b63840c6d2c409cb9e0ad54d68725ffbb94bef628db` | `005aa8fa8fa59c4f566f720297a80820c07d4a84eea0b299f125713b1e66df75` |
| `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Bio/BioProtocolTests.cs` | `be4e15a24b29550f3a2ba03deb5cfb6570894c1f1d0f8022a7c77332e0e1d815` | `0592e5416ec43e69c613fa0cb945c66aa1ceb0d9d33b913a317f4b5605b7ce67` |
| `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Cryptography/PrivateKeyOperationsTests.cs` | `6e2e6ca9d9e1312bd23a8224f48c8498b40617623a038fd8c8dc30c63feaf96f` | `d7b28780188e5d97a1fd4e986f69653b015afd64663b027c24f6c06473d4e1f6` |
| `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/DataObjects/DataObjectProtocolTests.cs` | `127ac5e76a52a3af9346b3352b85a187ceff466afebaf2deccd965fc3b1c3482` | `b1c55a9e1f9b8896433e6db26fdc3d2751c85fba0f75d2007b86ba8b4f287bdc` |
| `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs` | `aedd95a2341e2426782c6d72fa403503fa6525b6297048bf176fb4979e860268` | `f656db1258c4f8623fc8f39828fb87f66866fc40a8678ed19f5993859d04e01e` |
