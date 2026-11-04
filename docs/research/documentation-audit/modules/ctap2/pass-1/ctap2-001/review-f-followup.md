# F — independent correction follow-up

**Verdict: PASS for F documentation at the exact snapshot below.** RF-R1–R4 are corrected and
independently re-reviewed; the authorized RF-O1–O3 changes preserve useful meaning. No remaining
required correction was found in those complete affected units. This is semantic documentation
acceptance, **not compilation, image freshness, implementation completion or module closeout**.

**Reviewer:** GPT-6.1 Sol, same independent F reviewer. **Date:** 2026-11-04.
**Checkout:** `/tmp/opencode/ctap2-001-edit-f`.
**Unchanged HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
Read PRIMARY `correction-f-review.md`, updated `edit-f.md` and the initial review, then the complete
corrected paragraphs/remarks and affected fences with surrounding task/teaching context. Existing
root/module/test guidance, canonical workflow/style and loaded documentation/verification skills
remain applicable. Wrote only this PRIMARY report. No source edits, .NET operations, builds, tests,
hardware, CLI invocation, commits, delegation, integration or rendering.

## Correction re-review

| Review ID | Complete corrected unit re-reviewed | Independent outcome |
| --- | --- | --- |
| RF-R1 | `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs:23–48`, class/selection XML | **PASS.** NFC now explicitly requires current `Transport.Nfc` and FIDO AID; USB separately uses SDK5.8 gate/AID. Actual `Ctap2Session.cs:436–452` returns before the firmware comparison for NFC. Interface filtering, automatic/noninteractive selection, synchronous prompts, swallowed errors, duplicate labels and reset target discontinuity remain qualified; no transport-policy or hardware-qualification change. |
| RF-R2 | `src/Ctap2/examples/FidoTool/README.md:12–35,76–111,188–230`, complete prerequisite/access fences and support/selection/security context | **PASS.** PIN-changing/token-authorized paths name selectedV2 and advertised2 prerequisite; legacy command fallback retainsV2, not V1 negotiation. Plain info/reset and all PINless operations are not universally gated. Actual sample PIN/token paths constructV2; `ClientPin.cs:329–379` defaults rpIdnull, retains protocol on legacy fallback and emits an RP only if supplied. The access paragraph locally explains correct-PIN failure from the verify helper's missing GetAssertion RP scope, lost scope on fallback and no blind retry. `PinManagement.cs:154–193` still sends the defective request; it was not fixed by prose. |
| RF-R3 | `src/Ctap2/CLAUDE.md:399–433`, memory/disposal paragraphs and both complete fences | **PASS.** Final sentence now uses actual `AuthenticationTagLength`. Interface:46, V1:50–53 and V2:57–60 confirm readable Version/tag-length properties; guarded crypto methods and caller-owned outputs remain distinct. Stateless protocol/shared-secret/exceptional-copy limits and meaningful hash/finally fragment are retained. |
| RF-R4 | `docs/architecture/sdk-architecture-diagrams.md:92–188,239–283`, full L2 fence/CTAP teaching and L3b fence/teaching | **PASS.** L2 now names request encoding and make/get typed Decode, and separates the general `CtapResponseParser` utility from the traced credential path. Actual request helper:24–106 builds command-prefixed CBOR through `CtapRequestBuilder`; session:247–360 exchanges through backend then calls make/get Decode. Both backend status/notification resolution precede typed decoding. L3b remains the correct main-exchange sketch, not every validation/cleanup branch. SmartCard CLA80/INS10/shared-APDU convergence and HID CTAPHID_CBOR are unchanged. No unrelated Core/PIV diagram redesign or operation-specific executor layer was introduced. |
| RF-O1 | `Cli/Menus/InfoMenu.cs:55–73`, complete summary/remarks/display entry | **PASS.** Summary indentation now matches surrounding XML; metadata/state/units/capacity and prior-selection limits remain unchanged. No guard/display string change. |
| RF-O2 | `FidoExamples/CredentialManagement.cs:320–374`, complete update XML and method | **PASS.** Lasting automatic completed-map sorting fact replaces audit-defect narration. Existing handle, names versus account binding/RP database, token/request/catch/finally behavior preserved. Prior original v10.0.2 writer source and framework adjudication support the fact; no new probe or full update interoperability claim. |
| RF-O3 | `src/Ctap2/README.md:83–142`, complete create/allow-list/discoverable fences and adjacency/RP paragraphs | **PASS.** Existing discoverable `example.com` credential is now an explicit prerequisite; the preceding ResidentKeyfalse credential is ineligible. The alternate remains conditional and distinct from the prior singleton assertion, with real N−1 consecutive next calls before other commands/account selection. No fresh-key success, timer/lease or RP registration proof is invented. |

The entire FidoTool README was reread, not just its new sentences. The changed unit/fence checks above
retain real task calls, results, cleanup, typed prerequisites and protocol-demo versus RP acceptance.
Original standards evidence from the initial review remains authoritative: CTAP2.1/2.3 supported
protocol and RP-scope conditions, getNext state/count/adjacency and version-specific NFC timer rules,
and the original framework map-sorting mechanism were not weakened or replaced. The PIN evidence
file `evidence/b/original-clauses.md:54–72,120–144` was reread: platform selects a supported protocol;
mc/ga require RP ID; legacy0x05 is a different command/scope contract. No new remote standards
retrieval, device rejection or framework execution is claimed by this follow-up.

## Initial-review precision correction — 19 changed C# files, not 20

**Confirmed.** The initial `review-f.md` aggregate wording “all20 changed FidoTool C# files” and
“all20 changed C# files” was inaccurate. The F inventory contains20 sample C# files, but only
**19 are changed**. `src/Ctap2/examples/FidoTool/Cli/Output/OutputHelpers.cs` is the unchanged20th
inventory member/dependency. The exact30-path manifest was correct throughout and is still authoritative:
19 C# + two csproj + five CTAP Markdown + four external paths =30.

This follow-up explicitly read the complete80-line OutputHelpers dependency: its forwarding/inheritdoc,
hex facade and confirmation delegation do not claim new protocol behavior. Fresh byte comparison with
Git baseline confirms unchanged content, SHA256
`419069ee0bdf7b90e397f9f26d9dc9be873527455f47e640303656d11ab25cbc`.
Do not retrospectively count it as a twentieth changed-file read or infer a twentieth runtime edit.
The initial89 XML-block parse count was obtained from the **19 changed** files and remains correct.
Its19 exact C# file rows and coverage remain valid; the erroneous aggregate count is superseded here.

The initial report is preserved without rewriting it. Fresh SHA256 remains
`2793c7cd1669451b2145aa5df18081297210c6ad3387c8825911c34a8838cce5`, matching its prior final
recorded hash. This clarification changes no source disposition, review finding or bug count.

## Fresh preservation checks and snippet status

Read-only Python independently parsed the initial30-row hash manifest and compared every current
checkout file: **exactly the seven corrected paths differ; the other23 hashes are unchanged**.
`git diff --name-only 5b09a9b6` still contains exactly that same30-path set,26 CTAP/four external.
All seven current hashes agree with the editor correction report, as recorded below.

- All19 changed C# files still equal Git baseline after removing only whole-line `//` comments/blank
  lines. Runtime strings, calls, imports, directives, traits/assertions and member declarations unchanged.
- All89 contiguous changed-source XML blocks parse. This is XML syntax, not cref/compiler/analyzer proof.
- Both csproj hashes match the initial review. Their already-reviewed allowed Description/comment-only
  changes and analyzer/reference/warnings policy therefore have not changed during correction.
- Fresh module scan: **39 closed Markdown fences**. The affected protocol/memory and create/get/CLI
  fence bodies remain the reviewed task fragments; the correction adds surrounding prerequisites, not
  no-op substitutes or dropped cleanup. Five baseline A/B/E XML dependencies remain separate final-
  integration compilation inputs, not accepted stale technical XML in this isolated checkout.
- Local Markdown link paths over every changed Markdown file all resolve; this is filesystem existence,
  not remote-link/heading-anchor certification.
- `git diff --check 5b09a9b6`: exit0, no whitespace diagnostics; only the two previously recorded csproj
  LF→CRLF checkout warnings. No formatter/compiler/docs gate was invoked.

The updated `edit-f.md` correctly retains39 fence ordinals/typed contexts and now names the existing-
discoverable and CLI V2/verify-scope prerequisites. The compile-input specialist's independent extraction
and eventual **real consumer/UT/IT/sample-context** compilation remain separate coordinator work; no
.NET operation or compiler-success claim follows from this semantic review.

## Exact final30-path hash manifest

SHA256 is over actual reviewed checkout bytes. `new` means changed since the initial F review;
`same` means independently identical to that reviewed hash. These are not Git-blob hashes or images.

| Exact repository-relative path | Since initial review | Final SHA256 |
| --- | --- | --- |
| `docs/TESTING.md` | same | `1db21b42b4a8b6443a872b2b0be37fcc0ced84202ec49a8c8db1c5210e6af401` |
| `docs/architecture/sdk-architecture-diagrams.md` | new | `a38fe512d725b2f405796da6fbc7c2535fa4106fc816d333ad7bc306c69e9701` |
| `docs/migration/v1-to-v2-map.yml` | same | `e7f3b5d423bce9affd6f2d68bcd365d27cfdfd17cfa83472d5766f6efc0c3ad2` |
| `src/Ctap2/CLAUDE.md` | new | `0bfe308f3dcaf2307bc2e3d463f174bd4a341a85b583c1ac2cb07d0ad65c2dcf` |
| `src/Ctap2/PACKAGE_README.md` | same | `38094b395d2eb392765ea581fb6e0dfe49187a3bbd2905510b6a4be6ae37580b` |
| `src/Ctap2/README.md` | new | `2d41ea0fb0c4477355680cc6f271e7277a4f108b6012986a230aadf3d3df1225` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/AccessMenu.cs` | same | `d5a3969a2457e3e7d68802c760bd2899ed6031040594e4846916cb89d08f947f` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ConfigMenu.cs` | same | `a99f6b0b24b988f8bbdc10cf7ed3768f314317b312d0b74bc9e50b707ed05336` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialMenu.cs` | same | `b651994c0fd97a1234460590b7f47920176c72a52455de20fa5e5cfa65b48472` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialsMenu.cs` | same | `1d39b8c1e374ee9c1c40ecf4734c1cc7d1eb325771577b5b4c9007570d4edb20` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/FingerprintsMenu.cs` | same | `571d4f707af52a82fdc6af5731f9ca00e0fc3bf3f15734bbe4c44caace7c37bb` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs` | new | `e413b6d9c272c8b679b682b7bd4f097e7c5dcb03679e3afbe0f7bae3b7ae68f1` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ResetMenu.cs` | same | `441b661cf450ee2a5f9a82132c05f59998ed3fedf3c8927dfeba2e855a9f4e06` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | new | `783d6bc26251ab9ea7a609374601131cc161b37023f3810d28375b90c9eb8558` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | same | `b516c9c5159b634080a0f792adc07243cc1cf03c353d6d4b63f337dc5f303787` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoPinHelper.cs` | same | `3a35d47981b9ab6186108b0c117faac027f21832abf5c9f366830c6d0f1cc41b` |
| `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs` | same | `f3d3ce51362352ab3415bd1efdddfc8e53941b7329b8417882d4b5c2ffa21ca0` |
| `src/Ctap2/examples/FidoTool/FidoExamples/ConfigManagement.cs` | same | `2d23b8461397a110ff8da45db25885f1351a3c3b47bf4dcc670a90f09773dba3` |
| `src/Ctap2/examples/FidoTool/FidoExamples/CredentialManagement.cs` | new | `236bf3766f0f0630eb7c7096aaddbd25072d5ae04569edbda821e1b21581d706` |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAssertion.cs` | same | `18a05834d11e196cd6f96570eb9f904a9b3070234b2b9f41f138a25630c796fb` |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAuthenticatorInfo.cs` | same | `247f4f51001b40a21981ea872ce20716862d5fffb8c4d2de464cdcd58f5ffc3e` |
| `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs` | same | `a4040a8f2617008bc625ee4aaeba52605ad34cdf9750f010fb7e7558c29f9c80` |
| `src/Ctap2/examples/FidoTool/FidoExamples/PinManagement.cs` | same | `912e8a09871a83ccb3ac1ec2dc6a35b31ee5cded15d6fc9002e75a6998ce26eb` |
| `src/Ctap2/examples/FidoTool/FidoExamples/ResetAuthenticator.cs` | same | `6e8284576f2fc1a17ca79793849434ced34e14a32962aa92126b4a2ca0105fe8` |
| `src/Ctap2/examples/FidoTool/FidoTool.csproj` | same | `24482e538235542bd6d5609c9e6ec06ab1687cc1ada93efe32f4f45a27b25c76` |
| `src/Ctap2/examples/FidoTool/Program.cs` | same | `9ff0ac47971442c115aa7fbed71649c8c55220d08da1526bc65c5646cfb50866` |
| `src/Ctap2/examples/FidoTool/README.md` | new | `897c0f2a2775d10d41e6540932f44bed409919520418b93b71cdfe23ced42cea` |
| `src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj` | same | `859c43a9e8e3254af21bd74b46c9174ec2c65a977a489624a0785d1d16ba7344` |
| `src/Ctap2/tests/CLAUDE.md` | same | `b93c7a963573e93380ef82181baebce2b106b62ea4237d16efad1b0de602d266` |
| `src/WebAuthn/README.md` | same | `cf64612066288c5845cd5d9892038abae2ac0cc17e746ab4fbc7e401069208f7` |

## Remaining coordinator gates and preserved limitations

**Images are not current.** After integrated source acceptance, coordinator regeneration must cover
`docs/architecture/images/L2-layered-stack.svg`, `images/png/L2-layered-stack.png`,
`images/L3b-fido2-ctap-sequence.svg`, `images/png/L3b-fido2-ctap-sequence.png` and
`images/image-manifest.txt` under `docs/architecture/`, using the pinned renderer workflow and normal
architecture gate. Neither this review nor editor correction rendered or verified those outputs.

The existing YAML DOC002 parse defect remains unfixed; no parse/schema success claimed. Existing
hardware approval policy, direct-GetInfo versus PIN-normalizing setup distinction, API/lifecycle and
analyzer policy remain unchanged. Source/test/sample behavioral defects are deferred, including
uv+token, verifyRP, selector/reset identity, error/cancellation, bio wire/schema/completion and
secret-output/cleanup boundaries. COORD-001 and ARKG-10 partial agreement/experimental warnings,
rejected D-08 runtime-order diagnosis and prior platform/probe limits remain intact.

Final integrated A–E XML/snippet compilation, builds/unit/docs gates, image freshness, clean-unit
sampling, record integration/remote checkpoint and the separate fresh CTAP↔WebAuthn boundary review
remain outstanding coordinator work. Keep WA-XREF-001, existing WA-IMP dependencies and G-011's
native-result/serializer actor decision; do not treat this isolated F PASS as accepting stale A–E
baseline comments, fixing implementation, qualifying hardware or starting Pass2.
