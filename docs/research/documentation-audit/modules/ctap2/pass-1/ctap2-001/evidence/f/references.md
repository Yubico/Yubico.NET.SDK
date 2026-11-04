# F reference/check evidence and bounded gaps

Access date 2026-11-03. No compilation, test, CLI tool launch or device operation.

## Original external references

### WebAuthn Level 2

Title: *Web Authentication: An API for accessing Public Key Credentials — Level 2*.
Status: W3C Recommendation, 8 April 2021.
Pinned publication: https://www.w3.org/TR/2021/REC-webauthn-2-20210408/
Relevant actors/sections: RP §7.1 registration verification / §7.2 assertion verification;
client data and authenticator responsibilities must not be conflated with RP account acceptance.

Initial read: original W3C document through Exa fetch at
https://www.w3.org/TR/webauthn-2/#sctn-verifying-assertion . Extraction confirmed status/date and
introduction/roadmap, including the RP/client/authenticator role distinction. The long page extraction
did not reach all §7 verification steps despite the fragment URL. One focused follow-up fetched the
pinned original HTML and stripped markup to find the relevant verification passage. Retained quote:

> Verify that the rpIdHash in authData is the SHA-256 hash of the RP ID expected by the Relying Party.
> Verify that the User Present bit of the flags in authData is set.
> If user verification is required for this registration, verify that the User Verified bit of the flags
> in authData is set.
> Verify that the "alg" parameter in the credential public key in authData matches the alg attribute
> of one of the items in options.pubKeyCredParams.

These are RP verification instructions, not obligations that Ctap2Session currently enforces.
The fetched introduction also states that the challenge and other request inputs are supplied by the RP
and conveyed through the client/API/platform channel to the authenticator, with the signed response
returned to the RP. This supports F-01's responsibility distinction; the actual source confirms the
sample performs no such RP handoff. **Gap:** specific exact type/challenge/origin/signature-step quotes
were not recovered by the focused text search (markup whitespace separated the identifiers).
No further research loop is claimed; G must provide its original §7.2 clause evidence in the boundary map.
F does not settle new protocol conformance claims from the sibling README's wording.

### YubiKey Manager CLI guide

Original: https://docs.yubico.com/software/yubikey/tools/ykman/FIDO_Commands.html
Status: live vendor CLI guide; unversioned page, not a pinned binary compatibility guarantee.
Read complete extracted command tree, examples, options, and reset description.

Selected exact facts supporting F-07/F-09:

- `access change-pin`: "Set or change the PIN code." Options include `-n, --new-pin`, `-P, --pin`,
  `-u, --u2f`; access also lists `force-change` and `set-min-length` (introduced in CLI 5.3.0).
- `credentials delete CREDENTIAL_ID`: argument is "A unique substring match of a Credential ID."
- `credentials list` offers `--csv`; FidoTool's full-hex parser/list output is not that behavior.
- `reset`: "This action wipes all FIDO credentials on the YubiKey, including FIDO U2F credentials,
  and removes the PIN code. The reset is triggered immediately after the YubiKey is inserted,
  and it requires that the YubiKey be touched."
- `reset -f, --force`: "Confirm the action without prompting." It is not described as removing touch
  or the power-cycle restriction, and the repository separately prohibits unattended reset timing.

The vendor's use of "all FIDO applications" is therefore legitimate FIDO application/wire identity,
not an obsolete API name to mechanically rename to CTAP2. The sample's target-binding behavior remains
independently source-observed and deferred, not justified by this guide.

## Framework/library/CLI API documentation via Context7

Resolved library IDs with the named libraries before querying. These are documentation evidence,
not compiled validation against this repository's installed exact dependency versions.

| Library / ID | Topic and retained source evidence |
|---|---|
| .NET `/dotnet/docs` | .NET10 SDK noun-first aliases. `dotnet package add` aliases `dotnet add package`; verb-first forms continue to work. https://github.com/dotnet/docs/blob/main/docs/core/whats-new/dotnet-10/sdk.md and docs/core/tools/dotnet-package-add.md. `--prerelease` supported. No invalid-command finding on current installation snippets. |
| xUnit `/websites/xunit_net` | https://xunit.net/docs/getting-started/v3/microsoft-testing-platform confirms UseMicrosoftTestingPlatformRunner=true selects MTP CLI and test executables. https://xunit.net/docs/config-xunit-runner-json confirms Content CopyToOutputDirectory=PreserveNewest. parallelizeTestCollections=false disables within-assembly collection parallelization; config is not device-sequence proof. Newer v3 JSON mapping discussion does not change this repository's source-observed v2 integration project. |
| Spectre.Console `/spectreconsole/spectre.console` | IPrompt<T>.Show runs synchronously; ShowAsync(IAnsiConsole,CancellationToken) supports cancellable waiting. AnsiConsoleExtensions.PromptAsync takes an optional token. Docs expose an async alternative; source uses synchronous Prompt/Confirm/Ask, so merely passing tokens to device methods is not prompt cancellation. Current upstream docs may exceed installed version; no proposed API adoption is verified. |

## Mechanical local-link check

Read-only Python used `Path(document).parent / href_without_fragment`, `.resolve()` and `.exists()`;
no guessed parent count. All ten relative Markdown link occurrences in the five module Markdown files
resolve to existing paths. External URLs were excluded from filesystem existence checking. Anchor
existence was not checked; these links have no local fragments requiring an anchor check.

| Source:line | href | Resolved repository path |
|---|---|---|
| src/Ctap2/CLAUDE.md:3 | ../../CLAUDE.md | CLAUDE.md |
| src/Ctap2/README.md:7 | ../../README.md | README.md |
| src/Ctap2/README.md:12 | ../../docs/linux-setup.md | docs/linux-setup.md |
| src/Ctap2/README.md:219 | ../Core/README.md | src/Core/README.md |
| src/Ctap2/README.md:220 | ../WebAuthn/README.md | src/WebAuthn/README.md |
| src/Ctap2/README.md:221 | ../../docs/usage/user-interaction.md | docs/usage/user-interaction.md |
| src/Ctap2/README.md:221 | ../../docs/usage/device-discovery.md | docs/usage/device-discovery.md |
| src/Ctap2/README.md:223 | ../../docs/DEV-GUIDE.md | docs/DEV-GUIDE.md |
| src/Ctap2/tests/CLAUDE.md:7 | ../../../docs/TESTING.md | docs/TESTING.md |
| src/Ctap2/tests/CLAUDE.md:9 | ../CLAUDE.md | src/Ctap2/CLAUDE.md |

All eleven ProjectReference targets across FidoTool, library, unit and integration csproj also exist
after normalizing backslashes to filesystem separators. This is path validation only, not restore,
reference compatibility, warnings-as-errors or sample compilation.

## Byte-identity check / stop-bound gap

All 33 F-owned checkout files were SHA-256 compared to inventory.json. Twenty-nine match. Four csproj
paths differ; one focused follow-up `git diff -- <four exact paths>` returned no output. F stops here
and records the unexplained inventory/checkout byte discrepancy rather than asserting full hash identity.
Textual source reads/project-reference checks remain valid, but coordinator must reconcile normalization
or inventory provenance before final byte/hash counts. No source change or recovery was attempted.

| Exact path | Checkout SHA-256 |
|---|---|
| src/Ctap2/examples/FidoTool/FidoTool.csproj | 72c86b335f0ab91d7a7a4e509245cdf219838272c712d6b8889835841f5f9975 |
| src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj | ce9653bacb0d5dce2ac8ca44923928e30e3a1a0f9fa882d02bd0d92a0e60855f |
| src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Yubico.YubiKit.Ctap2.IntegrationTests.csproj | b01ad512410118f525cc5ed42093ca9400fb4e0dd46d6daeb4dd9b3e4bebbb93 |
| src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Yubico.YubiKit.Ctap2.UnitTests.csproj | 88a60d3cb57b3bb05ebe4de301d4bdd0236c771f900c1667793958bb7c3ccf7f |

No device/version/OS/transport behavior has been executed by this investigator. A test method's name,
trait or parsed fixture does not substitute for actual assertion evidence or device qualification.

## Record reconciliation check

A final read-only scanner compared tracked external discovery matches against the exact path strings
in external-paths.md: **133 matched paths, zero missing from the manifest**. It also enumerated every
tracked module Markdown fence and XML `<code>` block and compared source path/opening-line keys against
snippets.md: **44 blocks (39 fences + 5 XML code), zero missing**. This verifies inventory inclusion,
not correctness of each classification or source behavior. No build/test tool was invoked.
