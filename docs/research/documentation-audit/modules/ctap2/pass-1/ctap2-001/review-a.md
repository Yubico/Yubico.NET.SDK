# Independent review A — session, metadata, CBOR and shared tests

Review completed **2026-11-04**; original-source retrieval occurred across November 3–4.
Fresh independent reviewer: **GPT-6.1 Sol**, assigned subagent, no delegation.
Reviewed checkout: `/tmp/opencode/ctap2-001-edit-a`.
Baseline and checkout HEAD: `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
This report lives in the **primary** checkout. The reviewer changed no source, test, guide,
assertion, executable example, configuration or other audit report.

## Verdict and required versus optional feedback

**Overall documentation PASS: NO — one small factual correction requires re-review.**
All consequential reset/status/cancellation/ownership/firmware/encoding corrections reviewed
below are supported. This is not rejection of the batch or authorization for implementation work.
The remaining correction is confined to the model's introductory absence explanation.

| ID | Kind | Location in reviewed checkout | Required action / basis |
| --- | --- | --- | --- |
| A-REV-001 | **Required, low-severity factual precision** | `src/Ctap2/src/AuthenticatorInfo.cs:27–30` | “Absent scalar values remain null” is too broad for this model: `Aaguid` is a required byte-string identifier represented by nonnullable `ReadOnlyMemory<byte>`, and Parse assigns `aaguid ?? []` at line 521. Restrict the null statement to optional nullable fields and explicitly distinguish the missing-required-AAGUID empty fallback. Suggested meaning: “Absent lists and maps become empty; optional nullable fields remain null. A missing AAGUID becomes empty even though the protocol requires it.” Keep the existing required-field/length limitations at Aaguid and Decode. No parser/API/assertion change is requested. |

**Optional preferences:** none requested. The current direct, calm prose preserves useful local
conditions. There is no benefit in a preference-only rewrite or making every XML member repeat
the entire interface contract. In particular, keep the actual known-PIN and provisioned-SCP
prerequisites rather than weakening them for smoother prose.

**Re-review gate:** inspect the corrected introductory paragraph against Aaguid/Parse/Decode,
record the new `AuthenticatorInfo.cs` hash and diff hash, and resolve A-REV-001. The current
hash manifest is a review snapshot, not approval of future changed bytes. No compiler, test,
hardware or full-module closeout PASS is claimed.

## Instructions, evidence and complete inspection coverage

Read root `CLAUDE.md` in the primary and reviewed checkouts, available skill descriptions,
`src/Ctap2/CLAUDE.md`, `src/Ctap2/tests/CLAUDE.md`, narrow Core guidance, `docs/TESTING.md`,
`docs/SDK-HOUSE-STYLE.md`, campaign `workflow.md` and approved `records/style.md`.
Loaded `workflow-verify` for evidence-before-verdict discipline. Build/test/commit/delegation
workflows are inapplicable to this expressly read-only packet; none was invoked.

Read the primary run's complete `plan.md`, `triage.md`, `investigation-a.md`, `edit-a.md` and
`framework-adjudication.md`, plus relevant B/C/G findings. These are leads and disposition
records, not substitutes for independent evidence. No review-B/C comments were adopted as the
current verdict. All **26 changed files were read completely**, including unchanged surrounding
members, implementations, fixtures and **actual assertions**. Both full library and test diffs
were inspected. The four unchanged A-owned inventory files were also read completely:

- `src/Ctap2/src/Ctap/CtapCommand.cs`
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/UserPresenceProbeTests.cs`
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/CtapResponseParserTests.cs`
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2BackendLifecycleTests.cs`

Narrow independent implementation reads included complete `ApplicationSession.cs`, `Feature.cs`,
`ExchangeGuard.cs`, `FidoHidProtocol.cs`, `UserPresenceNotification.cs` and
`Ctap2SessionRequestEncoding.cs`; `FirmwareVersion.cs:22–81`,
`PcscProtocol.cs:100–194`, `ClientPin.cs:300–469,680–697` support specific claims below.
This is not a whole-Core, PIN, credential-model or WebAuthn re-audit.

## Consequential original-specification checks

Original FIDO publications were independently fetched successfully (HTTP 200), and the relevant
body passages and publication headers were read. The versions/statuses are not inferred from
filenames. Focused original quotations below make this review independently usable; the original
URLs are retained here rather than silently upgrading the source XML's reference revision.

| Reference | Original URL / publication header | Independently checked passages |
| --- | --- | --- |
| S20 | https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html — **Proposed Standard, January 30, 2019** | §5.4 requires versions and a 16-byte AAGUID; §5.6 invalidates generated credentials but does not supply the later universal ten-second receipt rule; §6.3 assigns `0x38 CTAP2_ERR_PIN_TOKEN_EXPIRED`, “pinToken expired on authenticator.” `0x36` is PIN_REQUIRED. §8.1.9.1.5 distinguishes CANCEL from the original CBOR response. |
| S21 | https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html — **Proposed Standard, June 21, 2022** | §§5,6,6.1–6.4,6.6,8,8.2,11.2.9.1.5–1.7: presence actor/transport, state adjacency, UV/token exclusion, required GetInfo fields and configured options, destructive reset and receipt timing, client encoding limits and status provenance, CANCEL and UPNEEDED. |
| S22RD | https://fidoalliance.org/specs/fido-v2.2-rd-20241003/fido-client-to-authenticator-protocol-v2.2-rd-20241003.html — **Review Draft, October 03, 2024** | §8.2 says `0x38 Reserved for Future Use`; this explicitly cited draft does not substantiate a new token-expiry status. No replacement with the later 2.2 publication was needed. |
| S23 | https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html — **Proposed Standard, February 26, 2026** | §§5,6,6.1–6.4,6.6,8,8.2,11.2.9.1.5–1.7 preserve the relevant distinctions; §6.4 defines longTouchForReset, encrypted device/store identifiers, reset transports and PIN-policy fields. §8.2 still reserves 0x38 and names complexity for PIN_POLICY_VIOLATION. |
| YR | https://docs.yubico.com/yesdk/users-manual/application-fido2/fido2-reset.html — living **Yubico product/legacy SDK guide**, not normative CTAP or the current v2 API | Steps 1–3 separately describe reboot-to-command and USB interaction windows; the Bio Multi-protocol note conditions individual reset on the FIDO application not being blocked. Legacy command classes/status-word examples were not imported into v2 contracts. |

### Reset: receipt clock is not interaction clock

S21/S23 §6.6 says: “In case of authenticators with no display, request MUST have come to the
authenticator within 10 seconds of powering up of the authenticator.” Late receipt returns
NOT_ALLOWED; disabled transport/explicit denial returns OPERATION_DENIED; USER_ACTION_TIMEOUT
is separate. S21 requires reset on at least one transport and on supported USB HID; S23 uses
SHOULD for support and requires an alternate factory-reset method when unsupported. The revised
XML does not flatten those version-dependent support rules into a universal guarantee.

YR step 2 says “10 seconds for YubiKeys with firmware version 5.5.4 and later or 5 seconds for
firmware versions prior to 5.5.2” to send the command. Step 3 separately requires USB touch within
30 seconds and continued reader contact for NFC. **The 5.5.2/5.5.3 gap remains unsettled and is
explicitly retained.** A timely host call is not proof of timely actual authenticator receipt.
Initialization/selection/GetInfo and any SCP work consume time; `ResetAsync` performs no power
cycle, local receipt timer or complete product procedure. The review did not execute any reset.

S21/S23 also explicitly invalidate generated credentials including CTAP1/U2F and erase discoverable
credentials. YR establishes the product no-PIN result. S23 long touch is conditional on the feature
being present/enabled; §6.4 says true requires touch >=5 seconds, false means supported/disabled,
absent means unsupported. The new XML keeps this **third duration** distinct from both windows,
does not promise every setting is erased, and does not choose Bio's alternate device-wide reset.

### Status, presence and cancellation

- `PinTokenExpired=0x38` is retained legacy S20 provenance, **not a CTAP2.2 addition**. Later
  inspected publications reserve it. E0–EF is extension-specific, not CTAP1/U2F error provenance.
- S21/S23 §5: “Platform-mediated user interactions such as clientPin may provide user verification
  but are not considered to assert user presence.” NFC placement is conditional on the authenticator's
  gesture mechanism; S23 also discusses ISO7816 contact. The revised task XML does not universally
  equate presence with physical touch, a PIN token, or verified response flags.
- S21/S23 §§6.1/6.2: platforms **MUST NOT** include the uv key when built-in UV is unsupported and
  **MUST NOT** include both uv and pinUvAuthParam. The exclusion is the key, including false,
  not just uv:true. Source emits these fields independently; A's caller guidance correctly discloses
  that the encoder is not complete capability/combination enforcement.
- S21/S23 §11.2.9.1.5 requires a cancellable outstanding request to return KEEPALIVE_CANCEL,
  forbids a response to CANCEL itself, and says the error is the **original CBOR request response**,
  not a HID error frame. §11.2.9.1.7 defines STATUS_UPNEEDED=2 as waiting for presence.
  No clause promises instantaneous host cancellation, rollback or unconditional reusable state.
- The actual HID implementation checks caller cancellation in keep-alive processing, sends at most
  one CANCEL and drains terminal framing. A raced CTAP success is retained; a callback failure can
  supersede successful device work after drain. SmartCard uses None inside an admitted exchange.
  The revised interface accurately describes cooperative cancellation, no host deadline and no undo.

### GetInfo required/optional fields and state

S20 §5.4 and S21/S23 §6.4 mark **versions and aaguid Required**; AAGUID is 16 bytes.
Other members are optional in the response table, with some conditional-presence rules (for example,
minPINLength with ClientPIN support). “Optional” does not authorize arbitrary malformed present
values, and successful decoding does not establish all those conditions. The revised Decode/Aaguid
XML correctly retains the current required-field/length/integer/trailing-envelope limitations.

S21/S23 explicitly note that GetInfo fields/options may change after platform commands. clientPin
true/false/absent distinguishes capable-and-set/capable-and-unset/unsupported; uv similarly distinguishes
built-in configured/unconfigured/unsupported. bioEnroll=false still advertises enrollment support.
Preference order, unknown transport tolerance, estimated discoverable capacity and Unicode code-point
PIN lengths are correctly separated from enforcement and authorization. S23 encrypted fields are
IV||ciphertext of **128-bit device or credential-store state**, keyed via the corresponding persistent
token derivation, not credential IDs or interchangeable ordinary permission-scoped token outputs.

S21 §6 permits device state assumptions **globally**, including independent channels, when no other
authenticator operation intervenes. State must be discarded when the initializing token expires.
§6.3 separately mandates NOT_ALLOWED for missing/exhausted assertion state and >30 seconds between
calls, with NFC timing optional. Caller N−1 collection/no-interposed-GetInfo guidance is sound; the
session does not enforce a whole-sequence lease/counter. S21 §8 places four-level and default1024
client-message constraints on clients/platforms/servers; writer mode is not device-limit enforcement.

## Framework adjudication independently checked, without rerunning the probe

Context7 resolved .NET and returned explicit-endian Guid overload documentation for .NET10;
its BitConverter discussion was **not** used as proof about default Guid serialization.
Read the retained official pinned source units and actual `evidence/framework/probe.log`:

- `Guid-v10.0.14.cs:65–97,987–1060`: default span construction compensates on native big-endian,
  and default ToByteArray/TryWriteBytes compensate when writing. Default byte convention therefore
  remains little-endian first4/2/2 and unchanged last8, independently of internal native layout.
  Current SDK reversal only on little-endian hosts establishes **CTAP-IMP-025 for a hypothetical
  big-endian runtime following this implementation**. The non-symmetric pure fixture was executed
  only on Linux x64 little-endian. No big-endian platform execution, supported affected deployment,
  SDK converter execution in this review or portability fix is claimed.
- `CborWriter.Map-v10.0.2.cs:53–61,121–175` plus `CborWriter-v10.0.2.cs:288–322` explicitly sort
  complete key/value ranges during map completion. Retained actual package10.0.2 output is
  `id,name,displayName` and `1,3,-1,-2,-3` despite different insertion orders. **AUTOSORT is established**;
  D-08/CTAP-IMP-017's insertion-order runtime-defect mechanism is rejected, not a remaining wire bug.
  A's revised builder explanation preserves the actual writes and correct mechanism.
- `CborReader-v10.0.2.cs:46–63` and `CborReader.Map-v10.0.2.cs:75–95` support the retained A0 01
  fixture: map completion with default multiple-root=false leaves BytesRemaining=1 while PeekState
  is Finished. AuthenticatorInfo does not check remaining bytes. The qualification is precise;
  this is not execution of every SDK decoder, proof every trailer is accepted, or a rule that nested
  parsers must consume their entire backing buffer. Release-tag source is pinned; the report's
  unproved exact package-source commit identity is not silently upgraded.

## Source/ownership and assertion checks

| Reviewed unit | Independent result / evidence bounds |
| --- | --- |
| Session/factories/firmware | **PASS for reviewed claims.** Direct factory borrows connection; convenience factory calls OwnConnection only after successful creation. Backends borrow protocol; Core owns disposal. Prompt and supplied SCP parameters remain caller-owned. Preparation rejects HID SCP before HID I/O; SmartCard SELECT/GetInfo precede the **detected** USB gate, override-based configuration and optional SCP/backend rebind. Major0—including missing0.0.0—passes Feature's optimistic gate, not proof of real5.8 firmware. Fresh GetInfo does not change the construction snapshot. One-shot helper has creation GetInfo plus a second fresh read. No hidden Management lookup or failure fallback. |
| XML factory examples | **PASS static task/lifetime review**, not compilation. Ordinary lexical scope ends before the SCP alternative opens; caller-owned default keys outlive secure-session disposal. IYubiKey/async context/Core and CTAP namespaces/provisioned authorized SmartCard key are real prerequisites. Defaults are not presented as production key management; SCP does not grant PIN/UV permissions. Coordinator owns meaningful offline compilation. |
| Backend notification/transfer | **PASS.** Both resolve successful CTAP status before raw view transfer; typed decode follows in session. Missing status synthesizes Other. Nonzero status, APDU/CTAPHID error, validation, callback, cancellation and decoder failures have separate provenance. SELECT wraps all ApduException values with original inner cause; wrapper alone does not prove missing AID. Failed call is not no-side-effects/rollback evidence. Fault cleanup covers untransferred storage, not all successful response copies. |
| Raw request/builder/parser | **PASS.** Prefix/schema/authorization/adjacency/size/depth and stable borrowed storage remain caller responsibilities. Raw method adds framing/status, not inferred touch. Immediate WithBytes/WithEncodedValue private clones, replacement without clearing, callbacks on repeated Build, complete map/array start/end responsibility, caller-owned command array and intermediate-only clearing match source. Optional readers inspect and consume the **current complete value**, treating encoded null/undefined as null; they do not detect an absent key or authorize null for a required field. Nullable-memory helper is a same-array view. |
| PIN-normalizing integration helpers | **PASS documentation correction, not execution/repair.** Both overloads default normalizePin=true after creation, before callback; the info overload also performs its fresh read after normalization. The helper can SetPin, known→temporary→known ChangePin, or request a token, consuming a retry on mismatch. Temporary buffer clearing is success-only; token is discarded uncleared. Skips do not undo prior work. A GetInfo-named final assertion does **not** make these helper paths agent-read-only. Direct no-helper reads are correctly distinguished. |
| GetInfo/simple/SmartCard assertions | **PASS bounded commentary.** Optional firmware/max-size checks are conditional. Guarded hmac-secret and rk disjunctions are tautological, not feature execution. Length16/nonzero AAGUID includes an extra test assumption. SmartCard first test checks model/nonempty versions/length, not precise unsupported cause. Selection awaits completion only; reset asserts some CtapException, not NOT_ALLOWED, timing or no deletion. |
| Transport/helper assertions | **PASS bounded commentary.** Creation checks fields/nonempty ID, not attestation/algorithm/full ceremony. Assertion checks nonempty signature, not cryptographic verification, RP hash or response-ID association. Cleanup is broad RP enumeration/deletion; no transaction guarantee. ClientPin disposal owns its protocol, not returned token/session; version-string token heuristic is not complete capability/permission negotiation. Known PIN/random32 helpers are dedicated-test data, not production prompting/client-data construction. |
| Unit/fake assertions | **PASS bounded commentary.** Versions-only fake responses omit required AAGUID; duplicate transport and arbitrary encrypted16-byte fixtures are decoder probes, not conforming advertisements/decryptability. Most builder tests assert prefix/length only; WithValue decodes one key/value without allocation measurement. Fake macOS bridge asserts specific cancellation/drain/reuse/pending-disposal behavior, not live OS recovery. Backend lifecycle retained-buffer assertions check actual zero bytes on callback faults, not just exception names. Constants tests do not establish all-revision status assignments. |

Existing deferred CTAP/WA implementation and policy IDs remain authoritative, especially
CTAP-IMP-001/002/003/004/020/021/022/023/025, CTAP-IMP-011, WA-IMP-002/003/009/010/013
and WA-XREF-001 dependencies. Current makeCredential up=false rejection and retained HID raced
success must not be reopened from stale historical findings. F/B/C/D/E/G own dependent guides,
options/results/helper contracts and final cross-layer review. No global standards conformance,
hardware support matrix, transaction recovery or result-zeroing guarantee is approved here.

## Fresh read-only preservation checks

Executed only Git/read-only inline Python comparisons; **no builds/tests/hardware/SDK or probe
execution/formatting/commits/delegation**. The reviewer authored no checker script or evidence file.

- `git diff 5b09a9b6 --stat/--name-only` confirms **26 A-owned files**, 481 insertions/207 deletions.
- Inline Python independently checked all **30/30 baseline Git-blob SHA-256 hashes** against
  `inventory.json`; confirmed **26/26 changed paths belong to A**; removed full-line // comments
  and compared remaining lines; inspected every added/removed nonblank line as a comment.
  **PASS: all26 noncomment line sequences identical**, including executable strings, imports,
  declarations, assertions and directives. EOF newline additions are whitespace. This is bounded
  text preservation, not Roslyn/API/compiler/token certification; coordinator owns those later gates.
- `git diff --check 5b09a9b6`: **exit0**, no whitespace diagnostics.
- `git diff --binary`: SHA-256 **`912274ce0b563958b40bbe6cf5dc481255aba5ef5564ed45eef57a1b41282b80`**,
  matching edit-a's final patch. Explicit `git diff --binary 5b09a9b6` hashes to
  **`11992a0c3cafbb0e40e4ba58bfa5fafdcbbea22d9265a5b5ba1e43a976a30053`**;
  Git's mnemonic prefixes differ (`i/` versus `c/`), not source content. Both outputs have109959 bytes.

### Exact 26-file baseline and reviewed-byte manifest

Baseline hashes are Git-blob bytes; reviewed hashes are actual worktree bytes. Every row was read
in full and independently reviewed. AuthenticatorInfo has the required correction above; the other
25 have no required documentation correction in this batch. This manifest remains the initial
review snapshot if a later re-review is appended.

| Path | Baseline SHA-256 | Reviewed SHA-256 |
| --- | --- | --- |
| `src/Ctap2/src/AuthenticatorInfo.cs` | `fc91bedc43b2d2dc6600808864d4a73192cd4965e6e5876cb3a82bceafe84935` | `a09236d605a4d29b483086d9a1c6942e5a6289beeef2b33a71bfe7032620116a` |
| `src/Ctap2/src/Backend/HidBackend.cs` | `335654513b9f236cbf413d9e46ac0eb3b13fd7f3dd4ca41df7894e1f40f188b7` | `b8d43bc9167e4f6f2066cfeba1bdf44a9304ea334b82b4603a5fe3670c83c8ea` |
| `src/Ctap2/src/Backend/ICtap2Backend.cs` | `5f8c5aad09050ec225b387b8db8dde55b34d0a39ab72527a3f4138b9803d8708` | `775983b21bc7f6ef1c8e42e640fa2957c12cd26f7aa515d835b635296d9b0d0f` |
| `src/Ctap2/src/Backend/SmartCardBackend.cs` | `aec6e747801c2066d188eb1193e1175a67ad1a76bae4a50b446dd401034f0ce0` | `63592ea8add83dc6ed9a6cfce0366272e958f4f2eb2c6d11cd15bbdefe4eb295` |
| `src/Ctap2/src/Cbor/AaguidConverter.cs` | `ebb5f6ce47d98a8b2b94a9333d9a72b12fe205f331d0cada76c5946116d22859` | `dfe700e949e12ddadb97bd80cda2a82a78d39256672df2b8474503375f93dd09` |
| `src/Ctap2/src/Cbor/CtapRequestBuilder.cs` | `3df6adae013a0006b6203dd6ba843757839114aeca01391b1e3add9366e4f5e6` | `e5b6de47d3a1e177ba12f8b5a6911d0037f53da1b223fe87d694e0104d40b7b7` |
| `src/Ctap2/src/Cbor/CtapResponseParser.cs` | `b92cacd4edfec7d7397cf086b6ef6f0f301666cd3218db03ff0026024b05f762` | `6de6a26c37a9f429977c886a5a48e1ecf39f9e9c661132296006f0330c84c33f` |
| `src/Ctap2/src/Ctap/CtapException.cs` | `4ad21336e63b5d65b0f5b3b743d10d6dab16d02f49de89a41762634d99c39056` | `d00285a610824aeae1407abe96ba1c1dd2592bee072a90b7bcd010429624b6e0` |
| `src/Ctap2/src/Ctap/CtapStatus.cs` | `35a9a829a53b5d7fbf980491e615cd07c8d2bd9db1e3d1b8013bf56dfbb8d6e2` | `ef56011b3bdca54eaee1149169901158ecc2c9e57bde07ba10ebc0ec01e024c9` |
| `src/Ctap2/src/Ctap2Session.cs` | `fffd6c87437e310cfd340d5265fe28c5ffdd4070dc0dc8643f27974796af6b5f` | `23fbfeec9b58730d49638c7557b160102dc01fa233f8e1fd1241778d41056eab` |
| `src/Ctap2/src/ICtap2Session.cs` | `5d2f4075ee510dd0f7fb8bf940676947905ace7e619968a4d59ebdfcbf0eda25` | `ec5e8dec71d67e76899d655aa0421f94aeef0d9fbb1a8a0ed0b2c7e17870fcfa` |
| `src/Ctap2/src/IYubiKeyExtensions.cs` | `bce49013ac83fdd1dbdc694063fe0871b91ec266d87938307e62abe715eb2204` | `141e10279591de25200a5d1a41d9a493326eb12b0f14b3f503ec3b8e5dcb3644` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetInfoTests.cs` | `8eaf5376139d4e5ec20a0b14b89f927e87cfbc9eeddb55327a8e4dd25ce48b35` | `0189a5de09887165dc8f010f0f57f295f751bd0362a7d0655d02aa6ae38bd3b6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SessionSimpleTests.cs` | `296a9b6ab597ab18a1d7ba8b7a2baa3bfae8b6f26c071e447ded81f520f66c4d` | `8431df6913b1ce29a27524de19d5142591317cca2e5d82c5ad236bb1961aa74a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SmartCardTests.cs` | `336ef4e412f1b42e5f44c0c5d1d82a0ee3ab4d3144bc9fee319a30a734f3ca42` | `31d06123090bb82be279aa1fb79788390eedc0abb26940e0cfdf786942212fa1` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestData.cs` | `4721331674180aed3296243418ab71d807fbb0c709c37c93c724295d166bd4c0` | `4ee7a26504e3e8d268056646b7dd37717851958728dae8efd0ff13f8edbd8eb6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs` | `93b214c26ee7f4063c258358a24bba63bd6431a48ebb61999d9810baae23bb33` | `fce6094f3f89cc82158997db5cf3c49f6f90a32efb132c0f1f054d65478d1cda` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TransportTests.cs` | `00bfeee3da192c9fdbd95197e811d2837904eb5f508061261d08163c80a44fa9` | `01875b75897b3f75898f45a1b26f2ae84fbe7ceb132b7ae17e7328f2cc7b15c0` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/IntegrationTestBase.cs` | `9dd0f9f7473c012650ae2e36da22f38029d082a5b6d2464d780819addb4767f0` | `8534fe77acc8606e50d9d67a250ede3ef98d93afe7c5d68e510d6cb263594a1f` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs` | `1697385e7513701c24041668a8cf43fec3b97cfd809498c050fc0d361f89dfec` | `e9dcfe37200e017ddf6d157e56f1422af4d7a2068752c557715008390b886490` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuthenticatorInfoTests.cs` | `bc6583ae183daffff41a6b010e8d1ff031be5e35ac12864666c83fd89c73da60` | `2f87c97f26887c16667165b8d7de65d62eb6fafacb6b507551260ed27da1f21a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionHidCrossLayerTests.cs` | `030b1f1b56bdec19ac9fa29cab679089fe3b368760010c9e0995e74989c5afb1` | `4b7e32722222c1d6cd2060bfacaaa7ca4a0ba2f63053e46e2d0310cab8b10dff` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionTests.cs` | `698a5e6b15eb3707a561771421e26deb9c3b5e81728a9a2358ae193b2fb26e40` | `25ce70917293498541396d32883c8a5edae683442c3f5f0403ed88fd8b1aa7b6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapExceptionTests.cs` | `e0aad9eb0a7249c6abc89952c7170fa655ecaf563c405ad99625a04f6964d692` | `88b9aaf518381cc2d383fc1858879b0612291aac24e2e145026e1d369e27b5cd` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapRequestBuilderTests.cs` | `471ff966af7e189ae7fc4dbd75c760c947fb5c7e7ab82b906ee5a291c56f2784` | `0c084830e84e043e623b6b0b78b9827d498ffa91461d32a650bf9f1054481ad2` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/IYubiKeyExtensionsTransportTests.cs` | `f874fe5a1fd1a997e1cebfc67252cd0456e73146c532abacebc44231c67b99bd` | `033d6c325a73962d7640e169dfbe7d3074b3dcb815f9faf107fbedeb2bfb3b28` |

Compiler/XML-reference/example and Roslyn preservation gates remain coordinator work. Future
hardware evidence must separately record setup, PIN changes, skips, exchanges and actual asserted
ceremonies. **Read-only review completion is not module documentation closeout or Pass2 authorization.**
